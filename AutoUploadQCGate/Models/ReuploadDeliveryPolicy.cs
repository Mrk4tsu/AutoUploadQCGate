namespace AutoUploadQCGate.Models
{
    internal static class ReuploadDeliveryPolicy
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Uploaded = "Uploaded";
        public const string Failed = "Failed";
        public const string NeedsReview = "NeedsReview";

        public static string FailureStatus(bool transferStarted, int attemptCount)
        {
            if (transferStarted)
                return NeedsReview;

            return attemptCount >= 3 ? Failed : Pending;
        }

        public static string StaleProcessingStatus(bool transferStarted)
        {
            return transferStarted ? NeedsReview : Pending;
        }
    }
}
