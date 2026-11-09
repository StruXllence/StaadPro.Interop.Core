# Root analysis requests and status

Preview `1.0.0-preview.20261109` adds five Root contracts with complete XML IntelliSense on `IOSRoot` and `OSRootAdapter`:

| Operation | Managed contract | Native dispatch |
| --- | --- | --- |
| `AnalyzeEx` | `AnalysisResult AnalyzeEx(SilentMode silentMode, HiddenMode hiddenMode, WaitAnalysisMode waitMode)` | One request with Int32 silent, hidden and wait codes |
| `Analyze` | `void Analyze()` | One no-argument `Analyze` request |
| `IsAnalyzing` | `bool IsAnalyzing()` | One live running-state query |
| `GetAnalysisStatus` | `AnalysisStatusResult GetAnalysisStatus(string modelFilePath)` | One query for the normalized requested path |
| `GetAnalysisStatus` | `AnalysisStatusResult GetAnalysisStatus()` | One native filename read, then one status query for that captured path |

## Requesting analysis

`AnalyzeEx` preserves the named enums, parameter order and required parameters. `SilentMode.Disable/Enable` and `HiddenMode.Disable/Enable` use 0/1. `WaitAnalysisMode.Return = 0` requests return without waiting; `Wait = 1` requests native completion waiting. Undefined values throw `ArgumentOutOfRangeException` with the public parameter name before dispatch. The native arguments are Int32 codes rather than enum objects or Boolean values.

Silent mode suppresses supported analysis-engine prompts. Hidden mode suppresses the analysis-engine dialog. These flags do not guarantee unattended execution or suppress every application, license or error dialog. The method makes no separate `SetSilentMode` call. `Wait` can block the calling thread indefinitely; `Return` does not certify analysis completion. There is no managed timeout or cancellation. The managed lease ends when the native request returns; Return mode does not keep later model edits locked out while the engine continues running.

`AnalysisResult` preserves signed Int32 native status codes: -1 Terminated, 0 GeneralError, 1 InProgress, 2 CompletedSuccessfully, 3 CompletedWithWarnings, 4 CompletedWithErrors and 5 NotPerformed. Unknown integer codes are retained as undefined enum values. Supported CLR integer representations within Int32 range are accepted. Null, Boolean, decimal, floating-point, string, pointer and out-of-range responses throw `InvalidOperationException` after the request; the method does not retry or undo a possibly started analysis.

`Analyze` uses the native no-argument entry point directly. Its native return is void, so a normal return reports neither success nor completion. It is not implemented by choosing flags for `AnalyzeEx`. Current application settings, native prompts and licensing still apply. Use the status APIs explicitly when their observations are needed.

Both requests can start analysis, create or change analysis output files and display native UI. They do not add a pre-save, filename/readiness check, result retrieval, polling, retry or rollback. Model ownership and engineering acceptance remain the caller's responsibility.

## Observing running state

`IsAnalyzing` accepts native Boolean values or supported integral 0/1 values. Other values, including integral -1, throw. A false result does not establish that a model has results or that its analysis completed successfully. It is a live application predicate, separate from cached wrapper metadata. It does not start analysis, wait, query a model filename or read output files.

## Querying a specified or current model

The explicit-path overload requires non-null, non-whitespace Windows path text and rejects invalid Windows path characters. It resolves relative paths against the process working directory before native access. It does not require the model to be open, check file existence or extension, parse result files or change the filesystem. A syntactically valid path can still receive a native InvalidModelPath response.

The current-model overload captures the working directory before reading `GetSTAADFile` once with a Boolean full-path flag and a string-shaped internal output. It rejects unwritten, null, non-string, empty or whitespace-only filenames before a status query. Relative native paths use the captured directory even if a native callback changes the process working directory. The subsequent query uses that captured path; an external model switch between reads is possible. Inspect `ModelFilePath` to see which path was queried.

Both overloads initialize internal warning/error buffers as Int32 and CPU time as Double, matching the native Windows long/double reference contract. No public reference outputs, tuples or mutable native buffers are exposed. Each call returns a new immutable `StaadPro.Interop.Core.Models.Root.AnalysisStatusResult`.

