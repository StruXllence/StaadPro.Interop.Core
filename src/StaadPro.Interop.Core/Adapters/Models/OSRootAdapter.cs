using System;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Acquisition;
using StaadPro.Interop.Core.Services.Root.Common;

namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Exposes guarded cached metadata and live model/process observations for the owning wrapper.</summary>
    /// <remarks>Metadata methods read wrapper snapshots; model/process methods dispatch to the captured native root.
    /// Use the constructing thread and reacquire Root after RefreshComObjects.</remarks>
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
        public ApplicationVersion GetApplicationVersion() => Wrapper.ReadRootObservation(this, static wrapper => wrapper.StaadVersion);

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
        public BaseUnitSystem GetBaseUnit() => Wrapper.ReadRootObservation(this, static wrapper => wrapper.BaseUnitSystem);

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
        public ForceInputUnit GetInputUnitForForce() => Wrapper.ReadRootObservation(this, static wrapper => wrapper.InputForceUnit);

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
        public LengthInputUnit GetInputUnitForLength() => Wrapper.ReadRootObservation(this, static wrapper => wrapper.InputLengthUnit);

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
        public IntPtr GetMainWindowHandle() => Wrapper.ReadRootObservation(this, static wrapper => wrapper.StaadApplicationWindowHandle);

        /// <summary>Reads the current model's full path directly from this bound OpenSTAAD root.</summary>
        /// <returns>The exact string written by native GetSTAADFile, including an explicitly written empty string for a blank/unnamed session.</returns>
        /// <remarks><para>Calls GetSTAADFile once with a BSTR-initialized internal output and Boolean true for full-path inclusion.
        /// Rejects an unwritten output, null or a non-string value rather than reporting a blank. Preserves Unicode,
        /// whitespace, case and separators without path normalization, filesystem checks or changing the current directory.
        /// This live snapshot may differ from acquisition's earlier ActualPath. Empty output alone does not prove readiness.</para>
        /// <para>Use the wrapper's constructing STA thread and serialize shared-session access. The binding is rejected
        /// after disposal or replacement by RefreshComObjects; RefreshUnits retains it. Calls during another managed Root
        /// observation or wrapper refresh/disposal are rejected before native dispatch. No acquisition, model opening,
        /// saving, implicit metadata refresh, retry or worker-thread dispatch occurs. A synchronous native call can block.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The native response is malformed, the adapter was replaced,
        /// the caller is on another thread, or a managed Root/lifecycle operation is in progress.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The bound root does not expose the required member.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// string modelPath = wrapper.Root.GetStaadFileFullPath();
        /// Console.WriteLine(modelPath.Length == 0 ? "No model filename reported" : modelPath);
        /// </code></example>
        public string GetStaadFileFullPath() => Wrapper.ReadRootObservation(this, static wrapper => OpenStaadModelIdentityReader.ReadPath(wrapper.RawOpenStaad));

        /// <summary>Reads the current model folder directly from this bound OpenSTAAD root.</summary>
        /// <returns>The exact folder string written by native GetSTAADFileFolder, preserving empty output, whitespace and trailing separators.</returns>
        /// <remarks><para>Calls the native void GetSTAADFileFolder once with a BSTR-initialized internal output.
        /// Rejects an unwritten output, null or a non-string value. Unlike deriving a folder from GetStaadFileFullPath,
        /// this preserves the folder reported by STAAD and performs no filename query. Does not normalize the directory,
        /// check its existence or create it. Empty output does not establish why the folder is unavailable.</para>
        /// <para>Use the wrapper's constructing STA thread and serialize shared-session access. The binding is rejected
        /// after disposal or replacement by RefreshComObjects; RefreshUnits retains it. Calls during another managed Root
        /// observation or wrapper refresh/disposal are rejected before native dispatch. No acquisition, model opening,
        /// saving, implicit metadata refresh, retry or worker-thread dispatch occurs. A synchronous native call can block.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The native response is malformed, the adapter was replaced,
        /// the caller is on another thread, or a managed Root/lifecycle operation is in progress.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The bound root does not expose the required member.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// string modelFolder = wrapper.Root.GetStaadFileFolder();
        /// Console.WriteLine(modelFolder);
        /// </code></example>
        public string GetStaadFileFolder() => Wrapper.ReadRootObservation(this, static wrapper => RootCommonQueryService.GetStaadFileFolder(wrapper.RawOpenStaad));

        /// <summary>Reads whether this bound OpenSTAAD root reports a physical model.</summary>
        /// <returns>True for native Boolean true or integral one; false for Boolean false or integral zero.</returns>
        /// <remarks><para>Calls native IsPhysicalModel once. Rejects every other integer, null, strings, floating-point,
        /// decimal and pointer values instead of coercing them into a model classification. False reports only the native
        /// predicate; it does not certify an analytical model is loaded or ready. Behavior for a blank or unsupported
        /// session is not inferred. Does not convert the model, query a path or initiate analysis.</para>
        /// <para>Use the wrapper's constructing STA thread and serialize shared-session access. The binding is rejected
        /// after disposal or replacement by RefreshComObjects; RefreshUnits retains it. Calls during another managed Root
        /// observation or wrapper refresh/disposal are rejected before native dispatch. No acquisition, model opening,
        /// saving, implicit metadata refresh, retry or worker-thread dispatch occurs. A synchronous native call can block.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The native response is malformed, the adapter was replaced,
        /// the caller is on another thread, or a managed Root/lifecycle operation is in progress.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The bound root does not expose the required member.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// bool isPhysical = wrapper.Root.IsPhysicalModel();
        /// Console.WriteLine(isPhysical);
        /// </code></example>
        public bool IsPhysicalModel() => Wrapper.ReadRootObservation(this, static wrapper => RootCommonQueryService.IsPhysicalModel(wrapper.RawOpenStaad));

        /// <summary>Reads the error-message text currently reported by this bound OpenSTAAD root.</summary>
        /// <returns>The exact native string, including Unicode, line breaks, whitespace or an explicitly returned empty string.</returns>
        /// <remarks><para>Calls native GetErrorMessage once on explicit request. Rejects null and non-string responses.
        /// Empty text does not establish that prior operations succeeded. The vendor contract does not specify whether
        /// reading consumes or clears the error; stable repeated reads, history and a nondestructive peek are not promised.
        /// Unlike catching a managed exception, this reads the application's current error text, which another operation
        /// may have replaced. It is never implicitly called from another operation's failure handler.</para>
        /// <para>Use the wrapper's constructing STA thread and serialize shared-session access. The binding is rejected
        /// after disposal or replacement by RefreshComObjects; RefreshUnits retains it. Calls during another managed Root
        /// observation or wrapper refresh/disposal are rejected before native dispatch. No acquisition, model opening,
        /// saving, implicit metadata refresh, retry or worker-thread dispatch occurs. A synchronous native call can block.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The native response is malformed, the adapter was replaced,
        /// the caller is on another thread, or a managed Root/lifecycle operation is in progress.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The bound root does not expose the required member.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// string nativeMessage = wrapper.Root.GetErrorMessage();
        /// Console.WriteLine(nativeMessage);
        /// </code></example>
        public string GetErrorMessage() => Wrapper.ReadRootObservation(this, static wrapper => RootCommonQueryService.GetErrorMessage(wrapper.RawOpenStaad));

        /// <summary>Reads the process identifier reported by this bound OpenSTAAD root.</summary>
        /// <returns>A positive UInt32 process identifier, between one and UInt32.MaxValue inclusive.</returns>
        /// <remarks><para>Calls native GetProcessId once and accepts integral CLR representations only. Rejects zero,
        /// negative/overflowing values, Boolean, string, floating-point, decimal and pointer-shaped responses.
        /// Unlike GetMainWindowHandle's cached borrowed HWND, this is a live PID query, not a process handle.
        /// Does not enumerate processes, consult a global active object or construct a Process. The identifier can
        /// become stale or be reused; no process-liveness, ownership or model-readiness guarantee is made.</para>
        /// <para>Use the wrapper's constructing STA thread and serialize shared-session access. The binding is rejected
        /// after disposal or replacement by RefreshComObjects; RefreshUnits retains it. Calls during another managed Root
        /// observation or wrapper refresh/disposal are rejected before native dispatch. No acquisition, model opening,
        /// saving, implicit metadata refresh, retry or worker-thread dispatch occurs. A synchronous native call can block.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The native response is malformed, the adapter was replaced,
        /// the caller is on another thread, or a managed Root/lifecycle operation is in progress.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The bound root does not expose the required member.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// uint processId = wrapper.Root.GetProcessId();
        /// Console.WriteLine(processId);
        /// </code></example>
        public uint GetProcessId() => Wrapper.ReadRootObservation(this, static wrapper => RootCommonQueryService.GetProcessId(wrapper.RawOpenStaad));
    }
}
