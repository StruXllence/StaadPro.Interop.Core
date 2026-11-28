# Release notes

## 1.0.0-preview.20261128.3 - 28 November 2026

Added `GetBeam(int beamNumber)` and `GetPlate(int pId)` to read qualified native incidence and hydrate global node coordinates. Added `GetBeam(int bId, IEnumerable<Node> fromModelNodes)` and `GetPlate(int pId, IEnumerable<Node> fromModelNodes)` to match incident node IDs to caller-supplied references. A node missing from the supplied collection leaves its matching member end or plate corner empty.

Added `GetPlateIncidence(int pId)` for a distinct node snapshot in native incidence order. All five queries run on the wrapper owning thread and check the incidence status and output slots before publishing results. Geometry provides twenty operations with matching XML IntelliSense on the interface and adapter. [Member and plate incidence guide](Geometry-Incidence.md).

## 1.0.0-preview.20261128.2 - 28 November 2026

Added `GetSelectedBeamsIds()` and `GetSelectedPlatesIds()` for validated ID sets and `GetSelectedBeamsListAsync()` and `GetSelectedPlatesListAsync()` for ordered ID arrays. The task-shaped calls complete native work on the wrapper's owning COM thread.

Added `GetSelectedBeamsExt(IEnumerable<Beam> fromModelBeams)`. It matches each selected member ID to the first supplied beam and keeps one null entry for unmatched IDs. Geometry provides fifteen operations with matching XML IntelliSense on the interface and adapter. [Beam and plate selection guide](Geometry-Beam-Plate-Selection.md).

## 1.0.0-preview.20261128.1 - 28 November 2026

Added `GetSelectedNodesIds()`, both `GetSelectedNodesExt` overloads and `GetSelectedNodesListAsync()`. The selection reader validates count and unique positive IDs before returning data. It hydrates native coordinates or matches the first supplied node by ID. The supplied-node overload keeps one null marker for unmatched IDs. The array task preserves native selection order and completes native work on the wrapper's owning COM thread.

Added `GetNode(int nId, IEnumerable<Node> fromModelNodes)` to return the first matching supplied node without a native read. Geometry now exposes ten operations with matching interface and adapter IntelliSense. [Node selection guide](Geometry-Node-Selection.md).

## 1.0.0-preview.20261128 - 28 November 2026

Added `GetAllBeamsList()`, `GetAllNodesList()`, `GetAllPlatesList()` and `GetAllSolidsList()`. These return detached, eager ID snapshots in native order. Each inventory checks the native count, array shape, unique positive IDs and count stability before returning results. Empty inventories skip the list call.

Added `GetNode(int nId)`. It reads three finite global coordinate values from OpenSTAAD without unit conversion and returns a detached node. Zero keeps its existing source meaning of no node. All five methods run under one current-binding operation on the wrapper's owning COM thread and ship matching interface and adapter IntelliSense. [Geometry inventory and node-coordinate guide](Geometry-Inventories.md).

## 1.0.0-preview.20261127.2 - 27 November 2026

Added `GetPlateStressAtPoint` with two finite global points and an owned 32-component result. The named fields follow native index order. Incomplete or malformed success responses fail before values reach callers.

Added two `GetMemberEndForcesConsideringOffsets` and two `GetMemberEndForcesAtPhysicalJoint` overloads. The supplied-member path captures the selected joint from the beam. The ID path resolves valid incidence through Geometry. The shared workflow reads the global offset through Property and transfers the reported global force couple to the incidence joint. Missing offset information is distinguished from a written zero offset. Ordinary native force failure returns null.

Output now provides 85 contracts with matching XML IntelliSense. [Point stress and member-offset guide](Point-Stress-and-Member-Offsets.md)

## 1.0.0-preview.20261127.1 - 27 November 2026

Added two member-local plate-corner overloads that preserve case, plate and resolved corner metadata. Both mark the components as member-local, allowing the detached transform to catch accidental double projection. The task overload completes the read and projection on the wrapper's owning thread. [Member-local plate-corner guide](Member-Local-Plate-Corner-Forces.md)

Added three plate cut-line resultant methods. `QueryPlateCutLineResultant` preserves the captured request and reports ordinary native failure explicitly. `GetPlateCutLineResultant` and its task counterpart require six complete, finite force and moment components. The wrapper passes the requested axes to OpenSTAAD and preserves native values and units. [Plate cut-line guide](Plate-Cut-Line-Resultants.md)

Output now provides 80 contracts with matching interface and adapter XML IntelliSense.

## 1.0.0-preview.20261127 - 27 November 2026

Added five Output contracts. `GetPlateCornerForcesGlobal(Plate, Node, int)` retains the queried identifiers and returns a copied six-component record on native success. An ordinary native failure keeps the identifiers and leaves `Forces` null. Invalid input, malformed output and transport errors propagate.

Added node and beam overloads of `GetPlateCornerForces` and `GetPlateCornerForcesAsync` that read a single corner and project its force and moment into the supplied member basis. Both task variants finish on the wrapper's owning thread before returning. Projection retains the original corner, units and case metadata. It applies no member-end sign reversal or moment transfer. See [the member-local guide](Member-Local-Plate-Corner-Forces.md) for usage and axis qualification.

The package provides 75 Output contracts, with matching interface and adapter XML documentation.

## 1.0.0-preview.20261126.2 - 26 November 2026

Added `GetMemberLocalCoordinateSystem(Beam member)` and the node and beam overloads of `TransformPlateCornerForcesToMemberLocal` and `TransformPlateCornerForcesToMemberLocalAsync`. All five have matching interface and adapter XML IntelliSense. See [member-local frames and force projection](Member-Local-Plate-Corner-Forces.md).

The frame captures origin and three orthonormal axes. It follows the start-to-end direction and uses supplied beam beta in degrees for the beam overload. The near-vertical reference is orthogonalized to retain a right-handed basis for tilted members. The node overload uses a zero-beta frame from the corner to the other node.

The transforms project already-global force and moment vectors independently at the same reference point. They preserve units and the moment reference point and return a detached `MemberForces` record with case and end metadata. Both task overloads finish the managed operation on the owning thread before returning a completed or faulted task.

