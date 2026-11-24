# Plate-center composite results

Use the individual plate queries when only one result family is needed. Use `GetPlateCenterResults` to retrieve all four families for the same captured case and analytical plate IDs. These functions observe existing results; they do not run analysis or change STAAD.

| API | Result and execution |
| --- | --- |
| `GetPlatePrincipalStressesAsync(ILoadCase loadCase, Plate plate)` | One principal-stress read, returned in an already settled task. |
| `GetPlateCenterResults(ILoadCase loadCase, Plate plate)` | One `PlateCenterResults` containing four detached records. |
| `GetPlateCenterResultsAsync(ILoadCase loadCase, Plate plate)` | The same four reads, then a settled task. |
| `GetPlateCenterResults(IEnumerable<ILoadCase> loadCases, IEnumerable<Plate> plates)` | An eager read-only collection in case-major, plate-minor order. |
| `GetPlateCenterResultsAsync(IEnumerable<ILoadCase> loadCases, IEnumerable<Plate> plates)` | The same complete collection, then a settled task. |

~~~csharp
var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
var result = wrapper.Output.GetPlateCenterResults(loadCase, plate);
Console.WriteLine(result.Forces.Sx);       // Local membrane stress, not a nodal force.
Console.WriteLine(result.Moments.Mx);      // Local moment per unit width.
Console.WriteLine(result.VonMises.Top);    // Native top surface.
Console.WriteLine(result.Principal.SmaxTop);
var rows = wrapper.Output.GetPlateCenterResults(new[] { loadCase }, new[] { plate });
foreach (var row in rows) Console.WriteLine(row.Moments.Mxy);
var completed = wrapper.Output.GetPlateCenterResultsAsync(loadCase, plate);
Console.WriteLine(completed.GetAwaiter().GetResult().VonMises.Bottom);
var batchTask = wrapper.Output.GetPlateCenterResultsAsync(new[] { loadCase }, new[] { plate });
Console.WriteLine(System.Linq.Enumerable.Count(batchTask.GetAwaiter().GetResult()));
var principalTask = wrapper.Output.GetPlatePrincipalStressesAsync(loadCase, plate);
Console.WriteLine(principalTask.GetAwaiter().GetResult().SminBottom);
~~~

## Component meaning and observation order

Each composite calls `GetAllPlateCenterForces`, `GetAllPlateCenterMoments`, `GetPlateCenterVonMisesStresses` and `GetPlateCenterNormalPrincipalStresses`, in that order. `Forces` contains SQX/SQY/SX/SY/SXY local stresses in native force/length-squared units despite its historical name. `Moments` contains local MX/MY/MXY moment per unit width in native moment/length units. `VonMises` contains native top/bottom stresses. `Principal` contains maximum/minimum top/bottom normal stresses without recalculation or reordering. Native surface labels do not denote global elevation. No allowable stress, direction, Wood-Armer design or engineering acceptance is inferred.

Every native call must report Boolean true or integral one. Arrays must contain exactly five stress or three moment Doubles. Distinct scalar outputs and every array component begin with NaN sentinels; all must be completely written finite Doubles. Wrong status/type/shape, partial writes, NaN and infinity fail explicitly. Replaced or nonzero-bound arrays are copied independently; fully written zero, signed and extreme finite components remain literal data.

## Identity and collection behavior

Single queries capture both positive IDs once. Batch queries capture every occurrence/reference/ID before any native access: load cases first, plates second, each sequence once. Duplicates remain independent observations. Input order defines a case-major Cartesian product: cases [A, B], plates [P, Q] yield A/P, A/Q, B/P, B/Q. At most 100,000 occurrences in each input and 100,000 pairs are accepted. Both sequences are validated even if the other is empty; null sequences/entries, nonpositive IDs, enumeration failures and exceeded bounds fail before native access.

All four reads for each pair use the captured IDs even if caller-owned metadata changes in a callback. The returned context references are retained, not cloned. Later fields may disagree with queried IDs; callers needing immutable provenance should capture their own identifiers and coordinate writers. Four sequential observations are not an atomic solver snapshot. No freshness or cross-version engineering qualification follows from successful retrieval.

Batch results are materialized before return. Re-enumeration never queries STAAD; collection storage is read-only, but contained component records and metadata remain editable. A failed component/pair stops processing, publishes no partial result or collection and performs no retry/rollback of earlier reads.

## Task and binding semantics

All five functions hold one current Output binding, lifetime, constructing-thread and reentrancy lease across validation/enumeration and every native call. Reacquire the adapter after wrapper refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal operations fail before native work. Raw native access, other wrappers and external UI are outside the lease.

Task methods execute synchronously before returning a completed success/fault task; invocation can block. Await preserves original validation/native exceptions and HRESULTs. No worker COM, Task.Run, parallel Task.WhenAll, apartment switching, cancellation or dispatcher is introduced. Prefer synchronous methods when task composition is unnecessary. No analysis, activation, selection, save, geometry hydration, unit conversion or native mutation is performed.

## Manual container construction

`PlateCenterResults` also offers its source constructor and `Create` factory. These retain the four component instances without copying. A null component throws `ArgumentNullException`. Load cases must be the same reference, even if IDs match; distinct non-null plates with equal analytical IDs are accepted to preserve source compatibility. All-null manual contexts and manual nonpositive IDs/scalars remain unvalidated. Compatibility is checked once; later changes can break it. Projected `LoadCase` and `Plate` come from `Forces`. Changing a scalar or context does not update STAAD or raise scalar notifications. Full unit-service conversion and geometry/design helper APIs remain outside this model subset.
