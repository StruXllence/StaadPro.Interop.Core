# Primary and reference load-case metadata

These five `IOSLoad` contracts read metadata without activating cases, loading assignments, saving or editing STAAD:

| Method | Result |
| --- | --- |
| `GetAllPrimaryLoadCases()` | Fresh `HashSet<ILoadCase>` of qualified primary metadata. |
| `GetAllReferenceLoadCases()` | Fresh `HashSet<ILoadCase>` of qualified reference metadata. |
| `GetPrimaryLoadCaseFromId(int lcId)` | One new primary `LoadCase` record. |
| `GetReferenceLoadCaseFromId(int lcId)` | One new reference `LoadCase` record. |
| `GetPrimaryLoadCasesFromIds(IEnumerable<int> loadCasesIds)` | New list preserving input order and duplicate IDs as independent objects. |

Single IDs must be positive; zero does not select the active case. The list input is non-null, enumerated once and fully validated before metadata access. Empty input returns a fresh empty list with no native calls. Inputs and inventories are bounded to 100,000 cases. Errors propagate without a partial result.

All-case queries read the appropriate family's native count. Zero returns an empty set without number/title/type requests. Nonzero counts require a one-dimensional Int32 ID array of the same size with every ID positive, written and unique. Replaced arrays and nonzero lower bounds are accepted. For primary numbers, Boolean true is success and triggers a separate count recheck after buffer qualification; false throws. An integral return must equal the expected count. Reference numbers require the integral count and reject Boolean values. Neither native success nor malformed IDs are coerced into an empty inventory.

Each case is read as title then engineering type. Primary and reference IDs can overlap; their metadata comes from the corresponding family. Exact string titles, including empty, whitespace, Unicode and literal `NONE`, are preserved. Non-string titles, unsupported scalar shapes and undefined engineering types throw. `LoadType` supports the documented codes 0–23, including `None=23`. `CaseType` is PrimaryLoad or ReferenceLoad according to the requested method.

`ILoadCase` extends the small `IEntity` identity contract. `LoadCase` uses the existing managed property store, raises change notifications and has no JSON or UI-framework dependency. Returned records contain metadata only. Constructor defaults match the data model, while manually constructed records do not assert native validity. Changing the object does not submit native updates. Equality and hashing use Id, CaseType and Type; remove a record from a HashSet before changing those fields, or rebuild the set. Title does not participate in equality. Set iteration order is unspecified.

~~~csharp
var load = wrapper.Load;
foreach (var item in load.GetAllPrimaryLoadCases())
    Console.WriteLine(item.Id + ": " + item.Title + " (" + item.Type + ")");
var primary = load.GetPrimaryLoadCaseFromId(7);
var reference = load.GetReferenceLoadCaseFromId(7);
var ordered = load.GetPrimaryLoadCasesFromIds(new[] { 7, 3, 7 });
Console.WriteLine(ordered.Count);
foreach (var item in load.GetAllReferenceLoadCases())
    Console.WriteLine(item.Title);
~~~

Each call runs on the constructing COM thread under one current Load lease, including list enumeration, validation, inventory qualification and metadata retrieval. Stale/manual-noncurrent adapters, other threads and reentrant Root/Load/Output/refresh/disposal work are rejected before native access. Refresh invalidates captured adapters; reacquire `wrapper.Load`. Disposed wrappers throw ObjectDisposedException. The guard cannot coordinate raw calls, other wrappers or external UI. Sequential reads are not an atomic model snapshot; count rechecking cannot detect every concurrent edit.

The implementation performs no analysis, activation, assignment hydration, unit conversion, retry or native mutation. Original exceptions/HRESULTs propagate and calls can block. Offline tests and the installed-package consumer verify native stand-ins, all documented type codes, Boolean/count separation, invalid arrays/counts, independent records, notifications/equality, signatures/defaults, expanded matching XML and compiled examples. No live model metadata read or new native-build runtime qualification was performed.
