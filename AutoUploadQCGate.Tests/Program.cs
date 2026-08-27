using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using AutoUploadQCGate.Models;
using DefaultNS;

namespace AutoUploadQCGate.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                AssertEqual(
                    ReuploadDeliveryPolicy.Pending,
                    ReuploadDeliveryPolicy.FailureStatus(false, 1),
                    "A safe pre-transfer failure should retry.");
                AssertEqual(
                    ReuploadDeliveryPolicy.Failed,
                    ReuploadDeliveryPolicy.FailureStatus(false, 3),
                    "A safe pre-transfer failure should stop after three attempts.");
                AssertEqual(
                    ReuploadDeliveryPolicy.NeedsReview,
                    ReuploadDeliveryPolicy.FailureStatus(true, 1),
                    "A failure after transfer starts must not retry automatically.");
                AssertEqual(
                    ReuploadDeliveryPolicy.Pending,
                    ReuploadDeliveryPolicy.StaleProcessingStatus(false),
                    "Stale work before transfer is safe to retry.");
                AssertEqual(
                    ReuploadDeliveryPolicy.NeedsReview,
                    ReuploadDeliveryPolicy.StaleProcessingStatus(true),
                    "Stale work after transfer starts requires review.");
                AssertTrue(
                    ReuploadSchemaCompatibility.IsReady(1),
                    "A successful schema probe should enable reupload.");
                AssertTrue(
                    !ReuploadSchemaCompatibility.IsReady(0),
                    "A legacy schema probe should block reupload.");
                AssertTrue(
                    ReuploadSchemaCompatibility.ProbeSql.Contains("transfer_started_at") &&
                    ReuploadSchemaCompatibility.ProbeSql.Contains("review_reason") &&
                    ReuploadSchemaCompatibility.ProbeSql.Contains("reviewed_at") &&
                    ReuploadSchemaCompatibility.ProbeSql.Contains("reviewed_by"),
                    "The worker schema probe must require every delivery review column.");
                AssertTrue(
                    ReuploadSchemaCompatibility.WorkQuerySql.Contains("r.created_at AS request_created_at") &&
                    ReuploadSchemaCompatibility.WorkQuerySql.Contains("ORDER BY r.created_at, ri.pkid") &&
                    ReuploadSchemaCompatibility.WorkQuerySql.Contains("abi.number_of_psc_ok") &&
                    ReuploadSchemaCompatibility.DisplaySyncQuerySql.Contains("'NeedsReview'") &&
                    !ReuploadSchemaCompatibility.DisplaySyncQuerySql.Contains("ri.status = 'Pending'"),
                    "The work query must project stable ordering and the selected bag quantity.");
                AssertEqual(
                    UploadStatusNames.NeedsReview,
                    UploadStatusNames.Normalize("NeedsReview"),
                    "NeedsReview must remain a distinct display status.");
                var cachedSyncQuery = ReuploadSchemaCompatibility.BuildCachedDisplaySyncQuery(new[] { 71, 71, 0, -1 });
                AssertTrue(
                    cachedSyncQuery.Contains("WHERE r.pkid IN (71)") &&
                    cachedSyncQuery.Contains("request_completed_at") &&
                    cachedSyncQuery.Contains("item_uploaded_at") &&
                    !cachedSyncQuery.Contains("r.status IN"),
                    "Request-id sync must retrieve terminal requests without restoring unrelated history.");
                var missingRequestQuery = ReuploadSchemaCompatibility.BuildCachedDisplaySyncQuery(new[] { 9, 8, 9, 0, -1 });
                AssertTrue(
                    missingRequestQuery.Contains("WHERE r.pkid IN (8,9)") &&
                    !missingRequestQuery.Contains("r.status IN"),
                    "Missing terminal requests must be synchronized in a stable batch.");
                var queueSummaryQuery = ReuploadSchemaCompatibility.BuildQueueSummaryQuery(new[] { 8, 7, 8, 0, -1 });
                AssertTrue(
                    queueSummaryQuery.Contains("IN (7,8)") &&
                    queueSummaryQuery.Contains("COUNT(request.pkid) AS reupload_request_count") &&
                    queueSummaryQuery.Contains("define_customers customer") &&
                    queueSummaryQuery.Contains("customer.customer_name"),
                    "Queue summaries must count all requests and include the server customer name.");
                VerifyReuploadDisplayMapping();
                VerifyReuploadAvailabilityTransitions();
                VerifyReuploadSourcePathFallback();
                VerifyFreshCacheSchema();

                Console.WriteLine("Reupload delivery policy, display, and schema checks passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static void AssertEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException($"{message} Expected '{expected}', got '{actual}'.");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void VerifyReuploadAvailabilityTransitions()
        {
            var state = new ReuploadAvailabilityState();
            AssertTrue(
                state.Block(ReuploadSchemaCompatibility.OutdatedMessage),
                "The first schema mismatch should produce a state change and one log event.");
            AssertTrue(
                !state.Block(ReuploadSchemaCompatibility.OutdatedMessage),
                "Repeated schema mismatches should not produce repeated log events.");
            AssertTrue(
                state.IsBlocked && state.Reason == ReuploadSchemaCompatibility.OutdatedMessage,
                "The worker must retain the reupload block reason for the UI.");
            AssertTrue(
                state.Allow() && !state.IsBlocked && state.Reason == "",
                "A successful later probe should automatically clear the reupload block.");
        }

        private static void VerifyReuploadDisplayMapping()
        {
            var createdAt = new DateTime(2026, 8, 13, 17, 9, 11);
            var completedAt = createdAt.AddSeconds(13);
            var summaries = ReuploadDisplayMapper.Build(new[]
            {
                new ReuploadDisplaySource
                {
                    RequestId = 71,
                    QueueId = 7,
                    RequestStatus = ReuploadDeliveryPolicy.Uploaded,
                    RequestCreatedAt = createdAt,
                    RequestCompletedAt = completedAt,
                    CombineIndication = "4M002303000B01082",
                    CustomerCode = "C-01",
                    BagCode = "BAG-1",
                    NumberOfPscOk = 100,
                    ItemStatus = ReuploadDeliveryPolicy.Uploaded,
                    ItemLogs = "attempt=1 phase=finalize outcome=Uploaded",
                },
                new ReuploadDisplaySource
                {
                    RequestId = 71,
                    QueueId = 7,
                    RequestStatus = ReuploadDeliveryPolicy.Uploaded,
                    RequestCreatedAt = createdAt,
                    RequestCompletedAt = completedAt,
                    CombineIndication = "4M002303000B01082",
                    CustomerCode = "C-01",
                    BagCode = "BAG-2",
                    NumberOfPscOk = 200,
                    ItemStatus = ReuploadDeliveryPolicy.Uploaded,
                    ItemLogs = "attempt=1 phase=finalize outcome=Uploaded",
                },
            });

            AssertTrue(summaries.Count == 1, "One request must create exactly one display row.");
            AssertTrue(summaries[0].UploadQuantity == 300, "The display row must sum selected bag quantities.");
            AssertEqual(UploadStatusNames.Success, summaries[0].Status, "Uploaded requests must display as Success.");
            AssertEqual("reupload:71", summaries[0].StableId, "Reupload rows need a request-based stable identity.");
            AssertTrue(summaries[0].Logs.Contains("[BAG-1]") && summaries[0].UploadedAt == completedAt,
                "Attempt logs and completion time must remain visible.");

            AssertEqual(
                UploadStatusNames.NeedsReview,
                ReuploadDisplayMapper.MapStatus(
                    ReuploadDeliveryPolicy.Pending,
                    new[] { ReuploadDeliveryPolicy.Pending, ReuploadDeliveryPolicy.NeedsReview }),
                "NeedsReview must take priority over all other request item states.");

            var normal = new UploadResultView { Pkid = 7, RecordKind = UploadRecordKinds.Normal };
            var reupload = new UploadResultView
            {
                Pkid = 7,
                RecordKind = UploadRecordKinds.Reupload,
                ReuploadRequestId = 71,
            };
            AssertTrue(normal.ReuploadRequestCount == 0 && normal.CustomerName == "-",
                "Queues without reupload requests or a customer must show zero and a placeholder.");
            AssertTrue(normal.StableId != reupload.StableId,
                "Normal and reupload rows sharing a queue id must not collide.");
            AssertTrue(reupload.DisplaySubtitle.Contains("Reupload #71") && reupload.DisplaySubtitle.Contains("Queue #7"),
                "The reupload row must identify both request and queue.");
        }

        private static void VerifyFreshCacheSchema()
        {
            var databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema-verification.db3");
            var workerCachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppConfig.db3");
            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema-verification-source.txt");
            var archiveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema-verification-archive");
            var legacyDatabasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema-verification-legacy.db3");
            DeleteDatabaseArtifacts(databasePath);
            DeleteDatabaseArtifacts(workerCachePath);
            DeleteDatabaseArtifacts(legacyDatabasePath);
            DeleteFileIfPresent(sourcePath);
            DeleteDirectoryIfPresent(archiveDirectory);

            try
            {
                if (!SQLiteHelper.EnsureSchemaForDatabase(databasePath))
                    throw new InvalidOperationException("A fresh SQLite cache schema could not be created.");
                if (!SQLiteHelper.EnsureSchemaForDatabase(databasePath))
                    throw new InvalidOperationException("A second SQLite schema migration should be idempotent.");

                using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
                {
                    connection.Open();
                    AssertTableExists(connection, "dynamic_upload_data_queues");
                    AssertTableExists(connection, "dynamic_aluminum_informations");
                    AssertTableExists(connection, "dynamic_reupload_requests");
                    AssertTableExists(connection, "dynamic_reupload_request_items");
                    AssertColumnExists(connection, "dynamic_upload_data_queues", "pkid_server");
                    AssertColumnExists(connection, "dynamic_upload_data_queues", "reupload_request_count");
                    AssertColumnExists(connection, "dynamic_aluminum_informations", "local_file_path");
                    AssertColumnExists(connection, "dynamic_reupload_request_items", "transfer_started_at");
                    AssertColumnExists(connection, "dynamic_reupload_request_items", "review_reason");
                    AssertColumnExists(connection, "dynamic_reupload_request_items", "number_of_psc_ok");

                    using (var command = new SQLiteCommand(
                        "INSERT INTO dynamic_upload_data_queues (pkid_server) VALUES (1);", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new SQLiteCommand(@"
UPDATE dynamic_upload_data_queues
SET reupload_request_count = 2,
    customer_name = 'Customer New'
WHERE pkid = 1;", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                if (!SQLiteHelper.EnsureSchemaForDatabase(databasePath))
                    throw new InvalidOperationException("Cached reupload summaries could not survive a schema recheck.");

                using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
                {
                    connection.Open();
                    AssertScalar(connection, "SELECT reupload_request_count FROM dynamic_upload_data_queues WHERE pkid = 1;", 2, "Cached reupload count should persist after restart.");
                    AssertTextScalar(connection, "SELECT customer_name FROM dynamic_upload_data_queues WHERE pkid = 1;", "Customer New", "Cached customer should persist after restart.");
                }

                using (var connection = new SQLiteConnection($"Data Source={legacyDatabasePath};Version=3;"))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(@"
CREATE TABLE dynamic_upload_data_queues (
    pkid INTEGER PRIMARY KEY AUTOINCREMENT,
    pkid_server INTEGER,
    latest_reupload_operator_code TEXT
);
INSERT INTO dynamic_upload_data_queues (pkid_server, latest_reupload_operator_code)
VALUES (2, 'LEGACY-OP');", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                if (!SQLiteHelper.EnsureSchemaForDatabase(legacyDatabasePath))
                    throw new InvalidOperationException("An existing SQLite cache could not be migrated.");

                using (var connection = new SQLiteConnection($"Data Source={legacyDatabasePath};Version=3;"))
                {
                    connection.Open();
                    AssertColumnExists(connection, "dynamic_upload_data_queues", "reupload_request_count");
                    AssertColumnExists(connection, "dynamic_upload_data_queues", "latest_reupload_operator_code");
                    AssertTextScalar(connection, "SELECT latest_reupload_operator_code FROM dynamic_upload_data_queues WHERE pkid_server = 2;", "LEGACY-OP", "An obsolete cache column should remain untouched in existing SQLite databases.");
                }

                File.WriteAllText(sourcePath, "fresh SQLite cache verification");
                var archivePath = Path.Combine(archiveDirectory, "bag.txt");
                if (!SQLiteHelper.EnsureSchema())
                    throw new InvalidOperationException("The worker SQLite cache could not be initialized.");
                if (!SQLiteHelper.InsertOrUpdateUploadGroups(new List<MainWindow.UploadGroup>
                {
                    new MainWindow.UploadGroup
                    {
                        Pkid = 99001,
                        CombineIndication = "SCHEMA-TEST",
                        CustomerCode = "TEST",
                        CustomerName = "Schema Customer",
                        CombineLocalPath = archiveDirectory,
                        AluminumBags = new List<MainWindow.AluminumBagInfo>
                        {
                            new MainWindow.AluminumBagInfo
                            {
                                AluminumBagInformationId = 88001,
                                AluminumBagCode = "BAG-SCHEMA-TEST",
                                FilePath = sourcePath,
                                LocalFilePath = archivePath,
                            },
                        },
                    },
                }))
                {
                    throw new InvalidOperationException("The worker could not write a queue to a fresh SQLite cache.");
                }

                using (var connection = new SQLiteConnection($"Data Source={workerCachePath};Version=3;"))
                {
                    connection.Open();
                    AssertScalar(connection, "SELECT COUNT(*) FROM dynamic_upload_data_queues WHERE pkid_server = 99001;", 1, "A queue should be stored in the fresh SQLite cache.");
                    AssertTextScalar(connection, "SELECT customer_name FROM dynamic_upload_data_queues WHERE pkid_server = 99001;", "Schema Customer", "The customer name should be stored in the fresh SQLite cache.");
                    AssertScalar(connection, "SELECT COUNT(*) FROM dynamic_aluminum_informations WHERE aluminum_bag_information_id_server = 88001;", 1, "A bag should be stored in the fresh SQLite cache.");
                    AssertScalar(connection, "SELECT is_download FROM dynamic_upload_data_queues WHERE pkid_server = 99001;", 1, "The archived test bag should mark the queue ready for upload.");
                }
            }
            finally
            {
                DeleteDatabaseArtifacts(databasePath);
                DeleteDatabaseArtifacts(workerCachePath);
                DeleteDatabaseArtifacts(legacyDatabasePath);
                DeleteFileIfPresent(sourcePath);
                DeleteDirectoryIfPresent(archiveDirectory);
            }
        }

        private static void AssertTableExists(SQLiteConnection connection, string tableName)
        {
            using (var command = new SQLiteCommand("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @table_name;", connection))
            {
                command.Parameters.AddWithValue("@table_name", tableName);
                if (Convert.ToInt32(command.ExecuteScalar()) != 1)
                    throw new InvalidOperationException($"SQLite table '{tableName}' was not created.");
            }
        }

        private static void AssertColumnExists(SQLiteConnection connection, string tableName, string columnName)
        {
            using (var command = new SQLiteCommand($"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = @column_name;", connection))
            {
                command.Parameters.AddWithValue("@column_name", columnName);
                if (Convert.ToInt32(command.ExecuteScalar()) != 1)
                    throw new InvalidOperationException($"SQLite column '{tableName}.{columnName}' was not created.");
            }
        }

        private static void DeleteDatabaseArtifacts(string databasePath)
        {
            foreach (var path in new[] { databasePath, databasePath + "-journal", databasePath + "-shm", databasePath + "-wal" })
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static void AssertScalar(SQLiteConnection connection, string sql, int expected, string message)
        {
            using (var command = new SQLiteCommand(sql, connection))
            {
                if (Convert.ToInt32(command.ExecuteScalar()) != expected)
                    throw new InvalidOperationException(message);
            }
        }

        private static void VerifyReuploadSourcePathFallback()
        {
            var testRoot = Path.Combine(Path.GetTempPath(), "AutoUploadQCGate-ReuploadSource-" + Guid.NewGuid().ToString("N"));
            var combineIndication = "4M002303000B01081";
            var bagCode = "FNJ055820A0FC9";
            var configuredDirectory = Path.Combine(testRoot, "Destination", combineIndication);
            var configuredPath = Path.Combine(configuredDirectory, bagCode + ".txt");
            var oldSourcePath = Path.Combine(testRoot, "Legacy", combineIndication, bagCode + ".txt");
            var snapshotPath = Path.Combine(testRoot, "Snapshot", bagCode + ".txt");

            Directory.CreateDirectory(configuredDirectory);
            File.WriteAllText(configuredPath, "current destination file");
            try
            {
                var fallback = ReuploadSourcePathResolver.Resolve(
                    snapshotPath,
                    oldSourcePath,
                    Path.Combine(testRoot, "Destination"),
                    combineIndication,
                    bagCode);
                AssertTrue(
                    fallback.UsesConfiguredSource && fallback.SourcePath == configuredPath && fallback.DiagnosticLog.Contains("Stored source file not found"),
                    "A missing legacy source should fall back to the current Combine Emap Log File Path.");

                Directory.CreateDirectory(Path.GetDirectoryName(oldSourcePath));
                File.WriteAllText(oldSourcePath, "legacy source file");
                var stored = ReuploadSourcePathResolver.Resolve(
                    snapshotPath,
                    oldSourcePath,
                    Path.Combine(testRoot, "Destination"),
                    combineIndication,
                    bagCode);
                AssertTrue(
                    !stored.UsesExistingSnapshot && !stored.UsesConfiguredSource && stored.SourcePath == oldSourcePath,
                    "An existing stored source must take priority over the configured fallback.");

                Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath));
                File.WriteAllText(snapshotPath, "immutable snapshot");
                var snapshot = ReuploadSourcePathResolver.Resolve(
                    snapshotPath,
                    oldSourcePath,
                    Path.Combine(testRoot, "Destination"),
                    combineIndication,
                    bagCode);
                AssertTrue(
                    snapshot.UsesExistingSnapshot && !snapshot.UsesConfiguredSource,
                    "An existing snapshot must remain the first re-upload source.");

                File.Delete(oldSourcePath);
                var missing = ReuploadSourcePathResolver.Resolve(
                    Path.Combine(testRoot, "missing-snapshot.txt"),
                    oldSourcePath,
                    Path.Combine(testRoot, "MissingDestination"),
                    combineIndication,
                    bagCode);
                AssertTrue(
                    missing.DiagnosticLog.Contains("Current Combine Emap Log File Path file not found"),
                    "A missing fallback must identify the current path that was checked.");
            }
            finally
            {
                DeleteDirectoryIfPresent(testRoot);
            }
        }

        private static void AssertTextScalar(SQLiteConnection connection, string sql, string expected, string message)
        {
            using (var command = new SQLiteCommand(sql, connection))
            {
                var actual = Convert.ToString(command.ExecuteScalar());
                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                    throw new InvalidOperationException(message);
            }
        }

        private static void DeleteFileIfPresent(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void DeleteDirectoryIfPresent(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }
}