The Release build completed with zero warnings and errors. All 3,245 tests passed, including 19 new cases. The installed consumer verified all seventy Output contracts, compiled 303 XML IntelliSense examples and twenty-four guide examples, and exercised the detached projection APIs.

## 1.0.0-preview.20261126.1 - 26 November 2026

Completed the family-specific plate-corner task overload, the hydrated load-case-ID batch queries with synchronous/task variants, and the global-coordinate naming alias pair. Output now provides sixty-five contracts with full matching interface/adapter XML IntelliSense. See [plate-corner forces](Plate-Corner-Forces.md) for overload selection, query order and examples.

Captured and validated batch IDs once, bounded to 100,000 occurrences including duplicates, retaining first unique request order. Complete primary/combination/reference case resolution and membership validation precede one shared plate hydration. Empty batches skip native work after scalar/ownership validation; later native failures stop without publishing a partial result set. Reused current same-wrapper bindings, captured-key indexes, independent force copies and original exceptions.

The global-named methods retain the same reported native components as the ordinary corner query. They add no plate-basis rotation or coordinate selector, preserving signs, normalization and units. Their naming follows the source convention; deployed native axes/signs still require engineering acceptance. Task methods finish synchronously on the owning COM thread before returning settled success/fault tasks.

Release build passed with zero warnings/errors and all **3,226 offline tests**, including **38 new cases**. Exact installed-package checks verify nineteen Load/sixty-five Output contracts, **293 compiled XML examples**, README/twenty-three guides and all five new native-stand-in workflows, with isolated restore/build/runtime/archive equality. No new live native, cross-version runtime or engineering qualification is claimed.

## 1.0.0-preview.20261126 - 26 November 2026

Ported five ID-based plate-corner overloads: automatic binding resolution, explicit current Geometry/Load bindings, their two task variants and explicit load-family discovery. Output now exposes sixty contracts. Complete matching interface/adapter XML IntelliSense and the [plate-corner guide](Plate-Corner-Forces.md) explain the differences from supplied-metadata, all-corner and sparse/Cartesian queries.

Hydrated detached plate incidence/coordinates and load-case metadata through existing qualified readers. Preserved primary, combination and reference precedence, complete bounded inventory validation and single-family discovery when explicitly requested. Explicit adapters must be current bindings from the same Output wrapper; foreign, manually reconstructed and replaced adapters fail before native work.

Kept the complete sequence under one owning-thread lifetime/reentrancy lease without recursive public adapter calls. Geometry/membership/discovery failures stop before force retrieval, native exceptions retain their original identity, and each successful six-Double force buffer is copied independently. Task variants finish on the caller before returning settled tasks. No worker COM, model acquisition, analysis, unit conversion, fallback or native mutation was added.

Release build passed with zero warnings/errors and all **3,188 offline tests**, including **62 new cases**. Exact installed-package checks verify nineteen Load/sixty Output contracts, **283 compiled XML examples**, README/twenty-three guides and all five new native-stand-in workflows, with isolated restore/build/runtime/archive equality. These checks do not add live native, cross-version runtime or engineering qualification.

## 1.0.0-preview.20261125.1 - 25 November 2026

Ported the single-case/multiple-plate task overload, Cartesian case/plate queries and sparse request queries, each with complete matching interface and adapter XML IntelliSense. Output now provides fifty-five contracts. See [plate-corner forces](Plate-Corner-Forces.md) for overload selection, examples and execution rules.

Added source-based PlateCornerForcesRequest metadata with editable case, plate and corner references and a current-identity key. Cartesian selections retain unique case/plate/corner order; sparse selections deduplicate complete case/plate/node keys while retaining each first request's context. Both validate and capture bounded inputs before force calls, require supplied A/B/C incidence without hydration and preserve captured result-set indexes across metadata edits. Wide arithmetic checks the 100,000-observation Cartesian bound.

Reused sequential native force qualification and independent six-Double copying. Later failures return no partial set; original native exceptions remain available. Task variants finish on the owning COM thread before returning settled tasks. No worker COM or additional native signature was introduced.

Release build passed with zero warnings/errors and all 3,126 offline tests, including 42 new cases. Exact installed-package checks verify nineteen Load/fifty-five Output contracts, 273 compiled XML examples, README/twenty-three guides, native stand-ins and isolated restore/build/runtime/archive equality. No new live native, cross-version runtime or engineering qualification is claimed.

## 1.0.0-preview.20261125 - 25 November 2026

Ported the `GetPlateCornerForces(IEnumerable<ILoadCase>, Plate, Node)` and `GetPlateCornerForces(IEnumerable<ILoadCase>, Plate)` selection queries with their task variants, plus `GetPlateCornerForces(ILoadCase, IEnumerable<Plate>)`, with complete matching interface/adapter XML IntelliSense. Output now provides fifty contracts. See [plate-corner forces](Plate-Corner-Forces.md) for selection behavior, deduplication, bounds and execution rules.

Preserved source first-occurrence order, duplicate-ID deduplication, captured observation keys and read-only result-set indexing across mutable entity edits. All five selection queries require supplied A/B/C plate incidence, validate scalar/sequence inputs and corner membership before native access (even when sequences are empty), bound inputs to 100,000 occurrences and 100,000 unique corner observations, and perform no Geometry hydration.

Qualified Boolean/integral-one native force success, exact six-Double finite array shapes, independent buffer copying and failure stopping without partial result sets. Task methods execute synchronously on the owning COM thread under one shared current-binding lease before returning settled success or faulted tasks, without worker COM.

Release build passed with zero warnings/errors and all 3,084 offline tests, including 39 new cases. Exact installed-package checks verify nineteen Load/fifty Output contracts, matching expanded IntelliSense, 263 compiled XML examples, README/twenty-three guides, native stand-ins and isolated restore/build/runtime/archive equality. No new live native, cross-version runtime or engineering qualification is claimed.

## 1.0.0-preview.20261124.1 - 24 November 2026

Ported the `GetPlateCenterResults(..., int nThreads)` compatibility overload and single/all-corners `GetPlateCornerForces` queries with their task variants, with complete matching interface/adapter XML IntelliSense. Output now provides forty-five contracts. See [plate-corner forces](Plate-Corner-Forces.md) for method choices, incidence resolution, native status conventions and execution behavior.

