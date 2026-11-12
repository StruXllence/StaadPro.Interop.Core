# Member-force observations

This preview completes the common Root workflow with `RetrieveMemberForces(IEnumerable<int> beams, IEnumerable<int> loadCases)` and introduces `Load.GetLoadCaseTitle(int lcId)`, `Output.AreResultsAvailable()` and two `Output.GetMemberEndForces(...)` overloads. Other Load/Output operations remain unimplemented. The library adds no WPF or third-party runtime dependency for these force records.

`AreResultsAvailable()` returns the validated native Boolean or integral zero/one. False is a valid observation; it does not indicate a transport exception. Availability does not certify freshness, matching model files or completed analysis. `GetLoadCaseTitle` requires a positive ID and preserves the exact native string, including an empty title; null/non-string output throws.

Both end-force overloads take `lcTitle`, `beamId`, `loadCaseId` and `memberEnd`. The four-argument overload requests local axes. The five-argument overload also takes `LocalOrGlobal coordinateSystem`: Local=0 or Global=1. MemberEnd is Start=0 or End=1. IDs must be positive and selectors defined. The supplied non-null title is display metadata; the end-force query does not fetch or verify it.

End-force methods return `MemberForceQueryResult`, which exposes native `IsSuccessful`, immutable identity/axis properties and `Forces` only after success. This named status/data contract replaces returning an unqualified force record: native false never looks like a valid all-zero result. A successful native response must provide six finite, written Double values in Fx/Fy/Fz/Mx/My/Mz order. Internal NaN buffers detect unwritten values; replaced arrays and nonzero lower bounds are supported. False discards any partial buffer. Malformed flags/successful buffers and original native failures remain exceptions.

`MemberForces` provides editable detached scalar components and member/load/end fields. It intentionally has no inherited WPF vector, conversion, JSON or component-equality behavior. Editing a record does not update STAAD or another result. Query identity and axis selection remain independent of edits to the payload. Values retain native result units, axes and signs; cached input-unit metadata does not certify result units. No managed transform or physical-joint offset transfer is applied.

The Root batch snapshots each input once and deduplicates positive IDs in first-seen order. It permits at most 100,000 member-end queries after deduplication. Empty inputs return a successful empty result with null availability and no native work. Otherwise it checks the captured Output availability once, reads each distinct case title once through the captured Load, and queries load-case/member/start/end order using local axes. Native false availability returns an unsuccessful empty batch. Individual false results retain their identity with null force data and do not stop later queries. Exceptions return no partial batch. The read-only result collections preserve distinct identities even when all six components match; they do not collapse through HashSet component equality.

~~~csharp
var root = wrapper.Root;
if (wrapper.Output.AreResultsAvailable())
{
    string title = wrapper.Load.GetLoadCaseTitle(1);
    var query = wrapper.Output.GetMemberEndForces(title, 1, 1,
        StaadPro.Interop.Core.Enums.MemberEnd.Start,
        StaadPro.Interop.Core.Enums.LocalOrGlobal.Global);
    if (query.IsSuccessful) Console.WriteLine(query.Forces);
}
var batch = root.RetrieveMemberForces(new[] { 1, 2 }, new[] { 1 });
foreach (var query in batch.Queries)
    if (query.IsSuccessful) Console.WriteLine(query.Forces);
~~~

All five contracts use the wrapper's constructing COM thread and one current-adapter lease per operation, spanning validation, input enumeration, dispatch and output qualification. Refresh invalidates captured Root/Load/Output adapters; reacquire them. Disposal throws ObjectDisposedException. Stale/manual-noncurrent adapters, other threads and nested Root/Load/Output/refresh/disposal calls throw InvalidOperationException before native work. This guard cannot coordinate raw calls, other wrappers or external UI. Batch observations are sequential snapshots, not a model transaction.

The implementation does not start analysis, activate a case, save, select entities, refresh units, retry failed queries or move COM work to a background thread. Original exceptions/HRESULTs propagate, and native calls may block. The installed-package consumer checks expanded interface/concrete XML, exact signatures, named data contracts, native stand-ins and compiled examples. Offline verification does not qualify real marshaling, units, engineering results or any native build; no live result retrieval was performed in this increment.
