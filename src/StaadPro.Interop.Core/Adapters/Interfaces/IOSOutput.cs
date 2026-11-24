using System;
using StaadPro.Interop.Core.Models;
using System.Threading.Tasks;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models.Output;
using System.Collections.Generic;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models.Root;
namespace StaadPro.Interop.Core.Adapters.Interfaces
{
    /// <summary>Provides guarded Output queries for availability, member forces, buckling, steel-design, support and static-check observations.</summary>
    /// <remarks>Other Output operations are not yet implemented.</remarks>
    public interface IOSOutput : IOSBase
    {
        /// <summary>Gets the captured native Output object for advanced, apartment-aware interoperation.</summary>
        /// <remarks>Raw access is outside managed operation validation. Do not use a captured binding after wrapper refresh/disposal.</remarks>
        dynamic ComObject { get; }

        /// <summary>Observes whether native analysis results are available.</summary>
        /// <returns>The qualified native Boolean or integral zero/one flag. False is valid availability data.</returns>
        /// <remarks>Calls AreResultsAvailable once; this is not an analysis-completion or freshness guarantee.
        /// Unsupported types and codes throw instead of coercing arbitrary values to success.<para>Runs on the wrapper's constructing COM thread under one current-binding operation lease, including
        /// validation, enumeration, native dispatch and output qualification. RefreshComObjects invalidates captured
        /// adapters; reacquire them. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output,
        /// refresh or disposal calls are rejected before native access. The guard cannot coordinate raw calls,
        /// other wrappers or external UI. No implicit analysis, case activation, selection, save, unit conversion,
        /// background COM, retry or rollback is added. Native failures preserve original exceptions/HRESULTs.
        /// Calls can block. Snapshots do not guarantee result freshness or engineering validity.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, another thread/reentrant operation is active,
        /// a native flag/title is malformed or successful outputs are unwritten, nonfinite or incorrectly shaped.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A compatible native member is unavailable.</exception>

        /// <example><code>Console.WriteLine(wrapper.Output.AreResultsAvailable());</code></example>
        bool AreResultsAvailable();

        /// <summary>Reads qualified member-end forces in local native axes.</summary>
        /// <param name="lcTitle">Non-null caller-supplied display title, retained unchanged; no title query is made.</param>
        /// <param name="beamId">Positive analytical member ID, not an index or physical-member ID.</param>
        /// <param name="loadCaseId">Positive native result load-case ID.</param>
        /// <param name="memberEnd">Start=0 or End=1; undefined values are rejected.</param>

        /// <returns>A named per-end result with native success, immutable query identity/axis selection and editable force
        /// data only after success. Native false discards partial/unwritten buffers and retains a null Forces property.</returns>
        /// <remarks>Issues one native GetMemberEndForces call with an internal six-Double NaN buffer and Int32 selectors.
        /// Successful output requires a one-dimensional six-Double array with every value finite and written;
        /// replacement arrays and nonzero lower bounds are accepted. Ordering is Fx,Fy,Fz,Mx,My,Mz.
        /// Values retain native result units and signs. No availability/title query, unit conversion, global transform
        /// or offset transfer is performed; native axis selection is explicit in the result.<para>Runs on the wrapper's constructing COM thread under one current-binding operation lease, including
        /// validation, enumeration, native dispatch and output qualification. RefreshComObjects invalidates captured
        /// adapters; reacquire them. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output,
        /// refresh or disposal calls are rejected before native access. The guard cannot coordinate raw calls,
        /// other wrappers or external UI. No implicit analysis, case activation, selection, save, unit conversion,
        /// background COM, retry or rollback is added. Native failures preserve original exceptions/HRESULTs.
        /// Calls can block. Snapshots do not guarantee result freshness or engineering validity.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, another thread/reentrant operation is active,
        /// a native flag/title is malformed or successful outputs are unwritten, nonfinite or incorrectly shaped.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A compatible native member is unavailable.</exception>

        /// <exception cref="ArgumentNullException">lcTitle is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An ID is nonpositive or an end/axis selector is undefined.</exception>
        /// <example><code>var query = wrapper.Output.GetMemberEndForces("Dead", 1, 1,
        ///     StaadPro.Interop.Core.Enums.MemberEnd.Start);
        /// if (query.IsSuccessful) Console.WriteLine(query.Forces);</code></example>
        MemberForceQueryResult GetMemberEndForces(string lcTitle, int beamId, int loadCaseId, MemberEnd memberEnd);

        /// <summary>Reads qualified member-end forces in the requested native axes.</summary>
        /// <param name="lcTitle">Non-null caller-supplied display title, retained unchanged; no title query is made.</param>
        /// <param name="beamId">Positive analytical member ID, not an index or physical-member ID.</param>
        /// <param name="loadCaseId">Positive native result load-case ID.</param>
        /// <param name="memberEnd">Start=0 or End=1; undefined values are rejected.</param>
        /// <param name="coordinateSystem">Local=0 or Global=1; undefined values are rejected.</param>
        /// <returns>A named per-end result with native success, immutable query identity/axis selection and editable force
        /// data only after success. Native false discards partial/unwritten buffers and retains a null Forces property.</returns>
        /// <remarks>Issues one native GetMemberEndForces call with an internal six-Double NaN buffer and Int32 selectors.
        /// Successful output requires a one-dimensional six-Double array with every value finite and written;
        /// replacement arrays and nonzero lower bounds are accepted. Ordering is Fx,Fy,Fz,Mx,My,Mz.
        /// Values retain native result units and signs. No availability/title query, unit conversion, global transform
        /// or offset transfer is performed; native axis selection is explicit in the result.<para>Runs on the wrapper's constructing COM thread under one current-binding operation lease, including
        /// validation, enumeration, native dispatch and output qualification. RefreshComObjects invalidates captured
        /// adapters; reacquire them. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output,
        /// refresh or disposal calls are rejected before native access. The guard cannot coordinate raw calls,
        /// other wrappers or external UI. No implicit analysis, case activation, selection, save, unit conversion,
        /// background COM, retry or rollback is added. Native failures preserve original exceptions/HRESULTs.
        /// Calls can block. Snapshots do not guarantee result freshness or engineering validity.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, another thread/reentrant operation is active,
        /// a native flag/title is malformed or successful outputs are unwritten, nonfinite or incorrectly shaped.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A compatible native member is unavailable.</exception>

        /// <exception cref="ArgumentNullException">lcTitle is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An ID is nonpositive or an end/axis selector is undefined.</exception>
        /// <example><code>var query = wrapper.Output.GetMemberEndForces("Dead", 1, 1,
        ///     StaadPro.Interop.Core.Enums.MemberEnd.Start, StaadPro.Interop.Core.Enums.LocalOrGlobal.Global);
        /// if (query.IsSuccessful) Console.WriteLine(query.Forces);</code></example>
        MemberForceQueryResult GetMemberEndForces(string lcTitle, int beamId, int loadCaseId, MemberEnd memberEnd, LocalOrGlobal coordinateSystem);
        /// <summary>Observes whether native buckling-analysis results are available.</summary>
        /// <returns>The native Boolean or integral zero/one availability flag. False is valid data.</returns>
        /// <remarks><para>Calls IsBucklingAnalysisResultsAvailable once on the captured Output binding. Rejects unsupported types/codes. Does not request factors, test ordinary result availability or infer completion, freshness, convergence or engineering acceptance. This is an immediate independent observation.</para>
        /// <para>Runs on the wrapper constructing COM thread under one current-binding lease covering input validation, every native call and output qualification. Reacquire the adapter after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal operations fail before native access. Calls may block and original native exceptions/HRESULTs propagate. Raw objects, other wrappers and external UI are outside the guard. Sequential observations are not atomic.</para>
        /// <para>No analysis, case activation, selection, save, unit conversion, retry or native mutation is performed. Coordinate model edits and shared-session access on the owning thread.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid or native data is malformed, unwritten, inconsistent or outside the documented bounds.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The native binding lacks a required signature.</exception>
        /// <example><code>
        /// bool available = wrapper.Output.AreBucklingAnalysisResultsAvailable();
        /// Console.WriteLine(available);
        /// </code></example>
        bool AreBucklingAnalysisResultsAvailable();

        /// <summary>Reads the reported number of computed buckling factors.</summary>
        /// <returns>A qualified nonnegative Int32 count from GetNoOfBucklingFactors. Zero is a valid count. Boolean, fractional, negative and out-of-range results throw.</returns>
        /// <remarks><para>Calls GetNoOfBucklingFactors exactly once without fetching factor values or issuing an availability query. This scalar observation accepts the full nonnegative Int32 range and allocates no factor buffer. Availability, factor count and analysis/model state may change between separate calls. No factor, eigenmode or engineering result is synthesized.</para>
        /// <para>Runs on the wrapper constructing COM thread under one current-binding lease covering input validation, every native call and output qualification. Reacquire the adapter after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal operations fail before native access. Calls may block and original native exceptions/HRESULTs propagate. Raw objects, other wrappers and external UI are outside the guard. Sequential observations are not atomic.</para>
        /// <para>No analysis, case activation, selection, save, unit conversion, retry or native mutation is performed. Coordinate model edits and shared-session access on the owning thread.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid or native data is malformed, unwritten, inconsistent or outside the documented bounds.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The native binding lacks a required signature.</exception>
        /// <example><code>
        /// int count = wrapper.Output.GetNumberOfBucklingFactorsComputed();
        /// Console.WriteLine(count);
        /// </code></example>
        int GetNumberOfBucklingFactorsComputed();


        /// <summary>Reads a finite buckling factor for one native mode.</summary>
        /// <param name="bucklingMode">Positive native buckling-mode number, not a zero-based index.</param>
        /// <returns>The written finite Double factor with its original sign; zero and negative factors are preserved. No default zero is synthesized.</returns>
        /// <remarks><para>Calls native GetBucklingFactor once with a by-reference Double NaN sentinel. Requires Boolean true or integral one success and a written finite Double. False or integral zero throws. This corrects a by-value output call that could leave an initialized zero unchanged. Does not request availability or factor count, select a mode, normalize values or infer convergence.</para><para>Executes native dispatch synchronously on the wrapper constructing COM thread under one current-binding operation lease spanning validation and output qualification. Reacquire Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal operations are rejected before native access. Raw objects, other wrappers and external UI remain outside the guard. Calls can block; original exceptions/HRESULTs propagate. Separate observations are not atomic or proof of result freshness.</para>
        /// <para>No analysis, activation, geometry hydration, selection, save, conversion, background COM, retry or mutation occurs. Positive identifiers do not prove native existence or engineering validity.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">bucklingMode is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, native retrieval fails, or successful outputs are unwritten, malformed or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// Console.WriteLine(wrapper.Output.GetBucklingFactor(1));
        /// </code></example>
        double GetBucklingFactor(int bucklingMode);

        /// <summary>Reads all six buckling-mode displacement components at a supplied node's captured identifier.</summary>
        /// <param name="bucklingMode">Positive native buckling-mode number.</param>
        /// <param name="atNode">Non-null detached node with a positive Id. Reads Id once; coordinates and other node data are unused and need not be populated.</param>
        /// <returns>An immutable BucklingModeDisplacementResult with captured mode/node identity and six finite components in unchanged native array order.</returns>
        /// <remarks><para>Uses the same single native GetBucklingModeDisplacementAtNode call and six-Double qualification as the ID overload. A six-component native vector cannot be represented by a scalar Double; this return model preserves every component rather than casting, selecting one component or inventing a magnitude. Later edits to atNode or native buffers do not alter the result.</para>
        /// <para>Requires Boolean true or integral one success and an exact-length one-dimensional Double array with every entry written and finite. Accepts replacement arrays and nonzero lower bounds. Preserves native signs, axes, units and eigenmode normalization without assuming engineering amplitude or adding an axis mapping. No availability/count/coordinate query is performed.</para><para>Executes native dispatch synchronously on the wrapper constructing COM thread under one current-binding operation lease spanning validation and output qualification. Reacquire Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal operations are rejected before native access. Raw objects, other wrappers and external UI remain outside the guard. Calls can block; original exceptions/HRESULTs propagate. Separate observations are not atomic or proof of result freshness.</para>
        /// <para>No analysis, activation, geometry hydration, selection, save, conversion, background COM, retry or mutation occurs. Positive identifiers do not prove native existence or engineering validity.</para></remarks>
        /// <exception cref="ArgumentNullException">atNode is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">bucklingMode or atNode.Id is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, native retrieval fails, or successful outputs are unwritten, malformed or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var node = new StaadPro.Interop.Core.Entities.Node(4, 0, 0, 0);
        /// var displacement = wrapper.Output.GetBucklingModeDisplacement(1, node);
        /// foreach (double component in displacement.Components) Console.WriteLine(component);
        /// </code></example>
        BucklingModeDisplacementResult GetBucklingModeDisplacement(int bucklingMode, Node atNode);

        /// <summary>Reads all six native buckling-mode displacement components at a node number.</summary>
        /// <param name="bucklingMode">Positive native buckling-mode number, not a zero-based index.</param>
        /// <param name="nodeId">Positive native node identifier; native existence is determined by the retrieval call.</param>
        /// <returns>An independent immutable BucklingModeDisplacementResult with mode/node identity and read-only owned storage for all six components.</returns>
        /// <remarks><para>Calls GetBucklingModeDisplacementAtNode once with six Double NaN sentinels. Requires Boolean true or integral one success; false/zero throws. Successful output must be a rank-one exact-length six-Double array with every entry written and finite. Replaced buffers and nonzero lower bounds are supported; output storage is copied.</para>
        /// <para>Corrects a scalar return contract for a six-component native vector. Preserves native order, signs, axes, units and eigenmode normalization without selecting a component, calculating a magnitude, assigning inferred axis labels or promising physical response amplitude. Does not request availability, factor count, geometry or coordinate conversion.</para><para>Executes native dispatch synchronously on the wrapper constructing COM thread under one current-binding operation lease spanning validation and output qualification. Reacquire Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal operations are rejected before native access. Raw objects, other wrappers and external UI remain outside the guard. Calls can block; original exceptions/HRESULTs propagate. Separate observations are not atomic or proof of result freshness.</para>
        /// <para>No analysis, activation, geometry hydration, selection, save, conversion, background COM, retry or mutation occurs. Positive identifiers do not prove native existence or engineering validity.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">bucklingMode or nodeId is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, native retrieval fails, or successful outputs are unwritten, malformed or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var displacement = wrapper.Output.GetBucklingModeDisplacementAtNode(1, 4);
        /// Console.WriteLine(displacement.NodeId + ": " + displacement.Components.Count);
        /// </code></example>
        BucklingModeDisplacementResult GetBucklingModeDisplacementAtNode(int bucklingMode, int nodeId);

        /// <summary>Reads the critical steel-design ratio for an analytical member.</summary>
        /// <param name="beamId">Positive analytical member number, not an index or physical-member identifier.</param>
        /// <returns>A written finite nonnegative dimensionless critical ratio, or native sentinel -1 (analysis not performed) or -999 (analysis performed but member not designed), only after successful native status.</returns>
        /// <remarks><para>Calls GetMemberSteelDesignRatio once with a by-reference Double NaN sentinel. Requires Boolean true or integral one success. False/zero status throws even if an output value was supplied. Successful output must be a finite Double; negative values other than -1 and -999 throw. Sentinel values are observations, not passing design results.</para>
        /// <para>The native result belongs to the last parameter block for which the member was designed. No code, load case, parameter block or design-completion inference is made. No availability query, analysis or design run occurs. Ratios above one are preserved; the helper does not decide design acceptance.</para><para>Executes native dispatch synchronously on the wrapper constructing COM thread under one current-binding operation lease spanning validation and output qualification. Reacquire Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal operations are rejected before native access. Raw objects, other wrappers and external UI remain outside the guard. Calls can block; original exceptions/HRESULTs propagate. Separate observations are not atomic or proof of result freshness.</para>
        /// <para>No analysis, activation, geometry hydration, selection, save, conversion, background COM, retry or mutation occurs. Positive identifiers do not prove native existence or engineering validity.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">beamId is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, native retrieval fails, or successful outputs are unwritten, malformed or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// Console.WriteLine(wrapper.Output.GetMemberSteelUtilizationRatio(1));
        /// </code></example>
        double GetMemberSteelUtilizationRatio(int beamId);

