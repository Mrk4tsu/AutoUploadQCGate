# Log Detail Modal

## Goal

Allow an AutoUpload user to click a truncated Log cell and read its complete content in a scrollable modal.

## Tasks

- [x] Replace the Log cell text with a click target that keeps the compact table preview. → Verify: the click binds to the row Log value.
- [x] Add an in-window modal with scrolling, text selection, and close controls. → Verify: it blocks the table while visible and closes from either control.
- [x] Make the modal square-cornered and close it when its dark backdrop is clicked. → Verify: clicks inside the dialog do not trigger the backdrop handler.
- [x] Build the WPF app and run focused checks. → Verify: compilation and existing AutoUpload tests complete successfully.

## Done When

- [x] Clicking any Log cell opens its full, readable log without relying on a tooltip.
