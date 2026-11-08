# Root job information and window controls

Preview `1.0.0-preview.20261108.1` provides `GetShortJobInfo()`, `GetFullJobInfo()`, `MinimizeStaadMainWindow()`, `MaximizeStaadMainWindow()` and `RestoreStaadMainWindow()`. All five preserve their public names, return types and parameterless signatures on `IOSRoot` and `OSRootAdapter`, with matching expanded XML IntelliSense.

## Job-information snapshots

`GetShortJobInfo()` makes one native call and returns a new `ShortJobInfo` with `Name`, `Client` and `Originator`. `GetFullJobInfo()` makes one native call for thirteen fields and returns a new `FullJobInfo`, inheriting those three properties and adding `Originated`, `Number`, `Revision`, `Part`, `Reference`, `Checker`, `Checked`, `Approver`, `Approved` and `Comments`.

Each native output starts as a string-shaped VARIANT. Every field must be written and string-valued. An explicitly empty string is preserved; null, DBNull, non-string or unwritten output throws `InvalidOperationException`. All thirteen output shapes are checked before parsing any date. Native void completion is not interpreted as a Boolean or integer status.

Non-date text preserves whitespace, Unicode and comment line breaks. `Originated`, `Checked` and `Approved` are `DateTime` properties: empty or whitespace-only native dates become `DateTime.MinValue`; nonempty dates use `DateTime.Parse` with the caller's `CurrentCulture` and `DateTimeStyles.None`. Invalid dates throw `FormatException`; ambiguous dates are not retried under another culture. These properties do not retain the original date spelling or distinguish a real minimum date from an unspecified one.

~~~csharp
var shortInfo = wrapper.Root.GetShortJobInfo();
Console.WriteLine(shortInfo.Name + ": " + shortInfo.Client);

var fullInfo = wrapper.Root.GetFullJobInfo();
Console.WriteLine(fullInfo.Number);
Console.WriteLine(fullInfo.Comments);
if (fullInfo.Originated != DateTime.MinValue)
    Console.WriteLine(fullInfo.Originated);

fullInfo.Name = "Local draft"; // Edits the managed snapshot only.
~~~

Both models are detached and editable. They inherit UI-framework-independent `ValidatablePropertyStore` change notifications. Default text is null and default dates are `DateTime.MinValue`; factories create independent storage. Manual `ShortJobInfo.From(...)` text can be null, while successful native queries require strings. `FullJobInfo.From(...)` keeps its thirteen string parameters and the same date rules. Editing a snapshot never calls a native setter or saves the model.

The store locks each read/write and raises `PropertyChanged` synchronously on the writing thread after releasing its lock. Equal existing values do not notify; storing the first value does, including a first null. Subscriber exceptions propagate after the write. References are not cloned, and multi-property changes are not atomic. Consumers coordinate UI-thread access as needed; the library does not require WPF.

## Main-window requests

Window controls query `GetMainWindowHandle` live on the bound root, validate its pointer-sized shape, then query its positive process ID. They verify that the HWND exists and belongs to that process before issuing one asynchronous Windows state request. Unlike the cached `GetMainWindowHandle()` observation, these methods do not rely on a cached HWND.

| Function | Windows command | Requested behavior |
| --- | --- | --- |
| `MinimizeStaadMainWindow()` | `SW_MINIMIZE` (6) | Minimize; Windows can activate the next top-level window. |
| `MaximizeStaadMainWindow()` | `SW_MAXIMIZE` (3) | Activate and maximize. |
| `RestoreStaadMainWindow()` | `SW_RESTORE` (9) | Activate and restore from a minimized or maximized state. |

These are the documented [Windows show-state commands](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow). `ShowWindowAsync` reports whether the request was started; it does not wait for completion. Normal managed return therefore does not certify the visible state or foreground focus. See [ShowWindowAsync](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindowasync).

~~~csharp
bool changeWindowState = false; // Enable only for the intended STAAD session.
if (changeWindowState)
{
    wrapper.Root.MinimizeStaadMainWindow();
    wrapper.Root.MaximizeStaadMainWindow();
    wrapper.Root.RestoreStaadMainWindow();
}
~~~

A malformed, zero, -1 or out-of-range handle, invalid PID, unavailable window, ownership mismatch or rejected Windows request throws `InvalidOperationException`. Invalid handle shapes fail before PID or Windows queries. [Window existence](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindow) and [owning process](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowthreadprocessid) checks reduce cross-session mistakes; they cannot eliminate handle recycling or external UI races after validation. Missing native handle/PID members propagate binding errors. No other process is selected as a fallback.

## Threading, lifetime and errors

All five methods use the wrapper's constructing COM thread and current Root binding under its shared operation lease. Disposed or replaced adapters, other-thread access and nested Root/refresh/disposal operations are rejected before dispatch. Reacquire `wrapper.Root` after `RefreshComObjects()`. Raw-object calls and external UI edits bypass the guard.

Job reads are synchronous snapshots. Window calls can block while obtaining native values, then return after Windows accepts the state request. No method launches STAAD, switches models, saves, runs analysis, refreshes caches, polls, retries, dispatches to a worker thread or rolls back native/UI effects. Native exceptions preserve their original identity and HRESULT. A transport failure can follow partial effects.

The installed STAAD.Pro 2025 help documents the three- and thirteen-field reference-output job contracts. Managed fixtures cover all output slots, missing writes, malformed shapes, date culture/blank behavior, snapshots and notifications, exact window commands, handle bit preservation, PID/ownership checks, rejected requests and binding/thread/reentrancy behavior. Tests use a managed window transport and do not change the user's desktop. Live STAAD/COM/window qualification remains separate from these checks.
