# Member batches and task-based Output queries

`GetMemberSteelUtilizationRatioAsync(Beam b)` captures one positive analytical `b.Id` and reads the same critical dimensionless steel ratio as the integer overload. Endpoints, geometry and the existing ratio are unused. It does not populate the Beam. All task APIs here execute synchronously on the constructing COM thread before returning an already-completed or already-faulted task. Await preserves original validation/native exceptions and HRESULTs. They do not offer nonblocking COM work, worker dispatch, parallel COM or cancellation. Calling through `Task.Run` fails the thread guard.

`GetMemberEndForcesAsync(string lcTitle, int beamId, int loadCaseId, MemberEnd memberEnd)` returns `Task<MemberForceQueryResult>`, consistent with the synchronous Core force contract. This named result preserves native success/failure instead of manufacturing a bare force record after a failed read. Positive member/case IDs, non-null literal display title and defined Start/End selector are required. A single native query explicitly requests Local axes. Boolean/integral zero-one status is qualified; false retains identity with `IsSuccessful=false` and null `Forces`. Success requires six written finite Doubles, with replacement/nonzero-bound arrays supported. Ordering is Fx,Fy,Fz,Mx,My,Mz; native signs/units are retained without conversion, availability/title queries or case activation.

## Steel-ratio batches

`GetMemberSteelUtilizationRatios(HashSet<Beam> beams)` and `GetMemberSteelUtilizationRatiosAsync(HashSet<Beam> beams)` return new editable dictionaries from original Beam references to qualified ratio observations. The synchronous and task versions share the same sequential query path. No Beam ratio property is populated by these methods.

The entire HashSet is enumerated once into original references and captured IDs before any native read. Null collection/entries, nonpositive IDs, duplicate current IDs or duplicate references throw, including duplicates created by stale mutable hash keys or a custom comparer. At most 100,000 members are accepted. Empty valid input produces a fresh empty dictionary without native calls. HashSet order is unspecified; captured membership does not follow later collection additions/removals. Caller must coordinate edits.

Each selected current ID is checked before its native query, then all selected IDs are rechecked after retrieval. An observed identity change throws with no partial dictionary returned. Transient changes restored before a check cannot be detected. Returned dictionaries deliberately use reference equality, so changing a retained Beam.Id later does not corrupt dictionary lookup or merge distinct references. A different Beam object with an equal ID is not a returned dictionary key. Dictionary storage/values are independent of subsequent calls and native buffers; the keys remain original editable Beam references.

Each native `GetMemberSteelDesignRatio` call requires Boolean true or integral one status and a written finite Double output, starting with a NaN sentinel. False/zero fails even if output was written. Nonnegative ratios and documented -1 (analysis not performed)/-999 (analyzed but member not designed) sentinels are preserved; unknown negative values throw. Results refer to the last parameter block used to design each member. Sentinel values are not passing design results; availability, freshness and engineering acceptance cannot be inferred. See [steel-design queries](Steel-Design-Queries.md).

## Detached population

`PopulateMemberSteelUtilizationRatios(HashSet<Beam> forBeams)` captures and qualifies the same bounded input and retrieves every ratio before assigning any value. Validation, native retrieval or qualification failure makes no ratio writes by this method. After successful reads and complete ID rechecks, assignments to captured `Beam.UtilizationRatio` properties occur sequentially, rechecking each ID immediately before its assignment.

Property notifications run synchronously after storing a changed value, while the same operation lease remains held. Handlers cannot reenter guarded Root/Load/Output/refresh/disposal work. A handler can throw or edit metadata after assignments start; the original handler exception propagates and earlier/current assignments may remain. A later ID change can stop subsequent writes. This is not an atomic transaction, and no automatic rollback of arbitrary handler edits is attempted. Population changes detached managed data only and never submits a design or model change to STAAD.

`Beam` supplies default, endpoint-pair and numbered-endpoint constructors, editable `Id`/`UtilizationRatio`, and retained endpoint references. Constructor parameter names/order match the source contracts. Constructors do not initialize release/load data, register node connectivity, calculate axes or validate native existence. Output queries need only the positive ID. Equality/operators/hash use mutable ID; ordinary HashSet/dictionary placement requires remove/reinsert before changing an ID. Batch results use reference-key dictionaries specifically to avoid that hazard. An initial ratio of zero is unpopulated metadata, not a verified design result. This support type does not imply the complete source Beam geometry/design API is available.

~~~csharp
var beam = new StaadPro.Interop.Core.Entities.Beam { Id = 7 };
var beams = new System.Collections.Generic.HashSet<StaadPro.Interop.Core.Entities.Beam> { beam };
var ratioTask = wrapper.Output.GetMemberSteelUtilizationRatioAsync(beam);
Console.WriteLine(ratioTask.GetAwaiter().GetResult());
var ratios = wrapper.Output.GetMemberSteelUtilizationRatios(beams);
Console.WriteLine(ratios[beam]);
var batchTask = wrapper.Output.GetMemberSteelUtilizationRatiosAsync(beams);
Console.WriteLine(batchTask.GetAwaiter().GetResult().Count);
wrapper.Output.PopulateMemberSteelUtilizationRatios(beams);
Console.WriteLine(beam.UtilizationRatio);
var forces = wrapper.Output.GetMemberEndForcesAsync("Dead", 7, 9,
    StaadPro.Interop.Core.Enums.MemberEnd.End).GetAwaiter().GetResult();
if (forces.IsSuccessful) Console.WriteLine(forces.Forces);
~~~

All five APIs use the current Output binding and one shared constructing-thread/lifetime/reentrancy lease spanning validation, native calls and population. Reacquire Output after refresh. Stale/manual-noncurrent, disposed, other-thread and reentrant operations fail before native dispatch. Calls may block and exceptions propagate, or fault returned tasks. Raw objects, other wrappers, external UI and concurrent metadata edits remain uncoordinated. Sequential observations are not atomic. No analysis, design, activation, selection, save, conversion, retry or native mutation is added. Managed regression and installed-consumer checks do not replace native-build or engineering qualification.
