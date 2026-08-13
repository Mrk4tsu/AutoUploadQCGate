using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoUploadQCGate.Models
{
    public static class ReuploadSchemaCompatibility
    {
        public const string OutdatedMessage =
            "Reupload database schema is outdated. Apply HardenReuploadDelivery.sql before creating reupload requests.";

        public const string ValidationFailureMessage =
            "Reupload database schema could not be validated. Check the database connection and worker log.";

        public const string OperationFailureMessage =
            "Reupload database operation failed. Check the worker log before retrying.";

        public const string ProbeSql = @"
SELECT CASE WHEN
       OBJECT_ID(N'dbo.dynamic_reupload_requests', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.dynamic_reupload_request_items', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.dynamic_reupload_request_items', N'transfer_started_at') IS NOT NULL
   AND COL_LENGTH(N'dbo.dynamic_reupload_request_items', N'review_reason') IS NOT NULL
   AND COL_LENGTH(N'dbo.dynamic_reupload_request_items', N'reviewed_at') IS NOT NULL
   AND COL_LENGTH(N'dbo.dynamic_reupload_request_items', N'reviewed_by') IS NOT NULL
THEN 1 ELSE 0 END;";

        public const string WorkQuerySql = @"
SELECT DISTINCT
    r.created_at AS request_created_at,
    r.pkid AS request_id,
    r.status AS request_status,
    r.operator_code,
    r.requested_bag_count,
    ri.pkid AS item_id,
    ri.aluminum_bag_information_id AS bag_id,
    ri.aluminum_bag_code,
    ri.source_file_path,
    ri.local_file_path,
    ri.file_hash,
    ri.status AS item_status,
    ri.attempt_count,
    ri.is_legacy_recovered,
    ri.logs AS item_logs,
    ri.transfer_started_at,
    ri.review_reason,
    ri.reviewed_at,
    ri.reviewed_by,
    abi.number_of_psc_ok,
    q.pkid AS queue_id,
    q.created_at AS queue_created_at,
    d.combine_indication,
    d.combine_indication_log_path,
    d.folder_name,
    d.ship_quantity,
    c.customer_code,
    c.customer_name,
    c.is_upload_folder,
    c.sftp_server,
    c.sftp_port,
    c.sftp_user,
    c.sftp_password,
    c.sftp_remote_path,
    c.is_use_proxy,
    c.is_use_key,
    des.item_name
FROM dynamic_reupload_requests r
INNER JOIN dynamic_reupload_request_items ri ON ri.reupload_request_id = r.pkid
INNER JOIN dynamic_aluminum_bag_informations abi
    ON abi.pkid = ri.aluminum_bag_information_id
INNER JOIN dynamic_upload_data_queues q ON q.pkid = r.upload_data_queue_id
INNER JOIN dynamic_aluminum_bag_information_queues qb
    ON qb.upload_data_queue_id = q.pkid
   AND qb.aluminum_bag_information_id = ri.aluminum_bag_information_id
LEFT JOIN dynamic_upload_data d ON d.pkid = q.upload_data_id
LEFT JOIN define_customers c ON c.pkid = d.customer_id
LEFT JOIN define_design_informations des ON des.pkid = d.design_information_id
WHERE r.status IN ('Pending', 'Processing', 'NeedsReview')
  AND ri.status = 'Pending'
  AND ri.attempt_count < 3
ORDER BY r.created_at, ri.pkid;";

        public const string DisplaySyncBaseSql = @"
SELECT
    r.created_at AS request_created_at,
    r.pkid AS request_id,
    r.status AS request_status,
    r.operator_code,
    r.requested_bag_count,
    r.logs AS request_logs,
    r.started_at AS request_started_at,
    r.completed_at AS request_completed_at,
    r.updated_at AS request_updated_at,
    ri.pkid AS item_id,
    ri.aluminum_bag_information_id AS bag_id,
    ri.aluminum_bag_code,
    ri.source_file_path,
    ri.local_file_path,
    ri.file_hash,
    ri.status AS item_status,
    ri.attempt_count,
    ri.is_legacy_recovered,
    ri.logs AS item_logs,
    ri.transfer_started_at,
    ri.review_reason,
    ri.reviewed_at,
    ri.reviewed_by,
    ri.created_at AS item_created_at,
    ri.uploaded_at AS item_uploaded_at,
    ri.updated_at AS item_updated_at,
    abi.number_of_psc_ok,
    r.upload_data_queue_id AS queue_id
FROM dynamic_reupload_requests r
INNER JOIN dynamic_reupload_request_items ri ON ri.reupload_request_id = r.pkid
INNER JOIN dynamic_aluminum_bag_informations abi
    ON abi.pkid = ri.aluminum_bag_information_id
";

        public const string DisplaySyncQuerySql = DisplaySyncBaseSql + @"
WHERE r.status IN ('Pending', 'Processing', 'NeedsReview')
ORDER BY r.created_at, ri.pkid;";

        public static string BuildCachedDisplaySyncQuery(IEnumerable<int> requestIds)
        {
            var ids = requestIds == null
                ? new List<int>()
                : requestIds.Where(x => x > 0).Distinct().OrderBy(x => x).ToList();
            if (ids.Count == 0)
                return string.Empty;

            return DisplaySyncBaseSql + Environment.NewLine +
                   $"WHERE r.pkid IN ({string.Join(",", ids)})" + Environment.NewLine +
                   "ORDER BY r.created_at, ri.pkid;";
        }

        public static bool IsReady(object probeValue)
        {
            if (probeValue == null || probeValue == DBNull.Value)
                return false;

            int value;
            return int.TryParse(probeValue.ToString(), out value) && value == 1;
        }
    }
}
