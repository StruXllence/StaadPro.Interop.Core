# Buckling-result observations

`AreBucklingAnalysisResultsAvailable()` calls native `IsBucklingAnalysisResultsAvailable` once on the captured Output binding. It preserves Boolean true/false and integral zero/one flags. False is valid availability data. Unknown codes, strings, null, fractional values and unsupported native shapes throw.

`GetNumberOfBucklingFactorsComputed()` calls native `GetNoOfBucklingFactors` exactly once and returns a qualified nonnegative Int32 count. Zero and Int32.MaxValue are supported. Boolean, negative, fractional and out-of-range values throw. The scalar query allocates no factor buffer and does not issue an availability query.

These availability/count observations are independent. Availability and factor count may change between calls as the application/model changes. They do not fetch buckling factors or eigenmodes, start analysis, certify freshness/convergence or provide engineering acceptance. Separate factor/vector queries are described below.

`GetBucklingFactor(int bucklingMode)` requires a positive native mode number and dispatches once with a by-reference Double NaN sentinel. A successful status must accompany a written finite Double. Zero and negative factors are preserved. This fixes a by-value call that could leave an initialized zero unchanged. The query does not automatically request availability/count or infer convergence.

`GetBucklingModeDisplacementAtNode(int bucklingMode, int nodeId)` and `GetBucklingModeDisplacement(int bucklingMode, Node atNode)` return `BucklingModeDisplacementResult`. The native output is a six-component array, so a scalar return cannot represent it. The named immutable result exposes captured `BucklingMode`, `NodeId` and six `Components` in owned read-only storage. No component is discarded and no scalar magnitude is invented. Components retain native order, signs, axes, units and eigenmode normalization without inferred axis labels. Interpret them using the installed build's conventions; eigenmode components do not establish a physical response amplitude.

Both identifiers must be positive. The node overload reads `atNode.Id` once; coordinates are unused, need not be hydrated and are not validated. Null nodes and invalid IDs fail before dispatch. `Node` supplies detached editable identity/coordinate metadata, original scalar constructor shapes and property notifications; it has no native binding or geometry operations and retains reference equality.

All factor/vector calls require native Boolean true or integral one success; false/zero throws. Vector outputs start as six NaN sentinels and must return an exact-length rank-one Double array with every component finite and written. Replacement arrays and nonzero lower bounds are supported; results copy native storage. Unexpected shapes/types, nonfinite/unwritten values and unknown statuses throw. Later node/model/buffer edits do not change a returned observation. No availability/count, geometry or unit-conversion call is added.

~~~csharp
bool available = wrapper.Output.AreBucklingAnalysisResultsAvailable();
Console.WriteLine("Buckling available: " + available);
int count = wrapper.Output.GetNumberOfBucklingFactorsComputed();
Console.WriteLine("Computed factors: " + count);
if (available && count > 0)
{
    Console.WriteLine(wrapper.Output.GetBucklingFactor(1));
    var node = new StaadPro.Interop.Core.Entities.Node(4, 0, 0, 0);
    var displacement = wrapper.Output.GetBucklingModeDisplacement(1, node);
    foreach (double component in displacement.Components) Console.WriteLine(component);
    var byId = wrapper.Output.GetBucklingModeDisplacementAtNode(1, 4);
    Console.WriteLine(byId.NodeId);
}
~~~

Calls execute under the existing shared current Output binding/constructing-thread/lifetime lease, including native output qualification. Reacquire wrapper.Output after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal work fails before native access. Raw calls, other wrappers and external UI are not coordinated. Original exceptions/HRESULTs propagate; calls may block. No fallback data, retry, model mutation, unit conversion or analysis is added.

Offline tests cover flag/count/factor/vector shapes and boundaries, by-reference outputs, read-only ownership, missing native members, native failures, same-thread dispatch, current binding, disposal and cross-domain reentrancy. The installed package consumer verifies exact contracts, expanded matching interface/concrete XML and compiled examples. No live buckling/model query or new native-build/engineering qualification was performed.