Added source-based `PlateCornerForces`, immutable value key and indexed result-set models, reusing `Forces` and existing entity metadata. Extended `Plate` with optional A/B/C/D references without porting the complete Geometry API. Single-corner queries can hydrate incomplete incidence into new detached metadata; all-corners queries require supplied incidence and retain first A/B/C/D references per positive node ID. Queries and indexes use captured identities, and every six-component buffer is copied independently.

Qualified Boolean/integral-one force success, integral-zero plate-incidence success and documented-void coordinate retrieval separately. NaN/invalid-ID sentinels reject unwritten or malformed outputs. Read-only result-set membership and captured-key filters remain stable after metadata edits; no partial set escapes a later failure. Task methods finish on the owning COM thread before returning settled success/fault tasks. The thread-count hint validates 1..512 while preserving sequential native work. No rotation, unit conversion, analysis, save or native mutation is introduced.

Release build passed with zero warnings/errors and all 3,045 offline tests, including 128 new cases. Exact installed-package checks verify nineteen Load/forty-five Output contracts, matching expanded IntelliSense, named model/index semantics, 253 compiled XML examples, README/twenty-three guides, native stand-ins and isolated restore/build/runtime/archive equality. No new live native, cross-version runtime or engineering qualification is claimed.

## 1.0.0-preview.20261124 - 24 November 2026

Ported `GetPlatePrincipalStressesAsync`, single and batch `GetPlateCenterResults`, and their task variants with complete matching interface/adapter XML IntelliSense. Output now exposes forty contracts. See [plate-center composite results](Plate-Center-Composite-Results.md) for method choices, data meaning, bounds and execution behavior.

Added the named `PlateCenterResults` container using existing local stress, moment-per-width, surface von Mises and normal-principal records. Retained source constructor/factory names, case-reference compatibility and analytical plate-ID compatibility without introducing unit-service or geometry/design dependencies.

Captured case/plate identities once per input occurrence and used them for all four sequential native calls. Batch selections are validated once before dispatch, retain duplicates and case-major/plate-minor order, enforce 100,000-occurrence/pair bounds and return eager read-only storage. Failed enumeration, malformed/unwritten/nonfinite outputs or a failed pair publish no partial collection. All methods share the current Output binding/constructing-thread lease. Task methods finish native work before returning settled tasks, preserving failures without worker COM. Sequential reads do not certify an atomic or fresh solver snapshot.

Release build passed with zero warnings/errors and 2,917 offline tests, including 155 new cases. Installed-package verification checks nineteen Load/forty Output contracts, matching expanded XML IntelliSense, 243 compiled XML examples, README/twenty-two guide examples, composite native stand-ins, isolated dependencies and archive equality. No new live native, cross-version runtime or engineering qualification is claimed.

## 1.0.0-preview.20261123.1 - 23 November 2026

Added `GetPlateCenterMoments(ILoadCase loadCase, Plate plate)`, `GetPlateCenterMomentsAsync(...)`, `GetPlateCenterVonMises(ILoadCase loadCase, Plate plate)`, `GetPlateCenterVonMisesAsync(...)` and `GetPlatePrincipalStresses(ILoadCase loadCase, Plate plate)` with complete matching interface/adapter XML IntelliSense. See [plate-center moments and surface stresses](Plate-Center-Moments-and-Surface-Stresses.md).

Preserved native local MX/MY/MXY moment-per-width values, top/bottom von Mises stress and maximum/minimum top/bottom in-plane principal stress output order. Added the scalar/context portions of `PlateCenterMoments`, `PlateCenterVonMises` and `PlatePrincipalStresses`, retaining supplied case/plate references and copied editable scalar values. No units, signs, surface labels or principal values are transformed or inferred. Unit-service, transformation and Wood-Armer helpers remain outside this model subset.

Qualified native Boolean/integral success and every written finite Double, with NaN sentinels preventing initialized-zero results. Moment arrays require exact rank/length/type and support copied replacements/nonzero bounds. All five APIs retain the shared current-binding, constructing-thread, lifetime and reentrancy lease. Async variants perform native reads synchronously before returning settled tasks and preserve original failures/HRESULTs.

Release build passed with zero warnings/errors and 2,762 offline tests, including 215 new cases. Exact installed-package checks verify nineteen Load/thirty-five Output contracts, matching expanded IntelliSense, 233 compiled XML examples, README/twenty-one guide examples, surface-query native stand-ins and archive/dependency isolation. No new live native, cross-version or engineering qualification is claimed.

## 1.0.0-preview.20261123 - 23 November 2026

Ported both `GetStaticCheckResults` overloads, `GetStaticCheckResultsAsync`, `GetPlateCenterForces` and `GetPlateCenterForcesAsync` with comprehensive matching interface/adapter XML IntelliSense. Output now exposes thirty contracts. See [static-check batches and plate-center stresses](Static-Check-Batches-and-Plate-Center-Stresses.md) for method differences and examples.

Static-check batches capture and validate the complete bounded ID sequence before native access, preserve repeated IDs as independent observations and return no partial set on failure. The thread-count compatibility overload validates 1–512 while keeping all COM work sequential on the constructing thread. Task variants complete native work before returning settled tasks.

Plate-center queries qualify native success and fully written finite five-Double stress arrays, preserve local SQX/SQY/SX/SY/SXY order and copy components out of native storage. Reused existing result types and added detached Plate identity and scalar/context plate-stress support without unit-service or UI dependencies. Metadata references remain caller-owned and mutable; no stress-to-force conversion or full geometry hydration is implied.

Passed a zero-warning Release build and **2,547 offline tests**, including **93 new cases**. The exact installed package verifies thirty Output signatures, matching expanded XML, **223 compiled XML examples**, README and **twenty Root/domain guides**, stand-in batch/stress behavior, model documentation and restored archive equality. No live native read, mutation or new runtime/engineering qualification was performed.



## 1.0.0-preview.20261122 - 22 November 2026

Added `GetSupportReactions(int supportId, int loadCaseId)`, `GetSupportForces(int supportId, int loadCaseId)`, `GetSupportReactionsAsync(...)`, `GetSupportForcesAsync(...)` and `GetStaticCheckResultAsync(int loadCaseId)` with complete matching interface/adapter XML IntelliSense. See [support reactions and static checks](Support-Reactions-and-Static-Checks.md).

