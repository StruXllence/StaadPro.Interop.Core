# Model creation, named views and application shutdown

These operations use the application already bound to `wrapper.Root`. They do not acquire a different STAAD.Pro instance. The caller controls the session and any changes to its models.

| Operation | Managed contract | Purpose |
| --- | --- | --- |
| `GetProcessHandle()` | `NativeProcessHandleValue` | Reads an opaque numeric diagnostic snapshot. |
| `NewSTAADFile(string filePath, LengthInputUnit lengthUnit, ForceInputUnit forceUnit)` | `void` | Requests a new model in an unnamed session. |
| `CreateNamedView(string viewName, int creationFlag)` | `void` | Sends a name and a caller-verified native creation flag. |
| `SaveNamedView(string viewName)` | `void` | Requests saving the current view under a name. |
| `Quit()` | `void` | Explicitly requests shutdown of the acquired application. |

## Thread and binding lifetime

Construct and use the wrapper on the same Windows STA thread. One operation lease covers input validation, destination inspection, native identity queries, dispatch and return-value qualification. Disposed wrappers, old Root adapters retained after `RefreshComObjects()`, manually constructed noncurrent adapters, other-thread calls and nested Root/refresh/disposal work are rejected before native access. Obtain `wrapper.Root` again after refreshing COM objects.

This guard applies to the managed wrapper, not raw COM calls, another wrapper or the STAAD.Pro UI. Coordinate all access to the session and destination. A synchronous native call may block, show a prompt or fail after native changes; the library does not retry or roll back these operations.

## Process-handle diagnostics

`GetProcessHandle()` issues one query on the captured native root. It accepts integral CLR values and `IntPtr`/`UIntPtr`, preserving unsigned values and sign-extending signed negative values to the immutable snapshot's `ulong RawValue`. Zero and all-one values remain unchanged. Null, Boolean, enum, text and fractional representations throw `InvalidOperationException`.

The snapshot owns no resource and does not establish the native handle's pointer width, originating process context, lifetime or access rights. Do not close it or pass its number to Windows handle APIs. Use `GetProcessId()` for a PID and `GetMainWindowHandle()` for the cached application-window handle; none proves connection liveness.

## Creating a model

`NewSTAADFile` differs from `OpenSTAADFile`: it requests a new model with explicit initial units rather than opening an existing file. It also differs from provider acquisition, which selects or starts an application.

The managed request:

1. Resolves a relative path against the process working directory captured before native callbacks.
2. Validates native length and force codes 0–7, the `.std` filename and Windows device-name restrictions.
3. Requires an existing parent directory and an absent target. Existing files and directories are rejected. Access/path errors propagate.
4. Reads `GetSTAADFile` once using an internal string output buffer. A nonblank path is rejected; malformed or unwritten output is an error.
5. Rechecks that the destination is absent after that native query, then submits the absolute path and Int32 unit codes once.

No placeholder file, parent directory or managed STD contents are created. A blank filename is not proof that the session contains no unsaved work. The caller must decide whether creating a model in that session is appropriate.

The final absence check and native request are **not atomic**. Another client can create the destination or switch the active model after validation. Exclusive coordination is required; this method cannot promise native no-overwrite behavior. It does not close or save an existing model.

The native return is void. Normal completion does not establish file persistence, initial-unit adoption, active-model identity or readiness. Read back the required state explicitly and refresh wrapper unit/metadata snapshots when appropriate. There is no implicit polling, save, retry or rollback.

## Creating versus saving a named view

Both methods require a non-null, non-whitespace name and preserve it literally, including Unicode and surrounding spaces. `CreateNamedView` requires an explicit Int32 flag; `SaveNamedView` submits only the name. They call their own native entry points and do not substitute for each other.

The inspected native interop declares two inputs for `CreateNamedView`, one for `SaveNamedView`, and void returns. The help's additional error parameter is inconsistent with that declaration. These methods follow the inspected call shapes without speculative error buffers or alternate-signature retries. Qualify them against the actual target STAAD.Pro build.

The reviewed help does not establish the creation flag's valid meanings or range. All Int32 values are forwarded; no enum or default flag is invented. Establish the appropriate flag before calling.

Normal return establishes dispatch completion only. Duplicate names, overwrite behavior, current-view contents, empty-model behavior and persistence remain native decisions. Neither method saves the model file, changes managed selection, refreshes the wrapper or verifies the saved view.

## Explicit application shutdown

`Quit()` sends one no-argument native shutdown request to the captured root. It can affect unsaved work and every client/interface sharing that application. Save or discard work under the application's policy before requesting shutdown.

It does not silently save, close a model first, change silent mode, kill a process, look up another instance or verify process exit. Native prompts or analysis can delay or prevent termination. A native exception is propagated unchanged even if effects already occurred.

The wrapper is not automatically disposed or retargeted. `IsConnected` describes managed binding presence and may still be true after native shutdown. Previously captured COM interfaces may be unusable. Dispose the wrapper explicitly on its constructing thread when finished; `Dispose()` itself never calls `Quit()`.

## C# example

The mutating requests below are disabled until the caller has confirmed the intended session, destination and native creation flag.

~~~csharp
Console.WriteLine(wrapper.Root.GetProcessHandle().RawValue); // diagnostic value only

bool createNewModel = false;
if (createNewModel)
{
    wrapper.Root.NewSTAADFile(
        @"C:\Models\NewFrame.std",
        StaadPro.Interop.Core.Enums.LengthInputUnit.Meter,
        StaadPro.Interop.Core.Enums.ForceInputUnit.KiloNewton);
    // Verify native identity/readiness as required before further work.
    wrapper.RefreshUnits(); // explicit snapshot refresh, if the model is ready
}

bool createView = false;
int confirmedCreationFlag = 0; // replace; zero is not asserted to be valid
if (createView) wrapper.Root.CreateNamedView("Frame view", confirmedCreationFlag);

bool saveCurrentView = false;
if (saveCurrentView) wrapper.Root.SaveNamedView("Frame view");

bool quitApplication = false;
if (quitApplication) wrapper.Root.Quit();
~~~

## Verification limits

Offline tests exercise argument order/types, numeric representations, literal names, destination checks and races, malformed identity output, original native exceptions and wrapper guards through managed native stand-ins. Installed-package checks compile these examples and execute stand-in requests. They do not perform live model creation, named-view changes or application shutdown, or certify every STAAD.Pro build.
