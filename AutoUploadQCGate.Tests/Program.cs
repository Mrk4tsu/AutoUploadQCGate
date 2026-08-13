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
                    ReuploadSchemaCompatibility.WorkQuerySql.Contains("ORDER BY r.created_at, ri.pkid"),
                    "Every ORDER BY expression in the DISTINCT work query must also be projected.");
                VerifyReuploadAvailabilityTransitions();
                VerifyFreshCacheSchema();

                Console.WriteLine("Reupload delivery policy and schema checks passed: 15/15");
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

        private static void VerifyFreshCacheSchema()
        {
            var databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema-verification.db3");
            var workerCachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppConfig.db3");
            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema-verification-source.txt");
            var archiveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema-verification-archive");
            DeleteDatabaseArtifacts(databasePath);
            DeleteDatabaseArtifacts(workerCachePath);
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
                    AssertColumnExists(connection, "dynamic_aluminum_informations", "local_file_path");
                    AssertColumnExists(connection, "dynamic_reupload_request_items", "transfer_started_at");
                    AssertColumnExists(connection, "dynamic_reupload_request_items", "review_reason");
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
                    AssertScalar(connection, "SELECT COUNT(*) FROM dynamic_aluminum_informations WHERE aluminum_bag_information_id_server = 88001;", 1, "A bag should be stored in the fresh SQLite cache.");
                    AssertScalar(connection, "SELECT is_download FROM dynamic_upload_data_queues WHERE pkid_server = 99001;", 1, "The archived test bag should mark the queue ready for upload.");
                }
            }
            finally
            {
                DeleteDatabaseArtifacts(databasePath);
                DeleteDatabaseArtifacts(workerCachePath);
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
