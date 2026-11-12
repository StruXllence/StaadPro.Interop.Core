using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models.Root;
namespace StaadPro.Interop.Core.Adapters.Interfaces
{
    /// <summary>Provides guarded Output foundation queries for member-force observations.</summary>
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
    }
}
