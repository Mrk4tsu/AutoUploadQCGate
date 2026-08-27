# Re-upload terminal display sync

## Goal

Store a request in the local cache immediately after the worker finalizes it so newly completed re-uploads always appear in AutoUpload.

## Tasks

- [x] Add a request-id sync after terminal and review outcomes. → Verify: the sync query retrieves a completed request without an active-status filter.
- [x] Keep the existing batch cache sync for historical requests. → Verify: existing history behavior remains unchanged.
- [x] Compile the WPF app and run the focused AutoUpload checks. → Verify: both complete successfully.

## Done When

- [x] A request created just before the worker cycle and completed in that cycle is present in the local display cache.
