# Steel-design records and governing physical members

`GetSteelDesignResultsForBeam(Beam beam, IEnumerable<ILoadCase> allLoadCases)` and its batch overload `GetSteelDesignResultsForBeams(IEnumerable<Beam> beams, IEnumerable<ILoadCase> allLoadCases)` use supplied case metadata. The two overloads taking `IOSLoad load` instead require the same wrapper's current Load adapter and discover qualified primary/combination metadata under the existing Output operation lease. Foreign, stale and manually constructed Load adapters are rejected. No nested public Load call bypasses or reenters the lease.

All selected beams and case metadata are captured/qualified before design reads, each sequence once. Limits are 100,000 beam occurrences and 100,000 case records. Beam IDs must be positive; endpoints and existing utilization are unused. Supplied cases need unique positive IDs, non-null literal titles and defined case/type enums. Ambiguous IDs across families throw. Automatic discovery includes primary and combination cases only. Its inventory/title/type failures propagate; no reference/repeat or factor inference is added.

Batch results are immediately materialized editable lists through `IEnumerable<MemberSteelDesignResults>`, preserving input order and repeated beam occurrences. Each occurrence is queried independently. Cases/force models are independently owned per result. Empty explicit batches still qualify supplied case metadata; empty Load-backed batches validate binding/input but skip native inventories/design reads. Observed beam ID changes before/during retrieval throw and no partial list escapes. Input/model captures are not atomic, so callers coordinate changes.

## Native output qualification

A record requires Boolean true or integral one success from `GetMemberSteelDesignResults`. False/zero throws. String outputs start with unwritten markers, scalars with NaN/Int32 sentinels and critical forces with three NaN entries. Every successful output must be written in its documented type. Ratios, allowable ratio, location and KL/r must be finite nonnegative Doubles. Critical-case ID must be a positive Int32 resolved uniquely from captured metadata. Native code/clause/section strings are retained literally, including empty written strings.

Design status accepts trimmed ordinal-ignore-case `PASS` and `FAIL` only. A numeric status or unfamiliar text is not guessed from a utilization ratio and raises `InvalidOperationException`. This handles the source status-conversion uncertainty explicitly; builds returning another representation need qualification before a mapping can be added. No analysis/design is triggered to obtain a result.

The returned record describes the last parameter block used to design that analytical member. Critical-section distance is from the member start in current native base length units. The native force array reports Fx, My and Mz in current base force/moment units; exact-length rank-one Double arrays are required, with replacements/nonzero lower bounds accepted. `SteelDesignForces` owns immutable scalar components and does not invent Fy/Fz/Mx or infer an axis transformation. This deliberately adapts the source UI-dependent six-component force interface to a named three-component Core result. Signs, native axes and code conventions remain unchanged.

`MemberSteelDesignResults` retains the original editable property names and constructor argument order. Its `DesignForces` property/constructor argument use `SteelDesignForces`; other scalar/entity types remain adapted to Core namespaces. Public manual construction/setters store values without query validation. Query records retain the original Beam reference, a fresh copied LoadCase and independent force model. Editing them does not update STAAD or recompute results. Native freshness, block identity beyond last-block semantics and engineering acceptance remain separate.

## Governing-member selection

`GetGoverningMember(PhysicalMemberIdentificationResult pmiResult, bool ratiosPopulated = false)` returns the original Member whose largest captured constituent-beam ratio is greatest. The first member occurrence wins equal maxima. Null Members, null member entries and null/empty Beams sets are ignored; no usable run yields null. Null top-level input throws. Nonempty sets must contain non-null beams. Endpoints, Nodes and DiscardedBeams are unused.

Default mode queries each unique Beam reference once under one lease and requires unique positive native IDs for distinct references. A shared Beam across groups is queried/populated once. Every native read and ratio qualification completes before any population. Cached mode instead captures supplied `UtilizationRatio` values with no native reads or writes; it allows draft IDs and trusts the caller's population claim. It cannot distinguish uninitialized zero from valid zero. Both modes require finite nonnegative ranked ratios; native -1/-999 absence sentinels and invalid cached values throw rather than selecting a misleading winner.

Member order/memberships are captured, bounded to 100,000 member entries and 100,000 total beam memberships. Ranking uses captured qualified values/membership. Default population writes detached ratios sequentially, checking IDs before writes and afterwards. Property notifications run synchronously under the same lease. A handler can throw or modify data after some writes; earlier/current writes may remain and no automatic rollback is attempted. Subsequent callback edits do not recompute the selected maximum. This is not an atomic transaction.

`Member` and `PhysicalMemberIdentificationResult` are supporting detached containers. Constructors preserve source scalar/reference shapes and initialize independent sets/lists. They do not identify physical members, hydrate connectivity, calculate axes/releases or expose the complete source geometry API. Edits to collections remain caller-managed.

~~~csharp
var beam = new StaadPro.Interop.Core.Entities.Beam { Id = 7 };
var cases = new[] { new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead") };
var supplied = wrapper.Output.GetSteelDesignResultsForBeam(beam, cases);
Console.WriteLine(supplied.Code + ": " + supplied.DesignForces.Fx);
var discovered = wrapper.Output.GetSteelDesignResultsForBeam(wrapper.Load, beam);
Console.WriteLine(discovered.CriticalLoadCase.Id);
foreach (var record in wrapper.Output.GetSteelDesignResultsForBeams(new[] { beam }, cases))
    Console.WriteLine(record.UtilizationRatio);
foreach (var record in wrapper.Output.GetSteelDesignResultsForBeams(wrapper.Load, new[] { beam }))
    Console.WriteLine(record.DesignSection);
var member = new StaadPro.Interop.Core.Entities.Member();
member.Beams.Add(beam);
var groups = new StaadPro.Interop.Core.Models.PhysicalMemberIdentificationResult();
groups.Members.Add(member);
var governing = wrapper.Output.GetGoverningMember(groups);
Console.WriteLine(governing == member);
~~~

All five operations use the current Output binding, constructing thread and shared lifetime/reentrancy lease, including native Load/Output calls, enumeration and managed population. Reacquire adapters after refresh. Raw objects, other wrappers, external UI and concurrent edits are uncoordinated. Calls may block and original failures/HRESULTs propagate. No worker-thread COM, conversion, activation, analysis, design, selection, save, native mutation or retry is added. Managed fixtures/installed examples do not replace native-build or engineering qualification.
