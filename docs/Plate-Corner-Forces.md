# Plate-corner forces and sequential selection

Use corner queries for native nodal forces and moments at analytical plate incidence nodes. Center queries return different quantities: local stresses, surface/principal stresses and moments per width. Corner output is copied in Fx/Fy/Fz/Mx/My/Mz order, retaining native axes, signs and base force/length units. No rotation, averaging, stress conversion or equilibrium/design acceptance is introduced.

| API | Behavior |
| --- | --- |
| GetPlateCornerForces(ILoadCase loadCase, Plate plate, Node cornerNode) | One resolved corner. An incomplete supplied plate is hydrated into a new detached plate through the acquired Geometry binding. |
| GetPlateCornerForcesAsync(ILoadCase loadCase, Plate plate, Node cornerNode) | The same work, then an already settled task. |
| GetPlateCornerForces(ILoadCase loadCase, Plate plate) | One observation per distinct supplied A/B/C/D node ID; requires A/B/C metadata and performs no hydration. |
| GetPlateCornerForcesAsync(ILoadCase loadCase, Plate plate) | The same complete indexed result set, then a settled task. |
| GetPlateCornerForces(IEnumerable<ILoadCase> loadCases, Plate plate, Node cornerNode) | One observation per unique load-case ID at one supplied corner; requires A/B/C metadata and performs no hydration. |
| GetPlateCornerForcesAsync(IEnumerable<ILoadCase> loadCases, Plate plate, Node cornerNode) | The same deduplicated corner result set, then a settled task. |
| GetPlateCornerForces(IEnumerable<ILoadCase> loadCases, Plate plate) | Unique load-case by unique plate-corner observations in case-major, incidence order; requires A/B/C metadata and performs no hydration. |
| GetPlateCornerForcesAsync(IEnumerable<ILoadCase> loadCases, Plate plate) | The same case-by-corner indexed result set, then a settled task. |
| GetPlateCornerForces(ILoadCase loadCase, IEnumerable<Plate> plates) | Unique plate by unique plate-corner observations in plate-major, incidence order; requires A/B/C metadata on every plate and performs no hydration. |
| GetPlateCornerForcesAsync(ILoadCase loadCase, IEnumerable<Plate> plates) | The same single-case plate selection, then a settled task. |
| GetPlateCornerForces(IEnumerable<ILoadCase> loadCases, IEnumerable<Plate> plates) | Cartesian product of unique cases, unique plates and each plate's distinct supplied corners, in that order. |
| GetPlateCornerForcesAsync(IEnumerable<ILoadCase> loadCases, IEnumerable<Plate> plates) | The same complete Cartesian result set, then a settled task. |
| GetPlateCornerForces(IEnumerable<PlateCornerForcesRequest> requests) | Only explicitly requested case/plate/node triples, deduplicated by the complete key in first-occurrence order. |
| GetPlateCornerForcesAsync(IEnumerable<PlateCornerForcesRequest> requests) | The same sparse result set, then a settled task. |
| GetPlateCornerForces(int loadCaseId, int plateId, int cornerNodeId) | Hydrates plate/case context using the wrapper's current Geometry/Load; searches all supported case families. |
| GetPlateCornerForcesAsync(int loadCaseId, int plateId, int cornerNodeId) | The same complete hydration/query, then a settled task. |
| GetPlateCornerForces(int loadCaseId, int plateId, int cornerNodeId, IOSGeometry geometry, IOSLoad load) | The same default-family query with explicitly supplied current adapters from this wrapper. |
| GetPlateCornerForcesAsync(int loadCaseId, int plateId, int cornerNodeId, IOSGeometry geometry, IOSLoad load) | The same explicit-binding query, then a settled task. |
| GetPlateCornerForces(int loadCaseId, int plateId, int cornerNodeId, LoadCaseType loadCaseType, IOSGeometry geometry, IOSLoad load) | Hydrates context and searches only the specified primary, combination or reference family. |
| GetPlateCenterResults(IEnumerable<ILoadCase> loadCases, IEnumerable<Plate> plates, int nThreads) | Source-compatible hint 1..512; reuses bounded sequential center queries. Prefer the overload without the hint for new code. |

