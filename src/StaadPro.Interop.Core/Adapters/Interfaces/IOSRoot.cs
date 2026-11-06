using System;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Adapters.Interfaces
{
    /// <summary>Observes cached STAAD metadata and live model, folder, classification, error and process information.</summary>
    /// <remarks>Metadata methods read wrapper snapshots; model/process methods query the bound native root.
    /// Use the constructing thread outside another Root/lifecycle operation. Reacquire Root after RefreshComObjects.</remarks>
    public interface IOSRoot : IOSBase
    {
        /// <summary>Gets the captured native Root object for advanced, apartment-aware interoperation.</summary>
        /// <remarks>Raw access bypasses managed lifetime and thread guards. Do not use a captured binding after wrapper refresh/disposal.</remarks>
        dynamic ComObject { get; }

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
        ApplicationVersion GetApplicationVersion();

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
        BaseUnitSystem GetBaseUnit();

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
        ForceInputUnit GetInputUnitForForce();

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
        LengthInputUnit GetInputUnitForLength();

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
        IntPtr GetMainWindowHandle();

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
        string GetStaadFileFullPath();

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
        string GetStaadFileFolder();

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
        bool IsPhysicalModel();

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
        string GetErrorMessage();

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
        uint GetProcessId();

        /// <summary>Reads the current model's filename and extension without its directory.</summary>
        /// <returns>The Windows filename component with its original spelling, case and Unicode text.
        /// An explicitly empty native path, or a path ending in a directory separator, yields an empty filename.</returns>
        /// <remarks><para>Reads native GetSTAADFile once with Boolean true, then extracts the filename from that one
        /// full-path snapshot. It does not use a separate native filename-only mode, normalize to an absolute path,
        /// require a .std extension, check file existence or change the model. Relative paths remain valid inputs to
        /// the managed extraction. Invalid Windows path characters are rejected explicitly for Framework compatibility.</para>
        /// <para>Uses the wrapper's constructing STA thread and current Root binding. Disposal, replaced bindings,
        /// another thread or a nested Root/lifecycle operation are rejected before native dispatch. No acquisition,
        /// metadata refresh, file access, retries or worker-thread dispatch occurs. The native call can block and the
        /// model may change after this observation; a filename is not proof of readiness or physical-file identity.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Native output is unwritten or non-string, or the binding/thread/operation state is invalid.</exception>
        /// <exception cref="ArgumentException">The native path contains invalid Windows path characters.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required native signature.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// string filename = wrapper.Root.GetStaadFileNameOnly();
        /// Console.WriteLine(filename.Length == 0 ? "No filename reported" : filename);
        /// </code></example>
        string GetStaadFileNameOnly();

        /// <summary>Derives the expected uppercase .ANL path from one current model-path observation.</summary>
        /// <returns>The snapshot's directory combined with its filename stem and .ANL, or empty for an explicitly
        /// empty native path. A relative native path produces a relative result despite this method's historic name.</returns>
        /// <remarks><para>Reads GetSTAADFile exactly once with Boolean true. Removes only the final extension and uses
        /// Windows Path directory/filename/combine semantics, preserving earlier dots in the name. Does not call the
        /// public filename/full-path helpers in succession: both directory and stem come from the same snapshot.
        /// No .std extension or existing filename is required; directory-shaped input can derive a .ANL filename
        /// without proving that it describes a real model. Invalid Windows path characters are rejected.</para>
        /// <para>The returned path is a prediction, not evidence that an analysis file or usable results exist.
        /// No file is read/created, analysis started, model opened/saved, or wrapper metadata refreshed. Uses the
        /// constructing STA thread and current Root binding; disposed/replaced/wrong-thread/nested operations are
        /// rejected before native access. No retry or background dispatch occurs; synchronous COM can block.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Native output is unwritten or non-string, or the binding/thread/operation state is invalid.</exception>
        /// <exception cref="ArgumentException">The native path contains invalid Windows path characters.</exception>
        /// <exception cref="System.IO.PathTooLongException">A path exceeds a limit enforced by the runtime's Path operations.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required native signature.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// string analysisPath = wrapper.Root.GetAnlFileFullPath();
        /// Console.WriteLine(analysisPath); // Expected path only; no analysis/result availability check.
        /// </code></example>
        string GetAnlFileFullPath();

        /// <summary>Reads the current model's filename as a normalized absolute Windows path.</summary>
        /// <returns>The absolute path resolved from one native full-path snapshot, or empty only when native explicitly
        /// writes an empty string. A nonempty whitespace-only path is invalid rather than an unnamed-model result.</returns>
        /// <remarks><para>Calls GetSTAADFile once with Boolean true and applies Windows Path.GetFullPath using the
        /// process working directory observed after the native read. Dot segments, relative paths and separator forms
        /// follow the modern Windows Path API. Invalid path characters are rejected explicitly. Unlike
        /// <c>GetStaadFileFullPath()</c>, this method normalizes the text; neither method resolves filesystem links,
        /// checks existence, requires a .std extension or proves model readiness.</para>
        /// <para>The working directory is process-wide; serialize changes to it when interpreting relative paths.
        /// Uses the wrapper's constructing STA thread and current Root binding. Disposal, replacement, another thread
        /// and nested Root/lifecycle work are rejected before COM access. No model mutation, acquisition, implicit
        /// refresh/save, retry or background call occurs. The native read can block and is not a lasting identity guarantee.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Native output is unwritten or non-string, or the binding/thread/operation state is invalid.</exception>
        /// <exception cref="ArgumentException">The native path is nonempty whitespace, contains invalid path characters, or cannot be normalized.</exception>
        /// <exception cref="System.IO.PathTooLongException">The native path exceeds a limit enforced by the runtime.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required native signature.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// string openedPath = wrapper.Root.GetOpenedStaadFilename();
        /// Console.WriteLine(openedPath.Length == 0 ? "Unnamed session" : openedPath);
        /// </code></example>
        string GetOpenedStaadFilename();

        /// <summary>Tests whether this bound session currently reports the requested complete model path.</summary>
        /// <param name="staadFileFullPath">Non-null, non-whitespace Windows path. Relative paths are accepted and
        /// resolved against the process working directory captured before native access; no .std extension is required.</param>
        /// <returns>True for an ordinal case-insensitive match of normalized complete paths; false for a different
        /// directory/model or an explicitly empty native filename, even if two different models share a basename.</returns>
        /// <remarks><para>Validates and normalizes the requested path before one GetSTAADFile call with Boolean true.
        /// Normalizes the native path against that same captured working directory, so a native callback changing the
        /// process directory cannot make the two relative paths use different bases. Does not trim text, resolve links,
        /// check file existence or enumerate other STAAD sessions. This is a textual comparison; Windows case-sensitive
        /// directories or filesystem aliases can differ from that comparison. Empty native output means false; malformed,
        /// unwritten and nonempty whitespace native output throws. Invalid Windows path characters are rejected.</para>
        /// <para>Equivalent to <c>ModelIsOpen(string)</c> under the corrected Framework full-path contract; it is not a
        /// basename check or a native readiness probe. Uses the constructing STA thread and current Root binding;
        /// disposed/replaced/wrong-thread/nested operations are rejected before dispatch. Performs no acquisition,
        /// opening, analysis, save, refresh, retry or worker-thread call. Native model changes can follow the snapshot.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="staadFileFullPath"/> is null; rejected before native access.</exception>
        /// <exception cref="ArgumentException">The requested path is blank/invalid, or the nonempty native path cannot be normalized.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Native output is unwritten or non-string, or the binding/thread/operation state is invalid.</exception>
        /// <exception cref="System.IO.PathTooLongException">A path exceeds a limit enforced by the runtime.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required native signature.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// bool matches = wrapper.Root.IsTarget(@"C:\Models\Frame.std");
        /// Console.WriteLine(matches); // Snapshot identity, not a readiness or file-existence guarantee.
        /// </code></example>
        bool IsTarget(string staadFileFullPath);

        /// <summary>Tests whether the specified complete path is the model currently reported by this bound root.</summary>
        /// <param name="fileFullPath">Non-null, non-whitespace Windows path, including relative paths. The comparison
        /// captures the process working directory before native access; a .std extension and existing file are not required.</param>
        /// <returns>True for a normalized ordinal case-insensitive full-path match; false for an unnamed native session
        /// or a different full path. Equal basenames in different directories do not identify the same model.</returns>
        /// <remarks><para>Uses the same shared identity comparison as <c>IsTarget(string)</c>, preserving this method's
        /// original parameter name. Validates/normalizes the request before one GetSTAADFile call with Boolean true;
        /// the model path is never passed as the native include-full-path flag. Both relative paths use a single captured
        /// working-directory base. Explicit empty native output yields false, while unwritten/non-string/nonempty
        /// whitespace output and invalid Windows path characters are rejected. No text trimming or link resolution occurs.</para>
        /// <para>This read-only snapshot neither searches all running models nor checks Geometry/Load readiness or
        /// filesystem identity. Use the constructing STA thread and current Root binding. Disposal, refresh replacement,
        /// wrong-thread access and nested Root/lifecycle work are rejected before native dispatch. No model mutation,
        /// acquisition, polling, retries, metadata refresh or worker-thread dispatch occurs. Native COM can block.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="fileFullPath"/> is null; rejected before native access.</exception>
        /// <exception cref="ArgumentException">The requested path is blank/invalid, or the nonempty native path cannot be normalized.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Native output is unwritten or non-string, or the binding/thread/operation state is invalid.</exception>
        /// <exception cref="System.IO.PathTooLongException">A path exceeds a limit enforced by the runtime.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required native signature.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// bool isOpen = wrapper.Root.ModelIsOpen(@"C:\Models\Frame.std");
        /// Console.WriteLine(isOpen);
        /// </code></example>
        bool ModelIsOpen(string fileFullPath);

    }
}
