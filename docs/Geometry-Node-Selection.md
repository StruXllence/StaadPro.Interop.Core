# Selected nodes and supplied-node lookup

`wrapper.Geometry` can read the current node selection as IDs or coordinates. Call the methods on the wrapper’s constructing COM thread and reacquire the adapter after `RefreshComObjects()`.

| Method | Result |
| --- | --- |
| `GetNode(int nId, IEnumerable<Node> fromModelNodes)` | The first supplied node with the exact ID, or `null`. This lookup uses supplied metadata only. |
| `GetSelectedNodesIds()` | A set of selected positive IDs. |
| `GetSelectedNodesExt()` | New, detached nodes with global XYZ coordinates in the length units supplied by OpenSTAAD. |
| `GetSelectedNodesExt(IEnumerable<Node> fromModelNodes)` | The first supplied node for each selected ID. One `null` entry marks one or more missing IDs. |
| `GetSelectedNodesListAsync()` | A completed or faulted task with an ID array in native selection order. |

Selection reads validate the native count and ID array before returning data. An empty selection skips the list call and produces an empty collection. For positive counts, Core requests native selection order, validates up to 1,000,000 unique positive Int32 IDs, and rechecks the count. A change in count or malformed output throws. External selection changes that leave the count unchanged can still affect the observation. Live coordinate reads use the captured IDs and fail the call if a coordinate read fails.

The supplied-node overload compares IDs without modifying the objects or converting coordinates. It processes the sequence once, stopping as soon as every selected ID has a match. The live selection and ID array methods finish all native work before returning. Awaiting the array task leaves COM on the caller’s apartment. Set iteration order is unspecified.

~~~csharp
var ids = wrapper.Geometry.GetSelectedNodesListAsync().GetAwaiter().GetResult();
Console.WriteLine($"Selected nodes: {ids.Length}");

var supplied = new[] { new StaadPro.Interop.Core.Entities.Node(3, 2, 0, 0) };
var node = wrapper.Geometry.GetNode(3, supplied);
if (node != null) Console.WriteLine(node.X);

foreach (var match in wrapper.Geometry.GetSelectedNodesExt(supplied))
    if (match != null) Console.WriteLine(match.Id);
~~~

Custom classes that implement `IOSGeometry` need to supply the new methods when upgrading. The package consumer exercises the managed contract with representative native-shaped fixtures. Live behavior can vary by STAAD.Pro build. Bentley documents a VB.NET limitation for node-selection array calls in STAAD.Pro 2025.0.1. [Bentley known issues](https://docs.bentley.com/LiveContent/web/STAAD.Pro-v2025.0.1/ReadMe/en/topics/ReadMe/r-stpst_SPRO_Known_Issues.html).
