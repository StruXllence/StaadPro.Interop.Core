# Member and plate incidence

Use `wrapper.Geometry` on the thread that constructed the wrapper. Reacquire the adapter after `RefreshComObjects()`. Incidence is the set of node IDs that defines how a member or plate connects to the model.

| Method | Result |
| --- | --- |
| `GetBeam(int beamNumber)` | A detached analytical beam with start and end nodes in native order. Zero returns `null` without a native call. |
| `GetBeam(int bId, IEnumerable<Node> fromModelNodes)` | A detached analytical beam whose endpoints reference supplied nodes. Zero returns `null` without a native call. |
| `GetPlate(int pId)` | A detached plate with A, B, C and optional D from native incidence and coordinates. |
| `GetPlate(int pId, IEnumerable<Node> fromModelNodes)` | A detached plate whose corner properties reference supplied nodes matched by ID. |
| `GetPlateIncidence(int pId)` | An eager snapshot of distinct nodes in native A/B/C/D order. |

The member query preserves start and end order and requires two distinct positive IDs. The plate queries require a positive plate number and distinct positive A, B and C IDs. The wrapper treats a written zero D as an absent corner. For a repeated positive D, `GetPlate` preserves the incidence slot and `GetPlateIncidence` returns the ID once. Bentley defines triangle connectivity with three nodes. [Bentley plate and shell incidence specification](https://docs.bentley.com/LiveContent/web/STAAD.Pro-v2025.0.0/Help/en/topics/Commands_TechRef/r-stpst_Plate_and_Shell_Element_Incidence_Specification.html).

The native readers require an integral success code of zero and validate the incidence outputs before resolving coordinates. A native failure or malformed output fails the query. Native coordinate reads use global XYZ in the length units supplied by OpenSTAAD and require three finite values. Each distinct incident node is read once per query. Beam hydration resolves endpoints only. Beta angle and utilization remain at their default values.

The supplied-node overloads take the first caller-supplied reference for each incident ID and leave missing endpoints or corners `null`. They read native incidence and use the supplied coordinates as-is. Edits to returned metadata affect managed objects only. The methods observe current connectivity and do not save or modify the model.

~~~csharp
var beam = wrapper.Geometry.GetBeam(7);
Console.WriteLine(beam.StartNode.Id);

var plateNodes = wrapper.Geometry.GetPlateIncidence(12);
foreach (var node in plateNodes)
    Console.WriteLine($"Node {node.Id} at ({node.X}, {node.Y}, {node.Z})");

var plate = wrapper.Geometry.GetPlate(12, plateNodes);
Console.WriteLine(plate.A?.Id);
~~~

Custom `IOSGeometry` implementations need to supply the five new methods when upgrading. The package verifies its contracts with native-shaped fixtures. Live incidence, coordinate units and COM marshaling remain build-specific.