| Native code | `AnalysisStatus` | `IsSuccessful` | `IsCompleted` |
| --- | --- | --- | --- |
| -2 | InvalidModelPath | false | false |
| -1 | Terminated | true | false |
| 0 | GeneralError | false | false |
| 1 | InProgress | true | false |
| 2 | CompletedSuccessfully | true | true |
| 3 | CompletedWithWarnings | true | true |
| 4 | CompletedWithErrors | true | true |
| 5 | NotPerformed | true | false |
| Other signed Int32 | null; exact code in `NativeStatus` | false | false |

`IsSuccessful` means a recognized analysis state was retrieved. It includes terminated, running, completed-with-errors and not-performed states. It does not mean the analysis succeeded. `IsCompleted` includes completion with errors. Inspect the status and diagnostics explicitly before deciding whether results are usable.

## Counts, CPU time and missing outputs

`WarningCount`, `ErrorCount` and `CpuTimeSeconds` are nullable. The time is STAAD's reported CPU time in seconds, preserving Double precision; it is not a library wall-clock measurement. Zero is valid data.

The managed query starts count buffers at `Int32.MinValue` and time at `Double.NaN`. Completed states 2, 3 and 4 require both counts and a finite nonnegative CPU time. Missing or invalid required fields throw rather than producing fabricated zero values. Counts must be nonnegative; status/count consistency beyond these checks is not inferred.

For recognized noncompleted states -1, 1 and 5, each valid written field is retained independently. An unchanged count sentinel and NaN CPU time become null. The query cannot distinguish an unwritten time from an explicitly written NaN for those states. Other negative counts/times and infinite times throw. Failed queries (-2 or 0) and unknown status codes expose no output data, even if native buffers were written. Those status responses remain inspectable results; native dispatch failures remain exceptions.

These output rules are managed validation policy. The native help does not specify every transient-state buffer value. A retrieved snapshot does not establish result freshness, model/result agreement or cross-version compatibility.

## Example

With an owned connected wrapper on its constructing STA thread:

~~~csharp
var root = wrapper.Root;
bool requestExtendedAnalysis = false;
if (requestExtendedAnalysis)
{
    var request = root.AnalyzeEx(StaadPro.Interop.Core.Enums.SilentMode.Enable,
        StaadPro.Interop.Core.Enums.HiddenMode.Enable,
        StaadPro.Interop.Core.Enums.WaitAnalysisMode.Return);
    Console.WriteLine(request); // Inspect the code; Return does not mean completion.
}

bool requestDefaultAnalysis = false;
if (requestDefaultAnalysis) root.Analyze();

bool running = root.IsAnalyzing();
Console.WriteLine(running);
var status = root.GetAnalysisStatus(@"C:\Models\Frame.std");
Console.WriteLine($"{status.ModelFilePath}: {status.NativeStatus}");
if (status.IsCompleted)
    Console.WriteLine($"Warnings: {status.WarningCount}; errors: {status.ErrorCount}; CPU seconds: {status.CpuTimeSeconds}");

bool queryCurrentModel = false;
if (queryCurrentModel) Console.WriteLine(root.GetAnalysisStatus().Status);
~~~

This example does not introduce automatic polling. Coordinate observations and model edits according to the application's session ownership and threading model.

## Lifetime, errors and verification

Every operation uses the wrapper's constructing COM thread and current Root binding under one operation lease. The current-model status overload holds the same lease for both native reads. Disposed bindings throw `ObjectDisposedException`; stale/manual-noncurrent bindings, other-thread calls and reentrant Root/refresh/disposal operations throw `InvalidOperationException` before dispatch. The guard does not serialize raw-object calls, external UI or another wrapper.

All native calls are synchronous and may block. No worker-thread COM dispatch, alternate-session acquisition, implicit unit/cache/domain refresh, pre-save, retries or rollback are added. Native exceptions retain their identity and HRESULT; a request exception can follow native side effects. Reacquire `wrapper.Root` after an explicit `RefreshComObjects`. Result snapshots remain detached and immutable after later native changes.

The installed STAAD.Pro 2025 OpenSTAAD Root help documents the Analyze/AnalyzeEx entry points, flag order, status codes, IsAnalyzing 0/1 return and GetAnalysisStatus Int32/Double reference outputs. Managed tests cover flag combinations, strict native shapes, every documented and unknown status, missing/invalid outputs, immutable snapshots, path capture, binding/thread/reentrancy guards and original errors. The installed NuGet consumer verifies all public contracts, supporting enum/model IntelliSense, examples and dependency isolation against a managed native stand-in. No live analysis was run and no new cross-version native qualification is claimed.
