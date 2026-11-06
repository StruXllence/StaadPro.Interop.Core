# Wrapper and acquisition foundation

## Scope

This preview implements OpenStaadWrapper and its acquisition prerequisites. It preserves all thirty source properties plus IsConnected, two constructors, RefreshUnits, RefreshComObjects and Dispose. Root now exposes five guarded cache observations and five guarded live model/process observations; other domain interfaces/adapters retain only binding properties. See [Root metadata](Root-Metadata.md) for the implemented contracts. Adapters remain single, non-partial classes.

The source reference is the separate [Framework project](https://github.com/StruXllence/StaadPro.Interop/tree/20821c50c9e1e3a3955d7e111cdf906d5b921588/src/StaadPro.Interop). It was inspected read-only. Earlier inherited Core experiments are not the implementation scope of this first clean package.

## Framework philosophy and Core changes

| Framework behavior | Core foundation |
| --- | --- |
| Discover existing roots and prefer a matching model | Preserve attachment preference; additionally inventory supported processes and verify PID plus start time |
| Await a known loading model instead of reopening it | Retain the known candidate and retry readiness within the logical budget |
| Read GetSTAADFile through its reference output | Use an internal BSTR-typed string sentinel; untouched/non-string results are not blanks |
| Probe Geometry and Load before returning | Validate strict integral counts, construct the full wrapper, then recheck captured interfaces and final identity |
| Open/create a target when needed | Validate launcher, coordinate a target, publish a complete non-overwriting STD file and verify the newly launched process |
| Start an installed application when no model/session exists | The static factory attaches first and can create an owned temporary STD target when no supported application runs; the instance provider without a path remains attachment only |
| Trace broad failures and return null | Acquire exposes outcome/diagnostics; static Get throws a focused acquisition exception on failure |
| Cache units, native version and window information | Preserve wrapper properties and documented fallback semantics; enforce owner-thread/non-reentrant refresh/disposal |

Core uses native OLE APIs instead of the Framework-only Marshal.GetActiveObject helper. It does not require WPF or a vendor interop assembly. Built-in COM remains Windows-only; Native AOT/trimming are not advertised as supported.

## Entry points

| API | Contract |
| --- | --- |
| OpenStaadWrapperProvider.Get(path) | Direct wrapper or OpenStaadAcquisitionException; default resolver uses the Core engine |
| OpenStaadWrapperProvider.Acquire(path, options, token) | Named result with process, path, outcome and diagnostics; automatically discovers a conventional installed launcher if not supplied |
| OpenStaadWrapperProvider.GetRunning() | Built-in attachment only, no file creation/startup; null on operational attachment failure |
| OpenStaadSessionProvider | Advanced per-STA owner for retained/registered roots; copied explicit options; Dispose when done |
| SessionProvider.Acquire()/Get() without a path | Attachment only; never starts STAAD |
| new OpenStaadWrapper(rawRoot) | Binds an already supplied root; does not discover, start or validate full acquisition readiness |

A custom Resolver affects the single-argument static Get only and must honor its own identity/apartment contract. It cannot be assumed to inherit the built-in engine's checks. Explicit Acquire and GetRunning bypass it. Do not share an instance provider across arbitrary task-pool threads.

## Selection and identity

1. Inventory Bentley.Staad processes in the caller's Windows session using PID/start time.
2. Combine ROT filename/class/ProgID candidates, the registered active root and retained/registered roots.
3. Map native GetProcessId to live inventory. Read the actual native filename; reject unreadable or conflicting aliases.
4. Prefer an identified target and wait if it is busy. Without a target, prefer a ready named application.
5. Apply strict or best-effort discovery policy, then try verified blanks.
6. If permitted, coordinate target fallback, rediscover and launch at most once in the instance request.
7. Recheck captured interfaces, native filename, PID and process start time before returning a wrapper.

~~~mermaid
flowchart TD
    A([Acquire]) --> B[Inventory processes and discover roots]
    B --> C{Matching ready named candidate?}
    C -->|Yes| V[Construct wrapper and revalidate identity]
    C -->|No| D{Known identity or policy requires waiting?}
    D -->|Yes| R[Bounded observation retry]
    D -->|No| E{Reachable verified blank?}
    E -->|Yes| V
    E -->|No| F{Target launch permitted?}
    F -->|No| X([Diagnostic failure])
    F -->|Yes| G[Coordinate; prepare target; launch once]
    G --> H[Await the exact launched process and target]
    H -->|Ready| V
    H -->|Terminal or expired| X
    V -->|Valid| S([Wrapper and actual outcome])
    V -->|Changed| R
    R -->|Budget remains| B
    R -->|Expired or cancelled| X
~~~

After startup, retry remains in launched-process verification and never initiates a second startup. Known busy targets and mapped unreadable identities wait rather than falling through to launch.

A blank requires an explicitly empty filename and zero nodes, members, plates, solids, primary loads, reference loads and combinations. Invalid/negative/unavailable counts are not zero. ExistingBlank leaves the requested file unopened. The checks do not prove the absence of every native setting/definition or prevent external edits immediately afterwards.

An application's PID alone does not provide its OpenSTAAD object. Unmapped blank startup windows cannot be claimed as safely bindable. The instance provider retains previously obtained roots and accepts RegisterRoot for an object the caller already has. Strict policy fails rather than guessing when complete coverage is required; best effort can launch while an inaccessible application hides a target.

## Installation, creation and ownership

Automatic lookup checks App Paths and conventional Program Files locations for 2026, 2025 and 2024, newest conventional version first. Supply StaadExecutablePath for nonstandard installations or an explicit launch-version requirement. An already-open candidate's native StaadVersion remains authoritative; a launcher path does not filter attachment versions.

The static no-path factory permits temporary-target fallback only after NoRunningSession. A busy, inaccessible or unreadable existing application is not replaced by that route. Generated targets are under the user's local application-data Models folder and are retained after later failure. Existing files are never overwritten. A returned launched PID is evidence, not automatic permission to close it.

The provider never opens a file into an existing named/blank instance during acquisition. It starts the configured executable with ArgumentList, then verifies the process actually hosting the target. Redirection and premature process exit are explicit failures. An existing attachment has IsDedicated false; only a verified newly launched host has it true. Disposal never shuts down STAAD.

## Metadata, refresh and lifetime

StaadVersion is the native version and returns an independent copy. ApplicationVersion is the host assembly version. Base/input unit properties are cached. A failed/unparseable input-unit observation may fall back to Kip/Feet for Imperial base units or kN/Meter otherwise; a fallback is not a proven native unit. A missing base member yields Unknown. The window handle is non-owning and can be zero.

RefreshUnits rereads units without rebinding. RefreshComObjects rebuilds adapters and metadata in place; reacquire domain bindings afterward. Root metadata reads reject replaced adapters even when the underlying native object is unchanged. They share the constructing-thread/non-reentrancy guard with refresh/disposal and reject reads during either refresh, preventing callbacks from observing a partially refreshed cache. Refresh is not a native transaction and can leave partially refreshed bindings if a native call fails. Guards do not prevent external model edits or validate arbitrary raw COM use.

Dispose clears native/managed bindings without final-releasing shared RCWs or disposing injected host services. It must run on the constructing thread while live; repeated completed disposal is harmless. Retained Root adapters reject managed observations after disposal. IsConnected is a managed retained-state observation, not server liveness.

## Reliability boundaries and qualification

Operational success always requires verified identity/readiness. Timeout/cancellation are observed between synchronous native calls; they cannot safely abort a hung COM call. Missing installation, license dialogs, permissions, startup failures and inaccessible roots remain possible. The library reports those limits and never fabricates a successful wrapper.

The foundation's 132 offline tests cover wrapper metadata/parity, lifecycle/context, process/model changes, blank validation, discovery policy, cancellation, one-shot startup, redirects, retained roots and atomic file races. Root contributes 43 cache and 181 live-observation cases for 356 total tests. Package verification checks a fresh reference, 24 XML examples, README/two Root-guide compilation, ten guarded Root observations and dependency isolation. Native acceptance across supported builds, blank states and licensing configurations remains a release qualification task.

Read-only native checks on 5 November 2026 succeeded for named-session attachment and matching-target reuse on STAAD.Pro 26.00.01.491 with Windows .NET 9. One snapshot was incomplete and reported an unmapped application; a later snapshot was complete. Matching-target acquisition returned the same verified process without a startup attempt. No file was opened/created and no application was launched or closed by these checks.
