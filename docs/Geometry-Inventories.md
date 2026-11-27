# Geometry inventories and node coordinates

`wrapper.Geometry` exposes five read-only queries. Call them on the same COM thread that acquired the wrapper. Reacquire the adapter after `RefreshComObjects()`.

| Method | Scope | Native calls |
| --- | --- | --- |
| `GetAllBeamsList()` | Analytical member IDs | `GetMemberCount`, `GetBeamList` |
| `GetAllNodesList()` | Node IDs | `GetNodeCount`, `GetNodeList` |
| `GetAllPlatesList()` | Plate element IDs | `GetPlateCount`, `GetPlateList` |
| `GetAllSolidsList()` | Solid element IDs | `GetSolidCount`, `GetSolidList` |
| `GetNode(int nId)` | One detached node with global XYZ | `GetNodeCoordinates` |

The four lists read the entire model and preserve the native order. Each call returns its own eager ID snapshot, so re-enumerating it makes no extra COM calls. A valid zero count returns an empty sequence. Positive counts up to 1,000,000 must match the length of a one-dimensional Int32 output array with unique positive IDs. The limit bounds allocation and covers Bentley’s published model counts. [Bentley model size limits](https://bentleysystems.service-now.com/community?id=kb_article_view&sysparm_article=KB0112809).

After a positive list read, Core checks the count again. A changed count or malformed list throws `InvalidOperationException` without returning a partial result. Two separate native calls can still observe different model states if an external edit leaves the count unchanged. Coordinate model edits through the owner thread.

`GetNode` reads a positive ID directly and requires three finite Double outputs. The returned `Node` uses the global coordinates and length units supplied by OpenSTAAD. Zero returns `null` without native dispatch, and negative IDs throw `ArgumentOutOfRangeException`. Edits to the detached `Node` affect its managed values only. Native failures preserve their original exception.

~~~csharp
foreach (int id in wrapper.Geometry.GetAllNodesList())
{
    var node = wrapper.Geometry.GetNode(id);
    Console.WriteLine($"{node.Id}: ({node.X}, {node.Y}, {node.Z})");
}
~~~

Custom classes that implement `IOSGeometry` must supply the five new methods when upgrading. Applications that use `wrapper.Geometry` get the implementations from the package.

The installed consumer verifies the managed array and coordinate contracts with COM-shaped fixtures. Live STAAD.Pro compatibility remains build-specific. Bentley documents a VB.NET limitation for these list methods in STAAD.Pro 2025.0.1. [Bentley STAAD.Pro known issues](https://docs.bentley.com/LiveContent/web/STAAD.Pro-v2025.0.1/ReadMe/en/topics/ReadMe/r-stpst_SPRO_Known_Issues.html).
