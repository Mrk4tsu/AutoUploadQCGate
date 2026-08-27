# Re-upload source-path fallback

## Goal

Allow old re-upload requests to recover files from the current Combine Destination when their stored absolute source path no longer exists.

## Tasks

- [x] Add a small resolver that prioritizes an existing local snapshot, then the stored source path, then the current General Settings Combine path. → Verify: focused resolver tests cover all three choices.
- [x] Read the current Combine path once per re-upload cycle and use it only as a fallback. → Verify: the worker keeps the stored path when it still exists.
- [x] Write clear audit messages for fallback and missing-file outcomes. → Verify: resolver test checks diagnostic text.
- [x] Build and run the existing AutoUpload test executable. → Verify: build and tests complete without errors.

## Done When

- [x] A moved Destination root does not break re-upload when the same combine folder and bag file exist at the current configured location.
- [x] Existing snapshots and valid historical paths remain unchanged.
