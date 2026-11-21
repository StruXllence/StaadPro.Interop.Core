# Member steel-utilization queries

`GetMemberSteelUtilizationRatio(int beamId)` submits one native `GetMemberSteelDesignRatio` query for a positive analytical member number. It reads the critical dimensionless ratio from the last parameter block for which that member was designed. It does not select a design code/block, run analysis/design or infer engineering acceptance. Values above one are preserved.

The output begins as a Double NaN sentinel. Boolean true or integral one status is required; false/zero throws even if output was written. Successful output must be a written finite Double. Nonnegative ratios are preserved along with documented -1 (analysis not performed) and -999 (analysis performed but member not designed) sentinels. Sentinels are absence observations, not passing results. Other negative values, malformed statuses/types, nonfinite values and unwritten outputs throw. The installed STAAD build still needs native qualification for status/sentinel behavior.

`GetMemberSteelUtilizationRatioAsync(int beamId)` returns `Task<double>` with the same validation and native call. Native dispatch happens synchronously on the constructing COM thread before the task returns. The task is already completed on success or faulted with the original validation/native exception; awaiting preserves that exception/HRESULT. This method provides a task-shaped API, not nonblocking native work. Calling through `Task.Run` or another worker fails the thread guard. No worker-thread COM, synchronization-context dispatch, cancellation or retry is introduced.

~~~csharp
double ratio = wrapper.Output.GetMemberSteelUtilizationRatio(7);
if (ratio == -1 || ratio == -999)
    Console.WriteLine("Design result unavailable: " + ratio);
else
    Console.WriteLine("Critical ratio: " + ratio);

// Native work finishes on this owning thread before the task returns.
var query = wrapper.Output.GetMemberSteelUtilizationRatioAsync(7);
Console.WriteLine(query.GetAwaiter().GetResult());
~~~

Both methods use the current Output binding and shared constructing-thread/lifetime/reentrancy lease through qualification. Reacquire the adapter after refresh. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Calls can block. Raw objects, other wrappers and external UI remain uncoordinated. No model mutation, case activation, geometry hydration, save, conversion, analysis or design is performed. Managed fixtures and installed-package checks do not replace native-build or engineering acceptance.

See [member batches and task APIs](Member-Batch-Queries.md) for the Beam task overload, qualified ratio dictionaries, detached population and local member-end force tasks. Native work remains on the constructing COM thread.

See [steel-design records and governing members](Steel-Design-Records.md) for reported record fields, strict PASS/FAIL qualification, current Load composition and cached/queried physical-member ranking.
