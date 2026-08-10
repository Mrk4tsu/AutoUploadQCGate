using System;
using AutoUploadQCGate.Models;

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

                Console.WriteLine("Reupload delivery policy checks passed: 5/5");
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
    }
}
