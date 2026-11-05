using System;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Exposes guarded observations of the owning wrapper's cached Root metadata.</summary>
    /// <remarks>Uses the existing wrapper snapshots without further native dispatch. Reacquire Root after RefreshComObjects.</remarks>
    public sealed class OSRootAdapter : OSBaseAdapter, IOSRoot
    {
        /// <summary>Captures the native Root object from the supplied wrapper.</summary>
        /// <param name="wrapper">The wrapper exposing the required native binding.</param>
        /// <remarks>Use wrapper.Root for the current managed binding. Construction performs no native query.</remarks>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        /// <exception cref="ArgumentException">The native Root binding is unavailable.</exception>
        public OSRootAdapter(OpenStaadWrapper wrapper) : base(wrapper)
        {
            ComObject = (object)wrapper.RawOpenStaad ?? throw new ArgumentException("The Root binding is unavailable.", nameof(wrapper));
        }
        /// <summary>Gets the native object captured when this adapter was constructed.</summary>
        /// <remarks>Raw access bypasses managed lifetime/thread guards. Do not use after wrapper refresh/disposal.</remarks>
        public dynamic ComObject { get; }

        /// <summary>Gets the cached native STAAD.Pro version for this wrapper's Root binding.</summary>
        /// <returns>An independent four-part version, or null when the optional native version member was unavailable.</returns>
        /// <remarks><para>Projects <see cref="OpenStaadWrapper.StaadVersion"/>, not the host/client
        /// <see cref="OpenStaadWrapper.ApplicationVersion"/>. Editing the returned mutable model cannot change the cache.
        /// Call <see cref="OpenStaadWrapper.RefreshComObjects"/> and reacquire Root to observe another native version.</para>
        /// <para>Reads the cache on the wrapper's constructing thread without COM dispatch, acquisition, retries or model changes.
        /// It does not establish connection liveness, model readiness or feature availability. Reads during refresh/disposal
        /// are rejected; a cache can become stale after external changes.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This adapter was replaced by RefreshComObjects, the caller is on another thread,
        /// or a wrapper lifecycle operation is in progress.</exception>
        /// <example>With a connected wrapper on its constructing thread:<code>
        /// var root = wrapper.Root;
        /// var version = root.GetApplicationVersion();
        /// Console.WriteLine(version?.Display ?? "Unavailable");
        /// </code></example>
        public ApplicationVersion GetApplicationVersion() => Wrapper.ReadRootMetadata(this, static wrapper => wrapper.StaadVersion);

        /// <summary>Gets the cached model base unit system for this Root binding.</summary>
        /// <returns>The observed base-system enum. Unknown represents an absent member, null or DBNull;
        /// unrecognized integral native codes are preserved as enum values.</returns>
        /// <remarks><para>Projects <see cref="OpenStaadWrapper.BaseUnitSystem"/> without a native call.
        /// RefreshUnits updates the snapshot without replacing this binding. RefreshComObjects updates it and requires a new Root reference.
        /// Base units differ from the current model input units; this method neither converts data nor infers units from them.</para>
        /// <para>Use the wrapper's constructing thread outside refresh/disposal. No native query, model mutation, implicit refresh,
        /// save or retry occurs. The snapshot does not certify current native state or engineering units of analysis results.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This adapter was replaced by RefreshComObjects, the caller is on another thread,
        /// or a wrapper lifecycle operation is in progress.</exception>
        /// <example>With a connected wrapper on its constructing thread:<code>
        /// var root = wrapper.Root;
        /// Console.WriteLine(root.GetBaseUnit());
        /// </code></example>
        public BaseUnitSystem GetBaseUnit() => Wrapper.ReadRootMetadata(this, static wrapper => wrapper.BaseUnitSystem);

        /// <summary>Gets the cached model input force unit for this Root binding.</summary>
        /// <returns>The wrapper's force-unit enum. When no usable native unit was observed, the compatibility fallback
        /// is KiloPound for Imperial base units and KiloNewton otherwise; the enum alone does not identify a fallback.</returns>
        /// <remarks><para>Projects <see cref="OpenStaadWrapper.InputForceUnit"/> without native dispatch. During construction/refresh,
        /// usable native reference output takes precedence over an eligible return value. Boolean returns and version 24
        /// success codes are not interpreted as units. A failed/unparseable unit query can use the documented fallback,
        /// including after unknown base units; that value is not proof of an observed unit.</para>
        /// <para>Call RefreshUnits after external input-unit changes; it preserves this binding. Reacquire Root after RefreshComObjects.
        /// This read changes no units, converts no values and does not determine output/result units. Use the constructing thread
        /// outside refresh/disposal. Cached reads perform no acquisition, retries, save or native-liveness check.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This adapter was replaced by RefreshComObjects, the caller is on another thread,
        /// or a wrapper lifecycle operation is in progress.</exception>
        /// <example>With a connected wrapper on its constructing thread:<code>
        /// var root = wrapper.Root;
        /// Console.WriteLine(root.GetInputUnitForForce()); // May be the documented compatibility fallback.
        /// </code></example>
        public ForceInputUnit GetInputUnitForForce() => Wrapper.ReadRootMetadata(this, static wrapper => wrapper.InputForceUnit);

        /// <summary>Gets the cached model input length unit for this Root binding.</summary>
        /// <returns>The wrapper's length-unit enum. When no usable native unit was observed, the compatibility fallback
        /// is Feet for Imperial base units and Meter otherwise; the enum alone does not identify a fallback.</returns>
        /// <remarks><para>Projects <see cref="OpenStaadWrapper.InputLengthUnit"/> without native dispatch. During construction/refresh,
        /// usable native reference output takes precedence over an eligible return value. Boolean returns and version 24
        /// success codes are not interpreted as units. Failed/unparseable native unit queries can use the documented fallback,
        /// which is not proof of an observed unit or a coordinate/result-unit declaration.</para>
        /// <para>RefreshUnits updates this snapshot without rebinding; RefreshComObjects requires reacquiring Root.
        /// Use the wrapper's constructing thread outside refresh/disposal. This read performs no native query, unit conversion,
        /// model change, acquisition, implicit save or retry and does not certify native connection liveness.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This adapter was replaced by RefreshComObjects, the caller is on another thread,
        /// or a wrapper lifecycle operation is in progress.</exception>
        /// <example>With a connected wrapper on its constructing thread:<code>
        /// var root = wrapper.Root;
        /// Console.WriteLine(root.GetInputUnitForLength()); // May be the documented compatibility fallback.
        /// </code></example>
        public LengthInputUnit GetInputUnitForLength() => Wrapper.ReadRootMetadata(this, static wrapper => wrapper.InputLengthUnit);

        /// <summary>Gets the cached, non-owning STAAD application main-window handle for this Root binding.</summary>
        /// <returns>The wrapper's cached handle, including a configured model-window service's value. Zero is used when
        /// native version is unavailable or major is 20 or earlier, and when the default service's optional window member is absent;
        /// the native API or configured service can also supply zero.</returns>
        /// <remarks><para>Projects <see cref="OpenStaadWrapper.StaadApplicationWindowHandle"/> without querying native windows,
        /// scanning processes, selecting another session or changing window state. RefreshComObjects rereads the handle and replaces Root;
        /// RefreshUnits does not. A supplied model-window service is retained; no global process-name fallback is introduced.</para>
        /// <para>The handle is borrowed: do not close it. A cached value can expire and proves neither current HWND validity,
        /// process ownership, model readiness nor native liveness. Use the wrapper's constructing thread outside refresh/disposal.
        /// This method performs no retries, acquisition, model mutation or native call.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This adapter was replaced by RefreshComObjects, the caller is on another thread,
        /// or a wrapper lifecycle operation is in progress.</exception>
        /// <example>With a connected wrapper on its constructing thread:<code>
        /// var root = wrapper.Root;
        /// Console.WriteLine(root.GetMainWindowHandle()); // Borrowed snapshot; do not close it.
        /// </code></example>
        public IntPtr GetMainWindowHandle() => Wrapper.ReadRootMetadata(this, static wrapper => wrapper.StaadApplicationWindowHandle);
    }
}
