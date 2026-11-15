using System;
using System.Threading.Tasks;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models.Output;
using System.Collections.Generic;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models.Root;
namespace StaadPro.Interop.Core.Adapters.Interfaces
{
    /// <summary>Provides guarded Output queries for availability, member forces, buckling and steel-design observations.</summary>
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
    }
}
