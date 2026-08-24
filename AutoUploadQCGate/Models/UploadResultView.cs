using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using static AutoUploadQCGate.MainWindow;

namespace AutoUploadQCGate.Models
{
    internal static class UploadStatusNames
    {
        public const string All = "All";
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Success = "Success";
        public const string Failed = "Failed";
        public const string NeedsReview = "NeedsReview";

        public static string FromPersistence(bool isUploaded, string judgement)
        {
            if (isUploaded)
                return Success;

            return string.Equals(judgement, "FAIL", StringComparison.OrdinalIgnoreCase)
                ? Failed
                : Pending;
        }

        public static string Normalize(string status)
        {
            if (string.Equals(status, Processing, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Uploading", StringComparison.OrdinalIgnoreCase))
                return Processing;

            if (string.Equals(status, Success, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Uploaded", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                return Success;

            if (string.Equals(status, Failed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Error", StringComparison.OrdinalIgnoreCase))
                return Failed;

            if (string.Equals(status, NeedsReview, StringComparison.OrdinalIgnoreCase))
                return NeedsReview;

            return Pending;
        }
    }

    internal static class UploadResultFilter
    {
        public static bool MatchesNonStatus(UploadResultView result, string searchText,
            DateTime? fromDate, DateTime? toDate)
        {
            searchText = (searchText ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(searchText) &&
                !ContainsIgnoreCase(result.CombineIndication, searchText) &&
                !ContainsIgnoreCase(result.CustomerCode, searchText) &&
                !ContainsIgnoreCase(result.Log, searchText))
                return false;

            if (fromDate.HasValue || toDate.HasValue)
            {
                if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                    return false;

                if (!result.UploadedAtValue.HasValue)
                    return false;

                var uploadedDate = result.UploadedAtValue.Value.Date;
                if (fromDate.HasValue && uploadedDate < fromDate.Value.Date)
                    return false;
                if (toDate.HasValue && uploadedDate > toDate.Value.Date)
                    return false;
            }

            return true;
        }

        public static bool MatchesStatus(UploadResultView result, string selectedStatus)
        {
            return string.Equals(selectedStatus, UploadStatusNames.All, StringComparison.Ordinal) ||
                   string.Equals(result.Status, selectedStatus, StringComparison.Ordinal);
        }

        private static bool ContainsIgnoreCase(string value, string searchText)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public class UploadResultView : INotifyPropertyChanged
    {
        private int _pkid;
        private int _pkidLocal;
        private string _combineIndication;
        private string _recordKind = UploadRecordKinds.Normal;
        private int? _reuploadRequestId;
        private int _uploadQuantity;
        private int _reuploadRequestCount;
        private string _requestedBy;
        private string _customerCode;
        private bool? _isUploadFolder;
        private bool? _isUseProxy;
        private bool? _isUseKey;
        private bool? _isReupload;
        private bool _isUploaded;
        private string _folderName;
        private string _combineServerPath;
        private string _combineLocalPath;
        private List<AluminumBagInfo> _aluminumBags = new List<AluminumBagInfo>();

        // SFTP Info
        private string _sftpServer;
        private int? _sftpPort;
        private string _sftpUser;
        private string _sftpPassword;
        private string _sftpRemotePath;

        // Result Info
        private string _log;
        private string _judgement;
        private string _status;
        private string _uploadedAt;
        private DateTime? _uploadedAtValue;

        public int PkidLocal
        {
            get => _pkidLocal;
            set { _pkidLocal = value; OnPropertyChanged(nameof(PkidLocal)); }
        }
        public int Pkid
        {
            get => _pkid;
            set
            {
                _pkid = value;
                OnPropertyChanged(nameof(Pkid));
                OnPropertyChanged(nameof(StableId));
                OnPropertyChanged(nameof(DisplaySubtitle));
            }
        }

        public string CombineIndication
        {
            get => _combineIndication;
            set { _combineIndication = value; OnPropertyChanged(nameof(CombineIndication)); }
        }
        public string RecordKind
        {
            get => _recordKind;
            set
            {
                _recordKind = string.IsNullOrWhiteSpace(value) ? UploadRecordKinds.Normal : value;
                OnPropertyChanged(nameof(RecordKind));
                OnPropertyChanged(nameof(StableId));
                OnPropertyChanged(nameof(DisplaySubtitle));
            }
        }
        public int? ReuploadRequestId
        {
            get => _reuploadRequestId;
            set
            {
                _reuploadRequestId = value;
                OnPropertyChanged(nameof(ReuploadRequestId));
                OnPropertyChanged(nameof(StableId));
                OnPropertyChanged(nameof(DisplaySubtitle));
            }
        }
        public string StableId => string.Equals(RecordKind, UploadRecordKinds.Reupload, StringComparison.Ordinal)
            ? $"reupload:{ReuploadRequestId.GetValueOrDefault()}"
            : $"normal:{Pkid}";
        public string DisplaySubtitle => string.Equals(RecordKind, UploadRecordKinds.Reupload, StringComparison.Ordinal)
            ? $"Reupload #{ReuploadRequestId.GetValueOrDefault()} \u2022 Queue #{Pkid}"
            : CustomerCode;
        public List<AluminumBagInfo> AluminumBags
        {
            get => _aluminumBags;
            set { _aluminumBags = value; OnPropertyChanged(nameof(AluminumBags)); }
        }
        public int UploadQuantity
        {
            get => _uploadQuantity;
            set { _uploadQuantity = value; OnPropertyChanged(nameof(UploadQuantity)); }
        }
        public int ReuploadRequestCount
        {
            get => _reuploadRequestCount;
            set { _reuploadRequestCount = value; OnPropertyChanged(nameof(ReuploadRequestCount)); }
        }
        public string RequestedBy
        {
            get => string.IsNullOrWhiteSpace(_requestedBy) ? "-" : _requestedBy;
            set
            {
                _requestedBy = value ?? string.Empty;
                OnPropertyChanged(nameof(RequestedBy));
            }
        }

        public string CustomerCode
        {
            get => _customerCode;
            set
            {
                _customerCode = value;
                OnPropertyChanged(nameof(CustomerCode));
                OnPropertyChanged(nameof(DisplaySubtitle));
            }
        }

        public bool? IsUploadFolder
        {
            get => _isUploadFolder;
            set { _isUploadFolder = value; OnPropertyChanged(nameof(IsUploadFolder)); }
        } 
        public bool? IsUseKey
        {
            get => _isUseKey;
            set { _isUseKey = value; OnPropertyChanged(nameof(IsUseKey)); }
        } 
        public bool? IsUseProxy
        {
            get => _isUseProxy;
            set { _isUseProxy = value; OnPropertyChanged(nameof(IsUseProxy)); }
        }

        public bool? IsReupload
        {
            get => _isReupload;
            set { _isReupload = value; OnPropertyChanged(nameof(IsReupload)); }
        }

        public bool IsUploaded
        {
            get => _isUploaded;
            set { _isUploaded = value; OnPropertyChanged(nameof(IsUploaded)); }
        }

        public string FolderName
        {
            get => _folderName;
            set { _folderName = value; OnPropertyChanged(nameof(FolderName)); }
        }

        public string CombineServerPath
        {
            get => _combineServerPath;
            set { _combineServerPath = value; OnPropertyChanged(nameof(CombineServerPath)); }
        }

        public string CombineLocalPath
        {
            get => _combineLocalPath;
            set { _combineLocalPath = value; OnPropertyChanged(nameof(CombineLocalPath)); }
        }

        public string SftpServer
        {
            get => _sftpServer;
            set { _sftpServer = value; OnPropertyChanged(nameof(SftpServer)); }
        }

        public int? SftpPort
        {
            get => _sftpPort;
            set { _sftpPort = value; OnPropertyChanged(nameof(SftpPort)); }
        }

        public string SftpUser
        {
            get => _sftpUser;
            set { _sftpUser = value; OnPropertyChanged(nameof(SftpUser)); }
        }

        public string SftpPassword
        {
            get => _sftpPassword;
            set { _sftpPassword = value; OnPropertyChanged(nameof(SftpPassword)); }
        }

        public string SftpRemotePath
        {
            get => _sftpRemotePath;
            set { _sftpRemotePath = value; OnPropertyChanged(nameof(SftpRemotePath)); }
        }

        public string Log
        {
            get => _log;
            set { _log = value; OnPropertyChanged(nameof(Log)); }
        }

        public string Judgement
        {
            get => _judgement;
            set
            {
                var normalizedJudgement = NormalizeJudgement(value);
                if (_judgement == normalizedJudgement)
                    return;

                _judgement = normalizedJudgement;
                OnPropertyChanged(nameof(Judgement));
            }
        }

        private static string NormalizeJudgement(string judgement)
        {
            if (string.Equals(judgement, "PASS", StringComparison.OrdinalIgnoreCase))
                return "PASS";
            if (string.Equals(judgement, "FAIL", StringComparison.OrdinalIgnoreCase))
                return "FAIL";

            return string.IsNullOrWhiteSpace(judgement) ? string.Empty : judgement.Trim();
        }

        public string Status
        {
            get => _status;
            set
            {
                var normalizedStatus = UploadStatusNames.Normalize(value);
                if (_status == normalizedStatus)
                    return;

                _status = normalizedStatus;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string UploadedAt
        {
            get => _uploadedAt;
            set
            {
                if (_uploadedAt == value)
                    return;

                _uploadedAt = value;
                _uploadedAtValue = ParseUploadedAt(value);
                OnPropertyChanged(nameof(UploadedAt));
                OnPropertyChanged(nameof(UploadedAtValue));
            }
        }

        public DateTime? UploadedAtValue
        {
            get => _uploadedAtValue;
        }

        private static DateTime? ParseUploadedAt(string value)
        {
            DateTime uploadedAt;
            if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out uploadedAt))
                return uploadedAt;

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture,
                DateTimeStyles.None, out uploadedAt))
                return uploadedAt;

            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
