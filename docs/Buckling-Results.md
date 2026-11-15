# Buckling-result observations

`AreBucklingAnalysisResultsAvailable()` calls native `IsBucklingAnalysisResultsAvailable` once on the captured Output binding. It preserves Boolean true/false and integral zero/one flags. False is valid availability data. Unknown codes, strings, null, fractional values and unsupported native shapes throw.

`GetNumberOfBucklingFactorsComputed()` calls native `GetNoOfBucklingFactors` exactly once and returns a qualified nonnegative Int32 count. Zero and Int32.MaxValue are supported. Boolean, negative, fractional and out-of-range values throw. The scalar query allocates no factor buffer and does not issue an availability query.

The two observations are independent. Availability and factor count may change between calls as the application/model changes. They do not fetch buckling factors or eigenmodes, start analysis, certify freshness/convergence or provide engineering acceptance.

~~~csharp
bool available = wrapper.Output.AreBucklingAnalysisResultsAvailable();
Console.WriteLine("Buckling available: " + available);
int count = wrapper.Output.GetNumberOfBucklingFactorsComputed();
Console.WriteLine("Computed factors: " + count);
~~~

Calls execute under the existing shared current Output binding/constructing-thread/lifetime lease, including native output qualification. Reacquire wrapper.Output after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal work fails before native access. Raw calls, other wrappers and external UI are not coordinated. Original exceptions/HRESULTs propagate; calls may block. No fallback data, retry, model mutation, unit conversion or analysis is added.

Offline tests cover flag/count shapes and boundaries, missing native members, native failures, same-thread dispatch, current binding, disposal and cross-domain reentrancy. The installed package consumer verifies exact contracts, expanded matching interface/concrete XML and compiled examples. No live buckling/model query or new native-build/engineering qualification was performed.
