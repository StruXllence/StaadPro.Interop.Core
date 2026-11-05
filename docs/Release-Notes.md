# Release notes

## 1.0.0-preview.20261105 - 5 November 2026

Established the clean Windows .NET 9 wrapper and acquisition foundation as an independent package. Preserved the complete wrapper property surface and introduced binding-only domain placeholders; no Root, Geometry, Load or other domain automation functions are included. OSRoot is the first planned domain.

Provided native OLE discovery, verified process/model identity and readiness, retained-root support, explicit discovery policies and diagnostic results. Unified the direct factory with the Core engine, installed-executable lookup and new temporary-model fallback when no supported application exists. Get reports failures through a focused exception; Acquire provides full outcome details. ExistingBlank explicitly leaves the requested file unopened.

Added wrapper owner-thread/reentrancy guards and modern process argument handling. Kept file preparation non-overwriting and avoided automatic process termination. Removed unrelated domain, WPF and third-party runtime dependencies from this first release scope.

Passed a Release build and 132 focused offline tests. A read-only named-session attachment check succeeded on STAAD.Pro 26.00.01.491 with explicit incomplete-discovery diagnostics. Broader STAAD build/license/startup qualification remains separate. This locally prepared preview is intended for manual NuGet.org upload after package-consumer validation.