~~~csharp
var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
var a = new StaadPro.Interop.Core.Entities.Node(11, 0, 0, 0);
var plate = new StaadPro.Interop.Core.Entities.Plate(a,
    new StaadPro.Interop.Core.Entities.Node(12, 1, 0, 0),
    new StaadPro.Interop.Core.Entities.Node(13, 0, 0, 1)) { Id = 17 };
var one = wrapper.Output.GetPlateCornerForces(loadCase, plate, a);
Console.WriteLine(one.Fx);
var all = wrapper.Output.GetPlateCornerForces(loadCase, plate);
Console.WriteLine(all[1, 17, 11].Mz);
foreach (var result in all.GetForPlate(17)) Console.WriteLine(result.CornerNodeId);
var completed = wrapper.Output.GetPlateCornerForcesAsync(loadCase, plate, a);
Console.WriteLine(completed.GetAwaiter().GetResult().Fy);
var completedSet = wrapper.Output.GetPlateCornerForcesAsync(loadCase, plate);
Console.WriteLine(completedSet.GetAwaiter().GetResult().Count);
var byCasesCorner = wrapper.Output.GetPlateCornerForces(new[] { loadCase }, plate, a);
Console.WriteLine(byCasesCorner[1, 17, 11].Fx);
var byCasesCornerTask = wrapper.Output.GetPlateCornerForcesAsync(new[] { loadCase }, plate, a);
Console.WriteLine(byCasesCornerTask.GetAwaiter().GetResult().Count);
var byCasesPlate = wrapper.Output.GetPlateCornerForces(new[] { loadCase }, plate);
Console.WriteLine(byCasesPlate.GetForLoadCaseAndPlate(1, 17).Count);
var byCasesPlateTask = wrapper.Output.GetPlateCornerForcesAsync(new[] { loadCase }, plate);
Console.WriteLine(byCasesPlateTask.GetAwaiter().GetResult().Count);
var byCasePlates = wrapper.Output.GetPlateCornerForces(loadCase, new[] { plate });
Console.WriteLine(byCasePlates.PlateIds.Count);
var byCasePlatesTask = wrapper.Output.GetPlateCornerForcesAsync(loadCase, new[] { plate });
Console.WriteLine(byCasePlatesTask.GetAwaiter().GetResult().Count);
var cartesian = wrapper.Output.GetPlateCornerForces(new[] { loadCase }, new[] { plate });
Console.WriteLine(cartesian[1, 17, 11].Fx);
var cartesianTask = wrapper.Output.GetPlateCornerForcesAsync(new[] { loadCase }, new[] { plate });
Console.WriteLine(cartesianTask.GetAwaiter().GetResult().Count);
var requests = new[] { new StaadPro.Interop.Core.Models.PlateCornerForcesRequest(loadCase, plate, a) };
var sparse = wrapper.Output.GetPlateCornerForces(requests);
Console.WriteLine(sparse[1, 17, 11].Fx);
var sparseTask = wrapper.Output.GetPlateCornerForcesAsync(requests);
Console.WriteLine(sparseTask.GetAwaiter().GetResult().Count);
var hydrated = wrapper.Output.GetPlateCornerForces(1, 17, 11);
Console.WriteLine(hydrated.LoadCase.Title);
var hydratedTask = wrapper.Output.GetPlateCornerForcesAsync(1, 17, 11);
Console.WriteLine(hydratedTask.GetAwaiter().GetResult().Fx);
var explicitBindings = wrapper.Output.GetPlateCornerForces(1, 17, 11, wrapper.Geometry, wrapper.Load);
Console.WriteLine(explicitBindings.Plate.Id);
var explicitTask = wrapper.Output.GetPlateCornerForcesAsync(1, 17, 11, wrapper.Geometry, wrapper.Load);
Console.WriteLine(explicitTask.GetAwaiter().GetResult().Mz);
var primaryOnly = wrapper.Output.GetPlateCornerForces(1, 17, 11,
    StaadPro.Interop.Core.Enums.LoadCaseType.PrimaryLoad, wrapper.Geometry, wrapper.Load);
