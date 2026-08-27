# Cache Customer for Reupload History

## Goal

Show the server Customer name for every AutoUpload reupload row, including historical queues that are no longer active locally.

## Tasks

- [x] Include Customer in the server reupload synchronization query. → Verify: the query joins Upload Data and Customers by the queue reference.
- [x] Persist Customer on the local reupload request with an additive SQLite migration. → Verify: fresh and existing cache schemas expose the column.
- [x] Bind the cached request Customer to the reupload display row. → Verify: the display mapper retains UTAC for a reupload request.
- [x] Run focused tests, WPF build, and lint. → Verify: all complete without errors.

## Done When

- [x] Reupload history uses the Customer cached directly on each request. → Verify: schema and display-mapper checks pass.
