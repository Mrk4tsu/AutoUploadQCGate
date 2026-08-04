using System;
using System.Collections.Generic;
using System.ComponentModel;
using static AutoUploadQCGate.MainWindow;

namespace AutoUploadQCGate.Models
{
    public class UploadResultView : INotifyPropertyChanged
    {
        private int _pkid;
        private int _pkidLocal;
        private string _combineIndication;
        private int _uploadQuantity;
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

        public int PkidLocal
        {
            get => _pkidLocal;
            set { _pkidLocal = value; OnPropertyChanged(nameof(PkidLocal)); }
        }
        public int Pkid
        {
            get => _pkid;
            set { _pkid = value; OnPropertyChanged(nameof(Pkid)); }
        }

        public string CombineIndication
        {
            get => _combineIndication;
            set { _combineIndication = value; OnPropertyChanged(nameof(CombineIndication)); }
        }
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

        public string CustomerCode
        {
            get => _customerCode;
            set { _customerCode = value; OnPropertyChanged(nameof(CustomerCode)); }
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
            set { _judgement = value; OnPropertyChanged(nameof(Judgement)); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public string UploadedAt
        {
            get => _uploadedAt;
            set { _uploadedAt = value; OnPropertyChanged(nameof(UploadedAt)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
