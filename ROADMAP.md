# StaadPro.Interop.Core roadmap

**Updated: 6 November 2026.** This independent repository contains the wrapper/acquisition foundation and fifteen OSRoot observations. Binding placeholders preserve the remaining domain surface while APIs are implemented and qualified incrementally.

## Current release scope

| Area | Current state |
| --- | --- |
| Runtime | Windows .NET 9; SDK 9.0.3xx selected by global.json for Visual Studio 2022 17.14 |
| Wrapper | All 30 source properties plus IsConnected, two constructors, RefreshUnits, RefreshComObjects and Dispose |
| Acquisition | Native OLE discovery, PID/start-time identity, filename-output validation, readiness, blank selection, retained/registered roots, explicit policies and diagnostics |
| Startup | Explicit or detected installation; coordinated non-overwrite model creation, one startup attempt per instance request and final process/model verification |
| Lifecycle | Constructing-thread and non-reentrancy checks for wrapper refresh/disposal; no automatic process shutdown |
| Domains | Five cached, five live model/process observations and five path/identity helpers; ten other domains remain binding-only |
| Verification | 555 passing managed tests; 34 compiled XML examples plus README/three Root-guide examples; native acceptance remains separate |
| Package | StaadPro.Interop.Core preview; manually uploaded after package verification |

## Implementation order

| Phase | Scope | Exit criteria |
| --- | --- | --- |
| 0: Wrapper foundation | Wrapper, provider, acquisition options/results, identity/readiness, metadata/units, installation lookup, Windows helpers and bindings | Focused fault tests, compiled XML examples, independent package consumer and controlled native acceptance for declared builds |
| **1: OSRoot** | Metadata projections first, then model observation/lifecycle, job/project information, analysis, window/process operations and optional capabilities | Every method has an approved contract, XML usage guidance, managed tests and a native qualification disposition |
| 2: Geometry | Named entity contracts, creation/readback, connectivity/groups, surfaces/meshes and qualified asynchronous execution | Explicit units/indices, ownership, numerical invariants and no unreviewed thread-pool COM calls |
| 3: Load | Main-source cases/assignments/readback and managed compositions before native-only expansion | Existing types reused, named group/results replacing tuple contracts, precise native status/output handling |
| 4: Property | Materials, sections/specifications, attributes and qualified UPT/catalog operations | Marshaling/layout/version evidence; unresolved contracts remain explicitly gated |
| 5: Output | Availability/units, displacements/reactions, member/plate results, design observations and detached transforms | Coordinate/sign/units and engineering checks, bounded extraction and supported async context |
| 6: Support and Commands | Support definitions/assignments and explicit analysis/design command configuration | Clear identity and mutation/partial-completion semantics |
| 7: Design | Common briefs/parameters, then verified optional/version-specific contracts | Shared implementations for unchanged contracts and internal strategies for actual differences |
| 8: Table and View | Report/table operations and graphical view workflows | Correct indexing, UI/context behavior and acknowledgments without invented success flags |
| 9: IDEA StatiCa | Models and pure transforms, followed by extraction/mapping/export | Prerequisite domains, equilibrium/units/axes and artifact-delivery validation |
| 10: Approved native expansion | Native-only operations not supplied by reviewed source workflows | Explicit contract evidence and approval for parked Load/Property work |

OSRoot is the first domain; its five metadata observations reuse the wrapper cache, and five model/process observations query the bound root. Internal metadata/readiness calls used solely by acquisition do not count as additional Root ports. Other placeholder interfaces will receive signatures only alongside implementations, documentation and tests. No partial adapters will be introduced.

## OSRoot first increments

1. Review the current Framework Root contracts and cached wrapper projections. Native version comes from StaadVersion, not the host ApplicationVersion.
2. Establish descriptive parameters, named models/enums and return/status conventions before dispatch implementation. Reuse suitable existing source types; no new public tuples or reference-output APIs.
3. Implement wrapper-backed version, base/input units and window-handle observations, documenting cache/fallback provenance.
4. Implement model/path/process observations, preserving BSTR-output semantics and precise empty/unwritten distinctions.
5. Implement model creation/open/save/close and job/project operations with explicit side effects and lifetime behavior.
6. Implement analysis operations/status/results, then separately acquired optional capabilities based on actual native version/member evidence.

