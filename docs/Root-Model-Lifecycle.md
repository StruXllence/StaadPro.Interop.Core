# Root model and application operations

Preview `1.0.0-preview.20261107` adds five model/application operations to `IOSRoot` and `OSRootAdapter`. Their names, return types and parameter names, types and order are preserved. The existing wrapper lease guards all calls on the constructing thread and current Root binding.

| Operation | Native request | Managed return |
| --- | --- | --- |
| `SetSilentMode(SilentMode silentMode)` | `SetSilentMode((int)silentMode)` | `true` after normal invocation; `false` if native dispatch throws |
| `SaveModel(SilentMode silentMode)` | `SaveModel((int)silentMode)` | The same `IOSRoot` after native void completion |
| `OpenSTAADFile(string filePath)` | `OpenSTAADFile(normalizedAbsolutePath)` | The same `IOSRoot` after native void completion |
| `CloseSTAADFile()` | `CloseSTAADFile()` | The same `IOSRoot` after native void completion |
| `ShowApplication()` | `ShowApplication()` | The same `IOSRoot` after invocation; ignores any native return |

## Silent mode and saving

`SilentMode.Disable = 0` and `SilentMode.Enable = 1`. Undefined values throw `ArgumentOutOfRangeException` for `silentMode` before native dispatch. The native input is an Int32 rather than the enum object or a Boolean.

Native `SetSilentMode` returns the previous setting. The managed Boolean reports normal invocation rather than that setting: a native previous value of zero or false still produces `true`. Native exceptions, including a missing member, produce `false`. This catch does not cover argument validation or wrapper disposal, binding, thread or reentrancy errors; those still throw. A failed invocation can follow a partial change. The setting is not queried, restored or treated as proof of unattended execution.

`SaveModel` passes its option directly to the native save call, without a separate `SetSilentMode` request. It does not write managed file contents, analyze, inspect filenames or verify disk output. Native void completion does not certify persistence or prompt acceptance. Silent mode suppresses supported warnings; licensing, filesystem and other errors may still display dialogs.

## Opening and closing

`OpenSTAADFile` requires a non-null, non-whitespace Windows path. It validates invalid path characters and resolves relative input using one captured working directory before the call. It forwards one absolute string. It does not check existence or require `.std`, launch a process, save/close first, poll readiness or validate the newly reported model identity. The existing session and its current model may be affected, including by prompts.

`CloseSTAADFile` acts on the current model in the bound session. It does not explicitly save, discard changes, terminate STAAD or dispose the wrapper. Native dirty-model and dialog handling belongs to STAAD. Normal return does not prove closure.

Neither method refreshes wrapper caches or domain adapters automatically. After establishing native readiness, call `wrapper.RefreshComObjects()` and reacquire `wrapper.Root` and other adapters. Previously captured adapters become stale after refresh. A low-level open request differs from the session provider's acquisition/readiness workflow.

## Displaying the application

`ShowApplication` sends one request to the exact bound root. It does not open/save/close a model, run analysis, launch another process or explicitly activate/maximize a window through Windows APIs. Its return does not prove foreground focus, visibility or repaint completion. Native member availability is build-dependent.

## Guarded examples

With an owned connected session on its constructing STA thread:

~~~csharp
var root = wrapper.Root;
bool saveCurrentModel = false;
if (saveCurrentModel)
{
    bool requested = root.SetSilentMode(StaadPro.Interop.Core.Enums.SilentMode.Enable);
    Console.WriteLine(requested); // Invocation result, not the previous mode.
    root.SaveModel(StaadPro.Interop.Core.Enums.SilentMode.Enable);
}

bool changeCurrentModel = false;
if (changeCurrentModel)
{
    root.OpenSTAADFile(@"C:\Models\Frame.std");
    // Establish model readiness before wrapper.RefreshComObjects(), then reacquire adapters.
}

bool closeCurrentModel = false;
if (closeCurrentModel) root.CloseSTAADFile();
bool showApplication = false;
if (showApplication) root.ShowApplication();
~~~

These independent flags describe separate operations; choose them according to ownership of the session and model. The methods are synchronous. Native calls can block or display dialogs; there is no hard in-process timeout, worker-thread dispatch, retry, rollback or alternate-instance attachment.

## Errors and validation

Disposed wrappers throw `ObjectDisposedException`. Replaced/manual-noncurrent bindings, another thread and nested Root/refresh/disposal operations throw `InvalidOperationException` before dispatch. Except for the documented Boolean behavior of `SetSilentMode`, native exceptions propagate unchanged, including their HRESULT. Failed writes can have partial effects. The wrapper guard does not coordinate raw-object access or external UI changes.

The installed STAAD.Pro 2025 help documents the previous-setting return of `SetSilentMode`, the 0/1 save option, and void open/close contracts. Managed fixtures verify argument types, exact calls, invalid inputs, returned adapter identity, status semantics, cache/refresh ownership, failures, lifetime, caller thread and nested operations. Complete matching XML is shipped beside the DLL and compiled through a fresh NuGet consumer. No live save/open/close or display mutation was performed for this increment; managed checks do not establish native compatibility or engineering qualification for every build.
