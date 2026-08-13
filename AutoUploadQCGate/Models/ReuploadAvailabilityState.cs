using System;

namespace AutoUploadQCGate.Models
{
    public sealed class ReuploadAvailabilityState
    {
        private readonly object _sync = new object();
        private bool _isBlocked;
        private string _reason = "";

        public bool IsBlocked
        {
            get
            {
                lock (_sync)
                    return _isBlocked;
            }
        }

        public string Reason
        {
            get
            {
                lock (_sync)
                    return _reason;
            }
        }

        public bool Block(string reason)
        {
            var normalizedReason = string.IsNullOrWhiteSpace(reason)
                ? ReuploadSchemaCompatibility.OperationFailureMessage
                : reason.Trim();

            lock (_sync)
            {
                var changed = !_isBlocked ||
                              !string.Equals(_reason, normalizedReason, StringComparison.Ordinal);
                _isBlocked = true;
                _reason = normalizedReason;
                return changed;
            }
        }

        public bool Allow()
        {
            lock (_sync)
            {
                var changed = _isBlocked;
                _isBlocked = false;
                _reason = "";
                return changed;
            }
        }
    }
}
