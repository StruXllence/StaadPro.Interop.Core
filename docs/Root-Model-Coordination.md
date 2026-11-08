# Root input units and model coordination

Preview `1.0.0-preview.20261108.2` adds five operations to `IOSRoot` and `OSRootAdapter` with matching XML IntelliSense. Names, parameter names, types, order, defaults and return types are preserved.

| Operation | Parameters and defaults | Result |
| --- | --- | --- |
| `SetInputUnitsEx` | `LengthInputUnit lengthUnit, ForceInputUnit forceUnit` | The same `IOSRoot` after one native `SetInputUnits` call |
| `AwaitModelHandle` | `string fileFullPath, double timeoutDurationInSeconds = 60, double sleepIntervalInSeconds = 0.1` | Whether the exact full model path was observed within the elapsed-time budget |
| `OpenStaadModel` | `string staadFileFullPath, int sleepInterval, int maxWaitTime = 600` | Whether the requested path was observed after one native open request |
| `CloseCurrentStaadModel` | None | A void silent-close request when a non-whitespace model filename is reported |
| `CloseWriteReopenStaadFile` | `string filePath, List<string> fileContents, int sleepTimeMilliseconds = 200, int maxTimeMilliseconds = 5000` | Whether replacement completed and the target was already identified or its reopen request returned |

## Input units and cached metadata

`SetInputUnitsEx` validates both named enum values before dispatching their Int32 codes in length/force order. Length codes are Inch=0, Feet=2, CentiMeter=3, Meter=4, MilliMeter=5, DeciMeter=6 and KiloMeter=7. Unnamed code 1 is rejected even though native reads recognize it as a feet alias. Force codes are KiloPound=0, Pound=1, KiloGram=2, MetricTon=3, Newton=4, KiloNewton=5, MegaNewton=6 and DecaNewton=7. Undefined codes throw `ArgumentOutOfRangeException` before dispatch.

The native unit setter is void. Normal return provides no unit read-back or rollback. It changes native input state for later operations; it does not convert existing managed values, establish result units, change the base unit system or save. Wrapper unit properties and cached Root getters retain their prior values. Call `wrapper.RefreshUnits()` after the setter returns to reread snapshots without replacing Root.

## Waiting and opening

`AwaitModelHandle` observes filenames only; it neither obtains a Windows handle nor opens a model. It checks immediately, then polls one `GetSTAADFile` output per attempt with a Boolean full-path flag. `OpenStaadModel` first requests `OpenSTAADFile` once, even when the target is already open, and then uses the same polling logic. Use `ModelIsOpen` before it when an unconditional open request is unwanted. These helpers use the bound session; the provider's separate acquisition behavior is described in the [wrapper guide](Wrapper-Foundation.md).

Requested paths must contain valid non-whitespace Windows path text. Both helpers capture the process working directory before dispatch, normalize the target and resolve all relative native filenames against that captured directory. Comparison is ordinal and case-insensitive over complete paths. A same-named file in another directory or a basename substring is not a match. An explicitly empty native filename is a nonmatch. Unwritten, null or non-string outputs and malformed native paths throw rather than becoming a timeout. This textual path comparison does not resolve symlinks, prove file identity, validate contents or establish domain/analysis readiness.

`AwaitModelHandle` requires finite positive durations within `TimeSpan` limits, both in seconds. `OpenStaadModel` requires a positive millisecond `sleepInterval` and a positive second `maxWaitTime`; the latter includes the open call. Invalid inputs are rejected before native dispatch. Both use a [Stopwatch elapsed-time budget](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-9.0), cap each sleep to remaining time and reject a match whose query completes after the deadline. A native call cannot be interrupted by this budget. Sleeps may overshoot due to timer scheduling. A timeout does not undo a prior open request or prove the model remained closed.

## Closing and replacing a model file

`CloseCurrentStaadModel` reads the full filename once. Empty or whitespace text produces no mutation. Otherwise, it requests native silent mode 1 followed by `CloseSTAADFile`. A silent-mode exception stops the close; it is not converted to the Boolean result of `SetSilentMode`. The previous setting is ignored and not restored. Normal return does not verify closure or explicitly save unsaved work.