Support queries preserve native global reactions or negate all six components for the source support-force convention. Static checks copy native load and reaction totals independently and retain literal component-wise load-minus-reaction differences without a balance tolerance or acceptance inference. Added editable detached `Forces`, `LoadCaseForces` and `StaticCheckResult` scalar models without WPF or unit-converter dependencies.

Both native queries require successful Boolean/integral flags and fully written finite six-Double arrays; initialized defaults, failed reads and malformed outputs cannot escape as valid results. Replacement/nonzero-bound arrays are copied, including aliased native buffers, with independent storage per result. All five APIs retain shared current-binding, constructing-thread, lifetime and reentrancy guards. Task variants perform native work synchronously before returning settled tasks; await observes original failures/HRESULTs.

Release build passed with zero warnings/errors and 2,454 offline tests, including 159 new cases. Exact installed-package verification covers nineteen Load/twenty-five Output signatures, matching expanded XML, 213 compiled XML examples, README/nineteen guide examples, support/static-check native stand-ins and archive/dependency isolation. No new live native, cross-version or engineering qualification is claimed.

## 1.0.0-preview.20261121 - 21 November 2026

Added both `GetSteelDesignResultsForBeam(...)` overloads, both `GetSteelDesignResultsForBeams(...)` overloads and `GetGoverningMember(PhysicalMemberIdentificationResult pmiResult, bool ratiosPopulated = false)` with complete matching XML IntelliSense. See [steel-design records and governing members](Steel-Design-Records.md).

Steel records preserve original member references, literal code/clause/section text, independently captured critical-case metadata and all three reported Fx/My/Mz force components. `SteelDesignForces` represents exactly those components without manufacturing unreported force values or introducing UI dependencies. Query records require native success, written finite typed outputs, nonnegative ratio/location/slenderness values and explicit PASS/FAIL status text. Ambiguous numeric statuses throw rather than being guessed. Critical-case identity must resolve uniquely.

Single/batch queries accept supplied case metadata or the same wrapper's current Load adapter. Load composition reads qualified primary/combination metadata within one shared operation lease. Input snapshots are bounded to 100,000 beam occurrences/case records; batches are eagerly materialized with order/duplicates retained and no partial result on failure. Beam identity changes observed during retrieval throw.

Governing selection ranks the greatest constituent-beam utilization, retaining first-member order on ties. Cached mode performs no native reads; default mode queries each unique Beam reference once, validates all ratios before population and updates detached ratios. Unavailable -1/-999 or invalid cached ratios cannot choose a winner. Property-handler failures can retain partial managed writes without rollback. Supporting Member/grouping metadata does not perform physical-member identification or geometry automation.

Release build passed with zero warnings/errors and 2,295 offline tests, including 133 new regression cases. Exact installed-package checks verify nineteen Load/twenty Output contracts, matching expanded XML, 203 compiled XML examples, README/eighteen guide examples, steel-record/governing native stand-ins and exact archive/dependency isolation. No new live native or engineering qualification is claimed.

## 1.0.0-preview.20261120 - 20 November 2026

Added `GetMemberEndForcesAsync(string lcTitle, int beamId, int loadCaseId, MemberEnd memberEnd)`, `GetMemberSteelUtilizationRatioAsync(Beam b)`, `GetMemberSteelUtilizationRatios(HashSet<Beam> beams)`, `PopulateMemberSteelUtilizationRatios(HashSet<Beam> forBeams)` and `GetMemberSteelUtilizationRatiosAsync(HashSet<Beam> beams)` with complete matching interface/adapter XML IntelliSense. See [member batch queries](Member-Batch-Queries.md).

Task APIs complete native work on the constructing COM thread before returning a completed or faulted task. The force query returns `Task<MemberForceQueryResult>`, matching the synchronous Core success/failure model: native false retains query identity and null force data, while success requires six written finite Double values in local axes. The beam ratio overload captures its positive ID without hydrating endpoints or populating its ratio.

Batch queries capture original member references and unique positive current IDs, qualify the entire input before dispatch and query sequentially under one operation lease. Inputs are limited to 100,000 entries; nulls and duplicate references/current IDs throw. Empty batches issue no native calls. Observed ID changes during retrieval throw. Returned editable dictionaries own their storage and use reference-identity keys, preserving lookup after member IDs change. A different Beam with the same ID is not the same returned key.

Population retrieves and qualifies every ratio before assigning detached `Beam.UtilizationRatio` values. Native retrieval failures perform no ratio assignments by the method. Property handlers can fail or change metadata after assignment starts; such failures propagate with earlier/current assignments retained and no invented rollback. Added supporting Beam metadata with source ID equality, supplied endpoints and property notifications, without native connectivity or geometry operations.

Release build passed with zero warnings/errors and 2,162 offline tests, including 63 new regression cases. Exact installed-package verification covers nineteen Load and fifteen Output contracts, matching expanded XML, 193 compiled XML examples, README and seventeen guide examples, member-batch/task native stand-ins and exact archive/dependency isolation. No new live native or engineering qualification is claimed.

## 1.0.0-preview.20261115.1 - 15 November 2026

Added `GetBucklingFactor(int bucklingMode)`, `GetBucklingModeDisplacement(int bucklingMode, Node atNode)`, `GetBucklingModeDisplacementAtNode(int bucklingMode, int nodeId)`, `GetMemberSteelUtilizationRatio(int beamId)` and `GetMemberSteelUtilizationRatioAsync(int beamId)` with complete matching interface/adapter XML IntelliSense. See [buckling results](Buckling-Results.md) and [steel-design queries](Steel-Design-Queries.md).

Buckling factors now use a by-reference NaN output sentinel rather than an unwritten initialized zero. Both mode-displacement queries return an immutable `BucklingModeDisplacementResult`, correcting a scalar return for a six-component native array. All components retain native order/signs without inferred axis or normalization changes. Added detached `Node` identity/coordinate metadata without UI dependencies; the node overload reads only its captured positive ID. Successful reads require qualified status and written finite Double outputs. Arrays require six entries and support replacement/nonzero-bound buffers.

