# Core implementation plan

## Foundation baseline

The first public tree established wrapper/acquisition prerequisites and empty domain bindings. The current increment adds five guarded OSRoot metadata observations without restoring the inherited full-adapter experiment. Domain methods are introduced alongside reviewed implementations, XML documentation and tests, with OSRoot first. See the [roadmap](../ROADMAP.md) and [metadata contracts](Root-Metadata.md).

The immediate target is net9.0-windows using SDK 9.0.3xx and Visual Studio 2022 17.14. SDKs 9.0.304 and 10.0.403 are installed on the reviewed PC. An isolated CLI SDK 10 lane can verify the same net9.0 target or evaluate a future net10.0 target; it does not make net10.0 supported in Visual Studio 2022. Runtime servicing must be revisited before a long-term supported release.

## Phase 0 acceptance

- Preserve the complete wrapper surface, raw objects and host/model service contracts.
- Keep acquisition identity/readiness inside internal services, independent of future OSRoot methods.
- Reuse the verified engine for both the static factory and advanced retained-root provider.
- Attach without reopening an existing target; distinguish verified blank from an opened file.
- Discover an installed launcher or accept an explicit executable; create a target only under the request's policy.
- Preserve files/user applications, reject malformed native responses and never blindly replay unknown mutations.
- Verify wrapper owner-thread/reentrancy, refresh/disposal and metadata fallback behavior.
- Produce a standalone package with complete XML, current README/release notes and a fresh consumer.
- Record native qualification limits explicitly; no universal acquisition or hard in-process deadline is claimed.

## Per-function porting procedure

1. Inspect the read-only Framework/source function, relevant models/enums/helpers and installed native contract. Identify managed alternatives before reporting a native name as missing.
2. Define the managed signature, parameter names/types, return model, enum meanings, optional overloads and error semantics. Prefer existing named types; no new tuples or public ref/out payloads.
3. Verify exact native outputs, BSTR/SAFEARRAY shapes, index bases, units, order/duplicates, empty/unwritten distinctions and operation-specific statuses.
4. Identify owning COM context, binding lifetime, required model state, mutation/partial-completion behavior and safe retry limits.
5. Implement the interface and single non-partial adapter with internal helpers where justified. Share unchanged version contracts; use internal polymorphism only for evidenced differences, with optional capabilities acquired after native/member checks.
6. Assess modern-.NET improvements: single enumeration, owned bounded snapshots, appropriate collections/Span for pure computation, native-call reduction, modern process/native APIs and qualified async dispatch. Record why an unchanged implementation remains appropriate; do not optimize every function mechanically.
7. Test meaningful valid, boundary, malformed-native, state/lifetime and failure cases. Qualify native/engineering behavior against controlled models for the claimed builds, or record the exact support gate.
8. Write XML summaries, parameters, returns, exceptions, units/preconditions and differences from similar methods. Compile examples through a package consumer and update coverage/status.

Parallelize detached managed processing, not raw COM calls through Task.Run. A Channel/Task does not create a pumping STA. Pooling must not change native array length/lifetime. Native AOT, trimming, unsafe optimizations and serializer replacements require separate evidence; they are not automatic modernizations.

## OSRoot sequence

Start with wrapper-backed native version, units and window information; distinguish native StaadVersion from host ApplicationVersion and document snapshots/fallbacks. Then implement file/process/model observations, model lifecycle/job/project operations, analysis operations/results and optional version-aware capabilities. No function is considered complete from an interface signature or stub alone.

The Framework Root surface has 53 common and three optional contracts. Five common metadata observations are now exposed in Core; 48 common and three optional contracts remain pending. Next review file/folder, physical-model, error-text and process-ID observations. Other domain inventories are references for later phases rather than present Core functionality. Main-source workflows take priority over native-only expansion; parked Load/Property contracts remain approval-gated. Review dependency order before expanding Output/Support/Design/IDEA workflows.

## Reliability and release gates

Test zero/one/multiple processes, reachable/unmapped blanks, known busy targets, conflicting/unwritten identity, PID reuse, external transitions, strict coverage, licensing/dialog states, cancellation, coordination, launch redirection/exit and file races. Use read-only attachment or explicitly owned test models; never terminate unverified user processes.

Direct Get returns a verified wrapper or a typed diagnostic exception. Acquire exposes detailed outcomes. No in-process implementation can guarantee a wrapper when STAAD/COM is unavailable or permanently blocked. A true hard deadline would require a separately designed broker owning all native calls and detached IPC results; a raw wrapper cannot cross that boundary.

Each release must reconcile implemented contracts, tests/native evidence, XML examples, dependency/asset identity, README/release notes and fresh consumer behavior. Keep Core and Framework repositories/packages distinct and publish manually after verification.
