using System;

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

        public static bool IsReady(object probeValue)
        {
            if (probeValue == null || probeValue == DBNull.Value)
                return false;

            int value;
            return int.TryParse(probeValue.ToString(), out value) && value == 1;
        }
    }
}
