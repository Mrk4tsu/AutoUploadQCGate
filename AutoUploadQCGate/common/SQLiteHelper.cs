using System.Data.SQLite;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using static AutoUploadQCGate.MainWindow;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;

namespace DefaultNS
{
    public static class SQLiteHelper
    {
        private static bool db_busy = false;
        private static readonly object _dbLock = new object();

        /// <summary>
        /// Chuỗi kết nối mặc định tới SQLite DB
        /// </summary>
        public static string GetDbFilePath()
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = Path.GetDirectoryName(exePath);
            return Path.Combine(exeDir, "AppConfig.db3");
        }
        public static string GetConnectionString()
        {
            string dbFile = GetDbFilePath();
            return $"Data Source={dbFile};Version=3;New=True;Compress=True;";
        }

        /// <summary>
        /// Idempotent local schema migration. Logical server ids are deliberately
        /// stored without SQLite foreign keys; AutoUpload validates them against
        /// SQL Server before processing a request.
        /// </summary>
        public static bool EnsureSchema()
        {
            lock (_dbLock)
            {
                try
                {
                    using (var conn = new SQLiteConnection(GetConnectionString()))
                    {
                        conn.Open();
                        using (var tx = conn.BeginTransaction())
                        {
                            ExecuteSchemaCommand(conn, tx, @"
CREATE TABLE IF NOT EXISTS dynamic_reupload_requests (
    pkid INTEGER PRIMARY KEY AUTOINCREMENT,
    pkid_server INTEGER NOT NULL,
    upload_data_queue_id INTEGER NOT NULL,
    operator_code TEXT,
    status TEXT NOT NULL DEFAULT 'Pending',
    requested_bag_count INTEGER NOT NULL DEFAULT 0,
    logs TEXT,
    created_at DATETIME NOT NULL,
    started_at DATETIME,
    completed_at DATETIME,
    updated_at DATETIME NOT NULL
);");
                            ExecuteSchemaCommand(conn, tx, @"
CREATE TABLE IF NOT EXISTS dynamic_reupload_request_items (
    pkid INTEGER PRIMARY KEY AUTOINCREMENT,
    pkid_server INTEGER NOT NULL,
    reupload_request_id INTEGER NOT NULL,
    aluminum_bag_information_id INTEGER NOT NULL,
    aluminum_bag_code TEXT,
    source_file_path TEXT,
    local_file_path TEXT,
    file_hash TEXT,
    status TEXT NOT NULL DEFAULT 'Pending',
    attempt_count INTEGER NOT NULL DEFAULT 0,
    is_legacy_recovered INTEGER NOT NULL DEFAULT 0,
    logs TEXT,
    transfer_started_at DATETIME,
    review_reason TEXT,
    reviewed_at DATETIME,
    reviewed_by TEXT,
    created_at DATETIME NOT NULL,
    uploaded_at DATETIME,
    updated_at DATETIME NOT NULL
);");
                            ExecuteSchemaCommand(conn, tx, "CREATE INDEX IF NOT EXISTS ix_reupload_requests_queue_status ON dynamic_reupload_requests(upload_data_queue_id, status);");
                            ExecuteSchemaCommand(conn, tx, "CREATE INDEX IF NOT EXISTS ix_reupload_items_request_status ON dynamic_reupload_request_items(reupload_request_id, status);");
                            ExecuteSchemaCommand(conn, tx, "CREATE INDEX IF NOT EXISTS ix_reupload_items_bag ON dynamic_reupload_request_items(aluminum_bag_information_id);");

                            EnsureColumn(conn, tx, "dynamic_aluminum_informations", "aluminum_bag_information_id_server", "INTEGER");
                            EnsureColumn(conn, tx, "dynamic_aluminum_informations", "source_file_path", "TEXT");
                            EnsureColumn(conn, tx, "dynamic_aluminum_informations", "local_file_path", "TEXT");
                            EnsureColumn(conn, tx, "dynamic_aluminum_informations", "file_hash", "TEXT");
                            EnsureColumn(conn, tx, "dynamic_aluminum_informations", "status", "TEXT NOT NULL DEFAULT 'Pending'");
                            EnsureColumn(conn, tx, "dynamic_aluminum_informations", "attempt_count", "INTEGER NOT NULL DEFAULT 0");
                            EnsureColumn(conn, tx, "dynamic_aluminum_informations", "is_legacy_recovered", "INTEGER NOT NULL DEFAULT 0");
                            EnsureColumn(conn, tx, "dynamic_aluminum_informations", "uploaded_at", "DATETIME");
                            EnsureColumn(conn, tx, "dynamic_aluminum_informations", "logs", "TEXT");
                            EnsureColumn(conn, tx, "dynamic_reupload_request_items", "transfer_started_at", "DATETIME");
                            EnsureColumn(conn, tx, "dynamic_reupload_request_items", "review_reason", "TEXT");
                            EnsureColumn(conn, tx, "dynamic_reupload_request_items", "reviewed_at", "DATETIME");
                            EnsureColumn(conn, tx, "dynamic_reupload_request_items", "reviewed_by", "TEXT");
                            tx.Commit();
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Global.WriteLogFile("[EnsureSchema] " + ex);
                    return false;
                }
            }
        }

        private static void ExecuteSchemaCommand(SQLiteConnection conn, SQLiteTransaction tx, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, conn, tx))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsureColumn(SQLiteConnection conn, SQLiteTransaction tx, string table, string column, string definition)
        {
            bool exists = false;
            using (var cmd = new SQLiteCommand($"PRAGMA table_info({table});", conn, tx))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(reader["name"]?.ToString(), column, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
            }

            if (!exists)
                ExecuteSchemaCommand(conn, tx, $"ALTER TABLE {table} ADD COLUMN {column} {definition};");
        }

        public static bool InsertOrUpdateUploadGroups(List<UploadGroup> groups)
        {
            if (groups == null || groups.Count == 0)
                return false;

            EnsureSchema();

            lock (_dbLock)
            {
                string connString = GetConnectionString();
                using (var conn = new SQLiteConnection(connString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var group in groups)
                            {
                                long pkid;
                                var existing = new DataTable();
                                using (var checkCmd = new SQLiteCommand(
                                    "SELECT pkid FROM dynamic_upload_data_queues WHERE pkid_server = @pkid;", conn, transaction))
                                {
                                    checkCmd.Parameters.Add(P("@pkid", group.Pkid));
                                    using (var adapter = new SQLiteDataAdapter(checkCmd))
                                        adapter.Fill(existing);
                                }

                                if (existing.Rows.Count > 0)
                                {
                                    pkid = Conv.atoi32(existing.Rows[0]["pkid"]);
                                    var previousBags = new DataTable();
                                    using (var bagCmd = new SQLiteCommand(@"
SELECT aluminum_bag_code, local_file_path, file_hash
FROM dynamic_aluminum_informations
WHERE upload_data_queue_id = @queue_id;", conn, transaction))
                                    {
                                        bagCmd.Parameters.Add(P("@queue_id", pkid));
                                        using (var adapter = new SQLiteDataAdapter(bagCmd))
                                            adapter.Fill(previousBags);
                                    }
                                    foreach (var file in group.AluminumBags ?? new List<AluminumBagInfo>())
                                    {
                                        var previous = previousBags.AsEnumerable().FirstOrDefault(x =>
                                            string.Equals(Conv.atos(x["aluminum_bag_code"]), file.AluminumBagCode, StringComparison.OrdinalIgnoreCase));
                                        if (previous != null)
                                        {
                                            var previousPath = Conv.atos(previous["local_file_path"]);
                                            if (!string.IsNullOrWhiteSpace(previousPath))
                                                file.LocalFilePath = previousPath;
                                            if (string.IsNullOrWhiteSpace(file.FileHash))
                                                file.FileHash = Conv.atos(previous["file_hash"]);
                                        }
                                    }
                                    using (var cmd = new SQLiteCommand(@"
UPDATE dynamic_upload_data_queues
SET combine_indication = @combine_indication,
    customer_code = @customer_code,
    customer_name = @customer_name,
    is_upload_folder = @is_upload_folder,
    folder_name = @folder_name,
    sftp_server = @sftp_server,
    sftp_port = @sftp_port,
    sftp_user = @sftp_user,
    sftp_password = @sftp_password,
    sftp_remote_path = @sftp_remote_path,
    combine_server_path = @combine_server_path,
    combine_local_path = @combine_local_path,
    ship_quantity = @ship_quantity,
    is_download = @is_download,
    is_reupload = 0
WHERE pkid_server = @pkid_server;", conn, transaction))
                                    {
                                        AddGroupParameters(cmd, group);
                                        cmd.Parameters.Add(P("@is_download", 0));
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    using (var cmd = new SQLiteCommand(@"
INSERT INTO dynamic_upload_data_queues
(combine_indication, customer_code, customer_name, is_upload_folder, folder_name,
 sftp_server, sftp_port, sftp_user, sftp_password, sftp_remote_path, pkid_server,
 combine_server_path, combine_local_path, is_download, ship_quantity, is_uploaded,
 is_use_proxy, is_use_key, is_reupload)
VALUES
(@combine_indication, @customer_code, @customer_name, @is_upload_folder, @folder_name,
 @sftp_server, @sftp_port, @sftp_user, @sftp_password, @sftp_remote_path, @pkid_server,
 @combine_server_path, @combine_local_path, 0, @ship_quantity, 0,
 @is_use_proxy, @is_use_key, 0);", conn, transaction))
                                    {
                                        AddGroupParameters(cmd, group);
                                        cmd.ExecuteNonQuery();
                                        using (var idCmd = new SQLiteCommand("SELECT last_insert_rowid();", conn, transaction))
                                            pkid = Convert.ToInt64(idCmd.ExecuteScalar());
                                    }
                                }

                                // Replace the queue snapshot atomically. Re-upload requests are
                                // stored in their own tables, so this cannot duplicate bag rows.
                                using (var delCmd = new SQLiteCommand(
                                    "DELETE FROM dynamic_aluminum_informations WHERE upload_data_queue_id = @queue_id;", conn, transaction))
                                {
                                    delCmd.Parameters.Add(P("@queue_id", pkid));
                                    delCmd.ExecuteNonQuery();
                                }

                                bool allArchived = true;
                                foreach (var file in group.AluminumBags ?? new List<AluminumBagInfo>())
                                {
                                    string sourcePath = (file.FilePath ?? "").Trim();
                                    string localPath = (file.LocalFilePath ?? "").Trim();
                                    string hash = file.FileHash;
                                    string archiveLog = "";
                                    bool archived = ArchiveSnapshot(sourcePath, localPath, ref hash, out archiveLog);
                                    if (!archived)
                                    {
                                        allArchived = false;
                                        archiveLog = string.IsNullOrWhiteSpace(archiveLog)
                                            ? "Archive/verify failed."
                                            : archiveLog;
                                    }

                                    using (var fileCmd = new SQLiteCommand(@"
INSERT INTO dynamic_aluminum_informations
(upload_data_queue_id, aluminum_bag_information_id_server, file_path, source_file_path,
 local_file_path, file_hash, aluminum_bag_code, status, attempt_count,
 is_legacy_recovered, logs)
VALUES
(@queue_id, @bag_id, @file_path, @source_file_path, @local_file_path, @file_hash,
 @aluminum_bag_code, @status, 0, 0, @logs);", conn, transaction))
                                    {
                                        fileCmd.Parameters.Add(P("@queue_id", pkid));
                                        fileCmd.Parameters.Add(P("@bag_id", file.AluminumBagInformationId));
                                        fileCmd.Parameters.Add(P("@file_path", sourcePath));
                                        fileCmd.Parameters.Add(P("@source_file_path", sourcePath));
                                        fileCmd.Parameters.Add(P("@local_file_path", archived ? localPath : ""));
                                        fileCmd.Parameters.Add(P("@file_hash", archived ? hash : ""));
                                        fileCmd.Parameters.Add(P("@aluminum_bag_code", file.AluminumBagCode ?? ""));
                                        fileCmd.Parameters.Add(P("@status", archived ? "Pending" : "Failed"));
                                        fileCmd.Parameters.Add(P("@logs", archived ? "" : archiveLog));
                                        fileCmd.ExecuteNonQuery();
                                    }
                                }

                                using (var cmd = new SQLiteCommand(
                                    "UPDATE dynamic_upload_data_queues SET is_download = @is_download WHERE pkid = @pkid;", conn, transaction))
                                {
                                    cmd.Parameters.Add(P("@is_download", allArchived ? 1 : 0));
                                    cmd.Parameters.Add(P("@pkid", pkid));
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Global.WriteLogFile("[InsertOrUpdateUploadGroups] " + ex.ToString());
                            return false;
                        }
                    }
                }
            }
        }

        private static void AddGroupParameters(SQLiteCommand cmd, UploadGroup group)
        {
            cmd.Parameters.Add(P("@combine_indication", group.CombineIndication));
            cmd.Parameters.Add(P("@customer_code", group.CustomerCode));
            cmd.Parameters.Add(P("@customer_name", ""));
            cmd.Parameters.Add(P("@is_upload_folder", group.IsUploadFolder.HasValue ? (group.IsUploadFolder.Value ? 1 : 0) : (object)DBNull.Value));
            cmd.Parameters.Add(P("@folder_name", group.FolderName));
            cmd.Parameters.Add(P("@sftp_server", group.SftpServer));
            cmd.Parameters.Add(P("@sftp_port", group.SftpPort));
            cmd.Parameters.Add(P("@sftp_user", group.SftpUser));
            cmd.Parameters.Add(P("@sftp_password", group.SftpPassword));
            cmd.Parameters.Add(P("@sftp_remote_path", group.SftpRemotePath));
            cmd.Parameters.Add(P("@pkid_server", group.Pkid));
            cmd.Parameters.Add(P("@combine_server_path", group.CombineServerPath));
            cmd.Parameters.Add(P("@combine_local_path", group.CombineLocalPath));
            cmd.Parameters.Add(P("@ship_quantity", group.QuantityUpload));
            cmd.Parameters.Add(P("@is_use_proxy", group.IsUseProxy));
            cmd.Parameters.Add(P("@is_use_key", group.IsUseKey));
        }

        private static bool ArchiveSnapshot(string sourcePath, string localPath, ref string hash, out string log)
        {
            log = "";
            try
            {
                if (string.IsNullOrWhiteSpace(localPath))
                {
                    log = "Local archive path is empty.";
                    return false;
                }

                // Existing local snapshot is immutable. Never overwrite it with a
                // changed source file during Re-upload.
                if (File.Exists(localPath))
                {
                    var localInfo = new FileInfo(localPath);
                    if (localInfo.Length == 0)
                    {
                        log = "Local archive exists but is empty.";
                        return false;
                    }
                    var localHash = ComputeSha256(localPath);
                    if (!string.IsNullOrWhiteSpace(hash) && !string.Equals(hash, localHash, StringComparison.OrdinalIgnoreCase))
                    {
                        log = "Local archive SHA-256 does not match the recorded snapshot.";
                        return false;
                    }
                    hash = localHash;
                    return true;
                }

                if (!File.Exists(sourcePath))
                {
                    log = $"Source file not found: {sourcePath}";
                    return false;
                }

                var dir = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);
                File.Copy(sourcePath, localPath, false);

                var sourceInfo = new FileInfo(sourcePath);
                var localInfoAfterCopy = new FileInfo(localPath);
                if (sourceInfo.Length != localInfoAfterCopy.Length)
                {
                    log = "Archive file size verification failed.";
                    return false;
                }

                var sourceHash = ComputeSha256(sourcePath);
                var localHashAfterCopy = ComputeSha256(localPath);
                if (!string.Equals(sourceHash, localHashAfterCopy, StringComparison.OrdinalIgnoreCase))
                {
                    log = "Archive SHA-256 verification failed.";
                    return false;
                }

                hash = localHashAfterCopy;
                return true;
            }
            catch (Exception ex)
            {
                log = ex.Message;
                return false;
            }
        }

        public static bool EnsureSnapshot(string sourcePath, string localPath, ref string hash, out string log)
        {
            return ArchiveSnapshot(sourcePath, localPath, ref hash, out log);
        }

        private static string ComputeSha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }


        /// <summary>
        /// Thực thi câu lệnh Insert/Update/Delete
        /// </summary>
        public static bool ExecuteNonQuery(string sql, params SQLiteParameter[] parameters)
        {
            lock (_dbLock)
            {
                try
                {
                    using (var conn = new SQLiteConnection(GetConnectionString()))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand(sql, conn))
                        {
                            if (parameters != null)
                                cmd.Parameters.AddRange(parameters);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.Print($"[ExecuteNonQuery] {ex}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Thực thi câu lệnh trả về một giá trị (Scalar)
        /// </summary>
        public static object ExecuteScalar(string sql, params SQLiteParameter[] parameters)
        {
            lock (_dbLock)
            {
                try
                {
                    using (var conn = new SQLiteConnection(GetConnectionString()))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand(sql, conn))
                        {
                            if (parameters != null)
                                cmd.Parameters.AddRange(parameters);

                            return cmd.ExecuteScalar();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"[ExecuteScalar] {ex}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Thực thi câu lệnh và trả về DataTable
        /// </summary>
        public static DataTable ExecuteDataTable(string sql, params SQLiteParameter[] parameters)
        {
            lock (_dbLock)
            {
                var dt = new DataTable();
                try
                {
                    using (var conn = new SQLiteConnection(GetConnectionString()))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand(sql, conn))
                        {
                            if (parameters != null)
                                cmd.Parameters.AddRange(parameters);

                            using (var adapter = new SQLiteDataAdapter(cmd))
                            {
                                adapter.Fill(dt);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"[ExecuteDataTable] {ex}");
                }
                return dt;
            }
        }

        /// <summary>
        /// Insert và trả về ID vừa insert (last_insert_rowid())
        /// </summary>
        public static long InsertAndGetId(string sql, params SQLiteParameter[] parameters)
        {
            lock (_dbLock)
            {
                long id = -1;
                try
                {
                    using (var conn = new SQLiteConnection(GetConnectionString()))
                    {
                        conn.Open();
                        using (var cmd = new SQLiteCommand(sql + "; SELECT last_insert_rowid();", conn))
                        {
                            if (parameters != null)
                                cmd.Parameters.AddRange(parameters);

                            id = Convert.ToInt64(cmd.ExecuteScalar());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"[InsertAndGetId] {ex}");
                }
                return id;
            }
        }

        /// <summary>
        /// Tạo SQLiteParameter dễ dàng
        /// </summary>
        public static SQLiteParameter P(string name, object value)
        {
            var param = new SQLiteParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            return param;
        }
    }
}
