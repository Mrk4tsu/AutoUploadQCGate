using System;
using System.IO;

namespace AutoUploadQCGate.Models
{
    public sealed class ReuploadSourcePathResolution
    {
        public string SourcePath { get; set; }
        public string DiagnosticLog { get; set; }
        public bool UsesExistingSnapshot { get; set; }
        public bool UsesConfiguredSource { get; set; }
    }

    public static class ReuploadSourcePathResolver
    {
        public static ReuploadSourcePathResolution Resolve(
            string localSnapshotPath,
            string storedSourcePath,
            string combineRootPath,
            string combineIndication,
            string aluminumBagCode)
        {
            var storedPath = (storedSourcePath ?? "").Trim();
            if (File.Exists(localSnapshotPath))
            {
                return new ReuploadSourcePathResolution
                {
                    SourcePath = storedPath,
                    UsesExistingSnapshot = true,
                    DiagnosticLog = ""
                };
            }

            if (File.Exists(storedPath))
            {
                return new ReuploadSourcePathResolution
                {
                    SourcePath = storedPath,
                    DiagnosticLog = ""
                };
            }

            var configuredPath = BuildConfiguredSourcePath(combineRootPath, combineIndication, aluminumBagCode);
            if (File.Exists(configuredPath))
            {
                return new ReuploadSourcePathResolution
                {
                    SourcePath = configuredPath,
                    UsesConfiguredSource = true,
                    DiagnosticLog = $"Stored source file not found: {storedPath}{Environment.NewLine}Using current Combine Emap Log File Path: {configuredPath}"
                };
            }

            var diagnostic = string.IsNullOrWhiteSpace(configuredPath)
                ? $"Stored source file not found: {storedPath}{Environment.NewLine}Current Combine Emap Log File Path is unavailable."
                : $"Stored source file not found: {storedPath}{Environment.NewLine}Current Combine Emap Log File Path file not found: {configuredPath}";
            return new ReuploadSourcePathResolution
            {
                SourcePath = string.IsNullOrWhiteSpace(configuredPath) ? storedPath : configuredPath,
                DiagnosticLog = diagnostic
            };
        }

        public static string BuildConfiguredSourcePath(string combineRootPath, string combineIndication, string aluminumBagCode)
        {
            var root = (combineRootPath ?? "").Trim();
            var indication = (combineIndication ?? "").Trim();
            var bagCode = (aluminumBagCode ?? "").Trim();
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(indication) || string.IsNullOrWhiteSpace(bagCode))
                return "";

            var fileName = bagCode.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                ? bagCode
                : bagCode + ".txt";
            return Path.Combine(root, indication, fileName);
        }
    }
}