Console.WriteLine(primaryOnly.LoadCase.CaseType);
var center = wrapper.Output.GetPlateCenterResults(new[] { loadCase }, new[] { plate }, 1);
Console.WriteLine(System.Linq.Enumerable.First(center).Moments.Mx);
~~~

## Incidence resolution and hydration

The corner argument supplies a **positive node number, not ordinal 1/2/3/4**. Single-corner queries capture positive load/plate/requested-node IDs once, then select the first A/B/C/D incidence reference with the requested ID. The returned node can differ from the supplied node object of the same ID. Supplied incidence requires A/B/C; D can be null or repeat an ID for a triangle. Every non-null supplied incidence ID must be positive; their coordinates are unused and are not geometry validity evidence.

If A/B/C is incomplete, the single-case/single-corner query obtains incidence through the acquired raw Geometry binding and hydrates each distinct positive node once. It creates new detached metadata without modifying the supplied plate. Incidence requires **integral zero success**, written positive A/B/C IDs and nonnegative D; D zero means absent. Coordinate retrieval is documented **void** and qualifies written finite Double XYZ outputs using NaN sentinels. It does not invent a Boolean return value. Hydrated coordinates retain native global coordinate values. Original native/dispatch errors propagate; incomplete or failed hydration does not fall back to the supplied partial geometry or dispatch a force query.

The all-corners and batch-selection overloads instead require supplied incidence and validate/capture all inputs before force calls, even when a sequence is empty. Sequences are enumerated once, bounded to 100,000 occurrences and 100,000 unique observations, deduplicated by captured ID while retaining first references and original order, and checked across every entry including duplicate plate entries. Geometric degeneracy, planarity and native membership of supplied metadata are not certified. These metadata members extend Plate without porting the complete Geometry API.

## ID-based hydration and case families

Use ID overloads when only case, plate and corner node numbers are available. They hydrate a fresh detached plate/corner graph and case record rather than requiring caller metadata. In contrast, entity-based single-corner queries reuse supplied complete incidence, while all-corner/collection/sparse queries require it and never hydrate. Each ID must be positive; corner IDs are node numbers rather than slot ordinals.

Default ID queries use this wrapper's current adapters and search **primary, combination, reference** inventories in that precedence. All three families, including metadata for unrequested IDs, are validated even after a match. The first matching family supplies case context; missing/invalid data in a later family still fails. The explicit-family overload limits discovery to one family and rejects RepeatLoad/undefined values before native calls. Family selection affects metadata; the force API receives a case ID without a family selector.

Explicit Geometry/Load arguments must be the exact current adapters of this Output wrapper. Foreign/custom, manually reconstructed and replaced adapters are rejected before native access, including adapters sharing an equal raw native object. This prevents accidentally combining different model contexts; it is not an injection API. Reacquire all three adapters after refresh.

The sequence is scalar/family/ownership validation, native incidence, each distinct corner's coordinates, corner membership, complete selected-family discovery/metadata and finally one force call. Incidence requires integral-zero success and valid written A/B/C/D; coordinates use documented-void retrieval with finite written Double XYZ. Repeated corners share hydrated node references. Each load family is bounded to 100,000 entries; positive unique Int32 IDs and operation-specific Boolean/count semantics are preserved by the existing Load reader. No synthetic title/case, missing-data fallback, public adapter reentry or partial force record is introduced.

The full sequence runs under one current Output/owning-thread/lifetime/reentrancy lease. Returned context and scalar data remain editable; each call creates independent metadata and component storage. Native/external edits remain outside this lease, so observations are not atomic or a result-freshness guarantee. Transport/missing-signature errors propagate unchanged. Task variants complete all work on the caller before returning settled tasks; no worker COM, additional model acquisition, selection, analysis, save, retry, transform or native mutation occurs.

## Cartesian or sparse selection

Use the two-sequence overload when every selected case is needed for every selected plate and every supplied corner. Cases are captured first, then plates, each sequence once. Positive case/plate IDs are deduplicated independently, retaining the first metadata reference. Output order is case, plate, then first A/B/C/D occurrence of each node ID. The product of unique cases and total unique plate-corner observations must not exceed 100,000; wide arithmetic prevents product overflow. Even an empty dimension still validates the other supplied sequence, including duplicate entries and incidence.