Steel ratios preserve nonnegative dimensionless values and documented -1/-999 absence sentinels after successful status. Unknown negative values and failed/unwritten outputs throw. The native ratio belongs to the last parameter block used to design that member. The task API performs native work synchronously on the constructing COM thread and returns an already-completed or faulted task, preserving original failures without worker-thread COM.

All five contracts retain shared current-binding/thread/lifetime/reentrancy guards. No analysis, design, activation, conversion, retry or native mutation is added. Release build passed with zero warnings/errors and 2,099 offline tests, including 144 new regression cases. Exact installed-package verification covers nineteen Load and ten Output contracts, 183 compiled XML examples, README and sixteen guide examples, native stand-ins, matching expanded XML and archive isolation. Native-build and engineering qualification remain pending.

## 1.0.0-preview.20261115 - 15 November 2026

Ported `GetLoadEnvelopeDetail(int envId)`, `GetAllLoadEnvelopeDetails()`, `GetLoadCombination(int id)`, `AreBucklingAnalysisResultsAvailable()` and `GetNumberOfBucklingFactorsComputed()` with complete matching interface/concrete XML IntelliSense. Added detached envelope/combination contracts and [load-definition](Load-Definitions.md) and [buckling observation](Buckling-Results.md) examples.

Envelope detail queries require written classification/count outputs, integer 1 statuses and unique positive Int32 membership arrays. Positive membership fetches recheck classification/count. The all-envelope query returns independent records and enforces a total 100,000-membership budget before excess allocation/fetch. Ordinary combinations verify bounded pair counts and identity, qualified ID/Double coefficient arrays, finite signed factors and matching primary metadata. Zero pair count returns null. An SRSS overall multiplier cannot be represented by `StaadLoadCombination` and raises `NotSupportedException` instead of being discarded.

Buckling availability accepts native Boolean or integral zero/one flags. Computed-factor count accepts the nonnegative Int32 range without fetching factors or issuing availability/analysis requests. All five operations retain the existing current-binding, constructing-thread, lifetime and reentrancy lease with preserved native exceptions and independent managed data.

Release build passed with zero warnings/errors. Passed 1,955 offline tests including 145 new cases. Installed-package checks cover nineteen Load and five Output contracts, matching expanded IntelliSense, 173 compiled XML examples, README and fifteen guide examples, native stand-ins and exact restored archive isolation. Native-build and engineering qualification remain pending.

## 1.0.0-preview.20261114 - 14 November 2026

Ported `GetLoadCase(int lcId, LoadCaseType loadCaseType)`, `GetLoadCases(int fromId, int toId, LoadCaseType loadCaseType)`, `GetLoadCasesFromIds(IEnumerable<int> loadCasesIds, LoadCaseType loadCaseType)`, `GetLoadCasesOfType(LoadCaseType loadCaseType)` and `GetEnvelopeIds()` with expanded matching interface/concrete XML IntelliSense and [consumer examples](Load-Case-Metadata.md).

Family conveniences eagerly read and validate the complete requested inventory. Missing single lookups return null. Inclusive range and requested-ID filters return owned lists, omit absent IDs and retain distinct existing cases. Requested filter IDs are captured and validated once, including duplicate entries toward the 100,000-entry bound. Empty valid filters skip native access. All family operations reject RepeatLoad and undefined values, and return independent metadata records. A malformed unselected case still fails the family read.

Envelope discovery requires integral counts from zero to 100,000 and integer 1 success from `GetEnvelopeIDs`. Every returned ID must be written, positive and unique in an exact-length one-dimensional Int32 array. Replacement/nonzero-bound arrays are supported. Zero count returns a fresh empty set, and a post-fetch count recheck detects changing inventories. Errors propagate through the shared current-binding, constructing-thread and reentrancy lease without partial results or native mutation.

Release build passed with zero warnings/errors. Passed 1,810 offline tests including 84 new lookup/filter/envelope cases. Installed-package verification checks sixteen Load contracts and expanded IntelliSense, compiles 163 XML examples and all thirteen guides, and exercises native stand-ins through an isolated consumer. No live model read or new native-build/engineering qualification was performed.

## 1.0.0-preview.20261113 - 13 November 2026

Ported `GetReferenceLoadCasesFromIds(...)`, `GetLoadCombinationCaseFromId(...)`, `GetLoadCombinationCasesFromIds(...)`, `GetAllLoadCombinationCases()` and `GetAllLoadCasesOfType(LoadCaseType loadCaseType = LoadCaseType.PrimaryLoad)` with comprehensive matching interface/adapter XML IntelliSense. Reused the detached LoadCase/ILoadCase models and existing family/type enums; Load now exposes eleven contracts.

Reference lists retain request order and duplicate IDs as independent records. Combination requests deduplicate IDs, verify membership before any requested titles and return Type=None without engineering type calls. Family enumeration routes to exactly one supported family, with an optional PrimaryLoad default; repeat and undefined families fail explicitly. Shared bounded native inventory qualification preserves Boolean success versus integral counts, Int32 sentinel validation and nonzero array bounds.

All validation and native reads run under one current-binding, constructing-thread and non-reentrant operation lease. Results are eager, with no case activation, factors, assignments, analysis, unit conversion, save or retry. See [load-case metadata](Load-Case-Metadata.md) for method-selection guidance, mutable record/hash semantics and native observation limits.

Passed a Release build with zero warnings/errors and **1,726 offline tests**, including **87 new cases**. Installed-package verification checks eleven Load signatures, expanded matching XML, **153 XML examples**, README and all thirteen Root/domain guides, stand-in metadata behavior, dependency isolation and exact restored archive equality. No live native metadata retrieval or new native-build/engineering qualification was performed.



## 1.0.0-preview.20261112.2 - 12 November 2026

Provides the primary/reference load-case metadata queries, detached load-case models and expanded XML IntelliSense. See [load-case metadata](Load-Case-Metadata.md) for exact contracts and consumer examples.

## 1.0.0-preview.20261112.1 - 12 November 2026

Ported `GetAllPrimaryLoadCases()`, `GetAllReferenceLoadCases()`, `GetPrimaryLoadCaseFromId(int lcId)`, `GetReferenceLoadCaseFromId(int lcId)` and `GetPrimaryLoadCasesFromIds(IEnumerable<int> loadCasesIds)` with full matching XML IntelliSense. Added `LoadCase`, `ILoadCase`, `IEntity` and documented load-family/engineering-type enums without JSON or UI dependencies.

