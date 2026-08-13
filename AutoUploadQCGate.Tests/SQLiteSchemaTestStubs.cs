using System;
using System.Collections.Generic;

public static class Global
{
    public static void WriteLogFile(string message)
    {
    }
}

public static class Conv
{
    public static int atoi32(object value)
    {
        int result;
        return int.TryParse(value == null ? "" : value.ToString(), out result) ? result : 0;
    }

    public static string atos(object value)
    {
        return value == null || value == DBNull.Value ? "" : value.ToString();
    }
}

namespace AutoUploadQCGate
{
    public class MainWindow
    {
        public class AluminumBagInfo
        {
            public int AluminumBagInformationId { get; set; }
            public string AluminumBagCode { get; set; }
            public string FilePath { get; set; }
            public string LocalFilePath { get; set; }
            public string FileHash { get; set; }
        }

        public class UploadGroup
        {
            public int Pkid { get; set; }
            public int QuantityUpload { get; set; }
            public string CombineIndication { get; set; }
            public string CustomerCode { get; set; }
            public bool? IsUploadFolder { get; set; }
            public bool? IsUseKey { get; set; }
            public bool? IsUseProxy { get; set; }
            public string FolderName { get; set; }
            public string CombineServerPath { get; set; }
            public string CombineLocalPath { get; set; }
            public string SftpServer { get; set; }
            public int? SftpPort { get; set; }
            public string SftpUser { get; set; }
            public string SftpPassword { get; set; }
            public string SftpRemotePath { get; set; }
            public List<AluminumBagInfo> AluminumBags { get; set; } = new List<AluminumBagInfo>();
        }
    }
}
