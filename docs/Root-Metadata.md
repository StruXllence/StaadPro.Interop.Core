# Root metadata observations

The first OSRoot increment exposes five parameterless contracts from the Framework `StaadPro.Interop` project. Interface and concrete adapter names, return types and semantics are retained under `StaadPro.Interop.Core`. Use the current `wrapper.Root`, which is bound to the acquired native root. See [model lifecycle](Root-Model-Lifecycle.md) for the additional model/application operations.

| Function | Return | Cache source and limits |
| --- | --- | --- |
| `GetApplicationVersion()` | `ApplicationVersion` or null | Independent copy of `StaadVersion`, never the host assembly `ApplicationVersion`; null when optional native version is unavailable |
| `GetBaseUnit()` | `BaseUnitSystem` | Cached native base system; Unknown for missing/null/DBNull; unrecognized integral codes retained |
| `GetInputUnitForForce()` | `ForceInputUnit` | Cached input force unit; compatibility fallback is KiloPound for Imperial and KiloNewton otherwise |
| `GetInputUnitForLength()` | `LengthInputUnit` | Cached input length unit; compatibility fallback is Feet for Imperial and Meter otherwise |
| `GetMainWindowHandle()` | `IntPtr` | Cached non-owning handle from the bound model-window service; zero can represent absence or a native/service zero |

## Usage and refresh

With a connected wrapper on its constructing STA thread:

~~~csharp
var root = wrapper.Root;
Console.WriteLine(root.GetApplicationVersion()?.Display ?? "Unavailable");
Console.WriteLine(root.GetBaseUnit());
Console.WriteLine(root.GetInputUnitForForce());
Console.WriteLine(root.GetInputUnitForLength());
Console.WriteLine(root.GetMainWindowHandle());

// After external input-unit changes, explicitly refresh the existing binding's snapshot.
wrapper.RefreshUnits();
Console.WriteLine(root.GetInputUnitForLength());

// After external model/session changes, refresh bindings and take the replacement Root.
wrapper.RefreshComObjects();
root = wrapper.Root;
Console.WriteLine(root.GetApplicationVersion()?.Display);
~~~
All five reads are synchronous cache observations with **zero extra COM calls**. Construction and explicit refresh perform native metadata queries; their normalization/failure contracts remain in the wrapper. Reads do not acquire a session, reopen a model, scan processes, change units, manipulate a window, save, retry or validate current native liveness. External changes can make snapshots stale. Refresh itself remains an in-place, nontransactional native observation and may leave partial state on failure.

`RefreshUnits()` updates base/input units without replacing Root or updating version/window. `RefreshComObjects()` replaces adapters and updates metadata, so an old Root throws `InvalidOperationException` even if its raw object is unchanged. Reads on a disposed wrapper throw `ObjectDisposedException`. Another thread or a callback during wrapper refresh also receives `InvalidOperationException`. The shared lifecycle lease is released on failure. Directly constructing another `OSRootAdapter(wrapper)` does not make it the wrapper's current Root; use `wrapper.Root` for managed observations. Raw `ComObject` access bypasses these guards.

The returned version is mutable but copied; caller edits never change the cached version or later results. Missing native version produces null. The default window service is consulted only for native major versions greater than 20; unavailable version, V8i/earlier versions or an absent optional window member yield zero. Injected window-service behavior is preserved. Never close the borrowed handle; neither a nonzero handle nor these cached observations establishes live-window validity, PID ownership, model readiness or feature compatibility.

## Unit provenance

Input units are distinct from base units and analysis-result units. During native metadata extraction, usable reference output wins over an eligible returned unit code. Boolean return values and the version-24 success-code contract are not interpreted as units. Unit-query failures or unusable outputs can select the compatibility fallback, including metric fallbacks when base units are unknown. A returned enum does not carry an observed-versus-fallback flag and must not be presented as proven native evidence. No numeric model values are converted by these methods.

## Verification and pending scope

Forty-three focused Root cases cover zero-dispatch snapshots, defensive version copies, explicit refresh, replacement/disposal, wrong-thread access, callback reads during both refresh modes, unknown/missing metadata, reference-output priority, version-24 status handling, compatibility fallbacks, edition/window-service behavior and matching expanded interface/concrete XML. Together with 181 live-observation cases, 199 path/identity cases and the 132 foundation cases, 555 managed tests pass. A fresh NuGet consumer exercises all fifteen Root methods, stale/disposed rejection and dependency isolation, and compiles 34 XML examples plus README and all three Root guides.