        /// <summary>Returns a settled task for a steel-design ratio queried on the owning COM thread.</summary>
        /// <param name="beamId">Positive analytical member number submitted to the same native query as the synchronous overload.</param>
        /// <returns>An already-completed Task containing a qualified dimensionless ratio or documented -1/-999 sentinel, or an already-faulted Task carrying the original validation/native exception. No failure becomes zero or success.</returns>
        /// <remarks><para>Performs the native call synchronously before returning the task. Awaiting does not make the COM call nonblocking. Calls must originate on the constructing thread; dispatching through Task.Run or another worker is rejected. No synchronization-context marshaling, worker thread, cancellation, retry or background COM is introduced. Unlike a synchronous throw, exceptions are retained by the returned faulted task and rethrown by await.</para>
        /// <para>Uses the same single native GetMemberSteelDesignRatio call, NaN sentinel, strict Boolean/integral zero-one status and written finite Double validation as the synchronous query. Preserves only nonnegative ratios and documented -1/-999 absence sentinels after success. Reads the last designed parameter block; sentinel values are not a passing result.</para><para>Executes native dispatch synchronously on the wrapper constructing COM thread under one current-binding operation lease spanning validation and output qualification. Reacquire Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal operations are rejected before native access. Raw objects, other wrappers and external UI remain outside the guard. Calls can block; original exceptions/HRESULTs propagate. Separate observations are not atomic or proof of result freshness.</para>
        /// <para>No analysis, activation, geometry hydration, selection, save, conversion, background COM, retry or mutation occurs. Positive identifiers do not prove native existence or engineering validity.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">The returned task faults when beamId is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation is invalid, native retrieval fails, or successful outputs are unwritten, malformed or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// double ratio = wrapper.Output.GetMemberSteelUtilizationRatioAsync(1).GetAwaiter().GetResult();
        /// Console.WriteLine(ratio);
        /// </code></example>
        Task<double> GetMemberSteelUtilizationRatioAsync(int beamId);

        /// <summary>Returns a settled task for a local-axis member-end force observation queried on the owning COM thread.</summary>
        /// <param name="lcTitle">Non-null caller display title retained literally, without a native title query.</param>
        /// <param name="beamId">Positive analytical member number.</param>
        /// <param name="loadCaseId">Positive native result load-case number.</param>
        /// <param name="memberEnd">Start=0 or End=1; undefined values fail before native dispatch.</param>
        /// <returns>An already-settled Task of MemberForceQueryResult, consistent with the synchronous Core force query. Native false produces IsSuccessful=false and null Forces with query identity retained. Native success supplies qualified editable force data. Validation/native failures fault the task.</returns>
        /// <remarks><para>Calls GetMemberEndForces once with Int32 end/axis selectors and six Double NaN sentinels, explicitly selecting Local axes. Success requires a rank-one six-Double array with every component written and finite; replacements and nonzero lower bounds are supported. Native false discards partial output. Ordering is Fx,Fy,Fz,Mx,My,Mz in unchanged native result units/signs. Does not activate a case or query availability/title.</para>
        /// <para>The named query result preserves native failure information that a bare MemberForces cannot represent; the return is Task of the same result model as the synchronous Core method. No unqualified initialized-zero record or invented force is returned.</para><para>Completes native work on the calling owning thread before returning an already-completed or already-faulted Task. Await rethrows the original validation/native exception without substituting defaults. This is a task-shaped API, not nonblocking dispatch. Task.Run/worker-thread use fails the thread guard. No synchronization-context dispatch, cancellation, parallel COM or retry is introduced.</para><para>Runs synchronously on the wrapper constructing COM thread under one current-binding Output operation lease covering input capture, every native read, qualification and managed population where applicable. Reacquire Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw calls, other wrappers and external UI are uncoordinated. Calls can block; original native exceptions/HRESULTs propagate. Observations are not atomic model snapshots.</para>
        /// <para>No analysis, design, load-case activation, selection, geometry hydration, unit conversion, save, native mutation or retry is added. Caller-supplied objects/collections must not be edited concurrently.</para></remarks>
        /// <exception cref="ArgumentNullException">The task faults when lcTitle is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The task faults for a nonpositive ID or undefined memberEnd.</exception>
        /// <exception cref="ObjectDisposedException">The wrapper is disposed; task-returning methods retain this exception in the faulted task.</exception>
        /// <exception cref="InvalidOperationException">Binding/thread/reentrancy is invalid, native status/output is malformed or unsuccessful, a selected batch ID changes, or collection enumeration fails. Task methods return a faulted task.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates, or is retained by a returned faulted task.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable, or the returned task retains this failure.</exception>
        /// <example><code>
        /// var query = wrapper.Output.GetMemberEndForcesAsync("Dead", 1, 1,
        ///     StaadPro.Interop.Core.Enums.MemberEnd.Start).GetAwaiter().GetResult();
        /// if (query.IsSuccessful) Console.WriteLine(query.Forces);
        /// </code></example>
        Task<MemberForceQueryResult> GetMemberEndForcesAsync(string lcTitle, int beamId, int loadCaseId, MemberEnd memberEnd);

        /// <summary>Returns a settled steel-utilization task using a supplied member's captured identifier.</summary>
        /// <param name="b">Non-null Beam with a positive analytical Id. Reads Id once; endpoints, geometry and existing ratio are unused.</param>
        /// <returns>An already-completed task containing the qualified ratio for the captured ID, or an already-faulted task containing the original failure. Does not populate b.UtilizationRatio.</returns>
        /// <remarks><para>Retains the original Beam reference parameter and captures its ID inside the operation lease. Later Beam edits do not recompute the scalar observation; the caller must coordinate identity changes.</para><para>Each native GetMemberSteelDesignRatio call uses a by-reference Double NaN sentinel. Requires Boolean true or integral one status and a written finite Double. False/zero status throws even if data was written. Preserves nonnegative dimensionless ratios and documented -1 (analysis not performed)/-999 (analyzed but member not designed) sentinels; other negative values throw. Results refer to the last parameter block used to design each analytical member. Sentinels and unpopulated zero values are not proof of passing design.</para><para>Completes native work on the calling owning thread before returning an already-completed or already-faulted Task. Await rethrows the original validation/native exception without substituting defaults. This is a task-shaped API, not nonblocking dispatch. Task.Run/worker-thread use fails the thread guard. No synchronization-context dispatch, cancellation, parallel COM or retry is introduced.</para><para>Runs synchronously on the wrapper constructing COM thread under one current-binding Output operation lease covering input capture, every native read, qualification and managed population where applicable. Reacquire Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw calls, other wrappers and external UI are uncoordinated. Calls can block; original native exceptions/HRESULTs propagate. Observations are not atomic model snapshots.</para>
        /// <para>No analysis, design, load-case activation, selection, geometry hydration, unit conversion, save, native mutation or retry is added. Caller-supplied objects/collections must not be edited concurrently.</para></remarks>
        /// <exception cref="ArgumentNullException">The task faults when b is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The task faults when b.Id is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The wrapper is disposed; task-returning methods retain this exception in the faulted task.</exception>
        /// <exception cref="InvalidOperationException">Binding/thread/reentrancy is invalid, native status/output is malformed or unsuccessful, a selected batch ID changes, or collection enumeration fails. Task methods return a faulted task.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates, or is retained by a returned faulted task.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable, or the returned task retains this failure.</exception>
        /// <example><code>
        /// var beam = new StaadPro.Interop.Core.Entities.Beam { Id = 7 };
        /// double ratio = wrapper.Output.GetMemberSteelUtilizationRatioAsync(beam).GetAwaiter().GetResult();
        /// Console.WriteLine(ratio);
        /// </code></example>
        Task<double> GetMemberSteelUtilizationRatioAsync(Beam b);

        /// <summary>Reads qualified steel-utilization ratios for a captured set of analytical members.</summary>
        /// <param name="beams">Non-null HashSet of distinct non-null Beam references with unique positive current IDs. Empty input is valid; at most 100,000 entries are allowed.</param>
        /// <returns>A fresh editable dictionary mapping the original Beam references to qualified ratios. The dictionary uses reference identity so later Beam.Id edits do not corrupt key lookup or merge entries. No Beam property is populated, and no partial dictionary is returned on failure.</returns>
        /// <remarks><para>Enumerates the supplied HashSet once into a bounded snapshot of original Beam references and captured positive IDs before any native read. Accepts at most 100,000 entries; rejects null entries, repeated references and duplicate current IDs, including duplicates caused by mutable hash keys or a custom comparer. Input order is unspecified. Empty valid input issues no native calls. Geometry/endpoints and existing UtilizationRatio values are unused.</para>
        /// <para>Queries sequentially once per captured member with no worker-thread COM. Checks each current ID before its query and all selected IDs after retrieval. An observed ID change throws; temporary changes restored before a check cannot be detected. Captured membership does not follow later HashSet additions/removals. Native/model edits outside this wrapper cannot make a series of reads atomic. Caller must coordinate edits.</para><para>Each native GetMemberSteelDesignRatio call uses a by-reference Double NaN sentinel. Requires Boolean true or integral one status and a written finite Double. False/zero status throws even if data was written. Preserves nonnegative dimensionless ratios and documented -1 (analysis not performed)/-999 (analyzed but member not designed) sentinels; other negative values throw. Results refer to the last parameter block used to design each analytical member. Sentinels and unpopulated zero values are not proof of passing design.</para><para>The input Beam type compares/hashes by mutable ID. Returned storage deliberately uses reference equality instead: a different Beam object with the same ID is not a dictionary key. Returned membership and values are detached; edits do not update STAAD or recompute ratios. Each call owns its dictionary.</para><para>Runs synchronously on the wrapper constructing COM thread under one current-binding Output operation lease covering input capture, every native read, qualification and managed population where applicable. Reacquire Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw calls, other wrappers and external UI are uncoordinated. Calls can block; original native exceptions/HRESULTs propagate. Observations are not atomic model snapshots.</para>
        /// <para>No analysis, design, load-case activation, selection, geometry hydration, unit conversion, save, native mutation or retry is added. Caller-supplied objects/collections must not be edited concurrently.</para></remarks>
        /// <exception cref="ArgumentNullException">beams is null.</exception>
        /// <exception cref="ArgumentException">An entry is null, has a nonpositive/duplicate ID or duplicate reference, or the 100,000-entry bound is exceeded.</exception>
        /// <exception cref="ObjectDisposedException">The wrapper is disposed; task-returning methods retain this exception in the faulted task.</exception>
        /// <exception cref="InvalidOperationException">Binding/thread/reentrancy is invalid, native status/output is malformed or unsuccessful, a selected batch ID changes, or collection enumeration fails. Task methods return a faulted task.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates, or is retained by a returned faulted task.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable, or the returned task retains this failure.</exception>
        /// <example><code>
        /// var beams = new System.Collections.Generic.HashSet&lt;StaadPro.Interop.Core.Entities.Beam&gt;
        ///     { new StaadPro.Interop.Core.Entities.Beam { Id = 7 } };
        /// var ratios = wrapper.Output.GetMemberSteelUtilizationRatios(beams);
        /// foreach (var pair in ratios) Console.WriteLine(pair.Key.Id + ": " + pair.Value);
        /// </code></example>
        Dictionary<Beam, double> GetMemberSteelUtilizationRatios(HashSet<Beam> beams);

        /// <summary>Reads a complete qualified ratio batch, then updates the supplied detached member records.</summary>
        /// <param name="forBeams">Non-null HashSet of distinct non-null Beam references with unique positive current IDs, limited to 100,000 entries. Empty input performs no queries or writes.</param>
        /// <returns>No value. After all reads qualify, assigns each captured Beam.UtilizationRatio sequentially on the owning thread.</returns>
        /// <remarks><para>Enumerates the supplied HashSet once into a bounded snapshot of original Beam references and captured positive IDs before any native read. Accepts at most 100,000 entries; rejects null entries, repeated references and duplicate current IDs, including duplicates caused by mutable hash keys or a custom comparer. Input order is unspecified. Empty valid input issues no native calls. Geometry/endpoints and existing UtilizationRatio values are unused.</para>
        /// <para>Queries sequentially once per captured member with no worker-thread COM. Checks each current ID before its query and all selected IDs after retrieval. An observed ID change throws; temporary changes restored before a check cannot be detected. Captured membership does not follow later HashSet additions/removals. Native/model edits outside this wrapper cannot make a series of reads atomic. Caller must coordinate edits.</para><para>Each native GetMemberSteelDesignRatio call uses a by-reference Double NaN sentinel. Requires Boolean true or integral one status and a written finite Double. False/zero status throws even if data was written. Preserves nonnegative dimensionless ratios and documented -1 (analysis not performed)/-999 (analyzed but member not designed) sentinels; other negative values throw. Results refer to the last parameter block used to design each analytical member. Sentinels and unpopulated zero values are not proof of passing design.</para><para>All native reads and full ID rechecks complete before any ratio assignment. Input-validation/native-retrieval/qualification failure therefore makes no ratio writes by this method. Rechecks each captured ID immediately before its assignment. Updates detached metadata only; it does not submit a native design change.</para>
        /// <para>PropertyChanged handlers run synchronously after a stored value changes, while the operation lease is still held. Reentrant guarded work is rejected. A handler can throw or edit metadata after some values were applied; that failure propagates without rollback, and earlier/current assignments may remain. This is not an atomic transaction and arbitrary external callback edits cannot be undone or fully detected.</para><para>Runs synchronously on the wrapper constructing COM thread under one current-binding Output operation lease covering input capture, every native read, qualification and managed population where applicable. Reacquire Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw calls, other wrappers and external UI are uncoordinated. Calls can block; original native exceptions/HRESULTs propagate. Observations are not atomic model snapshots.</para>
        /// <para>No analysis, design, load-case activation, selection, geometry hydration, unit conversion, save, native mutation or retry is added. Caller-supplied objects/collections must not be edited concurrently.</para></remarks>
        /// <exception cref="ArgumentNullException">forBeams is null.</exception>
        /// <exception cref="ArgumentException">A selected entry/reference/ID is invalid, duplicated, or exceeds the batch bound.</exception>
        /// <exception cref="Exception">A managed property notification handler throws after one or more assignments; the original handler exception propagates without rollback.</exception>
        /// <exception cref="ObjectDisposedException">The wrapper is disposed; task-returning methods retain this exception in the faulted task.</exception>
        /// <exception cref="InvalidOperationException">Binding/thread/reentrancy is invalid, native status/output is malformed or unsuccessful, a selected batch ID changes, or collection enumeration fails. Task methods return a faulted task.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates, or is retained by a returned faulted task.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable, or the returned task retains this failure.</exception>
        /// <example><code>
        /// var beam = new StaadPro.Interop.Core.Entities.Beam { Id = 7 };
        /// var beams = new System.Collections.Generic.HashSet&lt;StaadPro.Interop.Core.Entities.Beam&gt; { beam };
        /// wrapper.Output.PopulateMemberSteelUtilizationRatios(beams);
        /// Console.WriteLine(beam.UtilizationRatio);
        /// </code></example>
        void PopulateMemberSteelUtilizationRatios(HashSet<Beam> forBeams);

