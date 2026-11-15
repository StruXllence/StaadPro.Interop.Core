# Load-envelope and combination definitions

`GetLoadEnvelopeDetail(int envId)` and `GetAllLoadEnvelopeDetails()` return detached editable `LoadEnvelopeDetail` records through `ILoadEnvelopeDetail`. Each record contains `Id`, `EnvelopType`, the separately stored `LoadCasesCount` and a newly owned `HashSet<int>` of `LoadCaseIds`. Classification codes retain `LoadEnvelopType` values None=0, Stress=1, Serviceability=2, Column=3, Connection=4, Strength=5 and Temporary=6.

The direct envelope query requires a positive ID and calls native `GetLoadEnvelopeDetails` with unwritten Int32 sentinels. Type/count must be written and valid, and the return must be integer 1. Boolean true is rejected for this documented long return. Membership count is bounded to 100,000. A valid zero count returns an empty set without fetching membership. For a positive count, `GetLoadListfromLoadEnvelope` must return integer 1 and provide an exact-length rank-one Int32 array with positive unique written IDs. Replacement arrays and nonzero lower bounds are accepted. Classification/count are re-read after the membership fetch and must match. A missing native ID, unwritten output or malformed response throws.

The all-envelope query uses the existing qualified envelope inventory, then reads each detail eagerly. Empty inventory returns a fresh empty set and skips details. In addition to per-count bounds, the sum of memberships for the whole query is limited to 100,000. Remaining budget is checked after each detail count and before its membership allocation/fetch. Valid empty envelopes are retained. Errors return no partial set. Record and membership set order is unspecified.

`LoadEnvelopeDetail` uses the existing managed property store and raises property notifications. Editing it does not update STAAD. Mutating `LoadCaseIds` does not raise a property notification or synchronize `LoadCasesCount`. Manual constructors/setters store supplied values without native validation. Default/three-argument construction supplies a fresh empty set; the four-argument constructor retains the supplied set reference. Native queries always supply independent validated sets. Envelope classification/membership reads do not resolve case titles/factors or compute extrema.

## Ordinary combination coefficients

`GetLoadCombination(int id)` retains the original positive-ID signature and returns a detached `StaadLoadCombination`. Its base `LoadCombination` implements `ILoadCombination` and supplies immutable constructor identity/title. `Factors` is an editable `IDictionary<ILoadCase,double>` containing dimensionless signed coefficients and independently read primary metadata.

The reader first qualifies `GetNoOfLoadAndFactorPairsForCombination` as an integral count between zero and 100,000. Zero returns null and skips inventories, title and factor fetches. For a positive count the requested ID must belong to the combination inventory, and its title must be a literal native string. `GetLoadAndFactorForCombination` must return Boolean true or integral one. Source IDs require exact-length rank-one Int32 storage with unique positive written values. Coefficients require rank-one Double storage with every ordinary pair written and finite. Negative, zero and positive coefficients are preserved. Replacements and nonzero lower bounds are supported.

Bentley's native contract allows an extra overall multiplier for SRSS. The reader allocates a Double buffer with one extra NaN sentinel slot. An exact pair-count replacement array is accepted. If an extra slot contains a returned overall value, the method throws `NotSupportedException` because `StaadLoadCombination` has no field for that multiplier. Ordinary responses leave the extra sentinel unwritten. No overall coefficient is silently dropped and no combination result is calculated.

After array qualification the pair count is re-read and must match. The complete primary metadata inventory is read once to resolve all source IDs. Missing primary IDs and metadata failures, including errors for another primary case, throw. Source cases are qualified as PrimaryLoad; reference/repeat/nested-combination semantics are not inferred. Returned keys use `LoadCase` equality and hashing on Id, CaseType and Type. Remove/reinsert a key before editing those fields. Dictionary changes and metadata edits do not submit native updates. Manual construction creates a fresh empty factor dictionary and performs no native access.

~~~csharp
var envelope = wrapper.Load.GetLoadEnvelopeDetail(9);
Console.WriteLine(envelope.Id + ": " + envelope.EnvelopType);
foreach (int id in envelope.LoadCaseIds) Console.WriteLine(id);
foreach (var item in wrapper.Load.GetAllLoadEnvelopeDetails())
    Console.WriteLine(item.Id + ": " + item.LoadCasesCount);
var combination = wrapper.Load.GetLoadCombination(201);
if (combination != null)
    foreach (var pair in combination.Factors)
        Console.WriteLine(pair.Key.Id + " x " + pair.Value);
~~~

All calls use the existing shared Load operation lease on the constructing COM thread, including validation and result qualification. Refresh invalidates captured adapters. Stale/manual-noncurrent adapters, other threads and reentrant Root/Load/Output/lifecycle calls are rejected. Original exceptions/HRESULTs propagate, and synchronous COM can block. Rechecks detect some changes but cannot make these sequential reads atomic with external model edits. There is no implicit activation, analysis, save, conversion, retry or native mutation.

Managed tests and an installed NuGet consumer verify statuses, buffers, bounds, rechecks, ownership, metadata, signatures and expanded XML/examples. No live native definition read or new native-build/engineering acceptance was performed.
