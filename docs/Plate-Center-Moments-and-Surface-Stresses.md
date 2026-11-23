# Plate-center moments and surface stresses

These Output queries read existing plate results through the wrapper's current native binding. They preserve the original function names, parameter names/order and result type names in Core namespaces.

| Method | Returned components |
| --- | --- |
| `GetPlateCenterMoments(loadCase, plate)` | `Mx`, `My`, `Mxy`: local moments per unit width. |
| `GetPlateCenterMomentsAsync(loadCase, plate)` | The same moment record through a settled task. |
| `GetPlateCenterVonMises(loadCase, plate)` | `Top`, `Bottom`: native plate-center surface von Mises stresses. |
| `GetPlateCenterVonMisesAsync(loadCase, plate)` | The same surface record through a settled task. |
| `GetPlatePrincipalStresses(loadCase, plate)` | `SmaxTop`, `SminTop`, `SmaxBottom`, `SminBottom`: native in-plane normal principal stresses. |

~~~csharp
var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
var moments = wrapper.Output.GetPlateCenterMoments(loadCase, plate);
Console.WriteLine($"MX {moments.Mx}, MY {moments.My}, MXY {moments.Mxy}");
var momentTask = wrapper.Output.GetPlateCenterMomentsAsync(loadCase, plate);
Console.WriteLine(momentTask.GetAwaiter().GetResult().Mxy);
var von = wrapper.Output.GetPlateCenterVonMises(loadCase, plate);
Console.WriteLine($"Top {von.Top}, bottom {von.Bottom}, absolute maximum {von.AbsoluteMaximum}");
var vonTask = wrapper.Output.GetPlateCenterVonMisesAsync(loadCase, plate);
Console.WriteLine(vonTask.GetAwaiter().GetResult().Top);
var principal = wrapper.Output.GetPlatePrincipalStresses(loadCase, plate);
Console.WriteLine($"Top principal {principal.SmaxTop}, {principal.SminTop}");
Console.WriteLine($"Bottom principal {principal.SmaxBottom}, {principal.SminBottom}");
~~~

Both arguments must be non-null with positive native IDs. Only `loadCase.Id` and `plate.Id` are captured, once each; title/type metadata and plate corners are not required or read. Retrieval determines whether results exist for that native identity. No analysis, case activation, geometry hydration, selection or save occurs.

## Native output and units

`GetPlateCenterMoments` calls native `GetAllPlateCenterMoments` once. Its three-Double array is ordered **MX, MY, MXY**: moment per unit width about the local X face, about the local Y face, and twisting moment per unit width in the local X-Y plane. Values retain native moment/width units, for example kN-m/m. They are not converted to nodal/member-end moments, membrane stresses or reinforcement design values.

`GetPlateCenterVonMises` calls native `GetPlateCenterVonMisesStresses` once with distinct top and bottom scalar outputs. `GetPlatePrincipalStresses` calls native `GetPlateCenterNormalPrincipalStresses` once with maximum top, minimum top, maximum bottom and minimum bottom outputs, in that order. Both use native force/length-squared stress units. Native top/bottom labels and signs are preserved without interpreting them as global elevation labels.

All native reads require Boolean true or integral one success. False/zero raises `InvalidOperationException`; unsupported status representations are rejected. Buffers and scalar outputs start with NaN sentinels. Every component must be fully written as a finite `Double`: there is no integer/string/decimal coercion or initialized-zero fallback. Fully written zeros, finite signed values and finite extreme values remain literal observations; managed code does not normalize signs or reorder principal values. Qualification checks representation/completeness, not physical plausibility or engineering acceptance.

Moment output must be a rank-one, exact-length three-Double array. In-place writes, replacement arrays and nonzero lower bounds are supported. All values are copied, with new result storage per call. For scalar queries, every top/bottom output qualifies before a record is returned. Failed or partial reads expose no result; original native exceptions and HRESULTs propagate without retry or fallback.

## Detached model scope

`PlateCenterMoments`, `PlateCenterVonMises` and `PlatePrincipalStresses` reuse `PlateStress`/`IPlateStress` context. They retain the supplied case and plate references, while owning copied scalar values. Later metadata edits can show IDs different from those captured for the native call; retain immutable query identity separately if needed. Later edits do not trigger another query or recalculate components. Result scalars and manual constructors/setters are unvalidated, editable detached metadata without change notifications or native effects.

The moment model supplies the original context and five-argument scalar constructors plus `Mx`, `My`, `Mxy`. The von Mises model supplies the original context and concrete-`LoadCase` scalar constructors, `Top`, `Bottom` and computed `AbsoluteMaximum`. `AbsoluteMaximum` recomputes `Math.Max(Math.Abs(Top), Math.Abs(Bottom))` from current values; manual NaN/infinity propagates. It does not change either component or compare an allowable stress. The principal model supplies its original context constructor and four editable scalar properties.

This is the scalar/context portion of the models. It does not add the original `Transform`, `ConvertUsing`, `IUnitsConvertible` or Wood-Armer helpers, full plate geometry, principal directions or angles, coordinate transformation, UI services or reinforcement design. Existing [plate-center force queries](Static-Check-Batches-and-Plate-Center-Stresses.md) return five different local shear/membrane stresses; those values are not substituted for these results.

## Binding, lifetime and tasks

All five APIs run under one shared constructing-thread/current-Output-binding/lifetime/reentrancy lease spanning ID capture, validation, dispatch and output qualification. Refresh invalidates captured adapters; reacquire them. Stale, manually constructed noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Failure releases the lease. Raw native access, other wrappers and external UI remain outside this coordination; sequential observations are not an atomic model snapshot.

The two async variants perform native work **before returning** their completed or faulted tasks. Invocation can block the owning thread; await observes validation, guard and original native failures. They do not queue `Task.Run`, marshal to a UI dispatcher, switch apartments or add cancellation/parallel native execution.

The packaged examples and automated stand-ins check contracts, output mapping, validation, result ownership and guards. Live native result reads, cross-version compatibility, result freshness and solver/engineering qualification remain separate; these checks do not certify design acceptance.