`CloseWriteReopenStaadFile` deliberately replaces an existing model file. It validates arguments, copies the input list and requires the current full path to match the target before checking the file or closing. Nonmatching models return `false` without filesystem access or native mutation. Missing, directory and read-only targets fail before the close request. Null lines become empty lines; the copied list is independent of later caller edits. Callers must coordinate concurrent modifications while the list is being copied.

After enabling silent mode and requesting close, the workflow waits for an explicitly empty filename and an exclusive read/write probe. The wait budget begins after the close call returns. Only Windows sharing/lock violations are retried; other filesystem errors propagate. The positive sleep interval is in milliseconds. The maximum is nonnegative milliseconds; zero allows only an immediate availability check. A different intervening model aborts the workflow. A whitespace-only native path is invalid for this workflow, unlike the no-op behavior of `CloseCurrentStaadModel`.

The method rechecks blank identity before writing, creates an exclusive sibling temporary file, writes all lines as UTF-8 without BOM with platform newlines, and publishes with [File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace?view=net-9.0). It supplies no backup and never falls back to truncating the destination. Only its own temporary file is eligible for cleanup. Original encoding is not preserved. The exclusive probe and model checks are snapshots, not a filesystem/UI transaction; external changes and replacement failures remain possible.

After replacement, an empty filename triggers one reopen request. If the target is already reported, no reopen is sent. A different model returns `false` without reopening over it. `true` does not certify reopen readiness; use a separate `AwaitModelHandle` and then establish domain readiness. `false` or an exception can follow enabling silent mode, closing or replacing the file. No explicit save, rollback, process creation or directory creation occurs. The availability deadline does not bound file writes or close/open native calls.

## Example

With an owned connected wrapper on its constructing STA thread, choose each operation explicitly:

~~~csharp
var root = wrapper.Root;
bool changeUnits = false;
if (changeUnits)
{
    root.SetInputUnitsEx(StaadPro.Interop.Core.Enums.LengthInputUnit.Meter,
        StaadPro.Interop.Core.Enums.ForceInputUnit.KiloNewton);
    wrapper.RefreshUnits();
}

string target = @"C:\Models\Frame.std";
bool openTarget = false;
if (openTarget)
{
    bool identified = root.ModelIsOpen(target) || root.OpenStaadModel(target, 100, 60);
    Console.WriteLine(identified);
}

bool observeTarget = false;
if (observeTarget) Console.WriteLine(root.AwaitModelHandle(target, 5, 0.1));

bool replaceOwnedModel = false;
if (replaceOwnedModel)
{
    var completeLines = new System.Collections.Generic.List<string>
        { "STAAD SPACE", "UNIT METER KN", "FINISH" };
    bool requested = root.CloseWriteReopenStaadFile(target, completeLines);
    Console.WriteLine(requested); // Replacement/reopen request, not domain readiness.
}

bool closeCurrentModel = false;
if (closeCurrentModel) root.CloseCurrentStaadModel();
~~~

After establishing model readiness, refresh with `wrapper.RefreshComObjects()` outside the workflow and reacquire Root and other adapters. No method automatically refreshes cached metadata or domain bindings.

## Threading, errors and verification

One wrapper operation lease covers the entire workflow, including polls, sleeps, filesystem work and callbacks. All native calls stay on the constructing COM thread. Disposal, stale bindings, another thread and reentrant Root/refresh/disposal calls fail before dispatch. These synchronous methods do not pump UI messages or provide cancellation. Native dialogs can still block despite silent mode. The guard does not coordinate external UI, raw COM calls or another wrapper.

Original native and filesystem exceptions propagate, preserving COM HRESULTs. Managed fixtures verify unit-code mapping, cache ownership, exact full-path identity, deadline boundaries, bounded sleeps, caller thread, binding lifetime, reentrancy, malformed outputs, input snapshots and intervening model changes. Temporary-file tests cover UTF-8 replacement, sharing violations, read-only/missing targets, failed publication and cleanup. Installed-package checks compile the XML examples and this guide and exercise the managed workflows with an owned temporary file. No live model mutation or new cross-version COM qualification was performed.

The installed STAAD.Pro 2025 OpenSTAAD help documents `SetInputUnits`, full-path filename outputs, void open/close calls and the previous-setting silent-mode return. These native contracts do not promise model readiness, file unlocking or transactional behavior; the managed helper results have the limits described above.