Use the request overload when only particular case/plate/node triples are needed. It avoids creating unrequested Cartesian combinations. PlateCornerForcesRequest stores editable references and has an empty constructor and a three-reference constructor; neither queries or validates STAAD. Its Key property computes the current positive IDs, throws on incomplete/nonpositive metadata and does not validate plate membership. It is not a query receipt. The computed key is excluded from built-in System.Text.Json serialization; no Newtonsoft.Json runtime dependency is introduced.

Sparse queries validate every occurrence, including duplicates, then deduplicate the complete captured case/plate/node key. Each first unique request retains its own case and plate context: different keys sharing a case or plate ID do not replace each other's metadata references. A requested node selects the first supplied incidence reference with the same ID; it does not require the identical node object. Requests require populated A/B/C metadata and never hydrate. Null requests/references, nonpositive IDs, missing incidence and non-incidence nodes fail before force calls.

Each sequence is limited to 100,000 occurrences, including duplicates. All request identities, references and incidence IDs are captured before native work, and enumeration failures dispose enumerators and propagate without a partial result set. Later edits cannot redirect the captured work. Untyped null literals can be ambiguous between collection overloads; use the intended typed variable or an explicit cast when testing null handling.

## Native success and independent values

Force retrieval calls GetPlateCornerForces(plateId, cornerNodeId, loadCaseId, ref buffer). Unlike incidence retrieval, it requires Boolean true or integral one. NaN sentinels detect missing writes; every result must be an exact rank-one six-element Double array containing finite components. In-place, replaced and nonzero-bound buffers are supported and copied. Written zero, signed and extreme finite components remain literal data. No integer/string coercion or initialized-zero fallback is used. A failed later corner stops processing and returns no partial set.

Components remain in the native output coordinate convention; no coordinate-selector argument or additional transform is passed. Coordinate-specific transformation APIs require separate native qualification. These queries preserve the reported values without assuming an extra rotation.

## Captured keys and mutable metadata

PlateCornerForces extends the existing Forces model, including its scalar notifications. Case, plate and resolved corner references remain editable and shared. Its Key property computes an immutable PlateCornerForcesKey from **current** metadata and may throw if it lacks positive IDs. Manual constructors/factories copy six literal components without finite-value validation or native access.

PlateCornerForcesResultSet owns read-only collection storage and indexes immutable keys captured for native requests. Native callbacks or later user edits cannot redirect remaining captured requests or rewrite those indexes. Returned references and scalar values remain editable. Lookups and identifier lists continue to describe the original observation IDs even when a result's current Key differs. Indexers throw for absent keys; GetForLoadCase/GetForPlate/GetForCornerNode return empty lists for valid absent IDs. Combined filters also use captured keys. Manual set construction captures current positive metadata once and rejects null/incomplete/duplicate entries; at most 100,000 supplied records are accepted. No public reference-output lookup, unit-service conversion or transformation model APIs are included in this supporting subset.

## Thread, lifetime and task behavior

All corner-selection functions hold one current Output binding, lifetime, constructing-thread and reentrancy lease across argument capture, selection, optional hydration, force calls and qualification. Reacquire the adapter after refresh. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal operations fail before native work. Raw access, other wrappers and external UI remain outside that coordination.

Task-shaped calls execute synchronously before returning settled success/fault tasks; invocation can block and awaiting preserves original failures/HRESULTs. There is no Task.Run, concurrent COM, apartment switch, dispatcher or cancellation. The thread-count compatibility hint does not create workers; invalid hints fail before input enumeration. Existing 100,000-occurrence and Cartesian-pair bounds, duplicate observations and case-major ordering remain in force for center selections.

These are read-only observations. No analysis, case activation, selection, save, fallback, replay, unit conversion or native mutation occurs. Metadata capture and sequential results are not atomic and do not certify freshness, every native build or engineering suitability. Callers coordinate concurrent model/metadata writers.
