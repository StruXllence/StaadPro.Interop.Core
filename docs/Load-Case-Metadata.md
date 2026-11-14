# Load-case metadata

The Load adapter returns detached metadata through the existing `ILoadCase`, `LoadCase`, `LoadCaseType` and `LoadType` contracts. These methods do not activate cases or read assignments, combination factors or analysis results.

| Method | Return and selection behavior |
| --- | --- |
| `GetAllPrimaryLoadCases()` | Set of all primary metadata records. |
| `GetAllReferenceLoadCases()` | Set of all reference metadata records. |
| `GetPrimaryLoadCaseFromId(lcId)` | One primary record, read directly by positive ID. |
| `GetReferenceLoadCaseFromId(lcId)` | One reference record, read directly by positive ID. |
| `GetPrimaryLoadCasesFromIds(loadCasesIds)` | Ordered list with duplicate IDs preserved as independent records. |
| `GetReferenceLoadCasesFromIds(loadCasesIds)` | Ordered reference list with the same duplicate semantics. |
| `GetLoadCombinationCaseFromId(lcId)` | One combination record after verifying membership in the combination inventory. |
| `GetLoadCombinationCasesFromIds(lcIds)` | Eager set of unique requested combinations; membership checked before reading any titles. |
| `GetAllLoadCombinationCases()` | Set of all combination metadata records. |
| `GetAllLoadCasesOfType(loadCaseType = PrimaryLoad)` | Eager enumeration of exactly one supported family. |
| `GetLoadCase(lcId, loadCaseType)` | Full-family lookup returning null for an absent positive ID. |
| `GetLoadCases(fromId, toId, loadCaseType)` | Owned list of existing cases in an inclusive positive-ID range. |
| `GetLoadCasesFromIds(loadCasesIds, loadCaseType)` | Owned list of existing matching IDs, with repetitions collapsed and absent IDs omitted. |
| `GetLoadCasesOfType(loadCaseType)` | Owned list of all metadata from one supported family. |
| `GetEnvelopeIds()` | Fresh set of qualified positive envelope IDs; empty for zero inventory count. |

`GetLoadCaseTitle(lcId)` remains a separate literal-title query. A title alone does not prove whether an ID belongs to the primary, reference or combination family.

## IDs, input enumeration and duplicates

IDs must be positive; the native zero/active-case shortcut is not accepted. List input must be non-null and is enumerated once under the Load operation lease. Every ID is validated and captured before native reads. A failing enumerator or invalid ID throws without returning partial data.

Each supplied sequence is limited to **100,000 entries**, including duplicates before deduplication. Empty requested lists return fresh empty collections without native calls.

Primary and reference ID-list methods preserve order and repetitions. Repeated IDs produce distinct `LoadCase` instances. They call the relevant title API followed by the engineering type API, without enumerating the entire family.

Combination-list requests have different semantics: duplicate IDs produce one record, set iteration order is unspecified, and a nonempty request reads the combination inventory once. **All requested IDs must be present before any titles are read.** Single-combination retrieval also verifies membership first and reads only the requested title. An ID present only in another family throws `InvalidOperationException` rather than becoming a falsely labeled combination or null result.

Choose a family-specific ID-list method when the family and IDs are known. Use `GetAllLoadCasesOfType` when all metadata from one family is required. It defaults to PrimaryLoad and supports ReferenceLoad and LoadCombination; RepeatLoad and undefined enum values throw `NotSupportedException` before native access. It never merges families or silently substitutes a supported one.

## Native inventory qualification

All-case and combination-membership queries read a qualified count followed by native case numbers when the count is nonzero. Counts must be integral, nonnegative and at most 100,000. A zero count returns an empty inventory without requesting numbers or metadata; a requested single combination then fails membership.

Number buffers start as unwritten Int32 sentinels. Native arrays must have one dimension, exactly the expected length and Int32 elements; every ID must be positive and unique. Replacement arrays and nonzero lower bounds are accepted. Null, unwritten, duplicate, nonpositive or wrongly shaped buffers throw.

Primary and combination number retrieval accept **Boolean true or an integral count equal to the separate inventory count**. False fails; true is never coerced to one. Boolean success requires a count recheck after array qualification, before metadata retrieval. An integral zero is not success for a nonempty inventory. Reference number retrieval retains its integral-count contract and rejects Boolean values.

Rechecks detect some concurrent changes but cannot make sequential COM observations atomic. Model edits can change membership or titles between reads even when the count stays the same.

## Titles, types and detached records

Primary records use `GetLoadCaseTitle` then `GetLoadType`. References use `GetReferenceLoadCaseTitle` then `GetReferenceLoadType`. Exact string titles—including empty text, whitespace, Unicode and literal `NONE`—are preserved. Null/non-string responses are errors. Primary/reference IDs may overlap; their metadata is read from the specified family. Engineering types must be defined `LoadType` codes 0–23.

Combinations use `GetLoadCaseTitle` after membership qualification. Their `CaseType` is LoadCombination and `Type` is always **LoadType.None (23)**. No `GetLoadType` call is made, and no primary engineering classification is inferred. Factors, constituent cases, repeat-load definitions and result availability are not loaded.

`ILoadCase` exposes read-only metadata properties. Returned objects are concrete, editable `LoadCase` records; modifying a concrete record does not submit native changes. Their existing managed property store supplies notifications without JSON or UI-framework dependencies. Records are independent of native buffers and previous calls.

