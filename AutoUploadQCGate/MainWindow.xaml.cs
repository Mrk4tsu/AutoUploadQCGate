using DefaultNS.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using Renci.SshNet;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Windows.Threading;
using AutoUploadQCGate.Models;
using DefaultNS;
using WinSCP;

namespace AutoUploadQCGate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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
            public string CustomerName { get; set; }
            public bool? IsUploadFolder { get; set; }
            public bool? IsUseKey { get; set; }
            public bool? IsUseProxy { get; set; }
            public bool? IsReupload { get; set; }
            public bool? IsUploaded { get; set; }
            public string FolderName { get; set; }
            public string CombineServerPath { get; set; }
            public string CombineLocalPath { get; set; }
            public string ItemName { get; set; }

            // Thông tin SFTP
            public string SftpServer { get; set; }
            public int? SftpPort { get; set; }
            public string SftpUser { get; set; }
            public string SftpPassword { get; set; }
            public string SftpRemotePath { get; set; }

            // Danh sách file
            public List<AluminumBagInfo> AluminumBags { get; set; } = new List<AluminumBagInfo>();
        }

        private sealed class ReuploadWorkItem
        {
            public int RequestId { get; set; }
            public int ItemId { get; set; }
            public int QueueId { get; set; }
            public int BagId { get; set; }
            public string BagCode { get; set; }
            public string SourcePath { get; set; }
            public string LocalPath { get; set; }
            public string FileHash { get; set; }
            public int AttemptCount { get; set; }
            public string ExistingLogs { get; set; }
            public bool IsLegacyRecovered { get; set; }
            public UploadResultView Upload { get; set; }
        }
        private ObservableCollection<UploadResultView> _uploadResults = new ObservableCollection<UploadResultView>();
        private ICollectionView _uploadResultsView;
        private DispatcherTimer _searchDebounceTimer;
        private DispatcherTimer _filterRefreshTimer;
        private string _selectedStatus = UploadStatusNames.All;
        private AppSettings _appSetting = new AppSettings();
        private bool _isThreadProcess = false;
        private Task _scanTask;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lockObject = new object();
        private readonly ReuploadAvailabilityState _reuploadAvailability = new ReuploadAvailabilityState();
        private DataTable _queuesTable = new DataTable();
        List<UploadGroup> _updateRowSources = new List<UploadGroup>();
        private int _totalRecords = 0;
        private int _numberOfOK = 0;
        private int _numberOfNG = 0;

        public MainWindow()
        {
            InitializeComponent();
            InitializeFiltering();
            UpdateSetting();
            SQLiteHelper.EnsureSchema();
            InitializeScanThread();
            Global.WriteLog($"Log phần mềm QC GATE");
        }

        private void InitializeFiltering()
        {
            BindUploadResultsView();

            _searchDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchDebounceTimer.Tick += (sender, args) =>
            {
                _searchDebounceTimer.Stop();
                RefreshFilteredView();
            };

            _filterRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(75)
            };
            _filterRefreshTimer.Tick += (sender, args) =>
            {
                _filterRefreshTimer.Stop();
                RefreshFilteredView();
            };

            RefreshFilteredView();
            UpdateApplicationStatus();
        }

        private void BindUploadResultsView()
        {
            _uploadResultsView = CollectionViewSource.GetDefaultView(_uploadResults);
            _uploadResultsView.Filter = FilterUploadResult;
            DataList.ItemsSource = _uploadResultsView;
        }

        private bool FilterUploadResult(object item)
        {
            var result = item as UploadResultView;
            return result != null && MatchesNonStatusFilters(result) && MatchesSelectedStatus(result);
        }

        private bool MatchesNonStatusFilters(UploadResultView result)
        {
            var searchText = txtSearch == null ? string.Empty : txtSearch.Text.Trim();
            var fromDate = dateFrom == null ? null : dateFrom.SelectedDate;
            var toDate = dateTo == null ? null : dateTo.SelectedDate;
            return UploadResultFilter.MatchesNonStatus(result, searchText, fromDate, toDate);
        }

        private bool MatchesSelectedStatus(UploadResultView result)
        {
            return UploadResultFilter.MatchesStatus(result, _selectedStatus);
        }

        private void RefreshFilteredView()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(RefreshFilteredView));
                return;
            }

            if (_uploadResultsView == null)
                return;

            var hasInvalidDateRange = dateFrom.SelectedDate.HasValue && dateTo.SelectedDate.HasValue &&
                                      dateFrom.SelectedDate.Value.Date > dateTo.SelectedDate.Value.Date;
            txtDateValidation.Visibility = hasInvalidDateRange ? Visibility.Visible : Visibility.Collapsed;

            _uploadResultsView.Refresh();

            var nonStatusResults = _uploadResults.Where(MatchesNonStatusFilters).ToList();
            txtAllCount.Text = nonStatusResults.Count.ToString("N0");
            txtPendingCount.Text = nonStatusResults.Count(x => x.Status == UploadStatusNames.Pending).ToString("N0");
            txtProcessingCount.Text = nonStatusResults.Count(x => x.Status == UploadStatusNames.Processing).ToString("N0");
            txtSuccessCount.Text = nonStatusResults.Count(x => x.Status == UploadStatusNames.Success).ToString("N0");
            txtFailedCount.Text = nonStatusResults.Count(x => x.Status == UploadStatusNames.Failed).ToString("N0");
            txtNeedsReviewCount.Text = nonStatusResults.Count(x => x.Status == UploadStatusNames.NeedsReview).ToString("N0");

            var visibleResults = nonStatusResults.Where(MatchesSelectedStatus).ToList();
            txtTotalRecords.Text = visibleResults.Count.ToString("N0");
            txtOKRecords.Text = visibleResults.Count(x => x.Status == UploadStatusNames.Success).ToString("N0");
            txtNGRecords.Text = visibleResults.Count(x => x.Status == UploadStatusNames.Failed).ToString("N0");
            emptyState.Visibility = visibleResults.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ScheduleFilterRefresh()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(ScheduleFilterRefresh));
                return;
            }

            if (_filterRefreshTimer == null)
                return;

            _filterRefreshTimer.Stop();
            _filterRefreshTimer.Start();
        }

        private void ReplaceUploadResults(IEnumerable<UploadResultView> results)
        {
            foreach (var existingResult in _uploadResults)
                existingResult.PropertyChanged -= UploadResult_PropertyChanged;

            var replacementResults = new ObservableCollection<UploadResultView>();
            foreach (var result in results)
            {
                result.PropertyChanged += UploadResult_PropertyChanged;
                replacementResults.Add(result);
            }

            _uploadResults = replacementResults;
            BindUploadResultsView();
            RefreshFilteredView();
        }

        private void UploadResult_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UploadResultView.Status) ||
                e.PropertyName == nameof(UploadResultView.Judgement) ||
                e.PropertyName == nameof(UploadResultView.UploadedAt) ||
                e.PropertyName == nameof(UploadResultView.Log) ||
                e.PropertyName == nameof(UploadResultView.CustomerCode) ||
                e.PropertyName == nameof(UploadResultView.CustomerName) ||
                e.PropertyName == nameof(UploadResultView.CombineIndication))
                ScheduleFilterRefresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_searchDebounceTimer == null)
                return;

            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void DateFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ScheduleFilterRefresh();
        }

        private void StatusTab_Checked(object sender, RoutedEventArgs e)
        {
            var statusTab = sender as RadioButton;
            if (statusTab != null)
                _selectedStatus = statusTab.Tag as string ?? UploadStatusNames.All;

            ScheduleFilterRefresh();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            dateFrom.SelectedDate = null;
            dateTo.SelectedDate = null;
            _selectedStatus = UploadStatusNames.All;
            tabAll.IsChecked = true;
            _searchDebounceTimer.Stop();
            _filterRefreshTimer.Stop();
            RefreshFilteredView();
        }

        private void UpdateApplicationStatus()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(UpdateApplicationStatus));
                return;
            }

            var isReuploadBlocked = _reuploadAvailability.IsBlocked;
            var reuploadBlockReason = _reuploadAvailability.Reason;

            txtReuploadWarning.Text = reuploadBlockReason;
            txtReuploadWarning.Visibility = isReuploadBlocked ? Visibility.Visible : Visibility.Collapsed;

            if (_isThreadProcess && isReuploadBlocked)
            {
                txtAppStatus.Text = "Running — Reupload blocked";
                txtAppStatus.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                appStatusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 247, 237));
                appStatusDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }
            else if (_isThreadProcess)
            {
                txtAppStatus.Text = "Running";
                txtAppStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 131, 63));
                appStatusBadge.Background = new SolidColorBrush(Color.FromRgb(234, 248, 239));
                appStatusDot.Fill = new SolidColorBrush(Color.FromRgb(31, 157, 85));
            }
            else
            {
                txtAppStatus.Text = "Stopped";
                txtAppStatus.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105));
                appStatusBadge.Background = new SolidColorBrush(Color.FromRgb(238, 242, 246));
                appStatusDot.Fill = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            }
        }

        private bool SetReuploadBlocked(string reason)
        {
            var changed = _reuploadAvailability.Block(reason);

            if (changed)
            {
                Global.WriteLogFile("[ReuploadAvailability] Blocked: " + _reuploadAvailability.Reason);
                Dispatcher.BeginInvoke(new Action(UpdateApplicationStatus));
            }
            return changed;
        }

        private void SetReuploadAvailable()
        {
            var changed = _reuploadAvailability.Allow();

            if (changed)
            {
                Global.WriteLogFile("[ReuploadAvailability] Reupload database schema is ready; processing resumed.");
                Dispatcher.BeginInvoke(new Action(UpdateApplicationStatus));
            }
        }

        private void InitializeScanThread()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _scanTask = Task.Run(async () => await ScanProcess(_cancellationTokenSource.Token));
        }

        private async Task ScanProcess(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Kiểm tra nếu được phép chạy
                    if (_isThreadProcess)
                    {
                        await PerformScanAndUpload();
                    }
                    int scanInterval = GetScanIntervalInMilliseconds();
                    await Task.Delay(scanInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Thread bị cancel, thoát khỏi vòng lặp
                    break;
                }
                catch (Exception ex)
                {
                    // Log lỗi và tiếp tục chạy
                    LogError($"Scan process error: {ex}");
                    await Task.Delay(5000, cancellationToken); // Chờ 5 giây trước khi thử lại
                }
            }
        }

        private async Task PerformScanAndUpload()
        {
            try
            {
                await UpdateUI();
                await Task.Delay(100);
                await UpdateDataLocal();
                await Task.Delay(100);
                await UploadToSFTP();
                await Task.Delay(100);
                await UpdateReuploadDataLocal();
                await UpdateUI();
                await ProcessReuploadRequests();
                await UpdateUI();
                await Task.Delay(100);
                await UpdateDataServer();
                await Task.Delay(100);
                UpdateUIWithProgress();
                await Task.Delay(_appSetting.ScanInterval);
                
            }
            catch (Exception ex)
            {
                LogError($"Scan and upload cycle failed: {ex}");
                UpdateUIWithProgress();
            }
        }
        private async Task UpdateDataLocal()
        {
            if (msSQL.TestConnection())
            {
               var table = msSQL.ExecuteDataTable(@"
                    SELECT 
                        t1.pkid,
                        t1.created_at AS queue_created_at,
                        t3.pkid AS aluminum_bag_information_id,
                        t3.aluminum_bag_code,
                        t3.file_path,
                        t5.customer_code,
                        t5.customer_name,
                        t5.is_upload_folder,
                        t5.sftp_server,
                        t5.sftp_port,
                        t5.sftp_user,
                        t5.sftp_password,
                        t5.sftp_remote_path,
                        t5.is_use_proxy,
                        t5.is_use_key,
                        t4.combine_indication,
                        t4.combine_indication_log_path,
                        t4.folder_name,
                        t1.is_reupload,
                        t3.number_of_psc_ok,
                        t6.item_name
                    FROM dynamic_upload_data_queues t1 
                    LEFT JOIN dynamic_aluminum_bag_information_queues t2 ON t2.upload_data_queue_id = t1.pkid 
                    LEFT JOIN dynamic_aluminum_bag_informations t3 ON t2.aluminum_bag_information_id = t3.pkid 
                    LEFT JOIN dynamic_upload_data t4 ON t1.upload_data_id = t4.pkid 
                    LEFT JOIN define_customers t5 ON t4.customer_id = t5.pkid 
                    LEFT JOIN define_design_informations t6 ON t4.design_information_id = t6.pkid 
                    WHERE t1.is_active = 1");

                if (table.Rows.Count > 0)
                {
                    _updateRowSources = ConvertToGroupedList(table);
                    if (_updateRowSources.Count > 0)
                    {
                        SQLiteHelper.InsertOrUpdateUploadGroups(_updateRowSources);
                    }
                }
            }
            await Task.Delay(100);
        }

        private static SqlParameter SqlP(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        private bool IsReuploadSchemaReady()
        {
            object probeValue;
            string errorMessage;
            if (!msSQL.TryExecuteScalar(
                    ReuploadSchemaCompatibility.ProbeSql,
                    out probeValue,
                    out errorMessage))
            {
                if (SetReuploadBlocked(ReuploadSchemaCompatibility.ValidationFailureMessage))
                    Global.WriteLogFile("[ReuploadSchemaPreflight] " + errorMessage);
                return false;
            }

            if (!ReuploadSchemaCompatibility.IsReady(probeValue))
            {
                SetReuploadBlocked(ReuploadSchemaCompatibility.OutdatedMessage);
                return false;
            }

            return true;
        }

        private void RecomputeAllReuploadRequestStatuses()
        {
            var affectedRows = msSQL.ExecuteNonQueryCount(@"
UPDATE request
SET status = CASE
        WHEN EXISTS (
            SELECT 1 FROM dynamic_reupload_request_items item
            WHERE item.reupload_request_id = request.pkid AND item.status = 'NeedsReview'
        ) THEN 'NeedsReview'
        WHEN EXISTS (
            SELECT 1 FROM dynamic_reupload_request_items item
            WHERE item.reupload_request_id = request.pkid AND item.status = 'Processing'
        ) THEN 'Processing'
        WHEN EXISTS (
            SELECT 1 FROM dynamic_reupload_request_items item
            WHERE item.reupload_request_id = request.pkid AND item.status = 'Pending'
        ) THEN 'Pending'
        WHEN EXISTS (
            SELECT 1 FROM dynamic_reupload_request_items item
            WHERE item.reupload_request_id = request.pkid AND item.status = 'Failed'
        ) THEN 'Failed'
        ELSE 'Uploaded'
    END,
    completed_at = CASE
        WHEN EXISTS (
            SELECT 1 FROM dynamic_reupload_request_items item
            WHERE item.reupload_request_id = request.pkid
              AND item.status IN ('NeedsReview', 'Processing', 'Pending')
        ) THEN NULL
        ELSE COALESCE(request.completed_at, GETDATE())
    END,
    updated_at = GETDATE()
FROM dynamic_reupload_requests request
WHERE request.status IN ('Pending', 'Processing', 'NeedsReview')
   OR EXISTS (
       SELECT 1 FROM dynamic_reupload_request_items item
       WHERE item.reupload_request_id = request.pkid
         AND item.status IN ('NeedsReview', 'Processing', 'Pending')
   );");
            if (affectedRows < 0)
                Global.WriteLogFile("Re-upload request status recomputation failed.");
        }

        private DataTable LoadReuploadWorkTable()
        {
            if (!msSQL.TestConnection())
            {
                SetReuploadBlocked(ReuploadSchemaCompatibility.ValidationFailureMessage);
                return new DataTable();
            }
            if (!IsReuploadSchemaReady())
                return new DataTable();

            // Recover work items that were left in Processing by a terminated
            // AutoUpload instance. This is application-level state management;
            // the database intentionally has no foreign keys.
            var recoveredRows = msSQL.ExecuteNonQueryCount(@"
UPDATE dynamic_reupload_request_items
SET status = CASE WHEN transfer_started_at IS NULL THEN 'Pending' ELSE 'NeedsReview' END,
    review_reason = CASE
        WHEN transfer_started_at IS NULL THEN NULL
        ELSE 'Worker stopped after transfer started; receiver outcome must be verified.'
    END,
    updated_at = GETDATE(),
    logs = CONCAT(
        COALESCE(logs, ''),
        CASE WHEN COALESCE(logs, '') = '' THEN '' ELSE CHAR(13) + CHAR(10) END,
        '[', CONVERT(NVARCHAR(19), GETDATE(), 120), '] attempt=', attempt_count,
        ' phase=recovery outcome=',
        CASE WHEN transfer_started_at IS NULL THEN 'Pending' ELSE 'NeedsReview' END
    )
WHERE status = 'Processing' AND updated_at < DATEADD(MINUTE, -5, GETDATE());");
            if (recoveredRows < 0)
            {
                SetReuploadBlocked(ReuploadSchemaCompatibility.OperationFailureMessage);
                return new DataTable();
            }
            RecomputeAllReuploadRequestStatuses();

            DataTable orphanItems;
            string orphanQueryError;
            if (!msSQL.TryExecuteDataTable(@"
SELECT ri.pkid AS item_id
FROM dynamic_reupload_requests r
INNER JOIN dynamic_reupload_request_items ri ON ri.reupload_request_id = r.pkid
LEFT JOIN dynamic_aluminum_bag_information_queues qb
    ON qb.upload_data_queue_id = r.upload_data_queue_id
   AND qb.aluminum_bag_information_id = ri.aluminum_bag_information_id
WHERE r.status IN ('Pending', 'Processing', 'NeedsReview')
  AND ri.status = 'Pending'
  AND qb.pkid IS NULL;",
                    out orphanItems,
                    out orphanQueryError))
            {
                if (SetReuploadBlocked(ReuploadSchemaCompatibility.OperationFailureMessage))
                    Global.WriteLogFile("[LoadReuploadWorkTable/orphans] " + orphanQueryError);
                return new DataTable();
            }
            var orphanRowsUpdated = 0;
            foreach (DataRow orphan in orphanItems.Rows)
            {
                var affectedRows = msSQL.ExecuteNonQueryCount(@"
UPDATE dynamic_reupload_request_items
SET status = 'Failed',
    attempt_count = 3,
    logs = CONCAT(
        COALESCE(logs, ''),
        CASE WHEN COALESCE(logs, '') = '' THEN '' ELSE CHAR(13) + CHAR(10) END,
        '[', CONVERT(NVARCHAR(19), GETDATE(), 120),
        '] phase=snapshot outcome=Failed reason=logical queue/aluminum bag link is missing'
    ),
    updated_at = GETDATE()
WHERE pkid = @item_id;",
                    SqlP("@item_id", Conv.atoi32(orphan["item_id"])));
                if (affectedRows == 1)
                    orphanRowsUpdated++;
                else
                    Global.WriteLogFile($"Re-upload orphan item update affected {affectedRows} rows.");
            }
            if (orphanRowsUpdated > 0)
            {
                RecomputeAllReuploadRequestStatuses();
            }

            DataTable workTable;
            string workQueryError;
            if (!msSQL.TryExecuteDataTable(
                    ReuploadSchemaCompatibility.WorkQuerySql,
                    out workTable,
                    out workQueryError))
            {
                if (SetReuploadBlocked(ReuploadSchemaCompatibility.OperationFailureMessage))
                    Global.WriteLogFile("[LoadReuploadWorkTable/work] " + workQueryError);
                return new DataTable();
            }

            SetReuploadAvailable();
            return workTable;
        }

        private async Task UpdateReuploadDataLocal()
        {
            SQLiteHelper.EnsureSchema();
            LoadReuploadWorkTable();
            if (_reuploadAvailability.IsBlocked)
                return;

            DataTable table;
            string syncQueryError;
            if (!msSQL.TryExecuteDataTable(
                    ReuploadSchemaCompatibility.DisplaySyncQuerySql,
                    out table,
                    out syncQueryError))
            {
                if (SetReuploadBlocked(ReuploadSchemaCompatibility.OperationFailureMessage))
                    Global.WriteLogFile("[UpdateReuploadDataLocal/display-sync] " + syncQueryError);
                return;
            }

            UpsertReuploadDataLocal(table);

            var cachedRequestTable = SQLiteHelper.ExecuteDataTable(
                "SELECT pkid_server FROM dynamic_reupload_requests WHERE pkid_server > 0 ORDER BY pkid_server;");
            var cachedRequestIds = cachedRequestTable.Rows.Cast<DataRow>()
                .Select(row => Conv.atoi32(row["pkid_server"]))
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            const int syncBatchSize = 500;
            for (var offset = 0; offset < cachedRequestIds.Count; offset += syncBatchSize)
            {
                var cachedQuery = ReuploadSchemaCompatibility.BuildCachedDisplaySyncQuery(
                    cachedRequestIds.Skip(offset).Take(syncBatchSize));
                DataTable cachedTable;
                string cachedQueryError;
                if (!msSQL.TryExecuteDataTable(cachedQuery, out cachedTable, out cachedQueryError))
                {
                    if (SetReuploadBlocked(ReuploadSchemaCompatibility.OperationFailureMessage))
                        Global.WriteLogFile("[UpdateReuploadDataLocal/cached-sync] " + cachedQueryError);
                    return;
                }
                UpsertReuploadDataLocal(cachedTable);
            }

            SyncReuploadQueueSummaries();
            SetReuploadAvailable();
            await Task.Delay(50);
        }

        private void SyncReuploadQueueSummaries()
        {
            var queueTable = SQLiteHelper.ExecuteDataTable(@"
SELECT pkid_server
FROM dynamic_upload_data_queues
WHERE pkid_server > 0
ORDER BY pkid_server;");
            var queueIds = queueTable.Rows.Cast<DataRow>()
                .Select(row => Conv.atoi32(row["pkid_server"]))
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            const int syncBatchSize = 500;

            for (var offset = 0; offset < queueIds.Count; offset += syncBatchSize)
            {
                var batchQueueIds = queueIds.Skip(offset).Take(syncBatchSize).ToList();
                var summaryQuery = ReuploadSchemaCompatibility.BuildQueueSummaryQuery(batchQueueIds);
                DataTable summaryTable;
                string summaryQueryError;
                if (!msSQL.TryExecuteDataTable(summaryQuery, out summaryTable, out summaryQueryError))
                {
                    Global.WriteLogFile("[SyncReuploadQueueSummaries] " + summaryQueryError);
                    return;
                }

                // Only replace cache values after SQL Server returns this whole batch.
                // This preserves the last synchronized values during a connection failure.
                SQLiteHelper.ExecuteNonQuery(@"
UPDATE dynamic_upload_data_queues
SET reupload_request_count = 0
WHERE pkid_server IN (" + string.Join(",", batchQueueIds) + ");");

                foreach (DataRow summary in summaryTable.Rows)
                {
                    SQLiteHelper.ExecuteNonQuery(@"
UPDATE dynamic_upload_data_queues
SET reupload_request_count = @reupload_request_count,
    customer_name = @customer_name
WHERE pkid_server = @queue_id;",
                        SQLiteHelper.P("@reupload_request_count", Conv.atoi32(summary["reupload_request_count"])),
                        SQLiteHelper.P("@customer_name", Conv.atos(summary["customer_name"])),
                        SQLiteHelper.P("@queue_id", Conv.atoi32(summary["queue_id"])));
                }
            }
        }

        private void UpsertReuploadDataLocal(DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                var requestId = Conv.atoi32(row["request_id"]);
                var itemId = Conv.atoi32(row["item_id"]);
                var localRequestId = SQLiteHelper.ExecuteScalar(
                    "SELECT pkid FROM dynamic_reupload_requests WHERE pkid_server = @pkid_server;",
                    SQLiteHelper.P("@pkid_server", requestId));

                if (localRequestId == null || localRequestId == DBNull.Value)
                {
                    localRequestId = SQLiteHelper.InsertAndGetId(@"
INSERT INTO dynamic_reupload_requests
(pkid_server, upload_data_queue_id, operator_code, status, requested_bag_count,
 logs, created_at, started_at, completed_at, updated_at)
VALUES (@pkid_server, @queue_id, @operator_code, @status, @requested_bag_count, @logs,
 @created_at, @started_at, @completed_at, @updated_at);",
                        SQLiteHelper.P("@pkid_server", requestId),
                        SQLiteHelper.P("@queue_id", Conv.atoi32(row["queue_id"])),
                        SQLiteHelper.P("@operator_code", Conv.atos(row["operator_code"])),
                        SQLiteHelper.P("@status", Conv.atos(row["request_status"])),
                        SQLiteHelper.P("@requested_bag_count", Conv.atoi32(row["requested_bag_count"])),
                        SQLiteHelper.P("@logs", Conv.atos(row["request_logs"])),
                        SQLiteHelper.P("@created_at", SqliteDateValue(row["request_created_at"])),
                        SQLiteHelper.P("@started_at", SqliteDateValue(row["request_started_at"])),
                        SQLiteHelper.P("@completed_at", SqliteDateValue(row["request_completed_at"])),
                        SQLiteHelper.P("@updated_at", SqliteDateValue(row["request_updated_at"])));
                }
                else
                {
                    SQLiteHelper.ExecuteNonQuery(@"
UPDATE dynamic_reupload_requests
SET upload_data_queue_id = @queue_id,
    operator_code = @operator_code,
    status = @status,
    requested_bag_count = @requested_bag_count,
    logs = @logs,
    created_at = @created_at,
    started_at = @started_at,
    completed_at = @completed_at,
    updated_at = @updated_at
WHERE pkid_server = @pkid_server;",
                        SQLiteHelper.P("@pkid_server", requestId),
                        SQLiteHelper.P("@queue_id", Conv.atoi32(row["queue_id"])),
                        SQLiteHelper.P("@operator_code", Conv.atos(row["operator_code"])),
                        SQLiteHelper.P("@status", Conv.atos(row["request_status"])),
                        SQLiteHelper.P("@requested_bag_count", Conv.atoi32(row["requested_bag_count"])),
                        SQLiteHelper.P("@logs", Conv.atos(row["request_logs"])),
                        SQLiteHelper.P("@created_at", SqliteDateValue(row["request_created_at"])),
                        SQLiteHelper.P("@started_at", SqliteDateValue(row["request_started_at"])),
                        SQLiteHelper.P("@completed_at", SqliteDateValue(row["request_completed_at"])),
                        SQLiteHelper.P("@updated_at", SqliteDateValue(row["request_updated_at"])));
                }

                var localItem = SQLiteHelper.ExecuteScalar(
                    "SELECT pkid FROM dynamic_reupload_request_items WHERE pkid_server = @pkid_server;",
                    SQLiteHelper.P("@pkid_server", itemId));
                var itemSql = localItem == null || localItem == DBNull.Value
                    ? @"
INSERT INTO dynamic_reupload_request_items
(pkid_server, reupload_request_id, aluminum_bag_information_id, aluminum_bag_code,
 source_file_path, local_file_path, file_hash, status, attempt_count,
 is_legacy_recovered, logs, transfer_started_at, review_reason, reviewed_at,
 reviewed_by, number_of_psc_ok, created_at, uploaded_at, updated_at)
VALUES (@pkid_server, @request_id, @bag_id, @bag_code, @source_path, @local_path,
 @file_hash, @status, @attempt_count, @legacy, @logs, @transfer_started_at,
 @review_reason, @reviewed_at, @reviewed_by, @number_of_psc_ok, @created_at, @uploaded_at, @updated_at);"
                    : @"
UPDATE dynamic_reupload_request_items
SET reupload_request_id = (SELECT pkid FROM dynamic_reupload_requests WHERE pkid_server = @request_server_id),
    aluminum_bag_information_id = @bag_id,
    aluminum_bag_code = @bag_code,
    source_file_path = @source_path,
    local_file_path = @local_path,
    file_hash = @file_hash,
    status = @status,
    attempt_count = @attempt_count,
    logs = @logs,
    transfer_started_at = @transfer_started_at,
    review_reason = @review_reason,
    reviewed_at = @reviewed_at,
    reviewed_by = @reviewed_by,
    number_of_psc_ok = @number_of_psc_ok,
    uploaded_at = @uploaded_at,
    updated_at = @updated_at
WHERE pkid_server = @pkid_server;";
                SQLiteHelper.ExecuteNonQuery(itemSql,
                    SQLiteHelper.P("@pkid_server", itemId),
                    SQLiteHelper.P("@request_id", localRequestId ?? 0),
                    SQLiteHelper.P("@request_server_id", requestId),
                    SQLiteHelper.P("@bag_id", Conv.atoi32(row["bag_id"])),
                    SQLiteHelper.P("@bag_code", Conv.atos(row["aluminum_bag_code"])),
                    SQLiteHelper.P("@source_path", Conv.atos(row["source_file_path"])),
                    SQLiteHelper.P("@local_path", Conv.atos(row["local_file_path"])),
                    SQLiteHelper.P("@file_hash", Conv.atos(row["file_hash"])),
                    SQLiteHelper.P("@status", Conv.atos(row["item_status"])),
                    SQLiteHelper.P("@attempt_count", Conv.atoi32(row["attempt_count"])),
                    SQLiteHelper.P("@legacy", Conv.atob(row["is_legacy_recovered"]) ? 1 : 0),
                    SQLiteHelper.P("@logs", Conv.atos(row["item_logs"])),
                    SQLiteHelper.P("@transfer_started_at", SqliteDateValue(row["transfer_started_at"])),
                    SQLiteHelper.P("@review_reason", Conv.atos(row["review_reason"])),
                    SQLiteHelper.P("@reviewed_at", SqliteDateValue(row["reviewed_at"])),
                    SQLiteHelper.P("@reviewed_by", Conv.atos(row["reviewed_by"])),
                    SQLiteHelper.P("@number_of_psc_ok", Conv.atoi32(row["number_of_psc_ok"])),
                    SQLiteHelper.P("@created_at", SqliteDateValue(row["item_created_at"])),
                    SQLiteHelper.P("@uploaded_at", SqliteDateValue(row["item_uploaded_at"])),
                    SQLiteHelper.P("@updated_at", SqliteDateValue(row["item_updated_at"])));
            }
        }

        private string ResolveLocalSnapshotPath(DataRow row, out bool legacyRecovered)
        {
            legacyRecovered = false;
            var queueId = Conv.atoi32(row["queue_id"]);
            var bagId = Conv.atoi32(row["bag_id"]);
            var serverLocalPath = Conv.atos(row["local_file_path"]);
            var serverHash = Conv.atos(row["file_hash"]);
            var localQueue = SQLiteHelper.ExecuteDataTable(
                "SELECT pkid, combine_local_path FROM dynamic_upload_data_queues WHERE pkid_server = @queue_id;",
                SQLiteHelper.P("@queue_id", queueId));

            if (localQueue.Rows.Count > 0)
            {
                var localQueueId = Conv.atoi32(localQueue.Rows[0]["pkid"]);
                var localBag = SQLiteHelper.ExecuteDataTable(@"
SELECT local_file_path, file_hash, is_legacy_recovered
FROM dynamic_aluminum_informations
WHERE upload_data_queue_id = @queue_id
  AND (aluminum_bag_information_id_server = @bag_id OR aluminum_bag_code = @bag_code)
LIMIT 1;",
                    SQLiteHelper.P("@queue_id", localQueueId),
                    SQLiteHelper.P("@bag_id", bagId),
                    SQLiteHelper.P("@bag_code", Conv.atos(row["aluminum_bag_code"])));
                if (localBag.Rows.Count > 0)
                {
                    var path = Conv.atos(localBag.Rows[0]["local_file_path"]);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        row["local_file_path"] = path;
                        row["file_hash"] = Conv.atos(localBag.Rows[0]["file_hash"]);
                        legacyRecovered = Conv.atob(localBag.Rows[0]["is_legacy_recovered"]);
                        return path;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(serverLocalPath))
                return serverLocalPath;

            DateTime queueDate;
            if (!DateTime.TryParse(row["queue_created_at"]?.ToString(), out queueDate))
                queueDate = DateTime.Now;
            var localRoot = System.IO.Path.Combine(
                _appSetting.CombineLogPath,
                queueDate.Year.ToString(),
                Conv.atos(row["item_name"]),
                Conv.atos(row["customer_name"]),
                queueDate.Month.ToString(),
                queueDate.Day.ToString(),
                Conv.atos(row["combine_indication"]));
            return System.IO.Path.Combine(localRoot, $"{Conv.atos(row["aluminum_bag_code"])}.txt");
        }

        private string GetCurrentCombineSourceRoot()
        {
            object value;
            string errorMessage;
            if (!msSQL.TryExecuteScalar(@"
SELECT TOP (1) combine_indication_path
FROM dbo.define_general_settings
ORDER BY pkid;", out value, out errorMessage))
            {
                Global.WriteLogFile("[ReuploadSourcePath] Cannot read Combine Emap Log File Path: " + errorMessage);
                return "";
            }

            return Conv.atos(value).Trim();
        }

        private async Task ProcessReuploadRequests()
        {
            var table = LoadReuploadWorkTable();
            var combineSourceRoot = table.Rows.Count > 0 ? GetCurrentCombineSourceRoot() : "";
            foreach (DataRow row in table.Rows)
            {
                var work = new ReuploadWorkItem
                {
                    RequestId = Conv.atoi32(row["request_id"]),
                    ItemId = Conv.atoi32(row["item_id"]),
                    QueueId = Conv.atoi32(row["queue_id"]),
                    BagId = Conv.atoi32(row["bag_id"]),
                    BagCode = Conv.atos(row["aluminum_bag_code"]),
                    SourcePath = Conv.atos(row["source_file_path"]),
                    FileHash = Conv.atos(row["file_hash"]),
                    AttemptCount = Conv.atoi32(row["attempt_count"]),
                    ExistingLogs = Conv.atos(row["item_logs"]),
                };
                var localPath = ResolveLocalSnapshotPath(row, out var legacyRecovered);
                work.LocalPath = localPath;
                var resolvedHash = Conv.atos(row["file_hash"]);
                if (!string.IsNullOrWhiteSpace(resolvedHash))
                    work.FileHash = resolvedHash;

                if (!TryClaimReuploadItem(work))
                    continue;

                await UpdateUI();

                var sourceResolution = ReuploadSourcePathResolver.Resolve(
                    work.LocalPath,
                    work.SourcePath,
                    combineSourceRoot,
                    Conv.atos(row["combine_indication"]),
                    work.BagCode);
                work.SourcePath = sourceResolution.SourcePath;

                string log;
                var snapshotHash = work.FileHash;
                bool archived = SQLiteHelper.EnsureSnapshot(work.SourcePath, work.LocalPath, ref snapshotHash, out log);
                work.FileHash = snapshotHash;
                if (!archived)
                {
                    var safeFailureStatus = ReuploadDeliveryPolicy.FailureStatus(false, work.AttemptCount);
                    FinishReuploadItem(work, safeFailureStatus, CombineReuploadLogs(sourceResolution.DiagnosticLog, log), legacyRecovered);
                    await UpdateUI();
                    continue;
                }

                var result = new UploadResultView
                {
                    Pkid = work.QueueId,
                    CombineIndication = Conv.atos(row["combine_indication"]),
                    CustomerCode = Conv.atos(row["customer_code"]),
                    IsUploadFolder = row["is_upload_folder"] == DBNull.Value ? (bool?)null : (bool?)Convert.ToBoolean(row["is_upload_folder"]),
                    IsUseProxy = row["is_use_proxy"] == DBNull.Value ? (bool?)null : (bool?)Convert.ToBoolean(row["is_use_proxy"]),
                    IsUseKey = row["is_use_key"] == DBNull.Value ? (bool?)null : (bool?)Convert.ToBoolean(row["is_use_key"]),
                    FolderName = Conv.atos(row["folder_name"]),
                    SftpServer = Conv.atos(row["sftp_server"]),
                    SftpPort = row["sftp_port"] == DBNull.Value ? (int?)null : Conv.atoi32(row["sftp_port"]),
                    SftpUser = Conv.atos(row["sftp_user"]),
                    SftpPassword = Conv.atos(row["sftp_password"]),
                    SftpRemotePath = Conv.atos(row["sftp_remote_path"]),
                    AluminumBags = new List<AluminumBagInfo>
                    {
                        new AluminumBagInfo
                        {
                            AluminumBagInformationId = work.BagId,
                            AluminumBagCode = work.BagCode,
                            FilePath = work.SourcePath,
                            LocalFilePath = work.LocalPath,
                            FileHash = work.FileHash
                        }
                    }
                };

                var logs = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(sourceResolution.DiagnosticLog))
                    logs.AppendLine(sourceResolution.DiagnosticLog);
                bool success;
                bool transferStarted = false;
                Func<bool> markTransferStarted = () =>
                {
                    if (transferStarted)
                        return true;

                    transferStarted = MarkReuploadTransferStarted(work);
                    if (!transferStarted)
                        logs.AppendLine("Transfer was not started because its durable marker could not be saved.");
                    return transferStarted;
                };
                try
                {
                    success = result.CustomerCode?.ToUpper() == "C-02"
                        ? UploadBySSHNet(result, logs, markTransferStarted)
                        : UploadByWinSCP(result, logs, markTransferStarted);
                }
                catch (Exception ex)
                {
                    success = false;
                    logs.AppendLine(ex.Message);
                }

                var status = success
                    ? ReuploadDeliveryPolicy.Uploaded
                    : ReuploadDeliveryPolicy.FailureStatus(transferStarted, work.AttemptCount);
                var finalized = FinishReuploadItem(
                    work,
                    status,
                    logs.ToString(),
                    legacyRecovered,
                    success ? DateTime.Now : (DateTime?)null);
                if (success && !finalized)
                {
                    MarkReuploadNeedsReview(
                        work,
                        "SFTP reported success but the delivery state could not be persisted.",
                        logs.ToString());
                }
                await UpdateUI();
            }
        }

        private static string CombineReuploadLogs(string firstLog, string secondLog)
        {
            if (string.IsNullOrWhiteSpace(firstLog))
                return secondLog ?? "";
            if (string.IsNullOrWhiteSpace(secondLog))
                return firstLog;
            return firstLog.Trim() + Environment.NewLine + secondLog.Trim();
        }

        private bool TryClaimReuploadItem(ReuploadWorkItem work)
        {
            var claimed = msSQL.ExecuteDataTable(@"
UPDATE item WITH (UPDLOCK, ROWLOCK)
SET status = 'Processing',
    attempt_count = attempt_count + 1,
    transfer_started_at = NULL,
    review_reason = NULL,
    reviewed_at = NULL,
    reviewed_by = NULL,
    updated_at = GETDATE(),
    logs = CONCAT(
        COALESCE(logs, ''),
        CASE WHEN COALESCE(logs, '') = '' THEN '' ELSE CHAR(13) + CHAR(10) END,
        '[', CONVERT(NVARCHAR(19), GETDATE(), 120), '] attempt=', attempt_count + 1,
        ' phase=claim outcome=Claimed'
    )
OUTPUT INSERTED.attempt_count, INSERTED.logs
FROM dynamic_reupload_request_items item
WHERE item.pkid = @item_id
  AND item.status = 'Pending'
  AND item.attempt_count < 3;",
                SqlP("@item_id", work.ItemId));
            if (claimed.Rows.Count != 1)
                return false;

            work.AttemptCount = Conv.atoi32(claimed.Rows[0]["attempt_count"]);
            work.ExistingLogs = Conv.atos(claimed.Rows[0]["logs"]);
            var parentRows = msSQL.ExecuteNonQueryCount(@"
UPDATE dynamic_reupload_requests
SET started_at = COALESCE(started_at, GETDATE()), updated_at = GETDATE()
WHERE pkid = @request_id;",
                SqlP("@request_id", work.RequestId));
            if (parentRows != 1)
                Global.WriteLogFile($"Re-upload claim parent update affected {parentRows} rows for request {work.RequestId}.");
            RecomputeAllReuploadRequestStatuses();

            SQLiteHelper.ExecuteNonQuery(@"
UPDATE dynamic_reupload_request_items
SET status = 'Processing', attempt_count = @attempt_count,
    transfer_started_at = NULL, review_reason = NULL,
    reviewed_at = NULL, reviewed_by = NULL, logs = @logs,
    updated_at = @updated_at
WHERE pkid_server = @pkid_server;",
                SQLiteHelper.P("@attempt_count", work.AttemptCount),
                SQLiteHelper.P("@logs", work.ExistingLogs),
                SQLiteHelper.P("@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                SQLiteHelper.P("@pkid_server", work.ItemId));
            return true;
        }

        private bool MarkReuploadTransferStarted(ReuploadWorkItem work)
        {
            var marked = msSQL.ExecuteDataTable(@"
UPDATE item
SET transfer_started_at = GETDATE(),
    updated_at = GETDATE(),
    logs = CONCAT(
        COALESCE(logs, ''),
        CASE WHEN COALESCE(logs, '') = '' THEN '' ELSE CHAR(13) + CHAR(10) END,
        '[', CONVERT(NVARCHAR(19), GETDATE(), 120), '] attempt=', attempt_count,
        ' phase=transfer outcome=Started'
    )
OUTPUT INSERTED.transfer_started_at, INSERTED.logs
FROM dynamic_reupload_request_items item
WHERE item.pkid = @item_id AND item.status = 'Processing' AND item.transfer_started_at IS NULL;",
                SqlP("@item_id", work.ItemId));
            if (marked.Rows.Count != 1)
                return false;

            work.ExistingLogs = Conv.atos(marked.Rows[0]["logs"]);
            SQLiteHelper.ExecuteNonQuery(@"
UPDATE dynamic_reupload_request_items
SET transfer_started_at = @transfer_started_at, logs = @logs, updated_at = @updated_at
WHERE pkid_server = @pkid_server;",
                SQLiteHelper.P("@transfer_started_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                SQLiteHelper.P("@logs", work.ExistingLogs),
                SQLiteHelper.P("@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                SQLiteHelper.P("@pkid_server", work.ItemId));
            return true;
        }

        private bool FinishReuploadItem(
            ReuploadWorkItem work,
            string status,
            string logs,
            bool legacyRecovered,
            DateTime? uploadedAt = null)
        {
            var audit = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] attempt={work.AttemptCount} phase=finalize outcome={status}";
            if (!string.IsNullOrWhiteSpace(logs))
                audit += Environment.NewLine + logs.Trim();
            var reviewReason = status == ReuploadDeliveryPolicy.NeedsReview
                ? "Transfer started but the receiver outcome is unknown."
                : null;
            var affectedRows = msSQL.ExecuteNonQueryCount(@"
UPDATE dynamic_reupload_request_items
SET status = @status,
    local_file_path = @local_path,
    file_hash = @file_hash,
    is_legacy_recovered = @legacy,
    logs = CONCAT(
        COALESCE(logs, ''),
        CASE WHEN COALESCE(logs, '') = '' THEN '' ELSE CHAR(13) + CHAR(10) END,
        @audit
    ),
    review_reason = @review_reason,
    uploaded_at = @uploaded_at,
    updated_at = GETDATE()
WHERE pkid = @item_id AND status = 'Processing';",
                SqlP("@status", status),
                SqlP("@local_path", work.LocalPath),
                SqlP("@file_hash", work.FileHash),
                SqlP("@legacy", legacyRecovered ? 1 : 0),
                SqlP("@audit", audit),
                SqlP("@review_reason", (object)reviewReason ?? DBNull.Value),
                SqlP("@uploaded_at", uploadedAt.HasValue ? (object)uploadedAt.Value : DBNull.Value),
                SqlP("@item_id", work.ItemId));

            if (affectedRows == 1)
                RecomputeAllReuploadRequestStatuses();

            SQLiteHelper.ExecuteNonQuery(@"
UPDATE dynamic_reupload_request_items
SET status = @status, attempt_count = @attempt_count, local_file_path = @local_path,
    file_hash = @file_hash, is_legacy_recovered = @legacy,
    logs = CASE WHEN COALESCE(logs, '') = '' THEN @audit ELSE logs || CHAR(13) || CHAR(10) || @audit END,
    review_reason = @review_reason,
    uploaded_at = @uploaded_at, updated_at = @updated_at
WHERE pkid_server = @pkid_server;",
                SQLiteHelper.P("@status", status),
                SQLiteHelper.P("@attempt_count", work.AttemptCount),
                SQLiteHelper.P("@local_path", work.LocalPath),
                SQLiteHelper.P("@file_hash", work.FileHash),
                SQLiteHelper.P("@legacy", legacyRecovered ? 1 : 0),
                SQLiteHelper.P("@audit", audit),
                SQLiteHelper.P("@review_reason", (object)reviewReason ?? DBNull.Value),
                SQLiteHelper.P("@uploaded_at", uploadedAt.HasValue ? (object)uploadedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value),
                SQLiteHelper.P("@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                SQLiteHelper.P("@pkid_server", work.ItemId));
            SQLiteHelper.ExecuteNonQuery(@"
UPDATE dynamic_reupload_requests
SET status = CASE
    WHEN EXISTS (
        SELECT 1 FROM dynamic_reupload_request_items i
        WHERE i.reupload_request_id = dynamic_reupload_requests.pkid AND i.status = 'NeedsReview'
    ) THEN 'NeedsReview'
    WHEN EXISTS (
        SELECT 1 FROM dynamic_reupload_request_items i
        WHERE i.reupload_request_id = dynamic_reupload_requests.pkid AND i.status = 'Processing'
    ) THEN 'Processing'
    WHEN EXISTS (
        SELECT 1 FROM dynamic_reupload_request_items i
        WHERE i.reupload_request_id = dynamic_reupload_requests.pkid AND i.status = 'Pending'
    ) THEN 'Pending'
    WHEN EXISTS (
        SELECT 1 FROM dynamic_reupload_request_items i
        WHERE i.reupload_request_id = dynamic_reupload_requests.pkid AND i.status = 'Failed'
    ) THEN 'Failed'
    ELSE 'Uploaded'
END,
updated_at = @updated_at
WHERE pkid_server = @request_id;",
                SQLiteHelper.P("@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                SQLiteHelper.P("@request_id", work.RequestId));
            return affectedRows == 1;
        }

        private void MarkReuploadNeedsReview(ReuploadWorkItem work, string reason, string transferLogs)
        {
            var audit = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] attempt={work.AttemptCount} phase=finalize outcome=NeedsReview";
            if (!string.IsNullOrWhiteSpace(transferLogs))
                audit += Environment.NewLine + transferLogs.Trim();
            var affectedRows = msSQL.ExecuteNonQueryCount(@"
UPDATE dynamic_reupload_request_items
SET status = 'NeedsReview',
    review_reason = @review_reason,
    logs = CONCAT(
        COALESCE(logs, ''),
        CASE WHEN COALESCE(logs, '') = '' THEN '' ELSE CHAR(13) + CHAR(10) END,
        @audit
    ),
    updated_at = GETDATE()
WHERE pkid = @item_id AND status = 'Processing';",
                SqlP("@review_reason", reason),
                SqlP("@audit", audit),
                SqlP("@item_id", work.ItemId));
            if (affectedRows != 1)
                Global.WriteLogFile($"Re-upload NeedsReview update affected {affectedRows} rows for item {work.ItemId}.");
            RecomputeAllReuploadRequestStatuses();
            SQLiteHelper.ExecuteNonQuery(@"
UPDATE dynamic_reupload_request_items
SET status = 'NeedsReview', review_reason = @review_reason,
    logs = CASE WHEN COALESCE(logs, '') = '' THEN @audit ELSE logs || CHAR(13) || CHAR(10) || @audit END,
    updated_at = @updated_at
WHERE pkid_server = @pkid_server;",
                SQLiteHelper.P("@review_reason", reason),
                SQLiteHelper.P("@audit", audit),
                SQLiteHelper.P("@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                SQLiteHelper.P("@pkid_server", work.ItemId));
        }
        private async Task UpdateUI()
        {
            var queueTable = SQLiteHelper.ExecuteDataTable(
                "SELECT * FROM dynamic_upload_data_queues ORDER BY updated_at DESC LIMIT 100000");
            var loadedResults = new List<UploadResultView>();

            foreach (DataRow row in queueTable.Rows)
            {
                int pkid = Conv.atoi32(row["pkid_server"]);
                int pkidLocal = Conv.atoi32(row["pkid"]);
                bool isUploaded = Conv.atob(row["is_uploaded"]);
                string judgement = Conv.atos(row["judgement"]);


                var result = new UploadResultView
                {
                    Pkid = pkid,
                    PkidLocal = pkidLocal,
                    RecordKind = UploadRecordKinds.Normal,
                    CombineIndication = Conv.atos(row["combine_indication"]),
                    UploadQuantity = Conv.atoi32(row["ship_quantity"]),
                    ReuploadRequestCount = Conv.atoi32(row["reupload_request_count"]),
                    CustomerCode = Conv.atos(row["customer_code"]),
                    CustomerName = Conv.atos(row["customer_name"]),
                    IsUploadFolder = Conv.atob(row["is_upload_folder"]),
                    IsUseProxy = Conv.atob(row["is_use_proxy"]),
                    IsUseKey = Conv.atob(row["is_use_key"]),
                    IsReupload = Conv.atob(row["is_reupload"]),
                    IsUploaded = isUploaded,
                    FolderName = Conv.atos(row["folder_name"]),
                    CombineServerPath = Conv.atos(row["combine_server_path"]),
                    CombineLocalPath = Conv.atos(row["combine_local_path"]),

                    SftpServer = Conv.atos(row["sftp_server"]),
                    SftpPort = Conv.atoi32(row["sftp_port"]),
                    SftpUser = Conv.atos(row["sftp_user"]),
                    SftpPassword = Conv.atos(row["sftp_password"]),
                    SftpRemotePath = Conv.atos(row["sftp_remote_path"]),

                    Log = Conv.atos(row["logs"]),
                    Judgement = judgement,
                    Status = UploadStatusNames.FromPersistence(isUploaded, judgement),
                    UploadedAt = Conv.atos(row["uploaded_at"]),
                };

                var aluminumTable = SQLiteHelper.ExecuteDataTable(
                    $"SELECT * FROM dynamic_aluminum_informations WHERE upload_data_queue_id = {pkidLocal}");

                foreach (DataRow bag in aluminumTable.Rows)
                {
                    var sourcePath = Conv.atos(bag["source_file_path"]);
                    if (string.IsNullOrWhiteSpace(sourcePath))
                        sourcePath = Conv.atos(bag["file_path"]);
                    result.AluminumBags.Add(new AluminumBagInfo
                    {
                        AluminumBagInformationId = Conv.atoi32(bag["aluminum_bag_information_id_server"]),
                        AluminumBagCode = Conv.atos(bag["aluminum_bag_code"]),
                        FilePath = sourcePath,
                        LocalFilePath = Conv.atos(bag["local_file_path"]),
                        FileHash = Conv.atos(bag["file_hash"]),
                    });
                }

                // Add vào UI
                loadedResults.Add(result);
            }

            AppendReuploadDisplayResults(loadedResults);

            await Application.Current.Dispatcher.InvokeAsync(() => ReplaceUploadResults(loadedResults));

            await Task.Delay(200);
        }

        private void AppendReuploadDisplayResults(ICollection<UploadResultView> loadedResults)
        {
            var queueResults = loadedResults
                .Where(result => string.Equals(result.RecordKind, UploadRecordKinds.Normal, StringComparison.Ordinal))
                .GroupBy(result => result.Pkid)
                .ToDictionary(group => group.Key, group => group.First());
            var displayTable = SQLiteHelper.ExecuteDataTable(@"
SELECT
    request.pkid_server AS request_id,
    request.upload_data_queue_id AS queue_id,
    request.status AS request_status,
    request.logs AS request_logs,
    request.created_at AS request_created_at,
    request.completed_at AS request_completed_at,
    queue.combine_indication,
    queue.customer_code,
    item.aluminum_bag_code,
    item.number_of_psc_ok,
    item.status AS item_status,
    item.logs AS item_logs,
    item.review_reason,
    item.uploaded_at AS item_uploaded_at
FROM dynamic_reupload_requests request
LEFT JOIN dynamic_reupload_request_items item
    ON item.reupload_request_id = request.pkid
LEFT JOIN dynamic_upload_data_queues queue
    ON queue.pkid_server = request.upload_data_queue_id
ORDER BY request.created_at DESC, request.pkid_server DESC, item.pkid;");

            var source = displayTable.Rows.Cast<DataRow>().Select(row => new ReuploadDisplaySource
            {
                RequestId = Conv.atoi32(row["request_id"]),
                QueueId = Conv.atoi32(row["queue_id"]),
                RequestStatus = Conv.atos(row["request_status"]),
                RequestLogs = Conv.atos(row["request_logs"]),
                RequestCreatedAt = ReadNullableDate(row["request_created_at"]),
                RequestCompletedAt = ReadNullableDate(row["request_completed_at"]),
                CombineIndication = Conv.atos(row["combine_indication"]),
                CustomerCode = Conv.atos(row["customer_code"]),
                BagCode = Conv.atos(row["aluminum_bag_code"]),
                NumberOfPscOk = Conv.atoi32(row["number_of_psc_ok"]),
                ItemStatus = Conv.atos(row["item_status"]),
                ItemLogs = Conv.atos(row["item_logs"]),
                ReviewReason = Conv.atos(row["review_reason"]),
                ItemUploadedAt = ReadNullableDate(row["item_uploaded_at"]),
            });

            foreach (var summary in ReuploadDisplayMapper.Build(source))
            {
                UploadResultView queueResult;
                queueResults.TryGetValue(summary.QueueId, out queueResult);
                loadedResults.Add(new UploadResultView
                {
                    Pkid = summary.QueueId,
                    RecordKind = UploadRecordKinds.Reupload,
                    ReuploadRequestId = summary.RequestId,
                    IsReupload = true,
                    IsUploaded = summary.Status == UploadStatusNames.Success,
                    CombineIndication = summary.CombineIndication,
                    CustomerCode = summary.CustomerCode,
                    CustomerName = queueResult == null ? string.Empty : queueResult.CustomerName,
                    UploadQuantity = summary.UploadQuantity,
                    ReuploadRequestCount = queueResult == null ? 0 : queueResult.ReuploadRequestCount,
                    Log = summary.Logs,
                    Judgement = string.Empty,
                    Status = summary.Status,
                    UploadedAt = summary.UploadedAt.HasValue
                        ? summary.UploadedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                        : string.Empty,
                });
            }
        }

        private static DateTime? ReadNullableDate(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            DateTime parsed;
            return DateTime.TryParse(value.ToString(), out parsed) ? parsed : (DateTime?)null;
        }

        private static object SqliteDateValue(object value)
        {
            var parsed = ReadNullableDate(value);
            return parsed.HasValue
                ? (object)parsed.Value.ToString("yyyy-MM-dd HH:mm:ss")
                : DBNull.Value;
        }
        private async Task UpdateDataServer()
        {
            if (msSQL.TestConnection())
            {
                var tableServer = msSQL.ExecuteDataTable(@"
                    SELECT 
                        pkid
                    FROM dynamic_upload_data_queues
                    WHERE is_active = 1 OR is_reupload = 1");

                if (tableServer.Rows.Count > 0)
                {
                    var allPkids = new List<string>();
                    for (var i = 0; i < tableServer.Rows.Count; i++)
                    {
                        allPkids.Add(Conv.atos(tableServer.Rows[i]["pkid"]));
                    }

                    int batchSize = 500;
                    for (int k = 0; k < allPkids.Count; k += batchSize)
                    {
                        var batchPkids = allPkids.Skip(k).Take(batchSize).ToList();
                        var pkidList = string.Join(",", batchPkids);

                        var tableLocal = SQLiteHelper.ExecuteDataTable(
                            $"SELECT * FROM dynamic_upload_data_queues WHERE pkid_server IN ({pkidList})");

                        if (tableLocal.Rows.Count > 0)
                        {
                            var sqlBuilder = new StringBuilder();
                            for (var i = 0; i < tableLocal.Rows.Count; i++)
                            {
                                var dataActive = !Conv.atob(tableLocal.Rows[i]["is_uploaded"]) ? 1 : 0;
                                string safeLog = EscapeSQLiteString(Conv.atos(tableLocal.Rows[i]["logs"]));
                                string safeJudgement = EscapeSQLiteString(Conv.atos(tableLocal.Rows[i]["judgement"]));

                                sqlBuilder.Append("UPDATE dynamic_upload_data_queues ")
                                          .Append($"SET logs = '{safeLog}', ")
                                          .Append($"judgement = '{safeJudgement}', ")
                                          .Append($"is_active = {dataActive}, ")
                                          .Append("is_reupload = 0, ")
                                          .Append($"updated_at = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' ")
                                          .Append($"WHERE pkid = {Conv.atoi32(tableLocal.Rows[i]["pkid_server"])}; \n");
                            }

                            string sql = sqlBuilder.ToString();
                            if (!string.IsNullOrEmpty(sql))
                            {
                                msSQL.ExecuteNonQuery(sql);
                            }
                        }
                    }
                }
            }
            await Task.Delay(100);
        }
        private List<UploadGroup> ConvertToGroupedList(DataTable table)
        {
            var list = new List<UploadGroup>();

            try
            {
                if (table == null || table.Rows.Count == 0)
                {
                    Global.WriteLog("[ConvertToGroupedList] Table is null or empty");
                    return list;
                }

                Global.WriteLog($"[ConvertToGroupedList] Start processing rows: {table.Rows.Count}");

                var groups = table.AsEnumerable()
                    .GroupBy(r => r["pkid"]);

                foreach (var g in groups)
                {
                    try
                    {
                        int pkid = Conv.atoi32(g.Key ?? 0);

                        Global.WriteLog($"[ConvertToGroupedList] Processing Group PKID: {pkid}");

                        // CHECK INVALID BAG CODE
                        var invalidRow = g.FirstOrDefault(r =>
                        {
                            var code = r["aluminum_bag_code"]?.ToString()?.Trim();
                            return string.IsNullOrWhiteSpace(code);
                        });

                        if (invalidRow != null)
                        {
                            Global.WriteLog(
                                $"[ConvertToGroupedList][ERROR] Invalid aluminum_bag_code | " +
                                $"PKID: {pkid} | " +
                                $"Combine: {invalidRow["combine_indication"]} | " +
                                $"FilePath: {invalidRow["file_path"]}");

                            continue;
                        }

                        var first = g.FirstOrDefault();

                        string combineIndication = first?["combine_indication"]?.ToString() ?? "";
                        string customerName = first?["customer_name"]?.ToString() ?? "";
                        string itemName = first?["item_name"]?.ToString() ?? "";
                        DateTime queueDate;
                        if (!DateTime.TryParse(first?["queue_created_at"]?.ToString(), out queueDate))
                            queueDate = DateTime.Now;

                        var uploadGroup = new UploadGroup
                        {
                            Pkid = pkid,

                            CombineIndication = combineIndication,

                            CustomerCode = first?["customer_code"]?.ToString() ?? "",

                            CustomerName = customerName,

                            IsUploadFolder = first?["is_upload_folder"] == DBNull.Value
                                ? null
                                : (bool?)Convert.ToBoolean(first["is_upload_folder"]),

                            IsUseKey = first?["is_use_key"] == DBNull.Value
                                ? null
                                : (bool?)Convert.ToBoolean(first["is_use_key"]),

                            IsUseProxy = first?["is_use_proxy"] == DBNull.Value
                                ? null
                                : (bool?)Convert.ToBoolean(first["is_use_proxy"]),

                            IsReupload = first?["is_reupload"] == DBNull.Value
                                ? null
                                : (bool?)Convert.ToBoolean(first["is_reupload"]),

                            FolderName = first?["folder_name"]?.ToString() ?? "",

                            ItemName = itemName,

                            SftpServer = first?["sftp_server"]?.ToString() ?? "",

                            SftpPort = first?["sftp_port"] == DBNull.Value
                                ? null
                                : (int?)Convert.ToInt32(first["sftp_port"]),

                            SftpUser = first?["sftp_user"]?.ToString() ?? "",

                            SftpPassword = first?["sftp_password"]?.ToString() ?? "",

                            SftpRemotePath = first?["sftp_remote_path"]?.ToString() ?? "",

                            CombineServerPath = System.IO.Path.Combine(
                                _appSetting.CombineLogPathServer,
                                combineIndication),

                            QuantityUpload = g.Sum(r =>
                                r["number_of_psc_ok"] == DBNull.Value
                                    ? 0
                                    : Conv.atoi32(r["number_of_psc_ok"])),

                            CombineLocalPath = System.IO.Path.Combine(
                                _appSetting.CombineLogPath,
                                queueDate.Year.ToString(),
                                itemName,
                                customerName,
                                queueDate.Month.ToString(),
                                queueDate.Day.ToString(),
                                combineIndication),

                            AluminumBags = g.Select(r =>
                            {
                                string bagCode = r["aluminum_bag_code"]?.ToString()?.Trim() ?? "";

                                string filePath = r["file_path"]?.ToString()?.Trim() ?? "";
                                if (string.IsNullOrWhiteSpace(filePath))
                                {
                                    filePath = System.IO.Path.Combine(
                                        _appSetting.CombineLogPathServer,
                                        combineIndication,
                                        $"{bagCode}.txt");
                                }

                                string localPath = System.IO.Path.Combine(
                                    System.IO.Path.Combine(
                                        _appSetting.CombineLogPath,
                                        queueDate.Year.ToString(),
                                        itemName,
                                        customerName,
                                        queueDate.Month.ToString(),
                                        queueDate.Day.ToString(),
                                        combineIndication),
                                    $"{bagCode}.txt");

                                Global.WriteLog(
                                    $"[ConvertToGroupedList] Add File | " +
                                    $"PKID: {pkid} | " +
                                    $"Bag: {bagCode} | " +
                                    $"Path: {filePath}");

                                return new AluminumBagInfo
                                {
                                    AluminumBagInformationId = Conv.atoi32(r["aluminum_bag_information_id"]),
                                    AluminumBagCode = bagCode,
                                    FilePath = filePath,
                                    LocalFilePath = localPath
                                };
                            }).ToList()
                        };

                        list.Add(uploadGroup);

                        Global.WriteLog(
                            $"[ConvertToGroupedList] Success Group | " +
                            $"PKID: {pkid} | " +
                            $"Files: {uploadGroup.AluminumBags.Count}");
                    }
                    catch (Exception exGroup)
                    {
                        Global.WriteLog(
                            $"[ConvertToGroupedList][GROUP ERROR] {exGroup}");
                    }
                }

                Global.WriteLog(
                    $"[ConvertToGroupedList] Finish | Total Valid Groups: {list.Count}");

                return list;
            }
            catch (Exception ex)
            {
                Global.WriteLog($"[ConvertToGroupedList][FATAL ERROR] {ex}");
                return list;
            }
        }
        public static bool CopyFilesTopDirectory(string sourceDir, string destDir, bool overwrite = true)
        {
            try
            {
                if (!Directory.Exists(sourceDir))
                    Global.WriteLogFile($"Source directory does not exist: {sourceDir}"); 

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                var files = Directory.GetFiles(sourceDir);

                foreach (var filePath in files)
                {
                    string fileName = System.IO.Path.GetFileName(filePath);
                    string destPath = System.IO.Path.Combine(destDir, fileName);
                    File.Copy(filePath, destPath, overwrite);
                }
                return true;
            }
            catch(Exception ex)
            {
                Global.WriteLogFile($"Error copy file: {ex.ToString()}");
                return false;
                
            }
            
        }
        string EscapeSQLiteString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("'", "''"); 
        }
        private static string GetSnapshotPath(AluminumBagInfo file)
        {
            return (file?.LocalFilePath ?? "").Trim();
        }
        private bool UploadByWinSCP(
            UploadResultView result,
            StringBuilder logBuilder,
            Func<bool> beforeTransfer = null)
        {
            foreach (var file in result.AluminumBags)
            {
                var localPath = GetSnapshotPath(file);
                if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
                {
                    logBuilder.AppendLine($"Local snapshot not found: {file.AluminumBagCode}.txt");
                    return false;
                }
            }

            SessionOptions option = CreateSessionOption(result, result.IsUseProxy ?? false);
            option.SshHostKeyPolicy = SshHostKeyPolicy.GiveUpSecurityAndAcceptAny;

            using (var session = new WinSCP.Session())
            {
                session.Open(option);

                string baseRemotePath = result.SftpRemotePath?.TrimEnd('/') ?? "/";

                if (!string.IsNullOrEmpty(baseRemotePath) && baseRemotePath != "/")
                {
                    if (!session.FileExists(baseRemotePath))
                        session.CreateDirectory(baseRemotePath);
                }

                string targetRemotePath = baseRemotePath;

                if (result.IsUploadFolder == true && !string.IsNullOrEmpty(result.FolderName))
                {
                    targetRemotePath = $"{baseRemotePath}/{result.FolderName}";

                    if (!session.FileExists(targetRemotePath))
                        session.CreateDirectory(targetRemotePath);
                }

                foreach (var file in result.AluminumBags)
                {
                    string localPath = GetSnapshotPath(file);
                    string remotePath = $"{targetRemotePath}/{System.IO.Path.GetFileName(localPath)}";
                    if (beforeTransfer != null && !beforeTransfer())
                        return false;
                    session.PutFiles(localPath, remotePath).Check();

                    logBuilder.AppendLine($"Uploaded: {file.AluminumBagCode}.txt,");
                }

                session.Close();
            }

            return true;
        }
        private bool UploadBySSHNet(
            UploadResultView result,
            StringBuilder logBuilder,
            Func<bool> beforeTransfer = null)
        {
            try
            {
                foreach (var file in result.AluminumBags)
                {
                    var localPath = GetSnapshotPath(file);
                    if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
                    {
                        logBuilder.AppendLine($"Local snapshot not found: {file.AluminumBagCode}.txt");
                        return false;
                    }
                }

                AuthenticationMethod auth;

                if (result.IsUseKey == true)
                {
                    // Dùng KEY
                    var keyFile = new PrivateKeyFile(_appSetting.PrimaryKeyFilePath);
                    auth = new PrivateKeyAuthenticationMethod(result.SftpUser, keyFile);
                }
                else
                {
                    // Dùng PASSWORD
                    auth = new PasswordAuthenticationMethod(result.SftpUser, result.SftpPassword);
                }

                var connectionInfo = new ConnectionInfo(
                    result.SftpServer,
                    result.SftpPort ?? 22,
                    result.SftpUser,
                    auth
                );

                using (var client = new SftpClient(connectionInfo))
                {
                    client.Connect();

                    string baseRemote = result.SftpRemotePath?.TrimEnd('/') ?? "/";

                    if (baseRemote != "" && !client.Exists(baseRemote))
                        client.CreateDirectory(baseRemote);

                    string targetRemote = baseRemote;

                    if (result.IsUploadFolder == true && !string.IsNullOrEmpty(result.FolderName))
                    {
                        targetRemote = $"{baseRemote}/{result.FolderName}";
                        if (!client.Exists(targetRemote))
                            client.CreateDirectory(targetRemote);
                    }

                    foreach (var file in result.AluminumBags)
                    {
                        try
                        {
                            var localPath = GetSnapshotPath(file);
                            using (var fs = File.OpenRead(localPath))
                            {
                                string remoteFileName = System.IO.Path.GetFileName(localPath);
                                if (beforeTransfer != null && !beforeTransfer())
                                    return false;
                                client.UploadFile(fs, $"{targetRemote}/{remoteFileName}");
                            }
                            logBuilder.AppendLine($"Uploaded: {file.AluminumBagCode}.txt,");
                        }
                        catch (Exception ex)
                        {
                            logBuilder.AppendLine($"Error uploading {file.AluminumBagCode}: {ex.Message}");
                            return false;
                        }
                    }

                    client.Disconnect();
                }

                return true;
            }
            catch (Exception ex)
            {
                logBuilder.AppendLine($"SSH.NET upload failed: {ex.Message}");
                return false;
            }
        }


        private async Task UploadToSFTP()
        {
            var pendingTable = SQLiteHelper.ExecuteDataTable(
                "SELECT * FROM dynamic_upload_data_queues WHERE is_uploaded = 0 AND is_download = 1");

            if (pendingTable == null || pendingTable.Rows.Count == 0)
                return;

            var pendingList = new List<UploadResultView>();
            foreach (DataRow row in pendingTable.Rows)
            {
                int pkid = Conv.atoi32(row["pkid_server"]);
                int pkidLocal = Conv.atoi32(row["pkid"]);
                bool isUploaded = Conv.atob(row["is_uploaded"]);
                string judgement = Conv.atos(row["judgement"]);

                var result = new UploadResultView
                {
                    Pkid = pkid,
                    PkidLocal = pkidLocal,
                    CombineIndication = Conv.atos(row["combine_indication"]),
                    UploadQuantity = Conv.atoi32(row["ship_quantity"]),
                    CustomerCode = Conv.atos(row["customer_code"]),
                    IsUploadFolder = Conv.atob(row["is_upload_folder"]),
                    IsUseProxy = Conv.atob(row["is_use_proxy"]),
                    IsUseKey = Conv.atob(row["is_use_key"]),
                    IsReupload = Conv.atob(row["is_reupload"]),
                    IsUploaded = isUploaded,
                    FolderName = Conv.atos(row["folder_name"]),
                    CombineServerPath = Conv.atos(row["combine_server_path"]),
                    CombineLocalPath = Conv.atos(row["combine_local_path"]),

                    SftpServer = Conv.atos(row["sftp_server"]),
                    SftpPort = Conv.atoi32(row["sftp_port"]),
                    SftpUser = Conv.atos(row["sftp_user"]),
                    SftpPassword = Conv.atos(row["sftp_password"]),
                    SftpRemotePath = Conv.atos(row["sftp_remote_path"]),

                    Log = Conv.atos(row["logs"]),
                    Judgement = judgement,
                    Status = UploadStatusNames.FromPersistence(isUploaded, judgement),
                    UploadedAt = Conv.atos(row["uploaded_at"]),
                };

                var aluminumTable = SQLiteHelper.ExecuteDataTable(
                    $"SELECT * FROM dynamic_aluminum_informations WHERE upload_data_queue_id = {pkidLocal}");

                foreach (DataRow bag in aluminumTable.Rows)
                {
                    var sourcePath = Conv.atos(bag["source_file_path"]);
                    if (string.IsNullOrWhiteSpace(sourcePath))
                        sourcePath = Conv.atos(bag["file_path"]);
                    result.AluminumBags.Add(new AluminumBagInfo
                    {
                        AluminumBagInformationId = Conv.atoi32(bag["aluminum_bag_information_id_server"]),
                        AluminumBagCode = Conv.atos(bag["aluminum_bag_code"]),
                        FilePath = sourcePath,
                        LocalFilePath = Conv.atos(bag["local_file_path"]),
                        FileHash = Conv.atos(bag["file_hash"]),
                    });
                }
                pendingList.Add(result);
            }

            string sql = "";

            foreach (var result in pendingList)
            {
                if (result == null)
                    continue;

                // Đồng bộ trực quan trạng thái lên danh sách UI _uploadResults thời gian thực
                UploadResultView uiResult = null;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    uiResult = _uploadResults.FirstOrDefault(x =>
                        x.Pkid == result.Pkid &&
                        string.Equals(x.RecordKind, UploadRecordKinds.Normal, StringComparison.Ordinal));
                    if (uiResult != null)
                    {
                        uiResult.Status = UploadStatusNames.Processing;
                        uiResult.Log = "";
                        uiResult.Judgement = "";
                        uiResult.UploadedAt = "";
                    }
                });

                result.Status = UploadStatusNames.Processing;
                result.Log = "";
                result.Judgement = "";
                result.UploadedAt = "";

                bool isSuccess = false;
                StringBuilder logBuilder = new StringBuilder();

                try
                {
                    // =============================
                    // CHỌN THƯ VIỆN UPLOAD THEO CUSTOMER
                    // =============================
                    if (result.CustomerCode?.ToUpper() == "C-02")
                    {
                        // Upload bằng SSH.NET
                        isSuccess = UploadBySSHNet(result, logBuilder);
                    }
                    else
                    {
                        // Upload bằng WinSCP
                        isSuccess = UploadByWinSCP(result, logBuilder);
                    }

                    result.Status = isSuccess ? UploadStatusNames.Success : UploadStatusNames.Failed;
                    result.Judgement = isSuccess ? "PASS" : "FAIL";
                    result.Log = logBuilder.ToString();
                    result.UploadedAt = isSuccess
                        ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        : string.Empty;
                }
                catch (Exception ex)
                {
                    Global.WriteLog($"Upload failed: {ex.Message}");

                    result.Status = UploadStatusNames.Failed;
                    result.Judgement = "FAIL";
                    result.Log = $"Upload failed: {ex.Message}";
                    isSuccess = false;
                }

                if (uiResult != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        uiResult.Status = result.Status;
                        uiResult.Judgement = result.Judgement;
                        uiResult.Log = result.Log;
                        uiResult.UploadedAt = result.UploadedAt;
                        uiResult.IsUploaded = isSuccess;
                    });
                }

                // =============================
                // Cập nhật DB
                // =============================
                try
                {
                    if (isSuccess)
                    {
                        _numberOfOK++;
                        string safeLog = EscapeSQLiteString(result.Log);
                        string safeJudgement = EscapeSQLiteString(result.Judgement);

                        sql += $@"
                    UPDATE dynamic_upload_data_queues 
                    SET uploaded_at = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', 
                        is_uploaded = 1, 
                        is_reupload = 0, 
                        logs = '{safeLog}', 
                        judgement = '{safeJudgement}' 
                    WHERE pkid_server = {result.Pkid};
                ";
                    }
                    else
                    {
                        _numberOfNG++;
                        string safeLog = EscapeSQLiteString(result.Log);
                        string safeJudgement = EscapeSQLiteString(result.Judgement);

                        sql += $@"
                    UPDATE dynamic_upload_data_queues
                    SET is_uploaded = 0,
                        logs = '{safeLog}',
                        judgement = '{safeJudgement}'
                    WHERE pkid_server = {result.Pkid};
                ";
                    }

                    _totalRecords = _numberOfOK + _numberOfNG;
                }
                catch (Exception ex)
                {
                    result.Log += $"\nDB update error: {ex.Message}";
                }

                await Task.Delay(200);
            }

            if (!string.IsNullOrEmpty(sql))
                SQLiteHelper.ExecuteNonQuery(sql);

            await Task.Delay(200);
        }


        private SessionOptions CreateSessionOption(UploadResultView group, bool isUseProxy)
        {
            var opt = new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = group.SftpServer,
                UserName = group.SftpUser,
                PortNumber = group.SftpPort ?? 22
            };
            Global.WriteLog($"Server {group.SftpServer} - User : {group.SftpUser}");
            // ===== Customer ICT: Private Key =====

            opt.Password = group.SftpPassword;
            if (!string.IsNullOrEmpty(_appSetting.ProxyHost) &&
                Conv.atoi32(_appSetting.ProxyPort) > 0 && isUseProxy)
            {
                opt.AddRawSettings("ProxyMethod", "4"); // Telnet
                opt.AddRawSettings("ProxyHost", _appSetting.ProxyHost);
                opt.AddRawSettings("ProxyPort", _appSetting.ProxyPort.ToString());
                opt.AddRawSettings("ProxyUsername", "");
                opt.AddRawSettings("ProxyPassword", "");
                opt.AddRawSettings("ProxyTelnetCommand", "connect %host %port\n");

                //opt.AddRawSettings("ProxyMethod", "4"); // HTTP CONNECT
                //opt.AddRawSettings("ProxyHost", _appSetting.ProxyHost);
                //opt.AddRawSettings("ProxyPort", _appSetting.ProxyPort.ToString());
                //opt.AddRawSettings("ProxyUsername", "");
                //opt.AddRawSettings("ProxyPassword", "");

                Global.WriteLog($"Proxy host {_appSetting.ProxyHost} - Proxy port : {_appSetting.ProxyPort}");
            }
            else
            {
                opt.AddRawSettings("ProxyMethod", "0"); // No proxy
                Global.WriteLog($"No proxy");
            }
            return opt;
        }



        private int GetScanIntervalInMilliseconds()
        {
            if (_appSetting == null) return 30000;

            int baseInterval = _appSetting.ScanInterval;

            return baseInterval;
        }

        private void LogError(string message)
        {
            Global.WriteLog(message);
            Dispatcher.Invoke(() =>
            {
                // TODO: Thêm log vào UI hoặc file log
                System.Diagnostics.Debug.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
            });
        }

        private void UpdateUIWithProgress()
        {
            Dispatcher.Invoke(() =>
            {
                RefreshFilteredView();
                UpdateApplicationStatus();
            });
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            string appPassword = _appSetting.ApplicationPassword;

            var passwordDialog = new PasswordInputWindow(appPassword)
            {
                Owner = this
            };

            bool? result = passwordDialog.ShowDialog();

            if (result == true && passwordDialog.IsAuthenticated)
            {
                SettingsWindow settingsWindow = new SettingsWindow();
                settingsWindow.Closed += SettingsWindow_Closed;
                settingsWindow.Owner = this;
                bool? resultWindow = settingsWindow.ShowDialog();

                if (resultWindow == true)
                {
                    MessageBox.Show("Settings updated successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Access denied!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
          
        }

        private void SettingsWindow_Closed(object sender, EventArgs e)
        {
            UpdateSetting();
        }

        private void UpdateSetting()
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string exeDirectory = System.IO.Path.GetDirectoryName(exePath);
                string settingsFile = System.IO.Path.Combine(exeDirectory, "settings.xml");

                if (File.Exists(settingsFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    using (TextReader reader = new StreamReader(settingsFile))
                    {
                        _appSetting = (AppSettings)serializer.Deserialize(reader);
                    }
                }

                // Cập nhật biến điều khiển threada
                _isThreadProcess = _appSetting.AutoStartScan;

                // Cập nhật thông tin database
                msSQL.DBHostName = _appSetting.DbServer;
                msSQL.DBPort = _appSetting.DbPort.ToString();
                msSQL.DBUserName = _appSetting.DbUsername;
                msSQL.DBPasswordKey = _appSetting.DbPassword;
                msSQL.DBName = _appSetting.DbName;

                // Cập nhật UI dựa trên trạng thái auto start
                UpdateUIFromSettings();
            }
            catch (Exception ex)
            {
                LogError($"Settings refresh failed: {ex}");
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
      

        private void UpdateUIFromSettings()
        {
            Dispatcher.Invoke(() =>
            {
                if (_appSetting.AutoStartScan)
                {
                    btnStartScan.IsEnabled = false;
                    btnStopScan.IsEnabled = true;
                }
                else
                {
                    btnStartScan.IsEnabled = true;
                    btnStopScan.IsEnabled = false;
                }

                UpdateApplicationStatus();
            });
        }

        private void btnStartScan_Click(object sender, RoutedEventArgs e)
        {
            lock (_lockObject)
            {
                _isThreadProcess = true;
                btnStopScan.IsEnabled = true;
                btnStartScan.IsEnabled = false;
            }

            UpdateApplicationStatus();
            UpdateUIWithProgress();
        }

        private void btnStopScan_Click(object sender, RoutedEventArgs e)
        {
            lock (_lockObject)
            {
                _isThreadProcess = false;
                btnStopScan.IsEnabled = false;
                btnStartScan.IsEnabled = true;
            }

            UpdateApplicationStatus();
            UpdateUIWithProgress();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Dọn dẹp tài nguyên khi đóng ứng dụng
            _cancellationTokenSource?.Cancel();

            // Chờ task kết thúc (timeout 3 giây)
            if (_scanTask != null)
            {
                try
                {
                    _scanTask.Wait(3000);
                }
                catch (AggregateException)
                {
                    // Bỏ qua lỗi cancel/task kết thúc
                }
            }

            _cancellationTokenSource?.Dispose();

            base.OnClosed(e);
        }

    }
}
