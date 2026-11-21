using System;
using System.Threading.Tasks;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models.Output;
using System.Collections.Generic;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models.Root;
using StaadPro.Interop.Core.Services.Output;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Captures the Output binding with guarded availability, member-force, buckling and steel-design queries.</summary>
    public sealed class OSOutputAdapter : OSBaseAdapter, IOSOutput
    {
        /// <summary>Captures the native Output object from the supplied wrapper.</summary>
        /// <param name="wrapper">The wrapper exposing the required native binding.</param>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        /// <exception cref="ArgumentException">The native Output binding is unavailable.</exception>
        public OSOutputAdapter(OpenStaadWrapper wrapper) : base(wrapper)
        {
            ComObject = (object)wrapper.RawOutput ?? throw new ArgumentException("The Output binding is unavailable.", nameof(wrapper));
        }
        /// <summary>Gets the native object captured when this adapter was constructed.</summary>
        public dynamic ComObject { get; }

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
        public bool AreResultsAvailable() => Wrapper.UseOutputBinding(this, wrapper => MemberForceReader.AreResultsAvailable(wrapper.RawOutput));

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
        public MemberForceQueryResult GetMemberEndForces(string lcTitle, int beamId, int loadCaseId, MemberEnd memberEnd) => Wrapper.UseOutputBinding(this, wrapper => MemberForceReader.Read(wrapper.RawOutput, lcTitle, beamId, loadCaseId, memberEnd, LocalOrGlobal.Local));

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
        public MemberForceQueryResult GetMemberEndForces(string lcTitle, int beamId, int loadCaseId, MemberEnd memberEnd, LocalOrGlobal coordinateSystem) => Wrapper.UseOutputBinding(this, wrapper => MemberForceReader.Read(wrapper.RawOutput, lcTitle, beamId, loadCaseId, memberEnd, coordinateSystem));
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
        public bool AreBucklingAnalysisResultsAvailable() => Wrapper.UseOutputBinding(this, wrapper => BucklingResultReader.Available(wrapper.RawOutput));

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
        public int GetNumberOfBucklingFactorsComputed() => Wrapper.UseOutputBinding(this, wrapper => BucklingResultReader.Count(wrapper.RawOutput));


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
        public double GetBucklingFactor(int bucklingMode) => Wrapper.UseOutputBinding(this, wrapper => BucklingResultReader.Factor(wrapper.RawOutput, bucklingMode));

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
        public BucklingModeDisplacementResult GetBucklingModeDisplacement(int bucklingMode, Node atNode) => Wrapper.UseOutputBinding(this, wrapper =>
            {
                if (atNode == null) throw new ArgumentNullException(nameof(atNode));
                int nodeId = atNode.Id;
                if (nodeId <= 0) throw new ArgumentOutOfRangeException(nameof(atNode), "The node ID must be positive.");
                return BucklingResultReader.Displacement(wrapper.RawOutput, bucklingMode, nodeId);
            });

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
        public BucklingModeDisplacementResult GetBucklingModeDisplacementAtNode(int bucklingMode, int nodeId) => Wrapper.UseOutputBinding(this, wrapper => BucklingResultReader.Displacement(wrapper.RawOutput, bucklingMode, nodeId));

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
        public double GetMemberSteelUtilizationRatio(int beamId) => Wrapper.UseOutputBinding(this, wrapper => SteelDesignReader.Read(wrapper.RawOutput, beamId));

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
        public Task<double> GetMemberSteelUtilizationRatioAsync(int beamId)
        {
            try { return Task.FromResult(Wrapper.UseOutputBinding(this, wrapper => SteelDesignReader.Read(wrapper.RawOutput, beamId))); }
            catch (Exception error) { return Task.FromException<double>(error); }
        }

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
        public Task<MemberForceQueryResult> GetMemberEndForcesAsync(string lcTitle, int beamId, int loadCaseId, MemberEnd memberEnd)
        {
            try { return Task.FromResult(Wrapper.UseOutputBinding(this, wrapper => MemberForceReader.Read(wrapper.RawOutput, lcTitle, beamId, loadCaseId, memberEnd, LocalOrGlobal.Local))); }
            catch (Exception error) { return Task.FromException<MemberForceQueryResult>(error); }
        }

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
        public Task<double> GetMemberSteelUtilizationRatioAsync(Beam b)
        {
            try { return Task.FromResult(Wrapper.UseOutputBinding(this, wrapper => SteelDesignBatchReader.ReadOne(wrapper.RawOutput, b))); }
            catch (Exception error) { return Task.FromException<double>(error); }
        }

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
        public Dictionary<Beam, double> GetMemberSteelUtilizationRatios(HashSet<Beam> beams) => Wrapper.UseOutputBinding(this, wrapper => SteelDesignBatchReader.Read(wrapper.RawOutput, beams));

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
        public void PopulateMemberSteelUtilizationRatios(HashSet<Beam> forBeams) => Wrapper.UseOutputBinding(this, wrapper =>
        {
            SteelDesignBatchReader.Populate(wrapper.RawOutput, forBeams);
            return 0;
        });

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
        public Task<Dictionary<Beam, double>> GetMemberSteelUtilizationRatiosAsync(HashSet<Beam> beams)
        {
            try { return Task.FromResult(Wrapper.UseOutputBinding(this, wrapper => SteelDesignBatchReader.Read(wrapper.RawOutput, beams))); }
            catch (Exception error) { return Task.FromException<Dictionary<Beam, double>>(error); }
        }

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
        public MemberSteelDesignResults GetSteelDesignResultsForBeam(Beam beam, IEnumerable<ILoadCase> allLoadCases) => Wrapper.UseOutputBinding(this, wrapper => SteelDesignRecordReader.One(wrapper.RawOutput, beam, allLoadCases));

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
        public MemberSteelDesignResults GetSteelDesignResultsForBeam(IOSLoad load, Beam beam) => Wrapper.UseOutputBinding(this, wrapper => SteelDesignRecordReader.OneWithLoad(wrapper, load, beam));

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
        public IEnumerable<MemberSteelDesignResults> GetSteelDesignResultsForBeams(IEnumerable<Beam> beams, IEnumerable<ILoadCase> allLoadCases) => Wrapper.UseOutputBinding(this, wrapper => SteelDesignRecordReader.Many(wrapper.RawOutput, beams, allLoadCases));

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
        public IEnumerable<MemberSteelDesignResults> GetSteelDesignResultsForBeams(IOSLoad load, IEnumerable<Beam> beams) => Wrapper.UseOutputBinding(this, wrapper => SteelDesignRecordReader.ManyWithLoad(wrapper, load, beams));

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
        public Member GetGoverningMember(PhysicalMemberIdentificationResult pmiResult, bool ratiosPopulated = false) => Wrapper.UseOutputBinding(this, wrapper => GoverningMemberReader.Read(wrapper.RawOutput, pmiResult, ratiosPopulated));
    }
}