Equality and hashing use **Id, CaseType and Type**, not Title. Remove an object from a HashSet before changing those identity fields, or rebuild the set. Returned sets have unspecified iteration order. Re-enumerating a returned collection performs no COM work; call the method again for a new observation.

## Family lookup, range and ID filters

All four convenience methods require an explicit supported family and eagerly read its complete inventory and metadata. `GetLoadCase` requires a positive ID and returns null when that ID is absent. A malformed or failing metadata response for another case still propagates. `GetLoadCasesOfType` copies the family metadata set to a fresh editable list. It has no optional family default.

`GetLoadCases` validates positive inclusive bounds and rejects a reversed range. It filters existing metadata without generating every integer between the bounds, so an upper bound of `int.MaxValue` does not create a huge range allocation. Equal bounds select at most one existing case. Empty matches return an empty list.

`GetLoadCasesFromIds` is a family filter. It captures non-null positive IDs exactly once under the operation lease, validates every input and limits the sequence to 100,000 entries before deduplication. Duplicate IDs select one existing record and missing IDs are omitted. Input ordering does not determine output ordering. Empty valid input returns a fresh empty list without native calls. Unsupported families are rejected even for empty input. The direct primary/reference ID-list methods retain their request-order/repetition contracts, while combination requests retain their membership-error contract.

Results are eager and independently owned. Re-enumeration makes no native calls, and later list/input edits do not recompute selection. Family iteration order is unspecified. Coordinate external model edits with the owning thread because the sequence of native reads is not atomic.

## Envelope identifiers

`GetEnvelopeIds` reads `GetEnvelopeCount` as an integral number between zero and 100,000. Zero returns a fresh empty set without fetching IDs. For a positive count it initializes an Int32 sentinel buffer and calls `GetEnvelopeIDs`. Bentley's documented long return requires **integer 1** for success. Boolean true, an ID count greater than one, zero, negative values and unsupported scalar shapes throw.

A successful response must provide exactly the reported number of positive, distinct, written Int32 IDs in one array dimension. Replacement buffers and nonzero lower bounds are supported. The method rechecks the count after validating the buffer and throws if it changed. Count rechecking cannot detect every external model edit. The returned HashSet owns independent storage and has unspecified iteration order. Envelope identifiers have their own namespace; this method does not retrieve classifications, case membership or factors. A valid empty inventory returns an empty set, while malformed outputs and native failures throw.

## C# example

~~~csharp
var load = wrapper.Load;
var references = load.GetReferenceLoadCasesFromIds(new[] { 7, 3, 7 });
Console.WriteLine(references.Count); // three independent records, including repeated ID 7

var combination = load.GetLoadCombinationCaseFromId(201);
Console.WriteLine(combination.Title + ": " + combination.Type); // None

var requested = load.GetLoadCombinationCasesFromIds(new[] { 201, 301, 201 });
foreach (var item in requested)
    Console.WriteLine(item.Id + ": " + item.Title); // two unique combinations

foreach (var item in load.GetAllLoadCombinationCases())
    Console.WriteLine(item.Id);

var primary = load.GetAllLoadCasesOfType(); // defaults to PrimaryLoad
var referenceFamily = load.GetAllLoadCasesOfType(
    StaadPro.Interop.Core.Enums.LoadCaseType.ReferenceLoad);
Console.WriteLine(System.Linq.Enumerable.Count(primary) + " / " +
    System.Linq.Enumerable.Count(referenceFamily));

var found = load.GetLoadCase(7, StaadPro.Interop.Core.Enums.LoadCaseType.PrimaryLoad);
if (found != null) Console.WriteLine(found.Title);
var ranged = load.GetLoadCases(1, int.MaxValue, StaadPro.Interop.Core.Enums.LoadCaseType.PrimaryLoad);
var filtered = load.GetLoadCasesFromIds(new[] { 7, 3, 7 }, StaadPro.Interop.Core.Enums.LoadCaseType.PrimaryLoad);
var listed = load.GetLoadCasesOfType(StaadPro.Interop.Core.Enums.LoadCaseType.ReferenceLoad);
Console.WriteLine(System.Linq.Enumerable.Count(ranged) + System.Linq.Enumerable.Count(filtered) + listed.Count);
foreach (int id in load.GetEnvelopeIds()) Console.WriteLine("Envelope " + id);
~~~

## Lifetime, failures and validation limits

Each call runs on the constructing COM thread under one current Load lease spanning enumeration, input validation, inventory/membership checks, dispatch and output qualification. Disposed wrappers, stale adapters retained after refresh, manually constructed noncurrent adapters, other threads and reentrant Root/Load/Output/refresh/disposal calls are rejected before native work. Reacquire `wrapper.Load` after `RefreshComObjects()`.

The guard cannot coordinate raw native access, other wrappers or the STAAD.Pro UI. Coordinate shared-session edits. Synchronous calls can block. Native errors retain their original exceptions/HRESULTs, with no retry or fallback instance. The methods do not analyze, activate, save, convert units, hydrate assignments or mutate STAAD.

Offline tests and the installed-package consumer verify Boolean/count separation, malformed inventories, membership, ordered references versus deduplicated combinations, independent records, exact signatures/defaults, expanded matching XML and examples. The installed 2024/2025/2026 help declares matching combination count/number signatures, but this is not native runtime or engineering qualification. No live metadata retrieval or model mutation was performed for this increment.