Completed `GetApplicationVersion()`, `GetBaseUnit()`, `GetInputUnitForForce()`, `GetInputUnitForLength()` and `GetMainWindowHandle()`. These five parameterless contracts retain their Framework return types under the Core namespace. They perform no extra native calls, share the wrapper's lifecycle guard and reject replaced/disposed bindings. See [Root metadata](docs/Root-Metadata.md) for source mapping, modernization decisions, tests and qualification limits.

Also completed `GetStaadFileFullPath()`, `GetStaadFileFolder()`, `IsPhysicalModel()`, `GetErrorMessage()` and `GetProcessId()`. These live observations preserve native outputs and share the same binding/lifecycle guard. See [Root observations](docs/Root-Observations.md).

Also completed `GetStaadFileNameOnly()`, `GetAnlFileFullPath()`, `GetOpenedStaadFilename()`, `IsTarget(string)` and `ModelIsOpen(string)`. They share one native filename read per helper, preserve blank/malformed distinctions and compare complete normalized paths. See [path helpers](docs/Root-Path-Helpers.md).

The Framework reference has 53 common Root methods and three optional contracts. Core now exposes fifteen common contracts; 38 common and three optional contracts remain pending. Next review main-source model lifecycle operations, beginning with silent mode, save, open, close and application display, before job/project and analysis work. Define explicit mutation/status/lifetime contracts before implementation. These are planning counts, not native qualification claims.

## Per-function modernization gate

Every method must record its source mapping, parameters/return, native outputs/statuses, units/indices, state/lifetime, side effects/retry policy, type reuse, modern-.NET decision, meaningful tests, XML example and native qualification limit. An unchanged implementation needs a reason; optimization is not mandatory when native cost dominates or the existing code is appropriate.

Prioritize fewer unnecessary COM calls, bounded snapshots, single enumeration, appropriate modern process/native APIs and measured managed algorithms. Use worker threads only for detached managed data, not arbitrary raw COM operations. Do not trade numeric correctness, duplicates/order, validation or model identity for benchmark gains.

## Acquisition reliability gates

- Preserve matching-target priority and await identified busy models instead of opening duplicates.
- Distinguish application count from reachable-root count. No arbitrary PID-to-blank-root binding is claimed.
- Require explicit empty filename and seven valid zero counts for blank reuse. Reject unavailable/negative counts.
- Revalidate captured interfaces, PID/start time and path after wrapper construction.
- Keep creation non-overwriting and startup bounded; never terminate existing user applications during recovery.
- Qualify zero/one/multiple processes, blanks, unreadable roots, PID reuse, state changes, licensing/dialogs, redirection, cancellation and file races.
- Return a verified success or clear diagnostic failure. A synchronous hung COM call cannot be made interruptible by a stopwatch or Task.Run.

## Runtime and release policy

.NET 9 is the primary target for the current Visual Studio 2022 workflow. SDK 10.0.403 successfully built this same net9.0-windows solution with zero warnings/errors. It may be used in an isolated CLI lane to evaluate a future .NET 10 target; this is not a second shipped asset or a claim of Visual Studio 2022 support for net10.0. Both .NET 8 and .NET 9 reach servicing end on 10 November 2026, so runtime support must be revisited before a long-term supported release. See [SDK/Visual Studio compatibility](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs) and [runtime support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

Build the package with current README/release notes, verify a fresh PackageReference consumer and its XML documentation, and publish manually. Keep preview status while the supported native contracts and deployment scope are being qualified. Framework and Core remain distinct repositories and packages.

[Wrapper design and Framework comparison](docs/Wrapper-Foundation.md) · [Detailed modernization plan](docs/Core-Implementation-Plan.md) · [Installation](docs/Installation.md) · [Publishing](docs/Publishing.md)