        /// <summary>Returns a settled task containing a qualified captured member-to-ratio dictionary.</summary>
        /// <param name="beams">Non-null HashSet of distinct non-null Beam references with unique positive current IDs. Empty input is valid; at most 100,000 entries are allowed.</param>
        /// <returns>An already-completed task containing fresh editable reference-identity dictionary storage, or an already-faulted task containing the original failure. No partial result escapes and no Beam property is populated.</returns>
        /// <remarks><para>Enumerates the supplied HashSet once into a bounded snapshot of original Beam references and captured positive IDs before any native read. Accepts at most 100,000 entries; rejects null entries, repeated references and duplicate current IDs, including duplicates caused by mutable hash keys or a custom comparer. Input order is unspecified. Empty valid input issues no native calls. Geometry/endpoints and existing UtilizationRatio values are unused.</para>
        /// <para>Queries sequentially once per captured member with no worker-thread COM. Checks each current ID before its query and all selected IDs after retrieval. An observed ID change throws; temporary changes restored before a check cannot be detected. Captured membership does not follow later HashSet additions/removals. Native/model edits outside this wrapper cannot make a series of reads atomic. Caller must coordinate edits.</para><para>Each native GetMemberSteelDesignRatio call uses a by-reference Double NaN sentinel. Requires Boolean true or integral one status and a written finite Double. False/zero status throws even if data was written. Preserves nonnegative dimensionless ratios and documented -1 (analysis not performed)/-999 (analyzed but member not designed) sentinels; other negative values throw. Results refer to the last parameter block used to design each analytical member. Sentinels and unpopulated zero values are not proof of passing design.</para><para>Retains original Beam references as keys with reference equality, allowing later ID edits without invalidating dictionary lookup. Another Beam with the same ID is not the same key. Native calls execute sequentially within one operation lease, replacing unsafe parallel COM calls and concurrent dictionary writes.</para><para>Completes native work on the calling owning thread before returning an already-completed or already-faulted Task. Await rethrows the original validation/native exception without substituting defaults. This is a task-shaped API, not nonblocking dispatch. Task.Run/worker-thread use fails the thread guard. No synchronization-context dispatch, cancellation, parallel COM or retry is introduced.</para><para>Runs synchronously on the wrapper constructing COM thread under one current-binding Output operation lease covering input capture, every native read, qualification and managed population where applicable. Reacquire Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw calls, other wrappers and external UI are uncoordinated. Calls can block; original native exceptions/HRESULTs propagate. Observations are not atomic model snapshots.</para>
        /// <para>No analysis, design, load-case activation, selection, geometry hydration, unit conversion, save, native mutation or retry is added. Caller-supplied objects/collections must not be edited concurrently.</para></remarks>
        /// <exception cref="ArgumentNullException">The task faults when beams is null.</exception>
        /// <exception cref="ArgumentException">The task faults for a null/duplicate entry, invalid/duplicate ID or exceeded batch bound.</exception>
        /// <exception cref="ObjectDisposedException">The wrapper is disposed; task-returning methods retain this exception in the faulted task.</exception>
        /// <exception cref="InvalidOperationException">Binding/thread/reentrancy is invalid, native status/output is malformed or unsuccessful, a selected batch ID changes, or collection enumeration fails. Task methods return a faulted task.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates, or is retained by a returned faulted task.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable, or the returned task retains this failure.</exception>
        /// <example><code>
        /// var beams = new System.Collections.Generic.HashSet&lt;StaadPro.Interop.Core.Entities.Beam&gt;
        ///     { new StaadPro.Interop.Core.Entities.Beam { Id = 7 } };
        /// var ratios = wrapper.Output.GetMemberSteelUtilizationRatiosAsync(beams).GetAwaiter().GetResult();
        /// Console.WriteLine(ratios.Count);
        /// </code></example>
        Task<Dictionary<Beam, double>> GetMemberSteelUtilizationRatiosAsync(HashSet<Beam> beams);

        /// <summary>Reads a qualified steel-design record using supplied load-case metadata.</summary>
        /// <param name="beam">Non-null Beam with a positive captured analytical ID; geometry and existing ratio are unused.</param>
        /// <param name="allLoadCases">Non-null bounded metadata sequence with unique positive case IDs, non-null titles and defined enum values.</param>
        /// <returns>A new detached MemberSteelDesignResults with the original beam reference and independently captured critical-case/force data. Native failure or unresolved metadata throws; no initialized default record is returned.</returns>
        /// <remarks><para>Calls GetMemberSteelDesignResults once per selected beam occurrence with unwritten string/Int32/Double and three-Double array sentinels. Requires Boolean true or integral one success and every output written in its documented type. Design status accepts trimmed ordinal-ignore-case PASS or FAIL text; unknown text/numeric statuses throw rather than being inferred from a ratio. Ratios, allowable ratio, section distance and KL/r must be finite nonnegative Doubles. Native text is retained literally. Critical case ID must be positive and uniquely resolved.</para>
        /// <para>Copies a rank-one exact-length three-Double native array, allowing replacements/nonzero lower bounds, into SteelDesignForces containing Fx,My,Mz only. No unreported component is synthesized. Section distance and forces retain native current base length/force/moment units, signs and code conventions without transformation. The native record belongs to the last parameter block used to design the member; this is not an engineering acceptance or freshness guarantee.</para>
        /// <para>MemberSteelDesignResults remains editable detached metadata: Member retains the supplied Beam reference, while case metadata and immutable three-component forces are independently owned per result. Editing a result does not update STAAD or recalculate fields. Later Beam edits remain visible through Member without changing captured design values.</para><para>Enumerates allLoadCases exactly once before native dispatch into captured ID/title/family/type records. At most 100,000 records; null entries, nonpositive or duplicate IDs, null titles and undefined enum values throw. Defined case families are retained without inferring analysis eligibility. Missing critical-case identity throws rather than returning a null case. All supplied records are qualified, including unused entries. Caller metadata is never mutated.</para><para>Captures beam.Id before reading metadata and checks identity after native retrieval. Does not populate Beam.UtilizationRatio or query native Load metadata.</para><para>Runs on the constructing COM thread under one current Output binding/lifetime/reentrancy lease spanning input enumeration, native Load/Output reads, qualification and managed population where applicable. Reacquire adapters after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw objects, other wrappers and external UI are outside this guard. Calls may block; original exceptions/HRESULTs propagate. Sequential observations and mutable input captures are not atomic.</para>
        /// <para>No analysis, design, case activation, geometry hydration, selection, native mutation, save, conversion, retry or worker-thread COM is introduced. Callers must coordinate input/session edits.</para></remarks>
        /// <exception cref="ArgumentNullException">beam or allLoadCases is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">beam.Id is not positive.</exception>
        /// <exception cref="ArgumentException">Supplied case metadata is invalid, duplicated or exceeds the bound.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, successful native outputs are malformed/unwritten, retrieval fails, case identity is unresolved, selected IDs change, or a governing ratio is unavailable.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var beam = new StaadPro.Interop.Core.Entities.Beam { Id = 7 };
        /// var cases = new[] { new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead") };
        /// var result = wrapper.Output.GetSteelDesignResultsForBeam(beam, cases);
        /// Console.WriteLine(result.DesignStatus + ": " + result.DesignForces.Fx);
        /// </code></example>
        MemberSteelDesignResults GetSteelDesignResultsForBeam(Beam beam, IEnumerable<ILoadCase> allLoadCases);

        /// <summary>Reads a qualified steel-design record with critical-case metadata discovered through the current Load binding.</summary>
        /// <param name="load">This wrapper's non-null current Load adapter.</param>
        /// <param name="beam">Non-null Beam with a positive analytical ID; its original reference is retained in the result.</param>
        /// <returns>A new detached MemberSteelDesignResults with a resolved primary/combination critical case and three reported force components.</returns>
        /// <remarks><para>Calls GetMemberSteelDesignResults once per selected beam occurrence with unwritten string/Int32/Double and three-Double array sentinels. Requires Boolean true or integral one success and every output written in its documented type. Design status accepts trimmed ordinal-ignore-case PASS or FAIL text; unknown text/numeric statuses throw rather than being inferred from a ratio. Ratios, allowable ratio, section distance and KL/r must be finite nonnegative Doubles. Native text is retained literally. Critical case ID must be positive and uniquely resolved.</para>
        /// <para>Copies a rank-one exact-length three-Double native array, allowing replacements/nonzero lower bounds, into SteelDesignForces containing Fx,My,Mz only. No unreported component is synthesized. Section distance and forces retain native current base length/force/moment units, signs and code conventions without transformation. The native record belongs to the last parameter block used to design the member; this is not an engineering acceptance or freshness guarantee.</para>
        /// <para>MemberSteelDesignResults remains editable detached metadata: Member retains the supplied Beam reference, while case metadata and immutable three-component forces are independently owned per result. Editing a result does not update STAAD or recalculate fields. Later Beam edits remain visible through Member without changing captured design values.</para><para>Requires this Output wrapper's current Load adapter; null/foreign/stale/manual adapters are rejected. Reads qualified primary and combination inventories/metadata directly under the same lease without nesting public Load calls. Combined case metadata is bounded to 100,000 unique IDs. No reference/repeat discovery or factor inference is added. Input beam validation precedes inventory calls. Empty beam batches skip inventories and native design reads after validating the Load binding.</para><para>Captures beam identity before inventories and verifies it before/after design retrieval. No ratio property is populated. Inventory/title/type failures propagate without fallback or an unresolved record.</para><para>Runs on the constructing COM thread under one current Output binding/lifetime/reentrancy lease spanning input enumeration, native Load/Output reads, qualification and managed population where applicable. Reacquire adapters after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw objects, other wrappers and external UI are outside this guard. Calls may block; original exceptions/HRESULTs propagate. Sequential observations and mutable input captures are not atomic.</para>
        /// <para>No analysis, design, case activation, geometry hydration, selection, native mutation, save, conversion, retry or worker-thread COM is introduced. Callers must coordinate input/session edits.</para></remarks>
        /// <exception cref="ArgumentNullException">load or beam is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">beam.Id is not positive.</exception>
        /// <exception cref="ArgumentException">load is not this wrapper's current adapter, or discovered metadata is ambiguous/outside bounds.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, successful native outputs are malformed/unwritten, retrieval fails, case identity is unresolved, selected IDs change, or a governing ratio is unavailable.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var beam = new StaadPro.Interop.Core.Entities.Beam { Id = 7 };
        /// var result = wrapper.Output.GetSteelDesignResultsForBeam(wrapper.Load, beam);
        /// Console.WriteLine(result.Code + ": " + result.CriticalLoadCase.Id);
        /// </code></example>
        MemberSteelDesignResults GetSteelDesignResultsForBeam(IOSLoad load, Beam beam);

        /// <summary>Eagerly reads steel-design records for supplied beam occurrences and case metadata.</summary>
        /// <param name="beams">Non-null bounded sequence of non-null positive-ID Beams. Duplicates and order are preserved.</param>
        /// <param name="allLoadCases">Non-null bounded sequence of fully qualified unique-ID case metadata.</param>
        /// <returns>An immediately materialized editable list through IEnumerable, with one independent record per input occurrence. Empty beams returns an empty list after validating the case metadata sequence.</returns>
        /// <remarks><para>Eagerly captures at most 100,000 beam occurrences and validates every non-null entry/positive ID before native work. Preserves enumeration order, duplicates and original Beam references; each occurrence is queried independently. Checks captured beam IDs before queries and after all retrieval; an observed change throws. Full input failures return no partial list. Results own mutable list storage and independent case/force metadata. Endpoints/geometry and existing ratios are unused.</para><para>Enumerates allLoadCases exactly once before native dispatch into captured ID/title/family/type records. At most 100,000 records; null entries, nonpositive or duplicate IDs, null titles and undefined enum values throw. Defined case families are retained without inferring analysis eligibility. Missing critical-case identity throws rather than returning a null case. All supplied records are qualified, including unused entries. Caller metadata is never mutated.</para><para>Calls GetMemberSteelDesignResults once per selected beam occurrence with unwritten string/Int32/Double and three-Double array sentinels. Requires Boolean true or integral one success and every output written in its documented type. Design status accepts trimmed ordinal-ignore-case PASS or FAIL text; unknown text/numeric statuses throw rather than being inferred from a ratio. Ratios, allowable ratio, section distance and KL/r must be finite nonnegative Doubles. Native text is retained literally. Critical case ID must be positive and uniquely resolved.</para>
        /// <para>Copies a rank-one exact-length three-Double native array, allowing replacements/nonzero lower bounds, into SteelDesignForces containing Fx,My,Mz only. No unreported component is synthesized. Section distance and forces retain native current base length/force/moment units, signs and code conventions without transformation. The native record belongs to the last parameter block used to design the member; this is not an engineering acceptance or freshness guarantee.</para>
        /// <para>MemberSteelDesignResults remains editable detached metadata: Member retains the supplied Beam reference, while case metadata and immutable three-component forces are independently owned per result. Editing a result does not update STAAD or recalculate fields. Later Beam edits remain visible through Member without changing captured design values.</para><para>Runs on the constructing COM thread under one current Output binding/lifetime/reentrancy lease spanning input enumeration, native Load/Output reads, qualification and managed population where applicable. Reacquire adapters after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw objects, other wrappers and external UI are outside this guard. Calls may block; original exceptions/HRESULTs propagate. Sequential observations and mutable input captures are not atomic.</para>
        /// <para>No analysis, design, case activation, geometry hydration, selection, native mutation, save, conversion, retry or worker-thread COM is introduced. Callers must coordinate input/session edits.</para></remarks>
        /// <exception cref="ArgumentNullException">beams or allLoadCases is null.</exception>
        /// <exception cref="ArgumentException">An entry/ID/title/enum is invalid, case IDs are ambiguous, or a 100,000-item bound is exceeded.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, successful native outputs are malformed/unwritten, retrieval fails, case identity is unresolved, selected IDs change, or a governing ratio is unavailable.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var beams = new[] { new StaadPro.Interop.Core.Entities.Beam { Id = 7 } };
        /// var cases = new[] { new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead") };
        /// foreach (var result in wrapper.Output.GetSteelDesignResultsForBeams(beams, cases))
        ///     Console.WriteLine(result.Member.Id + ": " + result.UtilizationRatio);
        /// </code></example>
        IEnumerable<MemberSteelDesignResults> GetSteelDesignResultsForBeams(IEnumerable<Beam> beams, IEnumerable<ILoadCase> allLoadCases);

        /// <summary>Eagerly reads steel-design records using current primary/combination metadata.</summary>
        /// <param name="load">This wrapper's non-null current Load adapter, validated even for empty beam input.</param>
        /// <param name="beams">Non-null sequence of non-null positive-ID Beams, with order/duplicates retained and at most 100,000 occurrences.</param>
        /// <returns>An immediately materialized editable list of independent records; an empty selection yields a new empty list without native inventory/design calls. Failure returns no partial list.</returns>
        /// <remarks><para>Eagerly captures at most 100,000 beam occurrences and validates every non-null entry/positive ID before native work. Preserves enumeration order, duplicates and original Beam references; each occurrence is queried independently. Checks captured beam IDs before queries and after all retrieval; an observed change throws. Full input failures return no partial list. Results own mutable list storage and independent case/force metadata. Endpoints/geometry and existing ratios are unused.</para><para>Requires this Output wrapper's current Load adapter; null/foreign/stale/manual adapters are rejected. Reads qualified primary and combination inventories/metadata directly under the same lease without nesting public Load calls. Combined case metadata is bounded to 100,000 unique IDs. No reference/repeat discovery or factor inference is added. Input beam validation precedes inventory calls. Empty beam batches skip inventories and native design reads after validating the Load binding.</para><para>Calls GetMemberSteelDesignResults once per selected beam occurrence with unwritten string/Int32/Double and three-Double array sentinels. Requires Boolean true or integral one success and every output written in its documented type. Design status accepts trimmed ordinal-ignore-case PASS or FAIL text; unknown text/numeric statuses throw rather than being inferred from a ratio. Ratios, allowable ratio, section distance and KL/r must be finite nonnegative Doubles. Native text is retained literally. Critical case ID must be positive and uniquely resolved.</para>
        /// <para>Copies a rank-one exact-length three-Double native array, allowing replacements/nonzero lower bounds, into SteelDesignForces containing Fx,My,Mz only. No unreported component is synthesized. Section distance and forces retain native current base length/force/moment units, signs and code conventions without transformation. The native record belongs to the last parameter block used to design the member; this is not an engineering acceptance or freshness guarantee.</para>
        /// <para>MemberSteelDesignResults remains editable detached metadata: Member retains the supplied Beam reference, while case metadata and immutable three-component forces are independently owned per result. Editing a result does not update STAAD or recalculate fields. Later Beam edits remain visible through Member without changing captured design values.</para><para>Runs on the constructing COM thread under one current Output binding/lifetime/reentrancy lease spanning input enumeration, native Load/Output reads, qualification and managed population where applicable. Reacquire adapters after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw objects, other wrappers and external UI are outside this guard. Calls may block; original exceptions/HRESULTs propagate. Sequential observations and mutable input captures are not atomic.</para>
        /// <para>No analysis, design, case activation, geometry hydration, selection, native mutation, save, conversion, retry or worker-thread COM is introduced. Callers must coordinate input/session edits.</para></remarks>
        /// <exception cref="ArgumentNullException">load or beams is null.</exception>
        /// <exception cref="ArgumentException">load is not current/owned, beam input is invalid/oversized or metadata cannot be unambiguously captured.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, successful native outputs are malformed/unwritten, retrieval fails, case identity is unresolved, selected IDs change, or a governing ratio is unavailable.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var beams = new[] { new StaadPro.Interop.Core.Entities.Beam { Id = 7 } };
        /// foreach (var result in wrapper.Output.GetSteelDesignResultsForBeams(wrapper.Load, beams))
        ///     Console.WriteLine(result.DesignSection + ": " + result.CriticalSection);
        /// </code></example>
        IEnumerable<MemberSteelDesignResults> GetSteelDesignResultsForBeams(IOSLoad load, IEnumerable<Beam> beams);