Preserved exact native titles/types, independent editable metadata records and ordered duplicate requests. Qualified bounded Int32 inventories with explicit Boolean-primary versus integral-count semantics, positive unique IDs and count consistency. All validation and native access share the current Load thread/lifetime/reentrancy lease; no case activation, assignments, save or analysis is added. HashSet identity mutation rules and non-atomic native observations are documented.

Passed a zero-warning Release build and 1,639 tests, including 63 new cases. The installed consumer checks signatures/defaults, expanded matching XML, 143 compiled XML examples, thirteen guide examples, independent metadata, native stand-ins, exact restored archive and dependency isolation. No live native metadata/build qualification was performed. See [load-case metadata](Load-Case-Metadata.md).

## 1.0.0-preview.20261112 - 12 November 2026

Ported `Root.RetrieveMemberForces(IEnumerable<int> beams, IEnumerable<int> loadCases)`, `Load.GetLoadCaseTitle(int lcId)`, `Output.AreResultsAvailable()` and both `Output.GetMemberEndForces(...)` overloads with full matching XML IntelliSense. The package now implements all 53 common Root contracts, three optional Root operations and these four Load/Output foundation contracts.

Named status/data results preserve native false without exposing partial data, validate six finite written Double components, retain native units and explicit local/global axes, and keep equal components attached to distinct member/load/end identities. The batch snapshots inputs once, limits requests to 100,000 ends, reads availability once and titles once per distinct case. Force records contain detached scalar data without a WPF/vector/conversion dependency.

Root, Load and Output share constructing-thread, current-binding, disposal and nonreentrancy guards. No analysis, activation, save, conversion, offset transfer or retry is added. Passed a zero-warning Release build and 1,576 tests, including 39 new regressions. Installed-package verification compiles 133 XML examples and twelve guide examples and checks exact restored archive/expanded IntelliSense. No live native result/engineering qualification was performed. See [member-force observations](Member-Force-Queries.md).

## 1.0.0-preview.20261111.2 - 11 November 2026

Ported `RemoveNamedView(string viewName)`, `ModifyNamedView(string viewName, IEnumerable<int> entityIds, NamedViewEntityType entityType, int modificationFlag)`, `ExportModelToAdina(...)`, `GetAnalysisErrorMessages(string modelFilePath)` and `GetAnalysisWarningMessages(string modelFilePath)` with matching complete interface/concrete XML IntelliSense. Core exposes 52 common Root contracts and three optional Root operations.

Named-view updates preserve literal names, snapshot positive identifiers once and retain inspected native arity without invented status outputs or mode meanings. Optional operations require independent `Root.Capabilities` acquisition with exact candidate build and actual member-signature checks. Uncovered builds grant no interface. Captured gateways/grants are invalidated by refresh or disposal, even when the reported version is unchanged.

ADINA export returns immutable raw/recognized status and warning-success information without inventing output paths. Message retrieval retains native statuses, independent read-only text and count provenance. It rejects malformed/unwritten successful outputs, limits buffers to 100,000 messages and permits one fresh-count retry only after native insufficient capacity. Original failures propagate; no implicit analysis, save, thread migration or rollback is added.

Passed a zero-warning Release build and **1,537 offline tests**, including **126 new cases**. The exact installed package checks 52 common contracts and three optional operations, acquisition boundaries, expanded matching XML, **122 compiled XML examples**, README and all eleven Root guide examples, dependency isolation and restored archive equality. No live view mutation, export, message retrieval or new native-build runtime acceptance was performed. See [named-view changes and optional operations](Root-View-Changes-and-Optional-Operations.md) for profiles, usage and limits.

## 1.0.0-preview.20261111.1 - 11 November 2026

Ported `GetProcessHandle()`, `NewSTAADFile(string filePath, LengthInputUnit lengthUnit, ForceInputUnit forceUnit)`, `CreateNamedView(string viewName, int creationFlag)`, `SaveNamedView(string viewName)` and `Quit()` with complete matching interface/adapter XML IntelliSense. Core now exposes fifty Root operations.

Process-handle queries return an immutable, non-owning diagnostic value without assuming handle width or OS usability. Model creation validates units, the destination and the unnamed-session response, then rechecks target absence before native dispatch; these checks are not an atomic no-overwrite guarantee. Named-view operations preserve literal names and inspected native arities without guessing flag meanings or retrying a different signature. Shutdown is explicit and does not dispose the wrapper, save the model or verify process exit.

All five retain constructing-thread, current-binding, disposal and reentrancy guards. Original exceptions propagate, including after native effects; no implicit save, metadata refresh, retry or rollback is introduced. See [model creation, named views and shutdown](Root-Creation-Views-and-Shutdown.md) for exact contracts, examples and qualification limits.

Passed a Release build with zero warnings/errors and **1,411 offline tests**, including **93 new cases**. Installed-package verification checks fifty Root contracts, **104 compiled XML examples**, README usage, all ten Root guides, immutable handle data, expanded/installed XML and exact restored archive equality. No live model creation, view mutation, shutdown or new cross-version native qualification was performed.


## 1.0.0-preview.20261111 - 11 November 2026

Ported `SetFullJobInfo(...)`, `AnalyzeModel(AnalysisEngine engine = AnalysisEngine.StaadPro)`, `GetConnectedProjectInfo()`, `SetConnectedProjectInfo(string projectId, string projectName)` and `SetConnectedProjectInfo(ConnectedProjectInfo projectInfo)` with complete matching interface/adapter XML IntelliSense. Core now exposes forty-five Root operations.

Full job updates validate all thirteen strings and retain native field order and literal date text. Analysis explicitly submits documented engine code `-1` without inventing a status for the native void return. CONNECTED queries qualify zero/one flags and both string outputs, discard partial data on native false and return immutable named project snapshots. Both assignment overloads preserve nonblank text and report the qualified native flag through one shared internal implementation.

