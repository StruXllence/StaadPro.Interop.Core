# Member-local projections of plate-corner actions

Use the detached projection methods when you already have a `PlateCornerForces` record whose six components are in STAAD global XYZ. They resolve a member basis from supplied geometry and express the force and moment vectors at the original plate corner in that basis. The components retain their input units and point of application. Use compatible units, axes and end-action sign conventions when combining the resulting record with other member results.

| Method | Basis and result |
| --- | --- |
| `TransformPlateCornerForcesToMemberLocal(plateCornerForces, otherMemberNode)` | Conceptual zero-beta basis directed from the corner to the supplied node. Sets `BeamId` to zero and `End` to `Start`. |
| `TransformPlateCornerForcesToMemberLocal(plateCornerForces, member)` | Supplied beam's start-to-end direction and beta angle. Sets `BeamId` to the supplied member ID and identifies the matching end. |
| `TransformPlateCornerForcesToMemberLocalAsync(plateCornerForces, otherMemberNode)` | Conceptual projection through a settled task. |
| `TransformPlateCornerForcesToMemberLocalAsync(plateCornerForces, member)` | Beam projection through a settled task. |
| `GetMemberLocalCoordinateSystem(member)` | Immutable `LocalCS` containing the start origin, its captured node ID and the three unit axes in global XYZ. Supports draft member IDs. |

~~~csharp
var start = new StaadPro.Interop.Core.Entities.Node(11, 0, 0, 0);
var end = new StaadPro.Interop.Core.Entities.Node(12, 2, 0, 0);
var third = new StaadPro.Interop.Core.Entities.Node(13, 0, 0, 1);
var plate = new StaadPro.Interop.Core.Entities.Plate(start, end, third) { Id = 17 };
var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
var global = new StaadPro.Interop.Core.Models.PlateCornerForces(loadCase, plate, start,
    1, -2, 3, -4, 5, -6);
var member = new StaadPro.Interop.Core.Entities.Beam(21, start, end) { BetaAngle = 90 };

var zeroBeta = wrapper.Output.TransformPlateCornerForcesToMemberLocal(global, end);
var memberLocal = wrapper.Output.TransformPlateCornerForcesToMemberLocal(global, member);
var conceptualTask = wrapper.Output.TransformPlateCornerForcesToMemberLocalAsync(global, end);
var memberTask = wrapper.Output.TransformPlateCornerForcesToMemberLocalAsync(global, member);
var frame = wrapper.Output.GetMemberLocalCoordinateSystem(member);
Console.WriteLine(zeroBeta.Fy);
Console.WriteLine(memberLocal.Fy);
Console.WriteLine(conceptualTask.GetAwaiter().GetResult().Mz);
Console.WriteLine(memberTask.GetAwaiter().GetResult().Mz);
Console.WriteLine($"Origin node {frame.OriginNodeId}, local Y direction Z component {frame.V.Z}");
~~~

## Read and project from STAAD.Pro

The four read-and-project overloads resolve the incidence node and prepare the supplied frame before requesting one corner force packet. They reuse the same member frame and projection rules as the detached transformations. Hydration uses the wrapper's current Geometry binding when the supplied plate lacks A/B/C. Supply the beam's actual endpoints and beta when using a `Beam`. The corner must match one of those endpoints.

| Method | Result |
| --- | --- |
| `GetPlateCornerForces(loadCase, plate, cornerNode, otherMemberNode)` | Local `MemberForces` in the zero-beta direction from the corner to the other node. |
| `GetPlateCornerForces(loadCase, plate, cornerNode, member)` | Local `MemberForces` in the supplied beam's start-to-end frame, with beta and matched end. |
| `GetPlateCornerForcesAsync(loadCase, plate, cornerNode, otherMemberNode)` | The same conceptual result in a settled task. |
| `GetPlateCornerForcesAsync(loadCase, plate, cornerNode, member)` | The same beam-local result in a settled task. |

The global convenience `GetPlateCornerForcesGlobal(plate, cornerNode, loadCaseId)` returns a `PlateCornerForcesQueryResult`. Its `Key` preserves the case, plate and corner identifiers. Check `IsSuccessful` before accessing `Forces`. This overload passes the number directly to OpenSTAAD, so its managed case record has default metadata. Native false or zero discards the buffer. Invalid input, malformed reported success and dispatch exceptions propagate.