        /// <summary>Selects the supplied physical member whose largest constituent-beam utilization ratio is greatest.</summary>
        /// <param name="pmiResult">Non-null supplied grouping container. Members=null, null member entries, null Beams sets and empty Beams sets have no usable run and are ignored. Nonempty sets must contain non-null beams.</param>
        /// <param name="ratiosPopulated">False by default: query each unique Beam reference once, qualify all ratios, then populate its detached ratio. True: trust/capture supplied ratios without native calls or population. Cached mode allows draft IDs; it cannot distinguish unpopulated zero from valid zero.</param>
        /// <returns>The original Member reference with greatest captured constituent maximum; first member occurrence wins ties. Returns null when no usable run exists. Empty/null Members does not query native data.</returns>
        /// <remarks><para>Captures supplied member order and beam memberships before ranking/querying, with at most 100,000 member entries and 100,000 total beam memberships. Same Beam reference may participate in multiple runs. Endpoint/node/discarded metadata is unused. Does not identify physical members or populate geometry.</para>
        /// <para>Every ranked ratio must be finite and nonnegative. Native -1/-999 absence sentinels and invalid cached ratios throw instead of choosing a misleading winner. Default mode requires unique positive native IDs for distinct Beam references, queries sequentially using strict written Double/status qualification and checks observed ID changes. All reads and ratio qualification complete before any assignment. Cached mode reads no native inventory/results and leaves Beam data unchanged.</para>
        /// <para>Default mode then assigns detached UtilizationRatio values while the lease is held, checking IDs before writes and afterwards. Property handlers execute synchronously after writes and can throw/edit metadata after some values were applied; failures propagate without rollback and partial managed writes may remain. Ranking uses captured qualified ratios/membership, not subsequent handler edits. This is not atomic. The original Member result retains editable references; it is not engineering acceptance.</para><para>Runs on the constructing COM thread under one current Output binding/lifetime/reentrancy lease spanning input enumeration, native Load/Output reads, qualification and managed population where applicable. Reacquire adapters after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw objects, other wrappers and external UI are outside this guard. Calls may block; original exceptions/HRESULTs propagate. Sequential observations and mutable input captures are not atomic.</para>
        /// <para>No analysis, design, case activation, geometry hydration, selection, native mutation, save, conversion, retry or worker-thread COM is introduced. Callers must coordinate input/session edits.</para></remarks>
        /// <exception cref="ArgumentNullException">pmiResult is null.</exception>
        /// <exception cref="ArgumentException">Selected beam metadata is null/invalid/ambiguous for native reads or an entry/membership bound is exceeded.</exception>
        /// <exception cref="Exception">An original property handler failure propagates after possible managed writes; no rollback is attempted.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, successful native outputs are malformed/unwritten, retrieval fails, case identity is unresolved, selected IDs change, or a governing ratio is unavailable.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var beam = new StaadPro.Interop.Core.Entities.Beam { Id = 7, UtilizationRatio = 0.8 };
        /// var member = new StaadPro.Interop.Core.Entities.Member();
        /// member.Beams.Add(beam);
        /// var groups = new StaadPro.Interop.Core.Models.PhysicalMemberIdentificationResult();
        /// groups.Members.Add(member);
        /// Console.WriteLine(wrapper.Output.GetGoverningMember(groups, ratiosPopulated: true) == member);
        /// </code></example>
        Member GetGoverningMember(PhysicalMemberIdentificationResult pmiResult, bool ratiosPopulated = false);

        /// <summary>Reads qualified global support reactions for one node and case.</summary>
        /// <param name="supportId">Positive native support node number, not an ordinal in the support list. Existence/support suitability is determined by native retrieval.</param>
        /// <param name="loadCaseId">Positive native load-case reference number. This method does not activate, analyze or independently resolve the case.</param>
        /// <returns>A new detached LoadCaseForces with Id equal to loadCaseId and independently owned six-component Forces; supportId is not retained in this model. Unsuccessful native retrieval throws; no partial or initialized zero record is returned.</returns>
        /// <remarks><para>Uses one native GetSupportReactions(supportId, loadCaseId, ref reactions) call under the current Output binding. Six-Double buffers start with NaN sentinels. Requires Boolean true or integral one success; false/zero throws and malformed status is rejected. Every successful output must be a rank-one array of exactly six finite written Doubles in Fx,Fy,Fz,Mx,My,Mz order. Replacement arrays and nonzero lower bounds are copied; native storage is never retained.</para>
        /// <para>Native reactions are in global directions. Components retain native force/moment units and sign conventions without unit conversion or geometry hydration. Returns native reaction signs unchanged; GetSupportForces provides the opposite-sign convention. No existence, result freshness or engineering acceptance guarantee is inferred.</para>
        /// <para>Returned records are editable detached metadata. Mutations never update STAAD; manual model construction/setters are unvalidated. Each call owns new result/component storage, even when native arrays are reused or aliased.</para>
        /// <para>Runs synchronously on the wrapper constructing COM thread under one shared Output binding/lifetime/reentrancy lease covering validation, dispatch and qualification. Reacquire adapters after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw calls, other wrappers and external UI are outside this guard. Original exceptions and HRESULTs propagate; the lease is released on failure.</para>
        /// <para>No analysis, case activation, selection, save, native mutation, unit conversion, retry or rollback is introduced.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">supportId or loadCaseId is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, native retrieval is unsuccessful, or a successful output/status is malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates, including its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native signature is unavailable.</exception>
        /// <example><code>
        /// var result = wrapper.Output.GetSupportReactions(2, 1);
        /// Console.WriteLine(result.Id + ": " + result.Forces.Fy);
        /// </code></example>
        LoadCaseForces GetSupportReactions(int supportId, int loadCaseId);

        /// <summary>Reads qualified global support forces opposite to native reactions.</summary>
        /// <param name="supportId">Positive native support node number, not an ordinal in the support list. Existence/support suitability is determined by native retrieval.</param>
        /// <param name="loadCaseId">Positive native load-case reference number. This method does not activate, analyze or independently resolve the case.</param>
        /// <returns>A new detached LoadCaseForces with Id equal to loadCaseId and independently owned six-component Forces; supportId is not retained in this model. Unsuccessful native retrieval throws; no partial or initialized zero record is returned.</returns>
        /// <remarks><para>Uses one native GetSupportReactions(supportId, loadCaseId, ref reactions) call under the current Output binding. Six-Double buffers start with NaN sentinels. Requires Boolean true or integral one success; false/zero throws and malformed status is rejected. Every successful output must be a rank-one array of exactly six finite written Doubles in Fx,Fy,Fz,Mx,My,Mz order. Replacement arrays and nonzero lower bounds are copied; native storage is never retained.</para>
        /// <para>Native reactions are in global directions. Components retain native force/moment units and sign conventions without unit conversion or geometry hydration. Multiplies all six qualified reaction components by minus one, including moments, preserving the source support-force convention; does not make a second native call. No existence, result freshness or engineering acceptance guarantee is inferred.</para>
        /// <para>Returned records are editable detached metadata. Mutations never update STAAD; manual model construction/setters are unvalidated. Each call owns new result/component storage, even when native arrays are reused or aliased.</para>
        /// <para>Runs synchronously on the wrapper constructing COM thread under one shared Output binding/lifetime/reentrancy lease covering validation, dispatch and qualification. Reacquire adapters after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw calls, other wrappers and external UI are outside this guard. Original exceptions and HRESULTs propagate; the lease is released on failure.</para>
        /// <para>No analysis, case activation, selection, save, native mutation, unit conversion, retry or rollback is introduced.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">supportId or loadCaseId is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, native retrieval is unsuccessful, or a successful output/status is malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates, including its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native signature is unavailable.</exception>
        /// <example><code>
        /// var result = wrapper.Output.GetSupportForces(2, 1);
        /// Console.WriteLine(result.Id + ": " + result.Forces.Fy);
        /// </code></example>
        LoadCaseForces GetSupportForces(int supportId, int loadCaseId);

        /// <summary>Reads qualified global support reactions for one node and case.</summary>
        /// <param name="supportId">Positive native support node number, not an ordinal in the support list. Existence/support suitability is determined by native retrieval.</param>
        /// <param name="loadCaseId">Positive native load-case reference number. This method does not activate, analyze or independently resolve the case.</param>
        /// <returns>An already completed successful task containing a new detached LoadCaseForces with Id equal to loadCaseId and independently owned six-component Forces; supportId is not retained in this model. Validation, guard and native failures are returned as faulted tasks and rethrown when awaited. Unsuccessful native retrieval throws; no partial or initialized zero record is returned.</returns>
        /// <remarks><para>Uses one native GetSupportReactions(supportId, loadCaseId, ref reactions) call under the current Output binding. Six-Double buffers start with NaN sentinels. Requires Boolean true or integral one success; false/zero throws and malformed status is rejected. Every successful output must be a rank-one array of exactly six finite written Doubles in Fx,Fy,Fz,Mx,My,Mz order. Replacement arrays and nonzero lower bounds are copied; native storage is never retained.</para>
        /// <para>Native reactions are in global directions. Components retain native force/moment units and sign conventions without unit conversion or geometry hydration. Returns native reaction signs unchanged; GetSupportForces provides the opposite-sign convention. No existence, result freshness or engineering acceptance guarantee is inferred.</para>
        /// <para>Returned records are editable detached metadata. Mutations never update STAAD; manual model construction/setters are unvalidated. Each call owns new result/component storage, even when native arrays are reused or aliased.</para>
        /// <para>Runs synchronously on the wrapper constructing COM thread under one shared Output binding/lifetime/reentrancy lease covering validation, dispatch and qualification. Reacquire adapters after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw calls, other wrappers and external UI are outside this guard. Original exceptions and HRESULTs propagate; the lease is released on failure.</para>
        /// <para>The task-shaped API performs all native work before returning its settled task; it can block the calling thread. It does not queue COM on Task.Run, change apartments, marshal to a UI dispatcher or add cancellation/parallelism. Call it on the owning thread and await to observe failures.</para>
        /// <para>No analysis, case activation, selection, save, native mutation, unit conversion, retry or rollback is introduced.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">supportId or loadCaseId is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, native retrieval is unsuccessful, or a successful output/status is malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates, including its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native signature is unavailable.</exception>
        /// <example><code>
        /// var result = wrapper.Output.GetSupportReactionsAsync(2, 1).GetAwaiter().GetResult();
        /// Console.WriteLine(result.Id + ": " + result.Forces.Fy);
        /// </code></example>
        Task<LoadCaseForces> GetSupportReactionsAsync(int supportId, int loadCaseId);

        /// <summary>Reads qualified global support forces opposite to native reactions.</summary>
        /// <param name="supportId">Positive native support node number, not an ordinal in the support list. Existence/support suitability is determined by native retrieval.</param>
        /// <param name="loadCaseId">Positive native load-case reference number. This method does not activate, analyze or independently resolve the case.</param>
        /// <returns>An already completed successful task containing a new detached LoadCaseForces with Id equal to loadCaseId and independently owned six-component Forces; supportId is not retained in this model. Validation, guard and native failures are returned as faulted tasks and rethrown when awaited. Unsuccessful native retrieval throws; no partial or initialized zero record is returned.</returns>
        /// <remarks><para>Uses one native GetSupportReactions(supportId, loadCaseId, ref reactions) call under the current Output binding. Six-Double buffers start with NaN sentinels. Requires Boolean true or integral one success; false/zero throws and malformed status is rejected. Every successful output must be a rank-one array of exactly six finite written Doubles in Fx,Fy,Fz,Mx,My,Mz order. Replacement arrays and nonzero lower bounds are copied; native storage is never retained.</para>
        /// <para>Native reactions are in global directions. Components retain native force/moment units and sign conventions without unit conversion or geometry hydration. Multiplies all six qualified reaction components by minus one, including moments, preserving the source support-force convention; does not make a second native call. No existence, result freshness or engineering acceptance guarantee is inferred.</para>
        /// <para>Returned records are editable detached metadata. Mutations never update STAAD; manual model construction/setters are unvalidated. Each call owns new result/component storage, even when native arrays are reused or aliased.</para>
        /// <para>Runs synchronously on the wrapper constructing COM thread under one shared Output binding/lifetime/reentrancy lease covering validation, dispatch and qualification. Reacquire adapters after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw calls, other wrappers and external UI are outside this guard. Original exceptions and HRESULTs propagate; the lease is released on failure.</para>
        /// <para>The task-shaped API performs all native work before returning its settled task; it can block the calling thread. It does not queue COM on Task.Run, change apartments, marshal to a UI dispatcher or add cancellation/parallelism. Call it on the owning thread and await to observe failures.</para>
        /// <para>No analysis, case activation, selection, save, native mutation, unit conversion, retry or rollback is introduced.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">supportId or loadCaseId is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, native retrieval is unsuccessful, or a successful output/status is malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates, including its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native signature is unavailable.</exception>
        /// <example><code>
        /// var result = wrapper.Output.GetSupportForcesAsync(2, 1).GetAwaiter().GetResult();
        /// Console.WriteLine(result.Id + ": " + result.Forces.Fy);
        /// </code></example>
        Task<LoadCaseForces> GetSupportForcesAsync(int supportId, int loadCaseId);