All five contracts retain constructing-thread, current-binding, disposal and reentrancy guards under one wrapper lease. Native responses do not certify persistence, cloud synchronization or analysis completion. Original failures can follow partial effects. See [full job information, analysis engine and CONNECTED projects](Root-Job-Analysis-and-Projects.md) for validation, examples and native limits.

Passed a Release build with zero warnings/errors and **1,318 offline tests**, including **112 new cases**. Installed-package verification checks forty-five Root contracts, **94 compiled XML examples**, README usage, all nine Root guides, expanded/installed XML and exact restored archive equality. Managed native stand-ins cover the new operations. No live model mutation, analysis, project assignment or new cross-version native qualification was performed.

## 1.0.0-preview.20261110 - 10 November 2026

Ported `SetInputUnitForLength(LengthInputUnit lengthUnit)`, `SetInputUnitForForce(ForceInputUnit forceUnit)`, `SetShortJobInfo(string jobName, string jobClient, string engineerName)`, `SetShortJobInfo(ShortJobInfo jobInfo)` and `UpdateStructure()` with complete matching interface/adapter XML IntelliSense. Core now exposes forty Root operations.

Individual unit setters validate and submit one Int32 code without changing the other unit or refreshing snapshots. The individual length setter accepts documented feet alias 1; combined `SetInputUnitsEx` retains its named-value validation. Job setters preserve exact non-null text and native argument order; the model overload captures each short field once and reuses the existing `ShortJobInfo` model. Structure updates invoke the native no-argument member without adding analysis, save or read-back.

All five operations retain constructing-thread, current-binding, disposal and reentrancy guards under one wrapper lease. Native void completion does not certify persistence or read-back, and original failures can follow partial effects. See [unit, job-information and structure updates](Root-Model-Edits.md) for validation, cache ownership and native limits.

Passed a Release build with zero warnings/errors and **1,206 offline tests**, including **74 new cases**. Installed-package verification checks forty Root contracts, **84 compiled XML examples**, README usage, all eight Root guides, expanded/installed XML and exact archive restoration. No live model mutation or new native cross-version qualification was performed.

## 1.0.0-preview.20261109 - 9 November 2026

Ported `AnalyzeEx(SilentMode silentMode, HiddenMode hiddenMode, WaitAnalysisMode waitMode)`, `Analyze()`, `IsAnalyzing()`, `GetAnalysisStatus()` and `GetAnalysisStatus(string modelFilePath)`. Preserved names, parameter types/order and return contracts with complete matching interface/adapter XML IntelliSense. Core now exposes thirty-five Root operations.

Added `AnalysisResult`, `HiddenMode`, `WaitAnalysisMode`, `AnalysisStatus` and immutable `AnalysisStatusResult`. Validated analysis flags before a single native request, retained exact integer statuses and rejected malformed native values. Status queries distinguish retrieved state from successful analysis, retain nullable diagnostics and CPU seconds, and require valid outputs for completed states. The current-model overload captures one filename and its path base before querying status under the same wrapper lease.

Retained constructing-thread, binding, disposal and reentrancy checks without implicit saving, retries, polling, timeout, result parsing or cache refresh. Native analysis requests can affect output files and display UI; `Return` mode does not certify completion. See [analysis contracts](Root-Analysis.md) for status meanings, validation policy and native limits.

Passed a Release build with zero warnings/errors and **1,132 offline tests**, including **157 new analysis cases**. The installed package consumer checks thirty-five signatures, supporting models/enums, **74 compiled XML examples**, README usage and all seven Root guides, expanded/installed XML and exact archive equality. No live STAAD analysis or new native mutation/cross-version qualification was performed.

## 1.0.0-preview.20261108.2 - 8 November 2026

Ported `SetInputUnitsEx`, `AwaitModelHandle`, `OpenStaadModel`, `CloseCurrentStaadModel` and `CloseWriteReopenStaadFile` with preserved signatures, optional defaults and complete matching interface/adapter XML IntelliSense. Core now exposes thirty Root operations.

Validated named length/force codes before one native unit change; cached units refresh only on explicit request. Added exact normalized full-path polling with a captured working directory, elapsed-time deadlines, bounded sleeps and original native errors. Opening requests once and identifies the target; a timeout does not undo that request or establish domain readiness.

Guarded close/write/reopen with current-model matching, an independent line snapshot, pre-close file validation, bounded blank-model/lock-release waiting and sibling temporary-file publication without truncation fallback. Intervening model changes stop writing or reopening where detected. Partial native/file effects and original failures remain visible. All workflows retain the wrapper's constructing-thread, current-binding and non-reentrancy lease. See [input units and model coordination](Root-Model-Coordination.md) for contracts and limits.

Passed **975 offline tests**, including **174 new cases**, and a Release build with zero warnings/errors. Installed-package verification checks thirty Root signatures, compiles **64 XML examples**, README usage and all six Root guides, verifies expanded IntelliSense and archive equality, and exercises managed workflows against an owned temporary file. No live model mutation or new cross-version native qualification was performed.

## 1.0.0-preview.20261108.1 - 8 November 2026

Provides the twenty-five documented Root operations, including detached job-information snapshots and ownership-checked asynchronous main-window controls. Retains the signatures, output/date validation, notifications, native error behavior and wrapper lifetime/thread guards described in [job information and window contracts](Root-Job-Information-and-Windows.md).

Verified the Release package through an isolated consumer with **54 compiled XML examples**, README usage and all five Root guides. The unchanged implementation passed **801 offline tests**. Live STAAD/COM/window qualification remains separate.


## 1.0.0-preview.20261108 - 8 November 2026

Ported `GetShortJobInfo()`, `GetFullJobInfo()`, `MinimizeStaadMainWindow()`, `MaximizeStaadMainWindow()` and `RestoreStaadMainWindow()`, preserving public names and signatures. Added detached editable `ShortJobInfo` and `FullJobInfo` models, factories and managed property-change notifications without a WPF dependency.

Validated every native job-output string before constructing snapshots or parsing dates. Preserved empty text and culture-aware date parsing, with blank dates represented by `DateTime.MinValue`. Window operations read the live HWND and process ID, validate window ownership, and submit the documented asynchronous state commands. Reused the existing thread, binding, disposal and non-reentrancy guards without retries, polling or automatic cache refresh. See [job information and window contracts](Root-Job-Information-and-Windows.md).

