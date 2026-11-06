# Live Root observations

These five parameterless methods query the native root already bound to `wrapper.Root`. Each makes exactly one native call; none acquires another session, opens/saves a model, scans processes or refreshes wrapper metadata. They complement the five [cached metadata observations](Root-Metadata.md), bringing Core to ten common Root contracts.

| Managed signature | Native operation | Result and validation |
| --- | --- | --- |
| `string GetStaadFileFullPath()` | `GetSTAADFile(ref output, true)` | Exact written path; an explicitly written empty string is retained. Unwritten, null and non-string outputs throw. |
| `string GetStaadFileFolder()` | `GetSTAADFileFolder(ref output)` | Exact native folder, including trailing separators and whitespace. Unwritten, null and non-string outputs throw. |
| `bool IsPhysicalModel()` | `IsPhysicalModel()` | Boolean or integral zero/one only. Every other shape/code throws. |
| `string GetErrorMessage()` | `GetErrorMessage()` | Exact native message, including empty text, Unicode and line breaks. Null/non-string responses throw. |
| `uint GetProcessId()` | `GetProcessId()` | Positive UInt32-range integral response. Zero, negative, overflowing and non-integral responses throw. |

There are no public reference-output arguments, tuples or artificial success flags. Valid observations are returned directly; malformed responses produce `InvalidOperationException`. A native COM failure retains its original exception and HRESULT. A missing required late-bound member is reported rather than converted to a default value. No retries or automatic `GetErrorMessage()` follow-up obscure the original failure.

## Usage

With a connected wrapper on its constructing STA thread:

~~~csharp
var root = wrapper.Root;
string modelPath = root.GetStaadFileFullPath();
string modelFolder = root.GetStaadFileFolder();
bool isPhysical = root.IsPhysicalModel();
uint processId = root.GetProcessId();
Console.WriteLine($"PID {processId}: '{modelPath}' in '{modelFolder}', physical={isPhysical}");

// Read the current native error only when needed; it is not error history.
string nativeMessage = root.GetErrorMessage();
Console.WriteLine(nativeMessage);
~~~

## Choosing the right observation

`GetStaadFileFullPath()` reads the current native filename. Acquisition's `ActualPath` describes the earlier acquisition outcome and may differ after an external model change. The method requests full-path inclusion with a Boolean, as specified by OpenSTAAD. It does not normalize the returned text, require an absolute path, consult the filesystem or change the working directory.

`GetStaadFileFolder()` calls the native folder member directly. It does not derive the folder with `Path.GetDirectoryName()` or make a second filename query; the reported directory may differ in spelling, separator style or trailing separator. The native help describes a string written through the folder argument despite labeling it as an input parameter. The managed API exposes that written value as its return.

Both reference-output calls begin with a nonempty, per-call BSTR sentinel. An unchanged sentinel is an unwritten output and is rejected. An explicitly written empty string remains empty. Empty path/folder text alone does not prove a reusable blank: acquisition additionally checks the available interfaces, seven zero entity/load counts and process/model identity. Null, DBNull and other response types are not treated as empty models.

`IsPhysicalModel()` reports the native predicate; false does not prove an analytical model is loaded or ready. The method does not convert between physical and analytical representations. Numeric zero/one and actual Boolean values are accepted; other integers, Boolean-like strings, floating-point values, decimals, enum and pointer values are rejected.

`GetErrorMessage()` reads the application's current native error text on explicit request. Empty text does not prove success. The vendor contract does not specify whether reading consumes or clears the error, so stable repeated reads, error history and a nondestructive peek are not promised. Another session operation can replace the error before it is read. This is different from catching a managed exception and is never invoked implicitly to decorate another method's exception.

`GetProcessId()` is a live PID query. `GetMainWindowHandle()` is a cached, borrowed HWND; neither substitutes for the other or transfers process/window ownership. Process IDs can expire or be recycled. The accepted range is 1 through UInt32.MaxValue using built-in signed/unsigned integer representations; no Process object or liveness check is introduced.

## Thread, lifetime and observation limits

All ten Root methods share the existing wrapper operation lease. Use the constructing STA thread and coordinate all operations on the same native session. Wrong-thread calls, nested Root queries and callbacks attempting refresh/disposal are rejected before further native dispatch. The lease is released after success, validation failures and native exceptions. This managed gate does not serialize independently created wrappers or raw-object callers.

`RefreshUnits()` keeps Root usable. `RefreshComObjects()` replaces Root, so reacquire `wrapper.Root` afterward; an old adapter throws even if its native root object is unchanged. Disposal causes `ObjectDisposedException`. A separately constructed adapter is not the current wrapper binding and cannot bypass validation. Raw `ComObject` access bypasses these protections.

Separate calls are separate snapshots, not an atomic model/process record. External model changes can occur between calls. None certifies model readiness, supports a hard native-call deadline or changes the session intentionally. Native error-state side effects remain vendor-defined.

## Source mapping and Core decisions

The ReInvented source was inspected strictly read-only. Its `GetStaadFileFullPath()` is the original workflow; the Framework project strengthens it with BSTR-output validation and Boolean full-path inclusion. The other four methods come from the completed Framework Root expansion. All five retain their Framework signatures and return types under Core namespaces. No unrelated source domain implementations were restored.

Core reuses `OpenStaadModelIdentityReader.ReadPath()` for filename handling, avoiding a second inconsistent definition of a blank filename. The internal `RootCommonQueryService` contains folder/classification/error/PID dispatch and reuses the existing `NativeValues` validators. `IOSRoot` and its single non-partial `OSRootAdapter` remain the public entry points. No new public types or overload families are needed for parameterless observations.

The installed 2024, 2025 and 2026 Root manuals describe the same native contracts for these operations. One internal common service is appropriate; speculative version subclasses would add no behavior. This documentation comparison is not live cross-version qualification.

Modernization preserves one native call per observation, uses static guard delegates, shares existing validation and keeps COM on the caller's context. No Task.Run, thread-pool dispatch, unnecessary process lookup or filesystem access is added. Native round-trip cost dominates these small projections; no unmeasured throughput claim is made.

## Verification and remaining work

The 181 new managed cases cover exact/empty/Unicode/whitespace text, BSTR initialization and untouched outputs, non-string rejection, current-root identity and external changes, every built-in integer representation, Boolean/PID boundaries, malformed coercible values, original COM exceptions, missing members, stale/disposed/manual bindings, wrong-thread access, callbacks during refresh and nested query/lifecycle operations. Matching expanded interface/concrete XML is checked for every new method.

Together with the 175 existing cases, **356 managed tests pass**. The installed-package verifier exercises all ten Root methods, compiles **24 XML examples**, README usage and both Root guides, and checks the restored archive, installed XML and dependency isolation. Examples compile without running their native operations.

No new live native qualification was performed for this increment. The earlier foundation's limited read-only STAAD.Pro 26.00.01.491 observations are historical evidence only. Blank-session behavior, native error-state behavior, physical/analytical models and servicing builds still need controlled native acceptance. Core has **43 common and three optional Root contracts pending**; other domains remain binding-only.