        /// <summary>Reads qualified native static-check load and reaction totals for one case.</summary>
        /// <param name="loadCaseId">Positive native load-case reference number. This method does not activate, analyze or independently resolve the case.</param>
        /// <returns>An already completed successful task containing a new detached StaticCheckResult with the requested LoadCaseId and independent six-component loads/reactions. Validation, guard and native failures are returned as faulted tasks and rethrown when awaited. Unsuccessful native retrieval throws; no partial or initialized zero record is returned.</returns>
        /// <remarks><para>Uses one native GetStaticCheckResult(loadCaseId, ref loads, ref reactions) call under the current Output binding. Six-Double buffers start with NaN sentinels. Requires Boolean true or integral one success; false/zero throws and malformed status is rejected. Every successful output must be a rank-one array of exactly six finite written Doubles in Fx,Fy,Fz,Mx,My,Mz order. Replacement arrays and nonzero lower bounds are copied; native storage is never retained.</para>
        /// <para>Copies loads and reactions independently only after both arrays qualify. Preserves native units, axes, moment reference point and signs without a managed transform or balance assumption. Differentials computes component-wise Forces minus Reactions from current editable data; it is not an equilibrium tolerance or engineering acceptance check. No live freshness or cross-version qualification is implied.</para>
        /// <para>Returned records are editable detached metadata. Mutations never update STAAD; manual model construction/setters are unvalidated. Each call owns new result/component storage, even when native arrays are reused or aliased.</para>
        /// <para>Runs synchronously on the wrapper constructing COM thread under one shared Output binding/lifetime/reentrancy lease covering validation, dispatch and qualification. Reacquire adapters after refresh. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw calls, other wrappers and external UI are outside this guard. Original exceptions and HRESULTs propagate; the lease is released on failure.</para>
        /// <para>The task-shaped API performs all native work before returning its settled task; it can block the calling thread. It does not queue COM on Task.Run, change apartments, marshal to a UI dispatcher or add cancellation/parallelism. Call it on the owning thread and await to observe failures.</para>
        /// <para>No analysis, case activation, selection, save, native mutation, unit conversion, retry or rollback is introduced.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException">loadCaseId is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, native retrieval is unsuccessful, or a successful output/status is malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates, including its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native signature is unavailable.</exception>
        /// <example><code>
        /// var result = wrapper.Output.GetStaticCheckResultAsync(1).GetAwaiter().GetResult();
        /// Console.WriteLine(result.LoadCaseId + ": " + result.Forces.Fy);
        /// </code></example>
        Task<StaticCheckResult> GetStaticCheckResultAsync(int loadCaseId);
        /// <summary>Reads qualified static-check totals for every supplied load-case occurrence.</summary>
        /// <param name="loadCaseIds">Non-null sequence of positive native load-case IDs, enumerated once and fully validated
        /// before native access. At most 100,000 occurrences, including duplicates. Empty input makes no native calls.</param>
        /// <returns>A new, non-null HashSet of independent StaticCheckResult records; empty input returns a fresh empty set. Each input occurrence creates a distinct record, including repeated IDs; iteration order is unspecified. No partial set escapes on failure.</returns>
        /// <remarks><para>Materializes the complete ID sequence before one native GetStaticCheckResult call per occurrence,
        /// in captured input order. Duplicate IDs are deliberately not deduplicated: the HashSet uses result-object identity.
        /// This differs from the single-case task query and from unique-ID inventory/filter methods. Input enumeration and
        /// validation failures precede all native calls; later native failure stops remaining calls and discards the local result.</para>
        /// <para>Reuses the same strict single-case reader: Boolean true/integral one success, two separate NaN-marked
        /// six-Double by-reference arrays and fully written finite Fx,Fy,Fz,Mx,My,Mz outputs. Replacement arrays,
        /// nonzero lower bounds and aliased native buffers are copied into independent records. Failed/unwritten data
        /// never become apparent zero loads. Returned records retain native units, axes, moment reference and signs.</para>
        /// <para>StaticCheckResult is editable detached metadata. Differentials is literal component-wise loads minus
        /// reactions, not an equilibrium tolerance or engineering acceptance. Re-enumeration performs no COM reads.
        /// Earlier successful read effects cannot be rolled back, and a batch is not an atomic model snapshot.</para>
        /// <para>Runs synchronously on the wrapper's constructing COM thread under one shared current Output
        /// operation lease spanning input capture, validation, native calls and output qualification.
        /// Reacquire wrapper.Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/Load/Output/refresh/disposal work is rejected before native access.
        /// Raw objects, other wrappers and external UI are not coordinated. Observations are not atomic.
        /// No analysis, activation, selection, save, unit conversion, retry, fallback or rollback is added.
        /// Original native failures/HRESULTs propagate; the lease is released on failure. COM calls may block.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">loadCaseIds is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A case ID is not positive.</exception>
        /// <exception cref="ArgumentException">The input contains more than 100,000 case occurrences.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval failed, or native status/output is malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native member signature is unavailable.</exception>
        /// <example><code>
        /// var results = wrapper.Output.GetStaticCheckResults(new[] { 1, 2, 1 });
        /// Console.WriteLine(results.Count); // three separate records, including both observations of case 1
        /// </code></example>
        HashSet<StaticCheckResult> GetStaticCheckResults(IEnumerable<int> loadCaseIds);

        /// <summary>Reads a static-check batch with a validated source-compatible thread-count argument.</summary>
        /// <param name="loadCaseIds">Non-null sequence of positive native load-case IDs, enumerated once and fully validated
        /// before native access. At most 100,000 occurrences, including duplicates. Empty input makes no native calls.</param>
        /// <param name="nThreads">Compatibility thread-count hint in the source-supported range 1 through 512. Validated before input enumeration; native calls remain sequential on the owner COM thread for every value.</param>
        /// <returns>A new, non-null HashSet of independent StaticCheckResult records; empty input returns a fresh empty set. Each input occurrence creates a distinct record, including repeated IDs; iteration order is unspecified. No partial set escapes on failure.</returns>
        /// <remarks><para>Materializes the complete ID sequence before one native GetStaticCheckResult call per occurrence,
        /// in captured input order. Duplicate IDs are deliberately not deduplicated: the HashSet uses result-object identity.
        /// This differs from the single-case task query and from unique-ID inventory/filter methods. Input enumeration and
        /// validation failures precede all native calls; later native failure stops remaining calls and discards the local result.</para>
        /// <para>Reuses the same strict single-case reader: Boolean true/integral one success, two separate NaN-marked
        /// six-Double by-reference arrays and fully written finite Fx,Fy,Fz,Mx,My,Mz outputs. Replacement arrays,
        /// nonzero lower bounds and aliased native buffers are copied into independent records. Failed/unwritten data
        /// never become apparent zero loads. Returned records retain native units, axes, moment reference and signs.</para>
        /// <para>StaticCheckResult is editable detached metadata. Differentials is literal component-wise loads minus
        /// reactions, not an equilibrium tolerance or engineering acceptance. Re-enumeration performs no COM reads.
        /// Earlier successful read effects cannot be rolled back, and a batch is not an atomic model snapshot.</para>
        /// <para>The nThreads overload retains source call compatibility but never invokes parallel COM or PLINQ. Use the overload without nThreads for new code; increasing this hint does not increase native concurrency or change results.</para>
        /// <para>Runs synchronously on the wrapper's constructing COM thread under one shared current Output
        /// operation lease spanning input capture, validation, native calls and output qualification.
        /// Reacquire wrapper.Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/Load/Output/refresh/disposal work is rejected before native access.
        /// Raw objects, other wrappers and external UI are not coordinated. Observations are not atomic.
        /// No analysis, activation, selection, save, unit conversion, retry, fallback or rollback is added.
        /// Original native failures/HRESULTs propagate; the lease is released on failure. COM calls may block.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">loadCaseIds is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A case ID is not positive, or nThreads is outside 1 through 512.</exception>
        /// <exception cref="ArgumentException">The input contains more than 100,000 case occurrences.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval failed, or native status/output is malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native member signature is unavailable.</exception>
        /// <example><code>
        /// var results = wrapper.Output.GetStaticCheckResults(new[] { 1, 2 }, nThreads: 2);
        /// Console.WriteLine(results.Count); // native calls still run sequentially on this thread
        /// </code></example>
        HashSet<StaticCheckResult> GetStaticCheckResults(IEnumerable<int> loadCaseIds, int nThreads);

        /// <summary>Reads qualified static-check totals as an already settled task on the owning thread.</summary>
        /// <param name="loadCaseIds">Non-null sequence of positive native load-case IDs, enumerated once and fully validated
        /// before native access. At most 100,000 occurrences, including duplicates. Empty input makes no native calls.</param>
        /// <returns>An already completed task containing a new HashSet of independent StaticCheckResult records, or an already faulted task for validation, lifetime or native failure. Await to observe the original exception. Each input occurrence creates a distinct record, including repeated IDs; iteration order is unspecified. No partial set escapes on failure.</returns>
        /// <remarks><para>Materializes the complete ID sequence before one native GetStaticCheckResult call per occurrence,
        /// in captured input order. Duplicate IDs are deliberately not deduplicated: the HashSet uses result-object identity.
        /// This differs from the single-case task query and from unique-ID inventory/filter methods. Input enumeration and
        /// validation failures precede all native calls; later native failure stops remaining calls and discards the local result.</para>
        /// <para>Reuses the same strict single-case reader: Boolean true/integral one success, two separate NaN-marked
        /// six-Double by-reference arrays and fully written finite Fx,Fy,Fz,Mx,My,Mz outputs. Replacement arrays,
        /// nonzero lower bounds and aliased native buffers are copied into independent records. Failed/unwritten data
        /// never become apparent zero loads. Returned records retain native units, axes, moment reference and signs.</para>
        /// <para>StaticCheckResult is editable detached metadata. Differentials is literal component-wise loads minus
        /// reactions, not an equilibrium tolerance or engineering acceptance. Re-enumeration performs no COM reads.
        /// Earlier successful read effects cannot be rolled back, and a batch is not an atomic model snapshot.</para>
        /// <para>Performs enumeration, validation and all COM reads before returning its settled task; invocation can block. Uses no Task.Run, apartment switch, UI dispatch, cancellation or worker parallelism. Calling from another thread returns a faulted task before input enumeration or native access.</para>
        /// <para>Runs synchronously on the wrapper's constructing COM thread under one shared current Output
        /// operation lease spanning input capture, validation, native calls and output qualification.
        /// Reacquire wrapper.Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/Load/Output/refresh/disposal work is rejected before native access.
        /// Raw objects, other wrappers and external UI are not coordinated. Observations are not atomic.
        /// No analysis, activation, selection, save, unit conversion, retry, fallback or rollback is added.
        /// Original native failures/HRESULTs propagate; the lease is released on failure. COM calls may block.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">loadCaseIds is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A case ID is not positive.</exception>
        /// <exception cref="ArgumentException">The input contains more than 100,000 case occurrences.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval failed, or native status/output is malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native member signature is unavailable.</exception>
        /// <example><code>
        /// var results = wrapper.Output.GetStaticCheckResultsAsync(new[] { 1, 2 }).GetAwaiter().GetResult();
        /// foreach (var result in results) Console.WriteLine(result.LoadCaseId);
        /// </code></example>
        Task<HashSet<StaticCheckResult>> GetStaticCheckResultsAsync(IEnumerable<int> loadCaseIds);

        /// <summary>Reads five local plate-center shear and membrane stresses for one plate and case.</summary>
        /// <param name="loadCase">Non-null metadata with positive Id, captured once before dispatch. Title, Type and CaseType are not read; this method does not activate the case, qualify its family or query metadata.</param>
        /// <param name="plate">Non-null analytical plate metadata with positive Id, captured once. Geometry/corners are not required, read or hydrated. Native model membership and result readiness are caller responsibilities.</param>
        /// <returns>A new PlateCenterForces containing fully qualified Sqx, Sqy, Sx, Sy and Sxy values. Components are independent of native storage; supplied LoadCase and Plate references are retained rather than cloned.</returns>
        /// <remarks><para>Calls GetAllPlateCenterForces(plateId, loadCaseId, ref buffer) once using five Double NaN
        /// sentinels. Requires Boolean true or integral one success, then an exactly length-five rank-one Double array
        /// with every component written and finite. Replacement arrays/nonzero lower bounds are copied. False/zero,
        /// unknown flags, malformed types/shapes, partial/unwritten values and native errors throw; no zero fallback.</para>
        /// <para>The native order is SQX, SQY, SX, SY, SXY: local-X-face Z-direction shear, local-Y-face Z-direction shear,
        /// normal X membrane stress, normal Y membrane stress and in-plane XY shear. Despite the inherited Forces name,
        /// these are STRESSES in native force/length-squared units, not nodal/member forces, plate bending moments,
        /// principal stresses or corner results. Native local axes/signs remain unchanged; no rotation or area scaling.</para>
        /// <para>Uses captured positive IDs only. Result context references remain caller-owned mutable metadata;
        /// later Id edits can differ from the identity actually queried, without changing captured components.
        /// Retain requested IDs externally when immutable provenance is needed. Scalar setters/manual constructors
        /// are unvalidated, have no result notifications and do not write to STAAD. This ports scalar/context support,
        /// not the original conversion/WPF/full Plate geometry APIs. No solver or engineering acceptance is inferred.</para>
        /// <para>Runs synchronously on the wrapper's constructing COM thread under one shared current Output
        /// operation lease spanning input capture, validation, native calls and output qualification.
        /// Reacquire wrapper.Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/Load/Output/refresh/disposal work is rejected before native access.
        /// Raw objects, other wrappers and external UI are not coordinated. Observations are not atomic.
        /// No analysis, activation, selection, save, unit conversion, retry, fallback or rollback is added.
        /// Original native failures/HRESULTs propagate; the lease is released on failure. COM calls may block.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">loadCase or plate is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The supplied loadCase.Id or plate.Id is not positive; the exception names that object parameter.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval failed, or native status/output is malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native member signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 5 };
        /// var stresses = wrapper.Output.GetPlateCenterForces(loadCase, plate);
        /// Console.WriteLine(stresses.Sqx + ": " + stresses.Sxy); // local stresses, not resultant forces
        /// </code></example>
        PlateCenterForces GetPlateCenterForces(ILoadCase loadCase, Plate plate);

        /// <summary>Reads local plate-center stresses as an already settled owner-thread task.</summary>
        /// <param name="loadCase">Non-null metadata with positive Id, captured once before dispatch. Title, Type and CaseType are not read; this method does not activate the case, qualify its family or query metadata.</param>
        /// <param name="plate">Non-null analytical plate metadata with positive Id, captured once. Geometry/corners are not required, read or hydrated. Native model membership and result readiness are caller responsibilities.</param>
        /// <returns>An already completed task containing a new PlateCenterForces, or an already faulted task preserving validation, lifetime or native failure; await to observe exceptions. Components are independent of native storage; supplied LoadCase and Plate references are retained rather than cloned.</returns>
        /// <remarks><para>Calls GetAllPlateCenterForces(plateId, loadCaseId, ref buffer) once using five Double NaN
        /// sentinels. Requires Boolean true or integral one success, then an exactly length-five rank-one Double array
        /// with every component written and finite. Replacement arrays/nonzero lower bounds are copied. False/zero,
        /// unknown flags, malformed types/shapes, partial/unwritten values and native errors throw; no zero fallback.</para>
        /// <para>The native order is SQX, SQY, SX, SY, SXY: local-X-face Z-direction shear, local-Y-face Z-direction shear,
        /// normal X membrane stress, normal Y membrane stress and in-plane XY shear. Despite the inherited Forces name,
        /// these are STRESSES in native force/length-squared units, not nodal/member forces, plate bending moments,
        /// principal stresses or corner results. Native local axes/signs remain unchanged; no rotation or area scaling.</para>
        /// <para>Uses captured positive IDs only. Result context references remain caller-owned mutable metadata;
        /// later Id edits can differ from the identity actually queried, without changing captured components.
        /// Retain requested IDs externally when immutable provenance is needed. Scalar setters/manual constructors
        /// are unvalidated, have no result notifications and do not write to STAAD. This ports scalar/context support,
        /// not the original conversion/WPF/full Plate geometry APIs. No solver or engineering acceptance is inferred.</para>
        /// <para>Performs all validation/native reads before returning the settled task and can block. No Task.Run, apartment switch, UI dispatcher, cancellation or parallel COM is added; a wrong-thread call faults before native access.</para>
        /// <para>Runs synchronously on the wrapper's constructing COM thread under one shared current Output
        /// operation lease spanning input capture, validation, native calls and output qualification.
        /// Reacquire wrapper.Output after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/Load/Output/refresh/disposal work is rejected before native access.
        /// Raw objects, other wrappers and external UI are not coordinated. Observations are not atomic.
        /// No analysis, activation, selection, save, unit conversion, retry, fallback or rollback is added.
        /// Original native failures/HRESULTs propagate; the lease is released on failure. COM calls may block.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">loadCase or plate is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The supplied loadCase.Id or plate.Id is not positive; the exception names that object parameter.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval failed, or native status/output is malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native member signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 5 };
        /// var stresses = wrapper.Output.GetPlateCenterForcesAsync(loadCase, plate).GetAwaiter().GetResult();
        /// Console.WriteLine(stresses.Sqx + ": " + stresses.Sxy); // local stresses, not resultant forces
        /// </code></example>
        Task<PlateCenterForces> GetPlateCenterForcesAsync(ILoadCase loadCase, Plate plate);


