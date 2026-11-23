# Static-check batches and plate-center stresses

These Output methods read from the application already captured by the wrapper. They reuse the existing `StaticCheckResult` and the scalar/context portion of the plate stress model. No model is analyzed, activated, edited or saved.

| Method | Behavior |
| --- | --- |
| `GetStaticCheckResults(loadCaseIds)` | One static-check observation per input occurrence; returns a new set of independent records. |
| `GetStaticCheckResults(loadCaseIds, nThreads)` | Same sequential native behavior, with a validated compatibility argument. |
| `GetStaticCheckResultsAsync(loadCaseIds)` | Same batch read, completed synchronously before returning a settled task. |
| `GetPlateCenterForces(loadCase, plate)` | Five local plate-center shear/membrane stresses for captured case and plate IDs. |
| `GetPlateCenterForcesAsync(loadCase, plate)` | Same plate read, completed before returning a settled task. |

## Choosing a static-check method

Use the existing `GetStaticCheckResultAsync(loadCaseId)` for one case. Use the batch method when all IDs should be captured and validated before the first native read.

The non-null ID sequence is enumerated once. Every ID must be positive and at most **100,000 occurrences** are accepted, counting duplicates. Empty input returns a fresh empty set without native access. Invalid IDs, excessive input or a failing enumerator throw before any native calls.

Each occurrence then makes one `GetStaticCheckResult` call in captured input order. **Duplicate IDs are intentionally queried repeatedly.** The result set uses result-object identity, so repeated IDs produce separate records rather than being deduplicated. HashSet iteration order is unspecified. This differs from unique-ID load inventories and combination selection. Re-enumerating the returned set performs no COM work.

Native retrieval must return Boolean true or integral one and supply two completely written finite six-Double arrays, ordered Fx, Fy, Fz, Mx, My, Mz. Both arrays start as distinct NaN sentinels. Replacement arrays and nonzero lower bounds are accepted; output storage is copied independently even if the native arrays alias. Valid written zero totals remain zero; failed or unwritten data never become apparent zero loads.

All records are qualified before a set is returned. Failure on a later case stops further calls and no partial set escapes. Earlier native reads cannot be rolled back, and the sequence is not an atomic model snapshot.

Returned `StaticCheckResult` and `Forces` records remain editable, detached data. `Differentials` computes literal current loads minus reactions, not an equilibrium test or balance tolerance. Native axes, units, signs and moment reference point are retained; no engineering acceptance is implied.

## Thread-count compatibility

The overload with `nThreads` retains the established parameter/signature and accepts **1 through 512**. Invalid values fail before ID enumeration. It is a compatibility hint: **every native call remains sequential on the owning COM thread**, regardless of the value.

Use the overload without `nThreads` for new code. Increasing the value does not create workers or improve native concurrency. The implementation does not use PLINQ, concurrent collections or worker-thread COM.

## Plate-center stresses, despite the Forces name

`GetPlateCenterForces` calls native `GetAllPlateCenterForces` once with captured positive plate/case IDs. The case's title, engineering type and family are not read. The plate needs only an Id; corners and geometry are not hydrated.

The inherited name refers to **stresses in native force/length-squared units**, not nodal forces or plate moments:

| Component | Native local meaning |
| --- | --- |
| `Sqx` | Shear on the local X face in the local Z direction. |
| `Sqy` | Shear on the local Y face in the local Z direction. |
| `Sx` | Normal membrane stress in local X. |
| `Sy` | Normal membrane stress in local Y. |
| `Sxy` | Shear in the local XY plane. |

Requires Boolean true/integral one success followed by exactly five finite written Double values in this order. The rank-one array may be replaced or have nonzero bounds; it is copied into scalar properties. False/zero, malformed flags, missing/wrongly shaped arrays, unwritten values, NaN and infinities throw. No area multiplication, stress-to-force conversion, global rotation or principal-stress interpretation is performed.

The returned `PlateCenterForces` derives from `PlateStress` and implements `IPlateStress`. It retains the original load-case and plate metadata references; scalar components are independent of native storage. Metadata objects remain caller-owned and mutable: later ID edits can differ from the identity actually queried. Keep requested IDs externally if immutable provenance is needed.

Manual constructors and scalar setters preserve editable, unvalidated semantics and do not send native changes or raise result notifications. The supporting `Plate` type currently stores detached Id with managed property notifications; it does not provide complete geometry, corner, connectivity or conversion APIs. The stress model ports context/scalar storage, not unit-service/WPF conversion methods.

## Task and session lifetime

Task variants perform input capture, native work and qualification **before returning** an already successful or faulted task. Invocation can block. Await observes the original validation/native exception and HRESULT. There is no Task.Run, apartment switch, UI dispatcher, cancellation or parallel COM.

All five methods use one shared current Output operation lease. Constructing-thread, disposal, stale adapters after refresh, manually constructed noncurrent adapters and reentrant Root/Load/Output/refresh/disposal calls are checked before native access. Obtain `wrapper.Output` again after refreshing COM objects.

Raw native access, other wrappers and external STAAD UI actions are not coordinated. Serialize shared-session/model edits. No automatic analysis, case activation, selection, save, conversion, fallback instance, retry or rollback is added. Compatible native results and licensing must already be available.

## C# example

~~~csharp
var checks = wrapper.Output.GetStaticCheckResults(new[] { 1, 2, 1 });
Console.WriteLine(checks.Count); // three independent observations, including repeated case 1

var compatible = wrapper.Output.GetStaticCheckResults(new[] { 1, 2 }, nThreads: 2);
Console.WriteLine(compatible.Count); // still sequential COM

var taskChecks = wrapper.Output.GetStaticCheckResultsAsync(new[] { 1, 2 });
foreach (var check in taskChecks.GetAwaiter().GetResult())
    Console.WriteLine(check.LoadCaseId + ": " + check.Forces.Fy);

var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 5 };
var stresses = wrapper.Output.GetPlateCenterForces(loadCase, plate);
Console.WriteLine(stresses.Sqx + ": " + stresses.Sxy); // local stress components

var taskStresses = wrapper.Output.GetPlateCenterForcesAsync(loadCase, plate);
Console.WriteLine(taskStresses.GetAwaiter().GetResult().Sx);
~~~

Offline tests exercise statuses, buffer types/shapes/writes, all stress positions, signed/extreme/zero values, repeated IDs, input limits, snapshot timing, ownership, tasks, lifetime guards and exact XML/contracts. Installed-package checks compile these examples and run managed native stand-ins separately. No live native static-check/plate read or new native-build/engineering qualification was performed.