~~~csharp
var corner = new StaadPro.Interop.Core.Entities.Node(11, 0, 0, 0);
var other = new StaadPro.Interop.Core.Entities.Node(12, 2, 0, 0);
var third = new StaadPro.Interop.Core.Entities.Node(13, 0, 0, 1);
var plate = new StaadPro.Interop.Core.Entities.Plate(corner, other, third) { Id = 17 };
var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
var member = new StaadPro.Interop.Core.Entities.Beam(21, corner, other) { BetaAngle = 90 };

var query = wrapper.Output.GetPlateCornerForcesGlobal(plate, corner, loadCase.Id);
if (query.IsSuccessful) Console.WriteLine(query.Forces.Fx);
var conceptual = wrapper.Output.GetPlateCornerForces(loadCase, plate, corner, other);
var beamLocal = wrapper.Output.GetPlateCornerForcesAsync(loadCase, plate, corner, member).GetAwaiter().GetResult();
Console.WriteLine(conceptual.Fy);
Console.WriteLine(beamLocal.Fy);
~~~

Bentley's print reference describes global plate-corner actions in the printed results. The earlier read-only equilibrium check is the evidence for interpreting the OpenSTAAD packet on the sampled STAAD.Pro 2026.0.0.340 model. Verify axes and signs for other builds and fixtures. The queries preserve native units and do not establish analysis freshness. [Prior equilibrium check](https://github.com/StruXllence/StaadPro.Interop/blob/98d95f0ff44e37a45d045018182555d93dd84ae6/docs/Output-Plate-Corners.md#contract-evidence-and-coordinate-decision). [Bentley print reference](https://docs.bentley.com/LiveContent/web/STAAD.Pro%20Help-v21/en/STD_PRINT.html)

All five live methods keep one lease for hydration, force retrieval and projection as applicable. They do not launch or analyze a model, alter case selection, or change its data. Task overloads return after performing the read and projection and retain the original failure.

## Basis and projection

`LocalCS.U` follows the supplied start-to-end direction. At zero beta, the basis uses the projection of global +Y onto the plane perpendicular to U. For an upward vertical member the transverse V seed is global −X. For a downward vertical member it is global +X. A near-vertical direction follows the same seed choice when `abs(U.Y) >= 1 - 1e-9`. Orthogonalizing that seed keeps U, V and W perpendicular even for a slightly inclined member in this branch. The zero-beta W axis completes the right-handed basis and points along global +Z for an exactly vertical member.

`Beam.BetaAngle` is detached caller-supplied metadata in degrees and defaults to zero. Positive beta rotates V and W about U in the right-handed sense. Angles whose magnitude is at most `1e-9` degrees use the zero-beta basis. Populate the supplied beta and incidence from your authoritative member data before projecting results. These methods have no live check of the active model's axes or conventions.

Each result component is a dot product of the supplied global force or moment vector with the corresponding local unit axis. A member-end flag describes where the corner matched. The basis still points from start to end at either end. Force and moment signs follow the projected inputs without an end-specific reversal, averaging, moment transfer between points or conversion to member internal forces.

## Supplied metadata and validation

The projection expects the corner record's case, plate and corner metadata. The corner must match one of the plate's supplied incidence slots. The beam projection additionally requires a positive beam ID and a corner matching an endpoint. A start match takes precedence. Matching follows source identity semantics: an identical node reference matches, two positive node IDs match by ID, and other pairs are compared by Euclidean coordinate distance with a tolerance of `1e-9` model length units. Coincident or too-short directions, missing required endpoints and invalid numeric inputs fail explicitly. A nonfinite projected value fails rather than becoming a usable component. The returned `MemberForces` is editable detached data carrying the supplied load case ID and title.

`LocalCS` captures its origin and three vectors as immutable scalar values. Later edits to endpoints or beta take effect only on a new call. During a call, coordinate writes should be coordinated by the caller. The detached data has no native membership or axes certification.

All five detached methods run managed work through the wrapper's current Output binding on its owning thread. Use the current adapter after a refresh. The task variants perform computation before returning a completed or faulted task, and awaiting a failed call observes the original error.