        /// <summary>Reads three qualified local plate-center bending and twisting moments per unit width.</summary>
        /// <param name="loadCase">Non-null caller-owned case metadata with positive native reference ID. Only Id is read, once; Title/Type/CaseType are unused.</param>
        /// <param name="plate">Non-null caller-owned plate metadata with positive native number. Only Id is read, once; no corners/geometry are required.</param>
        /// <returns>A new detached PlateCenterMoments retaining the supplied context references with independently copied qualified components. Failed retrieval or malformed/unwritten data produces no result.</returns>
        /// <remarks><para>Calls GetAllPlateCenterMoments once with plate ID, case ID and a by-reference three-Double buffer. Requires an exact rank-one length-three Double array; replacements and nonzero lower bounds are copied. Output order is MX,MY,MXY: moments per unit width about local X face, local Y face and twisting in the local X-Y plane. Units are native moment/width, for example kN-m/m, not nodal/member moments or membrane stresses.</para>
        /// <para>Captures positive loadCase.Id and plate.Id once under the current Output lease. Other case metadata and plate geometry are unused. Retains the caller-owned LoadCase and Plate references without copying; later metadata edits may show different IDs without recomputing queried values. All result scalars are independently copied. Manual model construction/setters are unvalidated and do not update STAAD or raise notifications.</para>
        /// <para>Requires Boolean true or integral one success. False/zero throws; malformed status codes/types are rejected. Each output starts as an unwritten NaN Double sentinel. All successful components must be fully written finite Doubles; no integer/string/decimal coercion, default-zero fallback, sign normalization or partial result is allowed. Fully written zero, signed and finite extreme values are retained literally.</para>
        /// <para>Runs synchronously on the wrapper constructing COM thread under one shared current Output binding/lifetime/reentrancy lease spanning ID capture, validation, dispatch and qualification. Reacquire adapters after refresh. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal fail before native access. The lease is released on failure and original exceptions/HRESULTs propagate. Raw native calls, other wrappers and external UI remain uncoordinated; sequential observations and mutable context references are not atomic.</para>
        /// <para>No analysis, activation, geometry hydration, selection, save, unit conversion, coordinate transformation, Wood-Armer calculation, retry or native mutation is introduced. Native top/bottom/axis labels, sign conventions and current result units are preserved. Result freshness, cross-version qualification and engineering acceptance are not certified.</para></remarks>
        /// <exception cref="ArgumentNullException">loadCase or plate is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A supplied case or plate ID is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval is unsuccessful, or native status/components have unsupported types, shape, unwritten or nonfinite values.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
        /// var result = wrapper.Output.GetPlateCenterMoments(loadCase, plate);
        /// Console.WriteLine(result.Mx);
        /// </code></example>
        PlateCenterMoments GetPlateCenterMoments(ILoadCase loadCase, Plate plate);

        /// <summary>Reads three qualified local plate-center bending and twisting moments per unit width.</summary>
        /// <param name="loadCase">Non-null caller-owned case metadata with positive native reference ID. Only Id is read, once; Title/Type/CaseType are unused.</param>
        /// <param name="plate">Non-null caller-owned plate metadata with positive native number. Only Id is read, once; no corners/geometry are required.</param>
        /// <returns>An already settled successful task containing a new detached PlateCenterMoments retaining the supplied context references with independently copied qualified components. Failed retrieval or malformed/unwritten data produces no result. Validation, binding and native failures are returned as faulted tasks and rethrown when awaited.</returns>
        /// <remarks><para>Calls GetAllPlateCenterMoments once with plate ID, case ID and a by-reference three-Double buffer. Requires an exact rank-one length-three Double array; replacements and nonzero lower bounds are copied. Output order is MX,MY,MXY: moments per unit width about local X face, local Y face and twisting in the local X-Y plane. Units are native moment/width, for example kN-m/m, not nodal/member moments or membrane stresses.</para>
        /// <para>Captures positive loadCase.Id and plate.Id once under the current Output lease. Other case metadata and plate geometry are unused. Retains the caller-owned LoadCase and Plate references without copying; later metadata edits may show different IDs without recomputing queried values. All result scalars are independently copied. Manual model construction/setters are unvalidated and do not update STAAD or raise notifications.</para>
        /// <para>Requires Boolean true or integral one success. False/zero throws; malformed status codes/types are rejected. Each output starts as an unwritten NaN Double sentinel. All successful components must be fully written finite Doubles; no integer/string/decimal coercion, default-zero fallback, sign normalization or partial result is allowed. Fully written zero, signed and finite extreme values are retained literally.</para>
        /// <para>Runs synchronously on the wrapper constructing COM thread under one shared current Output binding/lifetime/reentrancy lease spanning ID capture, validation, dispatch and qualification. Reacquire adapters after refresh. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal fail before native access. The lease is released on failure and original exceptions/HRESULTs propagate. Raw native calls, other wrappers and external UI remain uncoordinated; sequential observations and mutable context references are not atomic.</para>
        /// <para>No analysis, activation, geometry hydration, selection, save, unit conversion, coordinate transformation, Wood-Armer calculation, retry or native mutation is introduced. Native top/bottom/axis labels, sign conventions and current result units are preserved. Result freshness, cross-version qualification and engineering acceptance are not certified.</para>
        /// <para>The task-shaped API performs native work before returning its completed/faulted task and can block the caller. It does not queue Task.Run, marshal to a UI dispatcher, switch apartments or add cancellation/parallel COM work. Invoke on the owning thread and await to observe faults.</para></remarks>
        /// <exception cref="ArgumentNullException">loadCase or plate is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A supplied case or plate ID is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval is unsuccessful, or native status/components have unsupported types, shape, unwritten or nonfinite values.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
        /// var result = wrapper.Output.GetPlateCenterMomentsAsync(loadCase, plate).GetAwaiter().GetResult();
        /// Console.WriteLine(result.Mx);
        /// </code></example>
        Task<PlateCenterMoments> GetPlateCenterMomentsAsync(ILoadCase loadCase, Plate plate);

        /// <summary>Reads qualified native top and bottom plate-center von Mises stresses.</summary>
        /// <param name="loadCase">Non-null caller-owned case metadata with positive native reference ID. Only Id is read, once; Title/Type/CaseType are unused.</param>
        /// <param name="plate">Non-null caller-owned plate metadata with positive native number. Only Id is read, once; no corners/geometry are required.</param>
        /// <returns>A new detached PlateCenterVonMises retaining the supplied context references with independently copied qualified components. Failed retrieval or malformed/unwritten data produces no result.</returns>
        /// <remarks><para>Calls GetPlateCenterVonMisesStresses once with plate ID, case ID and distinct by-reference Double outputs, ordered top then bottom. Components use native force/length-squared units and native surface labels, not global elevation labels. AbsoluteMaximum recomputes the larger current absolute component without changing data or comparing an allowable stress.</para>
        /// <para>Captures positive loadCase.Id and plate.Id once under the current Output lease. Other case metadata and plate geometry are unused. Retains the caller-owned LoadCase and Plate references without copying; later metadata edits may show different IDs without recomputing queried values. All result scalars are independently copied. Manual model construction/setters are unvalidated and do not update STAAD or raise notifications.</para>
        /// <para>Requires Boolean true or integral one success. False/zero throws; malformed status codes/types are rejected. Each output starts as an unwritten NaN Double sentinel. All successful components must be fully written finite Doubles; no integer/string/decimal coercion, default-zero fallback, sign normalization or partial result is allowed. Fully written zero, signed and finite extreme values are retained literally.</para>
        /// <para>Runs synchronously on the wrapper constructing COM thread under one shared current Output binding/lifetime/reentrancy lease spanning ID capture, validation, dispatch and qualification. Reacquire adapters after refresh. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal fail before native access. The lease is released on failure and original exceptions/HRESULTs propagate. Raw native calls, other wrappers and external UI remain uncoordinated; sequential observations and mutable context references are not atomic.</para>
        /// <para>No analysis, activation, geometry hydration, selection, save, unit conversion, coordinate transformation, Wood-Armer calculation, retry or native mutation is introduced. Native top/bottom/axis labels, sign conventions and current result units are preserved. Result freshness, cross-version qualification and engineering acceptance are not certified.</para></remarks>
        /// <exception cref="ArgumentNullException">loadCase or plate is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A supplied case or plate ID is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval is unsuccessful, or native status/components have unsupported types, shape, unwritten or nonfinite values.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
        /// var result = wrapper.Output.GetPlateCenterVonMises(loadCase, plate);
        /// Console.WriteLine(result.Top);
        /// </code></example>
        PlateCenterVonMises GetPlateCenterVonMises(ILoadCase loadCase, Plate plate);

        /// <summary>Reads qualified native top and bottom plate-center von Mises stresses.</summary>
        /// <param name="loadCase">Non-null caller-owned case metadata with positive native reference ID. Only Id is read, once; Title/Type/CaseType are unused.</param>
        /// <param name="plate">Non-null caller-owned plate metadata with positive native number. Only Id is read, once; no corners/geometry are required.</param>
        /// <returns>An already settled successful task containing a new detached PlateCenterVonMises retaining the supplied context references with independently copied qualified components. Failed retrieval or malformed/unwritten data produces no result. Validation, binding and native failures are returned as faulted tasks and rethrown when awaited.</returns>
        /// <remarks><para>Calls GetPlateCenterVonMisesStresses once with plate ID, case ID and distinct by-reference Double outputs, ordered top then bottom. Components use native force/length-squared units and native surface labels, not global elevation labels. AbsoluteMaximum recomputes the larger current absolute component without changing data or comparing an allowable stress.</para>
        /// <para>Captures positive loadCase.Id and plate.Id once under the current Output lease. Other case metadata and plate geometry are unused. Retains the caller-owned LoadCase and Plate references without copying; later metadata edits may show different IDs without recomputing queried values. All result scalars are independently copied. Manual model construction/setters are unvalidated and do not update STAAD or raise notifications.</para>
        /// <para>Requires Boolean true or integral one success. False/zero throws; malformed status codes/types are rejected. Each output starts as an unwritten NaN Double sentinel. All successful components must be fully written finite Doubles; no integer/string/decimal coercion, default-zero fallback, sign normalization or partial result is allowed. Fully written zero, signed and finite extreme values are retained literally.</para>
        /// <para>Runs synchronously on the wrapper constructing COM thread under one shared current Output binding/lifetime/reentrancy lease spanning ID capture, validation, dispatch and qualification. Reacquire adapters after refresh. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal fail before native access. The lease is released on failure and original exceptions/HRESULTs propagate. Raw native calls, other wrappers and external UI remain uncoordinated; sequential observations and mutable context references are not atomic.</para>
        /// <para>No analysis, activation, geometry hydration, selection, save, unit conversion, coordinate transformation, Wood-Armer calculation, retry or native mutation is introduced. Native top/bottom/axis labels, sign conventions and current result units are preserved. Result freshness, cross-version qualification and engineering acceptance are not certified.</para>
        /// <para>The task-shaped API performs native work before returning its completed/faulted task and can block the caller. It does not queue Task.Run, marshal to a UI dispatcher, switch apartments or add cancellation/parallel COM work. Invoke on the owning thread and await to observe faults.</para></remarks>
        /// <exception cref="ArgumentNullException">loadCase or plate is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A supplied case or plate ID is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval is unsuccessful, or native status/components have unsupported types, shape, unwritten or nonfinite values.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
        /// var result = wrapper.Output.GetPlateCenterVonMisesAsync(loadCase, plate).GetAwaiter().GetResult();
        /// Console.WriteLine(result.Top);
        /// </code></example>
        Task<PlateCenterVonMises> GetPlateCenterVonMisesAsync(ILoadCase loadCase, Plate plate);

        /// <summary>Reads four qualified native in-plane normal principal stresses at the plate center.</summary>
        /// <param name="loadCase">Non-null caller-owned case metadata with positive native reference ID. Only Id is read, once; Title/Type/CaseType are unused.</param>
        /// <param name="plate">Non-null caller-owned plate metadata with positive native number. Only Id is read, once; no corners/geometry are required.</param>
        /// <returns>A new detached PlatePrincipalStresses retaining the supplied context references with independently copied qualified components. Failed retrieval or malformed/unwritten data produces no result.</returns>
        /// <remarks><para>Calls GetPlateCenterNormalPrincipalStresses once with plate ID, case ID and four by-reference Double outputs, ordered maximum top, minimum top, maximum bottom, minimum bottom. Components use native force/length-squared units. Copies named outputs without sorting, recalculating principal stresses/directions/angles or imposing an acceptance criterion.</para>
        /// <para>Captures positive loadCase.Id and plate.Id once under the current Output lease. Other case metadata and plate geometry are unused. Retains the caller-owned LoadCase and Plate references without copying; later metadata edits may show different IDs without recomputing queried values. All result scalars are independently copied. Manual model construction/setters are unvalidated and do not update STAAD or raise notifications.</para>
        /// <para>Requires Boolean true or integral one success. False/zero throws; malformed status codes/types are rejected. Each output starts as an unwritten NaN Double sentinel. All successful components must be fully written finite Doubles; no integer/string/decimal coercion, default-zero fallback, sign normalization or partial result is allowed. Fully written zero, signed and finite extreme values are retained literally.</para>
        /// <para>Runs synchronously on the wrapper constructing COM thread under one shared current Output binding/lifetime/reentrancy lease spanning ID capture, validation, dispatch and qualification. Reacquire adapters after refresh. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal fail before native access. The lease is released on failure and original exceptions/HRESULTs propagate. Raw native calls, other wrappers and external UI remain uncoordinated; sequential observations and mutable context references are not atomic.</para>
        /// <para>No analysis, activation, geometry hydration, selection, save, unit conversion, coordinate transformation, Wood-Armer calculation, retry or native mutation is introduced. Native top/bottom/axis labels, sign conventions and current result units are preserved. Result freshness, cross-version qualification and engineering acceptance are not certified.</para></remarks>
        /// <exception cref="ArgumentNullException">loadCase or plate is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A supplied case or plate ID is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval is unsuccessful, or native status/components have unsupported types, shape, unwritten or nonfinite values.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
        /// var result = wrapper.Output.GetPlatePrincipalStresses(loadCase, plate);
        /// Console.WriteLine(result.SmaxTop);
        /// </code></example>
        PlatePrincipalStresses GetPlatePrincipalStresses(ILoadCase loadCase, Plate plate);

        /// <summary>Reads four native in-plane normal principal surface stresses as a settled task.</summary>
        /// <param name="loadCase">Non-null caller-owned case metadata with positive ID, captured once. Other metadata is unused.</param>
        /// <param name="plate">Non-null caller-owned plate metadata with positive ID, captured once. Geometry/corners are unused.</param>
        /// <returns>An already settled task containing a new detached principal stress record. Supplied context references are retained; qualified scalar values are copied. Validation, binding and native errors fault the task and are rethrown when awaited. Failed or malformed retrieval produces no result.</returns>
        /// <remarks>
        /// <para>Calls GetPlateCenterNormalPrincipalStresses once with captured plate/case IDs and four distinct NaN Double output sentinels, ordered maximum top, minimum top, maximum bottom, minimum bottom. Reuses the synchronous principal query qualification. Native surface labels, signs and force/length-squared units are preserved; values are not reordered and no principal angles are inferred.</para>
        /// <para>Requires Boolean true or integral-one native success. False/zero, unsupported status types/codes, incorrectly shaped arrays and unwritten/nonfinite components throw. All four scalar outputs must be Doubles. Distinct NaN sentinels detect missing writes; fully written zero/signed/extreme finite values retain their literal meaning.</para>
        /// <para>Supplied case/plate references are not cloned. Their later ID edits can differ from the IDs queried; current context fields are not proof of observation identity. Metadata changes during callbacks do not redirect captured native requests. Sequential component reads are not an atomic solver snapshot and do not guarantee freshness or engineering validity. Scalar edits have no native effects or notifications. No unit conversion, design/acceptance calculation, geometry hydration, Wood-Armer calculation or context refresh is provided.</para>
        /// <para>Runs under one shared current Output binding, constructing-thread, lifetime and reentrancy lease covering validation, enumeration, native calls and qualification. Refresh invalidates captured adapters; reacquire them. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw native calls, other wrappers and external UI are uncoordinated. Original exceptions/HRESULTs propagate; no analysis, activation, selection, save, fallback, retry or native mutation occurs.</para>
        /// <para>Performs all work synchronously before returning a successful or faulted task. Invocation can block; awaiting an already settled task does not dispatch COM. Unlike concurrent source task execution, no Task.Run, worker COM, Task.WhenAll, apartment switch, cancellation or UI dispatcher is used. Prefer the synchronous counterpart when a task-shaped API is unnecessary.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">An input reference/sequence is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A selected ID is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval fails or native status/components are malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
        /// var result = wrapper.Output.GetPlatePrincipalStressesAsync(loadCase, plate).GetAwaiter().GetResult();
        /// Console.WriteLine(result.SmaxTop);
        /// </code></example>
        Task<PlatePrincipalStresses> GetPlatePrincipalStressesAsync(ILoadCase loadCase, Plate plate);

