# Plate cut-line resultants

Use these Output queries to integrate forces and moments along a cut through a set of analyzed plates for one load-case or combination ID. Supply the cut endpoints in global coordinates and active OpenSTAAD base length units. Each plate needs a positive ID. Incidence and node coordinates on the managed plate objects are optional because the native call uses the IDs.

| Method | Use |
| --- | --- |
| `QueryPlateCutLineResultant` | Inspect native success explicitly. The result preserves the requested IDs, endpoints and axes. `Forces` is null when OpenSTAAD reports false or zero. |
| `GetPlateCutLineResultant` | Get the six components directly. Native false or zero raises `InvalidOperationException`. |
| `GetPlateCutLineResultantAsync` | Use the required-result contract in a task-based caller. The read finishes on the wrapper's owning thread before the task is returned. |

~~~csharp
var plates = new[] { new StaadPro.Interop.Core.Entities.Plate(null, null, null) { Id = 17 } };
var start = new StaadPro.Interop.Core.Models.CartesianVector3(0, 0, 0);
var end = new StaadPro.Interop.Core.Models.CartesianVector3(2, 0, 1);
var query = wrapper.Output.QueryPlateCutLineResultant(plates, 1, start, end, transformForceToGlobal: true);
if (query.IsSuccessful) Console.WriteLine(query.Forces.Fx);
var local = await wrapper.Output.GetPlateCutLineResultantAsync(plates, 1, start, end, transformForceToGlobal: false);
Console.WriteLine(local.Mz);
~~~

The reader enumerates the list once, skips null entries, and keeps distinct plate IDs in first-occurrence order. It bounds the input at 100,000 entries and validates positive IDs and finite endpoints before dispatch. Supply the plates that the cut crosses and a suitable analyzed case. The wrapper neither computes geometric intersections nor runs analysis.

OpenSTAAD supplies Fx, Fy, Fz, Mx, My and Mz for the cut in native force and moment units. The global flag requests STAAD XYZ. The local flag requests the native cut-line convention. The reader passes the documented default surface-axis identifiers (-1, -1, -1), leaving surface-axis construction to OpenSTAAD. Case selection, cut orientation and coordinates are passed as supplied. Component copying performs no further rotation, sign adjustment, unit conversion, or moment transfer.

The 2024, 2025 and 2026 OpenSTAAD Help signatures and VBA examples for `GetResultantForceAlongLineForPlateList` use a one-case ID array, two three-coordinate endpoint arrays and a one-row six-component result. The reader accepts that matrix or a flat six-Double equivalent and rejects incomplete and nonfinite data. Malformed status, malformed success data and dispatch errors are errors in all three variants. Live sign, axis and numerical integration behavior still need qualification against the target STAAD build and model.
