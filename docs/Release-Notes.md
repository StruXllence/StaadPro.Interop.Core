# Release notes

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
