using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoUploadQCGate.Models
{
    internal static class UploadRecordKinds
    {
        public const string Normal = "Normal";
        public const string Reupload = "Reupload";
    }

    internal sealed class ReuploadDisplaySource
    {
        public int RequestId { get; set; }
        public int QueueId { get; set; }
        public string RequestStatus { get; set; }
        public string RequestLogs { get; set; }
        public DateTime? RequestCreatedAt { get; set; }
        public DateTime? RequestCompletedAt { get; set; }
        public string CombineIndication { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string BagCode { get; set; }
        public int NumberOfPscOk { get; set; }
        public string ItemStatus { get; set; }
        public string ItemLogs { get; set; }
        public string ReviewReason { get; set; }
        public DateTime? ItemUploadedAt { get; set; }
    }

    internal sealed class ReuploadDisplaySummary
    {
        public int RequestId { get; set; }
        public int QueueId { get; set; }
        public string StableId { get; set; }
        public string CombineIndication { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public int UploadQuantity { get; set; }
        public string Status { get; set; }
        public string Logs { get; set; }
        public DateTime? UploadedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    internal static class ReuploadDisplayMapper
    {
        public static IReadOnlyList<ReuploadDisplaySummary> Build(IEnumerable<ReuploadDisplaySource> source)
        {
            if (source == null)
                return new List<ReuploadDisplaySummary>();

            return source
                .GroupBy(x => x.RequestId)
                .Select(BuildRequest)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.RequestId)
                .ToList();
        }

        private static ReuploadDisplaySummary BuildRequest(IGrouping<int, ReuploadDisplaySource> requestItems)
        {
            var first = requestItems.First();
            var quantity = requestItems.Sum(x => (long)Math.Max(0, x.NumberOfPscOk));
            var uploadedAt = first.RequestCompletedAt ?? requestItems.Max(x => x.ItemUploadedAt);

            return new ReuploadDisplaySummary
            {
                RequestId = first.RequestId,
                QueueId = first.QueueId,
                StableId = $"reupload:{first.RequestId}",
                CombineIndication = first.CombineIndication ?? string.Empty,
                CustomerCode = first.CustomerCode ?? string.Empty,
                CustomerName = first.CustomerName ?? string.Empty,
                UploadQuantity = quantity > int.MaxValue ? int.MaxValue : (int)quantity,
                Status = MapStatus(first.RequestStatus, requestItems.Select(x => x.ItemStatus)),
                Logs = BuildLogs(first.RequestLogs, requestItems),
                UploadedAt = uploadedAt,
                CreatedAt = first.RequestCreatedAt,
            };
        }

        internal static string MapStatus(string requestStatus, IEnumerable<string> itemStatuses)
        {
            var statuses = new List<string> { requestStatus ?? string.Empty };
            if (itemStatuses != null)
                statuses.AddRange(itemStatuses.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (statuses.Any(x => EqualsStatus(x, ReuploadDeliveryPolicy.NeedsReview)))
                return UploadStatusNames.NeedsReview;
            if (statuses.Any(x => EqualsStatus(x, ReuploadDeliveryPolicy.Processing)))
                return UploadStatusNames.Processing;
            if (statuses.Any(x => EqualsStatus(x, ReuploadDeliveryPolicy.Pending)))
                return UploadStatusNames.Pending;
            if (statuses.Any(x => EqualsStatus(x, ReuploadDeliveryPolicy.Failed)))
                return UploadStatusNames.Failed;
            if (statuses.Any(x => EqualsStatus(x, ReuploadDeliveryPolicy.Uploaded)))
                return UploadStatusNames.Success;

            return UploadStatusNames.Pending;
        }

        private static string BuildLogs(string requestLogs, IEnumerable<ReuploadDisplaySource> items)
        {
            var builder = new StringBuilder();
            AppendLog(builder, "Request", requestLogs);

            foreach (var item in items.OrderBy(x => x.BagCode).ThenBy(x => x.ItemUploadedAt))
            {
                var label = string.IsNullOrWhiteSpace(item.BagCode) ? "Item" : item.BagCode.Trim();
                AppendLog(builder, label, item.ItemLogs);
                AppendLog(builder, label + " review", item.ReviewReason);
            }

            return builder.ToString().Trim();
        }

        private static void AppendLog(StringBuilder builder, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (builder.Length > 0)
                builder.AppendLine();
            builder.Append('[').Append(label).Append("] ").Append(value.Trim());
        }

        private static bool EqualsStatus(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