Passed a Release build with zero warnings/errors and **801 offline tests**, including **177 new cases**. The installed consumer checked all 25 Root signatures and compiled **54 XML examples**, README usage and all five Root guides. Live STAAD/COM/window qualification remains separate.


## 1.0.0-preview.20261107 - 7 November 2026

Ported `SetSilentMode(SilentMode silentMode)`, `SaveModel(SilentMode silentMode)`, `OpenSTAADFile(string filePath)`, `CloseSTAADFile()` and `ShowApplication()` with complete matching interface/adapter XML IntelliSense. Added the 0/1 `SilentMode` enum and shared operation validation, preserving names, parameter contracts and fluent adapter returns.

Native silent-mode returns describe the previous setting; the managed Boolean reports normal invocation or caught native failure. Wrapper and argument errors still throw. Save/open/close completion does not certify persistence, identity or readiness. Paths normalize before one native open call; transitions leave refresh and readiness coordination to the caller. All operations reuse the constructing-thread, lifetime and non-reentrancy lease without retries or automatic model acquisition. See [model lifecycle contracts](Root-Model-Lifecycle.md).

Passed a Release build with zero warnings/errors and **624 offline tests**, including **69 new lifecycle cases**. An isolated installed-package consumer checked all twenty Root contracts, compiled **44 XML examples** plus README and all four Root guides, and verified expanded XML, dependency isolation and exact archive restoration. No live model mutation or new native compatibility qualification was performed.

## 1.0.0-preview.20261106.1 - 6 November 2026

Derived uppercase `.ANL` paths without mixing model snapshots. Kept explicit blank output distinct from malformed/unwritten data and compared normalized complete paths rather than basenames. Relative comparisons use one captured working directory, and explicit character checks retain the Framework rejection of quotes, angle brackets, pipe, NUL and controls under modern Windows Path APIs. Native failures propagate without retries, acquisition or model mutations. Added the [Root path/identity guide](Root-Path-Helpers.md).

## 1.0.0-preview.20261106 - 6 November 2026

Ported `GetStaadFileFullPath()`, `GetStaadFileFolder()`, `IsPhysicalModel()`, `GetErrorMessage()` and `GetProcessId()` from the reviewed Framework Root contracts. Each performs one native query on the current bound root. Preserved exact native text, distinguished explicitly empty from unwritten reference outputs, and validated physical-model flags and positive UInt32 process identifiers without permissive coercion.

Shared the existing wrapper operation guard with live reads. Disposed/replaced bindings, another thread and nested Root/refresh/disposal operations are rejected before dispatch. Native failures propagate unchanged without retries or secondary error-message probes. Reused the acquisition path reader and existing native-value validation; no new public return models, tuples or reference-output parameters were required.

Added complete matching interface/concrete XML IntelliSense and the [Root observations guide](Root-Observations.md). Passed 356 managed tests, including 181 new observation cases. A fresh installed-package consumer exercises all ten Root contracts and compiles 24 XML examples plus README and both Root guides. The Windows .NET 9 package remains independent of WPF and the Framework assembly.

## 1.0.0-preview.20261105.2 - 5 November 2026

Retained the five cached Root observations: `GetApplicationVersion()`, `GetBaseUnit()`, `GetInputUnitForForce()`, `GetInputUnitForLength()` and `GetMainWindowHandle()`. They reuse the wrapper snapshots without extra native dispatch, return an independent native version and preserve input-unit fallback and non-owning window semantics. Reacquire `wrapper.Root` after `RefreshComObjects()`; `RefreshUnits()` preserves the binding.

The Windows .NET 9 target, managed API and XML IntelliSense remain unchanged. The [Root metadata guide](Root-Metadata.md) describes lifecycle guards, unit provenance, examples and native qualification limits. Other domain adapters are binding-only placeholders.

## 1.0.0-preview.20261105.1 - 5 November 2026

Ported `GetApplicationVersion()`, `GetBaseUnit()`, `GetInputUnitForForce()`, `GetInputUnitForLength()` and `GetMainWindowHandle()` to `IOSRoot` and its single `OSRootAdapter`. Preserved the parameterless Framework contracts and existing named models/enums under Core namespaces. All five reuse wrapper snapshots with zero additional COM calls. Native version is copied and remains distinct from the host assembly version; unit compatibility fallbacks and borrowed window-handle semantics are explicit.

Shared the wrapper lifecycle guard with Root observations. Disposed/replaced adapters, another thread and callback reads during refresh are rejected. `RefreshUnits()` keeps the binding and updates units; `RefreshComObjects()` requires reacquiring Root. Added complete matching interface/concrete XML IntelliSense, usage examples and the [Root metadata guide](Root-Metadata.md).

Passed a Release build with zero warnings/errors and **175 offline tests**, including **43 Root cases**. A fresh PackageReference consumer exercises the five observations and binding guards, compiles **fourteen XML examples** plus README/guide usage, and checks installed XML and dependency isolation. Core now exposes five common Root contracts. Other domains remain binding-only. This increment adds no live native qualification; cross-version, units and engineering acceptance remain separate.

## 1.0.0-preview.20261105 - 5 November 2026

Established the clean Windows .NET 9 wrapper and acquisition foundation as an independent package. Preserved the complete wrapper property surface and introduced binding-only domain placeholders; no Root, Geometry, Load or other domain automation functions are included. OSRoot is the first planned domain.

Provided native OLE discovery, verified process/model identity and readiness, retained-root support, explicit discovery policies and diagnostic results. Unified the direct factory with the Core engine, installed-executable lookup and new temporary-model fallback when no supported application exists. Get reports failures through a focused exception; Acquire provides full outcome details. ExistingBlank explicitly leaves the requested file unopened.

Added wrapper owner-thread/reentrancy guards and modern process argument handling. Kept file preparation non-overwriting and avoided automatic process termination. Removed unrelated domain, WPF and third-party runtime dependencies from this first release scope.

Passed a Release build and 132 focused offline tests. A read-only named-session attachment check succeeded on STAAD.Pro 26.00.01.491 with explicit incomplete-discovery diagnostics. Broader STAAD build/license/startup qualification remains separate. This locally prepared preview is intended for manual NuGet.org upload after package-consumer validation.
