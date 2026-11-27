# Point stresses and member-end offset results

## Plate stress at a point

`GetPlateStressAtPoint(pId, lcId, stressPoint, facingPoint)` sends the two global coordinate triples and reads the 32 native Doubles in their documented order. Use finite coordinates in the model length units expected by OpenSTAAD. STAAD.Pro evaluates the point and applies the supplied facing geometry. The node IDs are unused. The result preserves the queried identifiers and points, exposes the original named fields, and returns a separate array from `Raw`.

The packet contains different result dimensions and index categories. Apply the native interpretation for each named value. Success requires a rank-one Double array with all 32 slots written and finite. Native false, malformed status and incomplete output raise an exception. [Bentley OpenSTAAD release note](https://docs.bentley.com/LiveContent/web/STAAD.Pro-v2026.0.1/ReadMe/en/topics/ReadMe/c-stpst_Revision_History_220300.html)

## Member face and the physical joint

- Use `GetMemberEndForcesConsideringOffsets` to inspect the native global face forces, the shifted force couple at the joint, the two points, and the global offset.
- Use `GetMemberEndForcesAtPhysicalJoint` for the detached joint record alone.

The `Beam` overload uses the supplied end's positive node ID and coordinates. The number overload resolves both incidence IDs and reads the requested joint coordinates through current Geometry. Both read `GetMemberGlobalOffSet` through current Property and request `GetMemberEndForces` with the global selector through current Output. Offset status zero requires three finite written Doubles. Status `-6027` yields a zero displacement with `HasOffsetSpecification` false. An explicit written zero still has `HasOffsetSpecification` true. Other offset failures raise an exception. Ordinary native force failure returns null.

The global offset `r` points from the incidence joint to the face. The wrapper calculates face position as `joint + r` and joint moment as `M_face + r × F`. Force components and native end-action signs are preserved. Use joint coordinates and offsets compatible with the native moment length unit. Native result age, axes and moment-reference conventions still require cross-build and project-specific checks. [Bentley member-offset definition](https://docs.bentley.com/LiveContent/web/STAAD.Pro%20Help-v17/en/STD_MEMBER_OFFSETS.html)

~~~csharp
var point = new Node(0, 1, 0, 1);
var facing = new Node(0, 2, 0, 1);
var stress = wrapper.Output.GetPlateStressAtPoint(17, 1, point, facing);
Console.WriteLine(stress.TopMax);

var beam = new Beam(21, new Node(11, 0, 0, 0), new Node(12, 3, 0, 0));
var offsets = wrapper.Output.GetMemberEndForcesConsideringOffsets("Dead", beam, 1, MemberEnd.Start);
var offsetsById = wrapper.Output.GetMemberEndForcesConsideringOffsets("Dead", 21, 1, MemberEnd.Start);
var joint = wrapper.Output.GetMemberEndForcesAtPhysicalJoint("Dead", beam, 1, MemberEnd.Start);
var jointById = wrapper.Output.GetMemberEndForcesAtPhysicalJoint("Dead", 21, 1, MemberEnd.Start);
if (offsets != null) Console.WriteLine(offsets.GlobalOffset.Z);
if (offsetsById != null) Console.WriteLine(offsetsById.MemberFace.Z);
if (joint != null) Console.WriteLine(joint.My);
if (jointById != null) Console.WriteLine(jointById.My);
~~~

All five calls execute on the wrapper's owning thread and use the current Output binding. They leave the caller's entities and the native model untouched. A refresh invalidates the captured adapter. The internal prerequisite reads do not expose new Geometry or Property methods.