        /// <summary>Reads complete plate-center components for one supplied case and plate.</summary>
        /// <param name="loadCase">Non-null caller-owned case metadata with positive ID, captured once. Other metadata is unused.</param>
        /// <param name="plate">Non-null caller-owned plate metadata with positive ID, captured once. Geometry/corners are unused.</param>
        /// <returns>a new composite retaining four independent detached component records. Supplied context references are retained; qualified scalar values are copied. Failed or malformed retrieval produces no result.</returns>
        /// <remarks>
        /// <para>For each pair, calls GetAllPlateCenterForces, GetAllPlateCenterMoments, GetPlateCenterVonMisesStresses and GetPlateCenterNormalPrincipalStresses sequentially in that order, using the same captured IDs for all four calls. Unlike the individual queries, this retrieves all four component families. Forces contains SQX/SQY/SX/SY/SXY local stresses, not nodal forces. Moments contains MX/MY/MXY moment per unit width. VonMises contains native top/bottom stresses; Principal contains maximum/minimum top/bottom normal stresses. Stress units are native force/length-squared; moments retain native moment/length units.</para>
        /// <para>Requires Boolean true or integral-one native success. False/zero, unsupported status types/codes, incorrectly shaped arrays and unwritten/nonfinite components throw. Array outputs must contain exactly five stress or three moment Doubles; scalar outputs must be Doubles. NaN sentinels detect missing writes. In-place/replacement/nonzero-bound arrays are qualified and copied; fully written zero/signed/extreme finite values retain their literal meaning.</para>
        /// <para>Supplied case/plate references are not cloned. Their later ID edits can differ from the IDs queried; current context fields are not proof of observation identity. Metadata changes during callbacks do not redirect captured native requests. Sequential component reads are not an atomic solver snapshot and do not guarantee freshness or engineering validity. Scalar edits have no native effects or notifications. No unit conversion, design/acceptance calculation, geometry hydration, Wood-Armer calculation or context refresh is provided.</para>
        /// <para>Runs under one shared current Output binding, constructing-thread, lifetime and reentrancy lease covering validation, enumeration, native calls and qualification. Refresh invalidates captured adapters; reacquire them. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw native calls, other wrappers and external UI are uncoordinated. Original exceptions/HRESULTs propagate; no analysis, activation, selection, save, fallback, retry or native mutation occurs.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">An input reference/sequence is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A selected ID is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval fails or native status/components are malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
        /// var result = wrapper.Output.GetPlateCenterResults(loadCase, plate);
        /// Console.WriteLine(result.Moments.Mx);
        /// </code></example>
        PlateCenterResults GetPlateCenterResults(ILoadCase loadCase, Plate plate);

        /// <summary>Reads complete plate-center components for one supplied case and plate.</summary>
        /// <param name="loadCase">Non-null caller-owned case metadata with positive ID, captured once. Other metadata is unused.</param>
        /// <param name="plate">Non-null caller-owned plate metadata with positive ID, captured once. Geometry/corners are unused.</param>
        /// <returns>An already settled task containing a new composite retaining four independent detached component records. Supplied context references are retained; qualified scalar values are copied. Validation, binding and native errors fault the task and are rethrown when awaited. Failed or malformed retrieval produces no result.</returns>
        /// <remarks>
        /// <para>For each pair, calls GetAllPlateCenterForces, GetAllPlateCenterMoments, GetPlateCenterVonMisesStresses and GetPlateCenterNormalPrincipalStresses sequentially in that order, using the same captured IDs for all four calls. Unlike the individual queries, this retrieves all four component families. Forces contains SQX/SQY/SX/SY/SXY local stresses, not nodal forces. Moments contains MX/MY/MXY moment per unit width. VonMises contains native top/bottom stresses; Principal contains maximum/minimum top/bottom normal stresses. Stress units are native force/length-squared; moments retain native moment/length units.</para>
        /// <para>Requires Boolean true or integral-one native success. False/zero, unsupported status types/codes, incorrectly shaped arrays and unwritten/nonfinite components throw. Array outputs must contain exactly five stress or three moment Doubles; scalar outputs must be Doubles. NaN sentinels detect missing writes. In-place/replacement/nonzero-bound arrays are qualified and copied; fully written zero/signed/extreme finite values retain their literal meaning.</para>
        /// <para>Supplied case/plate references are not cloned. Their later ID edits can differ from the IDs queried; current context fields are not proof of observation identity. Metadata changes during callbacks do not redirect captured native requests. Sequential component reads are not an atomic solver snapshot and do not guarantee freshness or engineering validity. Scalar edits have no native effects or notifications. No unit conversion, design/acceptance calculation, geometry hydration, Wood-Armer calculation or context refresh is provided.</para>
        /// <para>Runs under one shared current Output binding, constructing-thread, lifetime and reentrancy lease covering validation, enumeration, native calls and qualification. Refresh invalidates captured adapters; reacquire them. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw native calls, other wrappers and external UI are uncoordinated. Original exceptions/HRESULTs propagate; no analysis, activation, selection, save, fallback, retry or native mutation occurs.</para>
        /// <para>Performs all work synchronously before returning a successful or faulted task. Invocation can block; awaiting an already settled task does not dispatch COM. Unlike concurrent source task execution, no Task.Run, worker COM, Task.WhenAll, apartment switch, cancellation or UI dispatcher is used. Prefer the synchronous counterpart when a task-shaped API is unnecessary.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">An input reference/sequence is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A selected ID is not positive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval fails or native status/components are malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
        /// var result = wrapper.Output.GetPlateCenterResultsAsync(loadCase, plate).GetAwaiter().GetResult();
        /// Console.WriteLine(result.Moments.Mx);
        /// </code></example>
        Task<PlateCenterResults> GetPlateCenterResultsAsync(ILoadCase loadCase, Plate plate);

        /// <summary>Reads complete plate-center components for every supplied case/plate occurrence.</summary>
        /// <param name="loadCases">Non-null finite sequence of non-null case references with positive IDs; materialized once before plates. Order and duplicate occurrences are retained; at most 100,000 entries.</param>
        /// <param name="plates">Non-null finite sequence of non-null plate references with positive IDs; materialized once after cases. Order and duplicate occurrences are retained; at most 100,000 entries and 100,000 Cartesian pairs.</param>
        /// <returns>a new eager read-only collection of independent composite records in case-major, plate-minor order. Supplied context references are retained; qualified scalar values are copied. An empty valid dimension yields an empty collection after both inputs are validated. Failed or malformed retrieval produces no partial collection.</returns>
        /// <remarks>
        /// <para>For each pair, calls GetAllPlateCenterForces, GetAllPlateCenterMoments, GetPlateCenterVonMisesStresses and GetPlateCenterNormalPrincipalStresses sequentially in that order, using the same captured IDs for all four calls. Unlike the individual queries, this retrieves all four component families. Forces contains SQX/SQY/SX/SY/SXY local stresses, not nodal forces. Moments contains MX/MY/MXY moment per unit width. VonMises contains native top/bottom stresses; Principal contains maximum/minimum top/bottom normal stresses. Stress units are native force/length-squared; moments retain native moment/length units.</para>
        /// <para>Captures every occurrence, reference and positive ID before any native call, even when the other dimension is empty. Neither input is re-enumerated and duplicates are not collapsed. At most 100,000 occurrences per input and 100,000 Cartesian pairs; oversized inputs/products fail before dispatch. Four calls per pair run sequentially on the owning thread. The first validation, enumeration or native failure stops processing and publishes no partial collection; no rollback/replay occurs. Re-enumerating the returned collection never queries STAAD. Collection storage is read-only; contained records/context references remain mutable.</para>
        /// <para>Requires Boolean true or integral-one native success. False/zero, unsupported status types/codes, incorrectly shaped arrays and unwritten/nonfinite components throw. Array outputs must contain exactly five stress or three moment Doubles; scalar outputs must be Doubles. NaN sentinels detect missing writes. In-place/replacement/nonzero-bound arrays are qualified and copied; fully written zero/signed/extreme finite values retain their literal meaning.</para>
        /// <para>Supplied case/plate references are not cloned. Their later ID edits can differ from the IDs queried; current context fields are not proof of observation identity. Metadata changes during callbacks do not redirect captured native requests. Sequential component reads are not an atomic solver snapshot and do not guarantee freshness or engineering validity. Scalar edits have no native effects or notifications. No unit conversion, design/acceptance calculation, geometry hydration, Wood-Armer calculation or context refresh is provided.</para>
        /// <para>Runs under one shared current Output binding, constructing-thread, lifetime and reentrancy lease covering validation, enumeration, native calls and qualification. Refresh invalidates captured adapters; reacquire them. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw native calls, other wrappers and external UI are uncoordinated. Original exceptions/HRESULTs propagate; no analysis, activation, selection, save, fallback, retry or native mutation occurs.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">An input reference/sequence is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A selected ID is not positive.</exception>
        /// <exception cref="ArgumentException">An input entry is null or an occurrence/pair limit is exceeded.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval fails or native status/components are malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
        /// var result = wrapper.Output.GetPlateCenterResults(new[] { loadCase }, new[] { plate });
        /// Console.WriteLine(System.Linq.Enumerable.First(result).Moments.Mx);
        /// </code></example>
        IEnumerable<PlateCenterResults> GetPlateCenterResults(IEnumerable<ILoadCase> loadCases, IEnumerable<Plate> plates);

        /// <summary>Reads complete plate-center components for every supplied case/plate occurrence.</summary>
        /// <param name="loadCases">Non-null finite sequence of non-null case references with positive IDs; materialized once before plates. Order and duplicate occurrences are retained; at most 100,000 entries.</param>
        /// <param name="plates">Non-null finite sequence of non-null plate references with positive IDs; materialized once after cases. Order and duplicate occurrences are retained; at most 100,000 entries and 100,000 Cartesian pairs.</param>
        /// <returns>An already settled task containing a new eager read-only collection of independent composite records in case-major, plate-minor order. Supplied context references are retained; qualified scalar values are copied. An empty valid dimension yields an empty collection after both inputs are validated. Validation, binding and native errors fault the task and are rethrown when awaited. Failed or malformed retrieval produces no partial collection.</returns>
        /// <remarks>
        /// <para>For each pair, calls GetAllPlateCenterForces, GetAllPlateCenterMoments, GetPlateCenterVonMisesStresses and GetPlateCenterNormalPrincipalStresses sequentially in that order, using the same captured IDs for all four calls. Unlike the individual queries, this retrieves all four component families. Forces contains SQX/SQY/SX/SY/SXY local stresses, not nodal forces. Moments contains MX/MY/MXY moment per unit width. VonMises contains native top/bottom stresses; Principal contains maximum/minimum top/bottom normal stresses. Stress units are native force/length-squared; moments retain native moment/length units.</para>
        /// <para>Captures every occurrence, reference and positive ID before any native call, even when the other dimension is empty. Neither input is re-enumerated and duplicates are not collapsed. At most 100,000 occurrences per input and 100,000 Cartesian pairs; oversized inputs/products fail before dispatch. Four calls per pair run sequentially on the owning thread. The first validation, enumeration or native failure stops processing and publishes no partial collection; no rollback/replay occurs. Re-enumerating the returned collection never queries STAAD. Collection storage is read-only; contained records/context references remain mutable.</para>
        /// <para>Requires Boolean true or integral-one native success. False/zero, unsupported status types/codes, incorrectly shaped arrays and unwritten/nonfinite components throw. Array outputs must contain exactly five stress or three moment Doubles; scalar outputs must be Doubles. NaN sentinels detect missing writes. In-place/replacement/nonzero-bound arrays are qualified and copied; fully written zero/signed/extreme finite values retain their literal meaning.</para>
        /// <para>Supplied case/plate references are not cloned. Their later ID edits can differ from the IDs queried; current context fields are not proof of observation identity. Metadata changes during callbacks do not redirect captured native requests. Sequential component reads are not an atomic solver snapshot and do not guarantee freshness or engineering validity. Scalar edits have no native effects or notifications. No unit conversion, design/acceptance calculation, geometry hydration, Wood-Armer calculation or context refresh is provided.</para>
        /// <para>Runs under one shared current Output binding, constructing-thread, lifetime and reentrancy lease covering validation, enumeration, native calls and qualification. Refresh invalidates captured adapters; reacquire them. Stale/manual-noncurrent, disposed, other-thread and reentrant Root/Load/Output/refresh/disposal calls fail before native access. Raw native calls, other wrappers and external UI are uncoordinated. Original exceptions/HRESULTs propagate; no analysis, activation, selection, save, fallback, retry or native mutation occurs.</para>
        /// <para>Performs all work synchronously before returning a successful or faulted task. Invocation can block; awaiting an already settled task does not dispatch COM. Unlike concurrent source task execution, no Task.Run, worker COM, Task.WhenAll, apartment switch, cancellation or UI dispatcher is used. Prefer the synchronous counterpart when a task-shaped API is unnecessary.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">An input reference/sequence is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A selected ID is not positive.</exception>
        /// <exception cref="ArgumentException">An input entry is null or an occurrence/pair limit is exceeded.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid, retrieval fails or native status/components are malformed, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var plate = new StaadPro.Interop.Core.Entities.Plate { Id = 17 };
        /// var result = wrapper.Output.GetPlateCenterResultsAsync(new[] { loadCase }, new[] { plate }).GetAwaiter().GetResult();
        /// Console.WriteLine(System.Linq.Enumerable.First(result).Moments.Mx);
        /// </code></example>
        Task<IEnumerable<PlateCenterResults>> GetPlateCenterResultsAsync(IEnumerable<ILoadCase> loadCases, IEnumerable<Plate> plates);

        /// <summary>Reads complete plate-center result selections using the source-compatible thread-count hint.</summary>
        /// <param name="loadCases">Non-null sequence of non-null positive-ID case metadata, captured once before plates; at most 100,000 occurrences.</param>
        /// <param name="plates">Non-null sequence of non-null positive-ID plates, captured once; at most 100,000 occurrences and 100,000 Cartesian pairs.</param>
        /// <param name="nThreads">Compatibility hint in the source PLINQ range 1 through 512. Validated before input enumeration. Native work remains sequential on the owning COM thread; prefer the overload without this hint.</param>
        /// <returns>An eager read-only collection of independent plate-center composite records in case-major/plate-minor order. Failed validation/retrieval publishes no partial result collection.</returns>
        /// <remarks>
        /// <para>Reuses the non-thread-count composite query, including full once-only input validation before dispatch, 100,000 occurrence/pair bounds, preserved duplicates, four sequential native reads per pair, captured identities and read-only collection storage. The nThreads value does not create workers or concurrent COM, change values/order or relax native qualification. An invalid hint fails even for empty input. Both valid empty dimensions still require the other input to be valid.</para>
        /// <para>Runs under one current Output binding/lifetime/constructing-thread/reentrancy lease spanning validation, enumeration, optional hydration, every native call and output qualification. Refresh invalidates captured adapters; reacquire them. Stale/manual-noncurrent/disposed/other-thread and reentrant Root/Load/Output/refresh/disposal fail before native work. Raw access, other wrappers and external UI remain uncoordinated. Original exceptions/HRESULTs propagate; no analysis, activation, selection, save, retry, fallback or native mutation occurs. Successful reads do not certify result freshness, every native version or engineering validity.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">An input reference or sequence is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An analytical ID or thread-count hint is outside its permitted range.</exception>
        /// <exception cref="ArgumentException">A selection is invalid/oversized, supplied incidence is incomplete/invalid, or the requested node is not a plate corner.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid or native status, incidence, coordinates or components are malformed, unsuccessful, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var a = new StaadPro.Interop.Core.Entities.Node(11, 0, 0, 0);
        /// var plate = new StaadPro.Interop.Core.Entities.Plate(a,
        ///     new StaadPro.Interop.Core.Entities.Node(12, 1, 0, 0),
        ///     new StaadPro.Interop.Core.Entities.Node(13, 0, 0, 1)) { Id = 17 };
        /// var result = wrapper.Output.GetPlateCenterResults(new[] { loadCase }, new[] { plate }, 1);
        /// Console.WriteLine(System.Linq.Enumerable.First(result).Moments.Mx);
        /// </code></example>
        IEnumerable<PlateCenterResults> GetPlateCenterResults(IEnumerable<ILoadCase> loadCases, IEnumerable<Plate> plates, int nThreads);

