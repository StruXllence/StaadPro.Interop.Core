# Release notes

## 1.0.0-preview.20261106.1 - 6 November 2026

Ported the main-source `GetStaadFileNameOnly()`, `GetAnlFileFullPath()`, `GetOpenedStaadFilename()`, `IsTarget(string staadFileFullPath)` and `ModelIsOpen(string fileFullPath)` contracts. Preserved their public names, parameter names and return types with complete matching interface/concrete XML. All five reuse one validated native filename snapshot per call and the existing wrapper operation lease.

Derived uppercase `.ANL` paths without mixing model snapshots. Kept explicit blank output distinct from malformed/unwritten data and compared normalized complete paths rather than basenames. Relative comparisons use one captured working directory, and explicit character checks retain the Framework rejection of quotes, angle brackets, pipe, NUL and controls under modern Windows Path APIs. Native failures propagate without retries, acquisition or model mutations. Added the [Root path/identity guide](Root-Path-Helpers.md).

Passed **555 managed tests**, including **199 new path cases**. A fresh installed-package consumer exercises all fifteen Root contracts and compiles **34 XML examples**, README usage and all three Root guides. The Release build reports zero warnings/errors. Core now exposes **15 of 53 common Root contracts**; **38 common and three optional contracts remain pending**. Other domains remain binding-only. No new live native or engineering qualification is claimed.

## 1.0.0-preview.20261106 - 6 November 2026

Ported `GetStaadFileFullPath()`, `GetStaadFileFolder()`, `IsPhysicalModel()`, `GetErrorMessage()` and `GetProcessId()` from the reviewed Framework Root contracts. Each performs one native query on the current bound root. Preserved exact native text, distinguished explicitly empty from unwritten reference outputs, and validated physical-model flags and positive UInt32 process identifiers without permissive coercion.

Shared the existing wrapper operation guard with live reads. Disposed/replaced bindings, another thread and nested Root/refresh/disposal operations are rejected before dispatch. Native failures propagate unchanged without retries or secondary error-message probes. Reused the acquisition path reader and existing native-value validation; no new public return models, tuples or reference-output parameters were required.

Added complete matching interface/concrete XML IntelliSense and the [Root observations guide](Root-Observations.md). Passed 356 managed tests, including 181 new observation cases. A fresh installed-package consumer exercises all ten Root contracts and compiles 24 XML examples plus README and both Root guides. The Windows .NET 9 package remains independent of WPF and the Framework assembly.

Core now exposes ten of 53 common Root contracts; 43 common and three optional contracts remain pending. Other domains remain binding-only. Native blank-model, error-state and servicing-build qualification remains separate; this increment adds no live native acceptance.

## 1.0.0-preview.20261105.2 - 5 November 2026

Retained the five cached Root observations: `GetApplicationVersion()`, `GetBaseUnit()`, `GetInputUnitForForce()`, `GetInputUnitForLength()` and `GetMainWindowHandle()`. They reuse the wrapper snapshots without extra native dispatch, return an independent native version and preserve input-unit fallback and non-owning window semantics. Reacquire `wrapper.Root` after `RefreshComObjects()`; `RefreshUnits()` preserves the binding.

The Windows .NET 9 target, managed API and XML IntelliSense remain unchanged. The [Root metadata guide](Root-Metadata.md) describes lifecycle guards, unit provenance, examples and native qualification limits. Other domain operations remain pending.

## 1.0.0-preview.20261105.1 - 5 November 2026

Ported `GetApplicationVersion()`, `GetBaseUnit()`, `GetInputUnitForForce()`, `GetInputUnitForLength()` and `GetMainWindowHandle()` to `IOSRoot` and its single `OSRootAdapter`. Preserved the parameterless Framework contracts and existing named models/enums under Core namespaces. All five reuse wrapper snapshots with zero additional COM calls. Native version is copied and remains distinct from the host assembly version; unit compatibility fallbacks and borrowed window-handle semantics are explicit.

Shared the wrapper lifecycle guard with Root observations. Disposed/replaced adapters, another thread and callback reads during refresh are rejected. `RefreshUnits()` keeps the binding and updates units; `RefreshComObjects()` requires reacquiring Root. Added complete matching interface/concrete XML IntelliSense, usage examples and the [Root metadata guide](Root-Metadata.md).

Passed a Release build with zero warnings/errors and **175 offline tests**, including **43 Root cases**. A fresh PackageReference consumer exercises the five observations and binding guards, compiles **fourteen XML examples** plus README/guide usage, and checks installed XML and dependency isolation. Core now exposes five common Root contracts; 48 common and three optional Root contracts remain pending. Other domains remain binding-only. This increment adds no live native qualification; cross-version, units and engineering acceptance remain separate.

## 1.0.0-preview.20261105 - 5 November 2026

Established the clean Windows .NET 9 wrapper and acquisition foundation as an independent package. Preserved the complete wrapper property surface and introduced binding-only domain placeholders; no Root, Geometry, Load or other domain automation functions are included. OSRoot is the first planned domain.

Provided native OLE discovery, verified process/model identity and readiness, retained-root support, explicit discovery policies and diagnostic results. Unified the direct factory with the Core engine, installed-executable lookup and new temporary-model fallback when no supported application exists. Get reports failures through a focused exception; Acquire provides full outcome details. ExistingBlank explicitly leaves the requested file unopened.

Added wrapper owner-thread/reentrancy guards and modern process argument handling. Kept file preparation non-overwriting and avoided automatic process termination. Removed unrelated domain, WPF and third-party runtime dependencies from this first release scope.

Passed a Release build and 132 focused offline tests. A read-only named-session attachment check succeeded on STAAD.Pro 26.00.01.491 with explicit incomplete-discovery diagnostics. Broader STAAD build/license/startup qualification remains separate. This locally prepared preview is intended for manual NuGet.org upload after package-consumer validation.
