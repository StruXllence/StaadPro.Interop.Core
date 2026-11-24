# Plate-corner forces and sequential selection

Use corner queries for native nodal forces and moments at analytical plate incidence nodes. Center queries return different quantities: local stresses, surface/principal stresses and moments per width. Corner output is copied in Fx/Fy/Fz/Mx/My/Mz order, retaining native axes, signs and base force/length units. No rotation, averaging, stress conversion or equilibrium/design acceptance is introduced.

| API | Behavior |
| --- | --- |
| GetPlateCornerForces(ILoadCase loadCase, Plate plate, Node cornerNode) | One resolved corner. An incomplete supplied plate is hydrated into a new detached plate through the acquired Geometry binding. |
| GetPlateCornerForcesAsync(ILoadCase loadCase, Plate plate, Node cornerNode) | The same work, then an already settled task. |
| GetPlateCornerForces(ILoadCase loadCase, Plate plate) | One observation per distinct supplied A/B/C/D node ID; requires A/B/C metadata and performs no hydration. |
| GetPlateCornerForcesAsync(ILoadCase loadCase, Plate plate) | The same complete indexed result set, then a settled task. |
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
var center = wrapper.Output.GetPlateCenterResults(new[] { loadCase }, new[] { plate }, 1);
Console.WriteLine(System.Linq.Enumerable.First(center).Moments.Mx);
~~~

## Incidence resolution and hydration

The corner argument supplies a **positive node number, not ordinal 1/2/3/4**. Single-corner queries capture positive load/plate/requested-node IDs once, then select the first A/B/C/D incidence reference with the requested ID. The returned node can differ from the supplied node object of the same ID. Supplied incidence requires A/B/C; D can be null or repeat an ID for a triangle. Every non-null supplied incidence ID must be positive; their coordinates are unused and are not geometry validity evidence.

If A/B/C is incomplete, the single-corner query obtains incidence through the acquired raw Geometry binding and hydrates each distinct positive node once. It creates new detached metadata without modifying the supplied plate. Incidence requires **integral zero success**, written positive A/B/C IDs and nonnegative D; D zero means absent. Coordinate retrieval is documented **void** and qualifies written finite Double XYZ outputs using NaN sentinels. It does not invent a Boolean return value. Hydrated coordinates retain native global coordinate values. Original native/dispatch errors propagate; incomplete or failed hydration does not fall back to the supplied partial geometry or dispatch a force query.

The all-corners overload instead requires supplied incidence and validates/captures it before force calls. It queries each distinct node ID once in A/B/C/D encounter order, retaining the first reference for repeated IDs. Geometric degeneracy, planarity and native membership of supplied metadata are not certified. These metadata members extend Plate without porting the complete Geometry API.

## Native success and independent values

Force retrieval calls GetPlateCornerForces(plateId, cornerNodeId, loadCaseId, ref buffer). Unlike incidence retrieval, it requires Boolean true or integral one. NaN sentinels detect missing writes; every result must be an exact rank-one six-element Double array containing finite components. In-place, replaced and nonzero-bound buffers are supported and copied. Written zero, signed and extreme finite components remain literal data. No integer/string coercion or initialized-zero fallback is used. A failed later corner stops processing and returns no partial set.

Components remain in the native output coordinate convention; no coordinate-selector argument or additional transform is passed. Coordinate-specific transformation APIs require separate native qualification. These queries preserve the reported values without assuming an extra rotation.

## Captured keys and mutable metadata

PlateCornerForces extends the existing Forces model, including its scalar notifications. Case, plate and resolved corner references remain editable and shared. Its Key property computes an immutable PlateCornerForcesKey from **current** metadata and may throw if it lacks positive IDs. Manual constructors/factories copy six literal components without finite-value validation or native access.

PlateCornerForcesResultSet owns read-only collection storage and indexes immutable keys captured for native requests. Native callbacks or later user edits cannot redirect remaining captured requests or rewrite those indexes. Returned references and scalar values remain editable. Lookups and identifier lists continue to describe the original observation IDs even when a result's current Key differs. Indexers throw for absent keys; GetForLoadCase/GetForPlate/GetForCornerNode return empty lists for valid absent IDs. Combined filters also use captured keys. Manual set construction captures current positive metadata once and rejects null/incomplete/duplicate entries; at most 100,000 supplied records are accepted. No public reference-output lookup, unit-service conversion or transformation model APIs are included in this supporting subset.

## Thread, lifetime and task behavior

All five functions hold one current Output binding, lifetime, constructing-thread and reentrancy lease across argument capture, selection, optional hydration, force calls and qualification. Reacquire the adapter after refresh. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal operations fail before native work. Raw access, other wrappers and external UI remain outside that coordination.

Task-shaped calls execute synchronously before returning settled success/fault tasks; invocation can block and awaiting preserves original failures/HRESULTs. There is no Task.Run, concurrent COM, apartment switch, dispatcher or cancellation. The thread-count compatibility hint does not create workers; invalid hints fail before input enumeration. Existing 100,000-occurrence and Cartesian-pair bounds, duplicate observations and case-major ordering remain in force for center selections.

These are read-only observations. No analysis, case activation, selection, save, fallback, replay, unit conversion or native mutation occurs. Metadata capture and sequential results are not atomic and do not certify freshness, every native build or engineering suitability. Callers coordinate concurrent model/metadata writers.