        /// <summary>Reads six native force and moment components at one resolved plate incidence node.</summary>
        /// <param name="loadCase">Non-null analyzed case/combination metadata. Its positive Id is captured once; other metadata is unused.</param>
        /// <param name="plate">Non-null positive-ID plate. When A/B/C is incomplete, a new plate is hydrated from the acquired raw Geometry binding without changing the supplied object.</param>
        /// <param name="cornerNode">Non-null positive incidence-node ID, not ordinal 1/2/3/4. Resolved to the first A/B/C/D node with that ID; the returned context may be a different reference.</param>
        /// <returns>A new editable PlateCornerForces retaining the case and resolved plate/corner references with six independently copied values. Failed validation/retrieval publishes no partial result.</returns>
        /// <remarks>
        /// <para>Calls native GetPlateCornerForces with captured plate number, incidence-node number, case reference and a by-reference six-Double NaN-marked buffer. Requires Boolean true or integral one success; false/zero and malformed status types/codes throw. Every Fx,Fy,Fz,Mx,My,Mz component must be a written finite Double in an exact rank-one six-element Double array. In-place/replacement/nonzero-bound arrays are copied; zeros, signs and finite extremes are retained without fallback, coercion or partial values. These are native nodal force/moment components, not plate-center stresses or moment per width. Native axes and base units are preserved; no coordinate selector, extra rotation, averaging, equilibrium or acceptance calculation is introduced.</para>
        /// <para>Captures positive requested IDs before any native call. If A/B/C is missing, reads GetPlateIncidence once and requires integral zero success (negative/nonzero/error/Boolean status is rejected). A/B/C must be written positive Int32-compatible IDs; D may be zero or a positive repeated ID. Hydrates each distinct positive node once through documented-void GetNodeCoordinates with NaN-marked XYZ outputs, requiring written finite Doubles. A new detached plate/nodes are returned without modifying supplied metadata. With supplied incidence, no geometry read or coordinate validation is required. All non-null supplied corner IDs must be positive. Resolves the requested ID to the first A/B/C/D incidence reference; non-members fail before force retrieval. This differs from ID-only hydration overloads, which are not included in this increment.</para>
        /// <para>Retains editable metadata references and copied editable scalar values. Later ID changes can differ from queried IDs and do not rebuild result-set indexes; the result Key property reflects current metadata, whereas set lookups retain captured observation identity. Metadata captures and sequential observations are not atomic. Caller coordinates writers and result provenance. No full plate geometry, unit-service conversion, stress transformation, resultant integration or design helpers are supplied by these supporting models.</para>
        /// <para>Runs under one current Output binding/lifetime/constructing-thread/reentrancy lease spanning validation, enumeration, optional hydration, every native call and output qualification. Refresh invalidates captured adapters; reacquire them. Stale/manual-noncurrent/disposed/other-thread and reentrant Root/Load/Output/refresh/disposal fail before native work. Raw access, other wrappers and external UI remain uncoordinated. Original exceptions/HRESULTs propagate; no analysis, activation, selection, save, retry, fallback or native mutation occurs. Successful reads do not certify result freshness, every native version or engineering validity.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">An input reference or sequence is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An analytical ID or thread-count hint is outside its permitted range.</exception>
        /// <exception cref="ArgumentException">A selection is invalid/oversized, supplied incidence is incomplete/invalid, or the requested node is not a plate corner.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid or native status, incidence, coordinates or components are malformed, unsuccessful, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var a = new StaadPro.Interop.Core.Entities.Node(11, 0, 0, 0);
        /// var plate = new StaadPro.Interop.Core.Entities.Plate(a,
        ///     new StaadPro.Interop.Core.Entities.Node(12, 1, 0, 0),
        ///     new StaadPro.Interop.Core.Entities.Node(13, 0, 0, 1)) { Id = 17 };
        /// var result = wrapper.Output.GetPlateCornerForces(loadCase, plate, a);
        /// Console.WriteLine(result.Fx);
        /// </code></example>
        PlateCornerForces GetPlateCornerForces(ILoadCase loadCase, Plate plate, Node cornerNode);

        /// <summary>Reads six native force and moment components at one resolved plate incidence node.</summary>
        /// <param name="loadCase">Non-null analyzed case/combination metadata. Its positive Id is captured once; other metadata is unused.</param>
        /// <param name="plate">Non-null positive-ID plate. When A/B/C is incomplete, a new plate is hydrated from the acquired raw Geometry binding without changing the supplied object.</param>
        /// <param name="cornerNode">Non-null positive incidence-node ID, not ordinal 1/2/3/4. Resolved to the first A/B/C/D node with that ID; the returned context may be a different reference.</param>
        /// <returns>An already settled task containing A new editable PlateCornerForces retaining the case and resolved plate/corner references with six independently copied values. Validation, binding and native failures fault the task and are rethrown when awaited. Failed validation/retrieval publishes no partial result.</returns>
        /// <remarks>
        /// <para>Calls native GetPlateCornerForces with captured plate number, incidence-node number, case reference and a by-reference six-Double NaN-marked buffer. Requires Boolean true or integral one success; false/zero and malformed status types/codes throw. Every Fx,Fy,Fz,Mx,My,Mz component must be a written finite Double in an exact rank-one six-element Double array. In-place/replacement/nonzero-bound arrays are copied; zeros, signs and finite extremes are retained without fallback, coercion or partial values. These are native nodal force/moment components, not plate-center stresses or moment per width. Native axes and base units are preserved; no coordinate selector, extra rotation, averaging, equilibrium or acceptance calculation is introduced.</para>
        /// <para>Captures positive requested IDs before any native call. If A/B/C is missing, reads GetPlateIncidence once and requires integral zero success (negative/nonzero/error/Boolean status is rejected). A/B/C must be written positive Int32-compatible IDs; D may be zero or a positive repeated ID. Hydrates each distinct positive node once through documented-void GetNodeCoordinates with NaN-marked XYZ outputs, requiring written finite Doubles. A new detached plate/nodes are returned without modifying supplied metadata. With supplied incidence, no geometry read or coordinate validation is required. All non-null supplied corner IDs must be positive. Resolves the requested ID to the first A/B/C/D incidence reference; non-members fail before force retrieval. This differs from ID-only hydration overloads, which are not included in this increment.</para>
        /// <para>Retains editable metadata references and copied editable scalar values. Later ID changes can differ from queried IDs and do not rebuild result-set indexes; the result Key property reflects current metadata, whereas set lookups retain captured observation identity. Metadata captures and sequential observations are not atomic. Caller coordinates writers and result provenance. No full plate geometry, unit-service conversion, stress transformation, resultant integration or design helpers are supplied by these supporting models.</para>
        /// <para>Runs under one current Output binding/lifetime/constructing-thread/reentrancy lease spanning validation, enumeration, optional hydration, every native call and output qualification. Refresh invalidates captured adapters; reacquire them. Stale/manual-noncurrent/disposed/other-thread and reentrant Root/Load/Output/refresh/disposal fail before native work. Raw access, other wrappers and external UI remain uncoordinated. Original exceptions/HRESULTs propagate; no analysis, activation, selection, save, retry, fallback or native mutation occurs. Successful reads do not certify result freshness, every native version or engineering validity.</para>
        /// <para>All work executes synchronously before returning a successful/faulted task; invocation can block. Awaiting does not dispatch COM. Source Task.Run behavior is replaced with settled owning-thread tasks; no worker concurrency, apartment switch, cancellation or UI dispatcher is used. Prefer the synchronous counterpart when a task shape is unnecessary.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">An input reference or sequence is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An analytical ID or thread-count hint is outside its permitted range.</exception>
        /// <exception cref="ArgumentException">A selection is invalid/oversized, supplied incidence is incomplete/invalid, or the requested node is not a plate corner.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid or native status, incidence, coordinates or components are malformed, unsuccessful, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var a = new StaadPro.Interop.Core.Entities.Node(11, 0, 0, 0);
        /// var plate = new StaadPro.Interop.Core.Entities.Plate(a,
        ///     new StaadPro.Interop.Core.Entities.Node(12, 1, 0, 0),
        ///     new StaadPro.Interop.Core.Entities.Node(13, 0, 0, 1)) { Id = 17 };
        /// var result = wrapper.Output.GetPlateCornerForcesAsync(loadCase, plate, a).GetAwaiter().GetResult();
        /// Console.WriteLine(result.Fx);
        /// </code></example>
        Task<PlateCornerForces> GetPlateCornerForcesAsync(ILoadCase loadCase, Plate plate, Node cornerNode);

        /// <summary>Reads one qualified six-component observation for each distinct supplied plate corner.</summary>
        /// <param name="loadCase">Non-null analyzed case/combination metadata. Its positive Id is captured once; other metadata is unused.</param>
        /// <param name="plate">Non-null positive-ID plate. Requires supplied A/B/C references; D is optional. Does not hydrate missing incidence.</param>
        /// <returns>A new read-only indexed result set in first A/B/C/D encounter order, deduplicated by captured positive node ID. Failed validation/retrieval publishes no partial result collection.</returns>
        /// <remarks>
        /// <para>Calls native GetPlateCornerForces with captured plate number, incidence-node number, case reference and a by-reference six-Double NaN-marked buffer. Requires Boolean true or integral one success; false/zero and malformed status types/codes throw. Every Fx,Fy,Fz,Mx,My,Mz component must be a written finite Double in an exact rank-one six-element Double array. In-place/replacement/nonzero-bound arrays are copied; zeros, signs and finite extremes are retained without fallback, coercion or partial values. These are native nodal force/moment components, not plate-center stresses or moment per width. Native axes and base units are preserved; no coordinate selector, extra rotation, averaging, equilibrium or acceptance calculation is introduced.</para>
        /// <para>Captures load/plate IDs, corner references and positive corner IDs before force retrieval. A/B/C must be supplied; D may be null, or repeat another ID for a triangle. Repeated IDs retain the first supplied node reference; coordinates/equality are unused, and degeneracy/planarity is not certified. Reads each distinct corner sequentially, stopping on failure with no partial set. Unlike the single-corner query, this all-corners query requires supplied incidence and does not hydrate it. Indexed membership and identifier lists use immutable keys captured for the native requests, not later editable metadata.</para>
        /// <para>Retains editable metadata references and copied editable scalar values. Later ID changes can differ from queried IDs and do not rebuild result-set indexes; the result Key property reflects current metadata, whereas set lookups retain captured observation identity. Metadata captures and sequential observations are not atomic. Caller coordinates writers and result provenance. No full plate geometry, unit-service conversion, stress transformation, resultant integration or design helpers are supplied by these supporting models.</para>
        /// <para>Runs under one current Output binding/lifetime/constructing-thread/reentrancy lease spanning validation, enumeration, optional hydration, every native call and output qualification. Refresh invalidates captured adapters; reacquire them. Stale/manual-noncurrent/disposed/other-thread and reentrant Root/Load/Output/refresh/disposal fail before native work. Raw access, other wrappers and external UI remain uncoordinated. Original exceptions/HRESULTs propagate; no analysis, activation, selection, save, retry, fallback or native mutation occurs. Successful reads do not certify result freshness, every native version or engineering validity.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">An input reference or sequence is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An analytical ID or thread-count hint is outside its permitted range.</exception>
        /// <exception cref="ArgumentException">A selection is invalid/oversized, supplied incidence is incomplete/invalid, or the requested node is not a plate corner.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid or native status, incidence, coordinates or components are malformed, unsuccessful, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var a = new StaadPro.Interop.Core.Entities.Node(11, 0, 0, 0);
        /// var plate = new StaadPro.Interop.Core.Entities.Plate(a,
        ///     new StaadPro.Interop.Core.Entities.Node(12, 1, 0, 0),
        ///     new StaadPro.Interop.Core.Entities.Node(13, 0, 0, 1)) { Id = 17 };
        /// var result = wrapper.Output.GetPlateCornerForces(loadCase, plate);
        /// Console.WriteLine(result.Count);
        /// </code></example>
        PlateCornerForcesResultSet GetPlateCornerForces(ILoadCase loadCase, Plate plate);

        /// <summary>Reads one qualified six-component observation for each distinct supplied plate corner.</summary>
        /// <param name="loadCase">Non-null analyzed case/combination metadata. Its positive Id is captured once; other metadata is unused.</param>
        /// <param name="plate">Non-null positive-ID plate. Requires supplied A/B/C references; D is optional. Does not hydrate missing incidence.</param>
        /// <returns>An already settled task containing A new read-only indexed result set in first A/B/C/D encounter order, deduplicated by captured positive node ID. Validation, binding and native failures fault the task and are rethrown when awaited. Failed validation/retrieval publishes no partial result collection.</returns>
        /// <remarks>
        /// <para>Calls native GetPlateCornerForces with captured plate number, incidence-node number, case reference and a by-reference six-Double NaN-marked buffer. Requires Boolean true or integral one success; false/zero and malformed status types/codes throw. Every Fx,Fy,Fz,Mx,My,Mz component must be a written finite Double in an exact rank-one six-element Double array. In-place/replacement/nonzero-bound arrays are copied; zeros, signs and finite extremes are retained without fallback, coercion or partial values. These are native nodal force/moment components, not plate-center stresses or moment per width. Native axes and base units are preserved; no coordinate selector, extra rotation, averaging, equilibrium or acceptance calculation is introduced.</para>
        /// <para>Captures load/plate IDs, corner references and positive corner IDs before force retrieval. A/B/C must be supplied; D may be null, or repeat another ID for a triangle. Repeated IDs retain the first supplied node reference; coordinates/equality are unused, and degeneracy/planarity is not certified. Reads each distinct corner sequentially, stopping on failure with no partial set. Unlike the single-corner query, this all-corners query requires supplied incidence and does not hydrate it. Indexed membership and identifier lists use immutable keys captured for the native requests, not later editable metadata.</para>
        /// <para>Retains editable metadata references and copied editable scalar values. Later ID changes can differ from queried IDs and do not rebuild result-set indexes; the result Key property reflects current metadata, whereas set lookups retain captured observation identity. Metadata captures and sequential observations are not atomic. Caller coordinates writers and result provenance. No full plate geometry, unit-service conversion, stress transformation, resultant integration or design helpers are supplied by these supporting models.</para>
        /// <para>Runs under one current Output binding/lifetime/constructing-thread/reentrancy lease spanning validation, enumeration, optional hydration, every native call and output qualification. Refresh invalidates captured adapters; reacquire them. Stale/manual-noncurrent/disposed/other-thread and reentrant Root/Load/Output/refresh/disposal fail before native work. Raw access, other wrappers and external UI remain uncoordinated. Original exceptions/HRESULTs propagate; no analysis, activation, selection, save, retry, fallback or native mutation occurs. Successful reads do not certify result freshness, every native version or engineering validity.</para>
        /// <para>All work executes synchronously before returning a successful/faulted task; invocation can block. Awaiting does not dispatch COM. Source Task.Run behavior is replaced with settled owning-thread tasks; no worker concurrency, apartment switch, cancellation or UI dispatcher is used. Prefer the synchronous counterpart when a task shape is unnecessary.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">An input reference or sequence is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An analytical ID or thread-count hint is outside its permitted range.</exception>
        /// <exception cref="ArgumentException">A selection is invalid/oversized, supplied incidence is incomplete/invalid, or the requested node is not a plate corner.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/lease is invalid or native status, incidence, coordinates or components are malformed, unsuccessful, unwritten or nonfinite.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure propagates with its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">A required native signature is unavailable.</exception>
        /// <example><code>
        /// var loadCase = new StaadPro.Interop.Core.Entities.LoadCase(1, "Dead");
        /// var a = new StaadPro.Interop.Core.Entities.Node(11, 0, 0, 0);
        /// var plate = new StaadPro.Interop.Core.Entities.Plate(a,
        ///     new StaadPro.Interop.Core.Entities.Node(12, 1, 0, 0),
        ///     new StaadPro.Interop.Core.Entities.Node(13, 0, 0, 1)) { Id = 17 };
        /// var result = wrapper.Output.GetPlateCornerForcesAsync(loadCase, plate).GetAwaiter().GetResult();
        /// Console.WriteLine(result.Count);
        /// </code></example>
        Task<PlateCornerForcesResultSet> GetPlateCornerForcesAsync(ILoadCase loadCase, Plate plate);
    }
}
