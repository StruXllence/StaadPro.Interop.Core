# Support reactions, support forces and static checks

`GetSupportReactions(int supportId, int loadCaseId)` and `GetSupportForces(int supportId, int loadCaseId)` return detached `LoadCaseForces` records. `GetSupportReactionsAsync(...)` and `GetSupportForcesAsync(...)` provide the same values through settled tasks. `GetStaticCheckResultAsync(int loadCaseId)` returns detached native load and reaction totals for one case.

The support identifier is the native node number, not a position in the list of supports. Both identifiers must be positive. Native retrieval determines whether that node/case has usable results. The methods do not discover supports, activate cases, run analysis, save the model or guarantee that existing results are current.

~~~csharp
var reactions = wrapper.Output.GetSupportReactions(2, 1);
var forces = wrapper.Output.GetSupportForces(2, 1);
Console.WriteLine($"Case {reactions.Id}: global Fy reaction {reactions.Forces.Fy}");
Console.WriteLine($"Opposite-sign support Fy: {forces.Forces.Fy}");
var reactionTask = wrapper.Output.GetSupportReactionsAsync(2, 1);
var forceTask = wrapper.Output.GetSupportForcesAsync(2, 1);
var checkTask = wrapper.Output.GetStaticCheckResultAsync(1);
var check = checkTask.GetAwaiter().GetResult();
Console.WriteLine($"Case {check.LoadCaseId}: native load Mz {check.Forces.Mz}");
Console.WriteLine($"Native reaction Mz {check.Reactions.Mz}");
Console.WriteLine($"Literal load-minus-reaction Mz {check.Differentials.Mz}");
Console.WriteLine(reactionTask.GetAwaiter().GetResult().Forces.Fx);
Console.WriteLine(forceTask.GetAwaiter().GetResult().Forces.Fx);
~~~

Each support query makes one native `GetSupportReactions` call. Reactions retain native **global** directions and signs. Support forces multiply **all six** qualified components by minus one, including moments; no second native query is needed. Values retain native force/moment units without conversion. `LoadCaseForces.Id` is the requested load-case number; the container does not store the support node number, so retain that query identity separately.

Each static check makes one native `GetStaticCheckResult` call with two independent by-reference buffers. It copies six load totals and six reaction totals in `Fx,Fy,Fz,Mx,My,Mz` order. Native units, axes, moment reference point and signs are retained without a managed transformation. `Differentials` preserves component-wise `Forces - Reactions`. This literal source convention is not a balance sum, absolute residual, tolerance test or engineering acceptance criterion. Each access computes a new record from current editable values; extreme/manual data can overflow ordinary Double subtraction.

## Qualification and ownership

Native success must be Boolean true or integral one. False/zero retrieval raises `InvalidOperationException`; unsupported status types/codes are rejected. Every buffer starts with six NaN sentinels, so an unwritten successful result cannot become apparent zero loads. Each successful output must be a rank-one array of exactly six finite written Doubles. Replacements and nonzero lower bounds are supported and copied. A static check returns only after **both** arrays qualify; failure exposes no partial record.

Every call owns fresh record/component storage even if native buffers are reused or aliased. Successful all-zero results remain valid when all six zeros are explicitly written. Original native exceptions/HRESULTs propagate. Qualification does not certify result freshness, unit compatibility with other queries, equilibrium or engineering correctness. Live native-build and cross-version qualification remain separate from automated stand-in checks.

`Forces` provides editable six scalar properties, default/six-component constructors, `Scale(double factor)` and `ParseFromArray(double[] forcesArray)` with independent copied storage. It does not include the original WPF vector, UI, unit-converter or complete force-arithmetic surface. Manual values are unvalidated; notifications are synchronous after assignment, and handler exceptions can leave partial multi-component edits. `LoadCaseForces` supplies `Id`/`Forces`, while `StaticCheckResult` supplies `LoadCaseId`, editable `Forces`/`Reactions`, and computed `Differentials`. Unpopulated static-check inputs cannot produce differences. Component edits do not forward notifications through their enclosing static-check record. These models never update native state.

## Thread and task behavior

All five entry points use the wrapper's current Output adapter on its constructing COM thread under one shared binding/lifetime/reentrancy lease. Validation, dispatch and output qualification happen inside that lease. Refresh invalidates captured adapters; reacquire them. Disposed, stale, manually constructed noncurrent, worker-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Exceptions release the lease. Raw native calls, other wrappers and external UI remain outside this coordination.

The three async variants perform native work **synchronously before returning** an already completed or faulted task. They can block the caller. Await the task to observe validation, lifetime and native failures; no `Task.Run`, apartment switch, UI dispatcher, cancellation or parallel COM work is introduced. A sequence of calls is not an atomic snapshot; coordinate model/session edits explicitly.
