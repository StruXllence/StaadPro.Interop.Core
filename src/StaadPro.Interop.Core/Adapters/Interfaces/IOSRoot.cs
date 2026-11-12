using System;
using StaadPro.Interop.Core.Adapters.Interfaces.Capabilities;
using System.Collections.Generic;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Enums.Root;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Root;

namespace StaadPro.Interop.Core.Adapters.Interfaces
{
    /// <summary>Provides guarded metadata, model observations and STAAD model/application operations.</summary>
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


        /// <summary>Requests warning suppression on the bound STAAD application.</summary>
        /// <param name="silentMode">A defined SilentMode: Disable (0) or Enable (1), forwarded once as a native Int32. It is an application setting; license and filesystem errors can still display dialogs.</param>
        /// <returns>True when the native invocation returns without throwing; false when native dispatch throws, including a missing member. The native return is the previous mode and is deliberately ignored, even when false, zero, null or another shape. This Boolean does not report that previous value or certify warning suppression.</returns>
        /// <remarks><para>Unlike SaveModel(silentMode), this changes the application setting without saving. It issues one SetSilentMode call and does not query, restore or persist that setting. The native previous-mode VARIANT is not interpreted as a success flag. Native exceptions are converted to false to preserve the Boolean contract; enum validation and wrapper lifetime/thread/reentrancy errors occur outside that catch and propagate. A false return can follow a partial native change; no automatic retry is performed.</para>
        /// <para>Use the constructing COM thread and the current wrapper.Root binding. The shared wrapper lease rejects disposal, stale adapters, other-thread calls and nested Root/refresh/disposal operations before native dispatch. Raw-object access and external UI edits bypass this guard. Invocation is synchronous and may block; there is no worker-thread dispatch, hard in-process timeout, rollback or alternate-instance fallback.</para>
        /// <para>No metadata or adapter refresh is performed. Coordinate model ownership and UI access before calling. Normal completion does not certify native readiness, dialog completion or engineering correctness.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">silentMode is not Disable (0) or Enable (1); no native call occurs.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter was replaced, the caller uses another thread, or another guarded operation is in progress.</exception>
        /// <example>With an owned connected session on its constructing STA thread:<code>
        /// bool requestNativeChange = false; // Enable only when this session/model change is intended.
        /// if (requestNativeChange)
        /// {
        ///     var result = wrapper.Root.SetSilentMode(StaadPro.Interop.Core.Enums.SilentMode.Enable);
        ///     Console.WriteLine(result);
        /// }</code></example>
        bool SetSilentMode(SilentMode silentMode);

        /// <summary>Requests a native save of the model currently associated with this bound STAAD session.</summary>
        /// <param name="silentMode">A defined SilentMode: Disable (0) or Enable (1), sent as the native save-option Int32. Suppression covers supported warnings only; unnamed models and errors may still display dialogs.</param>
        /// <returns>This same Root adapter after the native void invocation returns without throwing. It does not acknowledge file persistence or prove that a prompt was accepted.</returns>
        /// <remarks><para>Unlike SetSilentMode(silentMode), this passes the mode directly to SaveModel without changing a separate application setting. Saves through STAAD; it does not write managed file contents, run analysis, query paths, restore settings or verify disk output. Saving can write files or require user interaction. Native void completion has no Boolean or numeric status to interpret.</para>
        /// <para>Use the constructing COM thread and the current wrapper.Root binding. The shared wrapper lease rejects disposal, stale adapters, other-thread calls and nested Root/refresh/disposal operations before native dispatch. Raw-object access and external UI edits bypass this guard. Invocation is synchronous and may block; there is no worker-thread dispatch, hard in-process timeout, rollback or alternate-instance fallback.</para>
        /// <para>No metadata or adapter refresh is performed. Coordinate model ownership and UI access before calling. Normal completion does not certify native readiness, dialog completion or engineering correctness.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">silentMode is not Disable (0) or Enable (1); no native call occurs.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter was replaced, the caller uses another thread, or another guarded operation is in progress.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; its original exception and HRESULT propagate, potentially after partial effects.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required native member/signature.</exception>
        /// <example>With an owned connected session on its constructing STA thread:<code>
        /// bool requestNativeChange = false; // Enable only when this session/model change is intended.
        /// if (requestNativeChange)
        /// {
        ///     var result = wrapper.Root.SaveModel(StaadPro.Interop.Core.Enums.SilentMode.Enable);
        ///     Console.WriteLine(result);
        /// }</code></example>
        IOSRoot SaveModel(SilentMode silentMode);

        /// <summary>Requests that the bound STAAD application open the specified model.</summary>
        /// <param name="filePath">A non-null, non-whitespace Windows model path. Relative paths use one captured process working directory and are normalized to an absolute path before dispatch. A .std extension and existing file are not required or checked by this method.</param>
        /// <returns>This same Root adapter after the native void invocation returns without throwing; model identity and database readiness are not verified.</returns>
        /// <remarks><para>Unlike session acquisition, this requests a model switch on this exact root and never attaches to another process. Validates and normalizes the path before one OpenSTAADFile call. Explicit invalid-character checks retain Windows path validation under modern .NET. Does not check existence, create files, close/save first, wait or poll Geometry/Load readiness. The current model may be affected and native prompts can appear. A normal return is not evidence that the target became available.</para>
        /// <para>Use the constructing COM thread and the current wrapper.Root binding. The shared wrapper lease rejects disposal, stale adapters, other-thread calls and nested Root/refresh/disposal operations before native dispatch. Raw-object access and external UI edits bypass this guard. Invocation is synchronous and may block; there is no worker-thread dispatch, hard in-process timeout, rollback or alternate-instance fallback.</para>
        /// <para>Opening or closing can leave cached units, metadata and domain bindings stale. After the caller establishes native readiness, call wrapper.RefreshComObjects and reacquire wrapper.Root and other adapters. This method does not refresh automatically and does not wait for the transition; a failure may follow partial native effects.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">filePath is null.</exception>
        /// <exception cref="ArgumentException">filePath is blank or contains invalid Windows path characters.</exception>
        /// <exception cref="System.IO.PathTooLongException">Path normalization exceeds a runtime path limit.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter was replaced, the caller uses another thread, or another guarded operation is in progress.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; its original exception and HRESULT propagate, potentially after partial effects.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required native member/signature.</exception>
        /// <example>With an owned connected session on its constructing STA thread:<code>
        /// bool requestNativeChange = false; // Enable only when this session/model change is intended.
        /// if (requestNativeChange)
        /// {
        ///     var result = wrapper.Root.OpenSTAADFile(@"C:\Models\Frame.std");
        ///     Console.WriteLine(result);
        /// }</code></example>
        IOSRoot OpenSTAADFile(string filePath);

        /// <summary>Requests that the bound STAAD application close its current model.</summary>
        /// <returns>This same Root adapter after the native void invocation returns without throwing. It does not certify that the file was closed or that the session remains ready.</returns>
        /// <remarks><para>Unlike disposing OpenStaadWrapper, this requests a native model change. Calls CloseSTAADFile once with no arguments; it does not invoke SaveModel, discard changes explicitly, terminate STAAD, dispose the wrapper or acquire a replacement session. STAAD controls prompts and dirty-model handling. Native void completion supplies no Boolean or numeric acknowledgement.</para>
        /// <para>Use the constructing COM thread and the current wrapper.Root binding. The shared wrapper lease rejects disposal, stale adapters, other-thread calls and nested Root/refresh/disposal operations before native dispatch. Raw-object access and external UI edits bypass this guard. Invocation is synchronous and may block; there is no worker-thread dispatch, hard in-process timeout, rollback or alternate-instance fallback.</para>
        /// <para>Opening or closing can leave cached units, metadata and domain bindings stale. After the caller establishes native readiness, call wrapper.RefreshComObjects and reacquire wrapper.Root and other adapters. This method does not refresh automatically and does not wait for the transition; a failure may follow partial native effects.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter was replaced, the caller uses another thread, or another guarded operation is in progress.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; its original exception and HRESULT propagate, potentially after partial effects.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required native member/signature.</exception>
        /// <example>With an owned connected session on its constructing STA thread:<code>
        /// bool requestNativeChange = false; // Enable only when this session/model change is intended.
        /// if (requestNativeChange)
        /// {
        ///     var result = wrapper.Root.CloseSTAADFile();
        ///     Console.WriteLine(result);
        /// }</code></example>
        IOSRoot CloseSTAADFile();

        /// <summary>Requests that the bound STAAD application become visible.</summary>
        /// <returns>This same Root adapter after the native invocation returns without throwing. Visibility, foreground focus and repaint completion are not verified.</returns>
        /// <remarks><para>Unlike window-handle queries and model opening, this issues the source-compatible ShowApplication request on this exact root. Ignores any native return. Does not launch another process, open/save/close a model, run analysis or explicitly maximize, restore or activate a window through Windows APIs. Native availability and graphical effects are build-dependent.</para>
        /// <para>Use the constructing COM thread and the current wrapper.Root binding. The shared wrapper lease rejects disposal, stale adapters, other-thread calls and nested Root/refresh/disposal operations before native dispatch. Raw-object access and external UI edits bypass this guard. Invocation is synchronous and may block; there is no worker-thread dispatch, hard in-process timeout, rollback or alternate-instance fallback.</para>
        /// <para>No metadata or adapter refresh is performed. Coordinate model ownership and UI access before calling. Normal completion does not certify native readiness, dialog completion or engineering correctness.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter was replaced, the caller uses another thread, or another guarded operation is in progress.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; its original exception and HRESULT propagate, potentially after partial effects.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required native member/signature.</exception>
        /// <example>With an owned connected session on its constructing STA thread:<code>
        /// bool requestNativeChange = false; // Enable only when this session/model change is intended.
        /// if (requestNativeChange)
        /// {
        ///     var result = wrapper.Root.ShowApplication();
        ///     Console.WriteLine(result);
        /// }</code></example>
        IOSRoot ShowApplication();

        /// <summary>Reads the current model's job name, client and originating engineer into a detached editable snapshot.</summary>
        /// <returns>A fresh ShortJobInfo with exact native text, including empty strings, whitespace and Unicode. Edits raise managed property notifications and do not update STAAD.</returns>
        /// <remarks><para>Invokes GetShortJobInfo once with three BSTR-shaped reference outputs. Unlike GetFullJobInfo, this reads no dates or extended job fields. Every output must be written and string-valued; unwritten, null, DBNull and other shapes throw instead of becoming empty text. Native void completion is not interpreted as a numeric or Boolean status. No filename, analysis, acquisition, setter, cache refresh or model mutation is requested.</para>
        /// <para>Uses the current captured root and the wrapper's constructing COM thread under its shared operation lease. Reacquire wrapper.Root after RefreshComObjects. Disposed or replaced bindings, another thread and nested Root/refresh/disposal operations are rejected before dispatch. Raw callers and external UI/model edits bypass the guard. Reads are synchronous snapshots, not proof of native readiness or an atomic view across multiple calls. No worker dispatch, retries or alternate-instance fallback occurs.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">An output is unwritten/non-string, or the binding, thread or operation state is invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required GetShortJobInfo reference-output contract.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// var info = wrapper.Root.GetShortJobInfo();
        /// Console.WriteLine(info.Name + ": " + info.Client);
        /// </code></example>
        ShortJobInfo GetShortJobInfo();

        /// <summary>Reads all thirteen current-model job fields into a detached editable full job snapshot.</summary>
        /// <returns>A fresh FullJobInfo preserving native text and parsing the originated, checked and approved dates. Empty or whitespace-only dates map to DateTime.MinValue; other dates use CurrentCulture. No result is cached or written back.</returns>
        /// <remarks><para>Unlike GetShortJobInfo, this reads name, client, originator, engineer date, number, revision, part, reference, checker, checked date, approver, approved date and comments, in that exact native order. One call initializes thirteen BSTR-shaped reference outputs and validates every value as written text before any date parsing. Null, DBNull, non-string and unwritten outputs throw. The existing factory retains DateTime properties and uses DateTime.Parse with CurrentCulture and DateTimeStyles.None; ambiguous dates are not retried under another culture. Raw non-date text, Unicode, whitespace and comment line breaks are preserved. Native void completion supplies no status flag. Edits affect managed storage only; no setter, cache refresh, acquisition, save or analysis occurs.</para>
        /// <para>Uses the current captured root and the wrapper's constructing COM thread under its shared operation lease. Reacquire wrapper.Root after RefreshComObjects. Disposed or replaced bindings, another thread and nested Root/refresh/disposal operations are rejected before dispatch. Raw callers and external UI/model edits bypass the guard. Reads are synchronous snapshots, not proof of native readiness or an atomic view across multiple calls. No worker dispatch, retries or alternate-instance fallback occurs.</para></remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">An output is unwritten/non-string, or the binding, thread or operation state is invalid.</exception>
        /// <exception cref="FormatException">A nonempty native date cannot be parsed using the current culture.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native dispatch fails; the original exception and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks the required GetFullJobInfo reference-output contract.</exception>
        /// <example>With a connected wrapper on its constructing STA thread:<code>
        /// var info = wrapper.Root.GetFullJobInfo();
        /// Console.WriteLine(info.Number);
        /// if (info.Originated != DateTime.MinValue) Console.WriteLine(info.Originated);
        /// </code></example>
        FullJobInfo GetFullJobInfo();

        /// <summary>Requests the minimized state for the main window reported by this bound STAAD root.</summary>
        /// <remarks>
        /// <para>Unlike cached GetMainWindowHandle, this reads the bound root's live window handle. After validating its pointer-sized shape, reads GetProcessId once and verifies the HWND with IsWindow and GetWindowThreadProcessId. Only a nonzero existing window owned by the reported positive native process ID receives ShowWindowAsync with command 6. This requests minimization; Windows may activate the next top-level window. No foreground-focus guarantee or completed-state read-back is provided.</para>
        /// <para>This void request returns after Windows accepts the asynchronous show-state operation; acceptance does not certify that the state became visible. A zero/-1, malformed or out-of-range handle, invalid PID, missing/recycled window, ownership mismatch or rejected Windows request throws. Validation reduces cross-session mistakes but cannot eliminate handle recycling or external UI races after the checks. Unlike ShowApplication, this uses Windows show-state control rather than a native visibility request; it does not launch STAAD, change the active model, save, analyze, refresh cached metadata, poll or retry.</para>
        /// <para>Runs on the wrapper's constructing COM thread under the shared Root operation lease, using the captured session only. Reacquire wrapper.Root after RefreshComObjects. Disposal, stale binding, wrong-thread and nested Root/refresh/disposal work are rejected before native/Windows access. Raw-object calls and external UI changes bypass the guard. Native COM queries can block; no worker dispatch, automatic rollback or alternate-instance fallback occurs. A native/Windows exception can follow partial effects.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation state, live handle, PID or window ownership is invalid, or Windows rejects the request.</exception>
        /// <exception cref="PlatformNotSupportedException">The default window transport is invoked outside Windows.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">A live native handle or process query fails; its original HRESULT propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks GetMainWindowHandle or GetProcessId.</exception>
        /// <exception cref="System.DllNotFoundException">The required Windows window-management library is unavailable.</exception>
        /// <exception cref="EntryPointNotFoundException">A required Windows window-management function is unavailable.</exception>
        /// <example>With an owned connected STAAD session on its constructing STA thread:<code>
        /// bool changeWindowState = false; // Enable only when this window change is intended.
        /// if (changeWindowState) wrapper.Root.MinimizeStaadMainWindow();
        /// </code></example>
        void MinimizeStaadMainWindow();

        /// <summary>Requests the maximized state for the main window reported by this bound STAAD root.</summary>
        /// <remarks>
        /// <para>Unlike cached GetMainWindowHandle, this reads the bound root's live window handle. After validating its pointer-sized shape, reads GetProcessId once and verifies the HWND with IsWindow and GetWindowThreadProcessId. Only a nonzero existing window owned by the reported positive native process ID receives ShowWindowAsync with command 3. This requests the maximized state. No foreground-focus guarantee or completed-state read-back is provided.</para>
        /// <para>This void request returns after Windows accepts the asynchronous show-state operation; acceptance does not certify that the state became visible. A zero/-1, malformed or out-of-range handle, invalid PID, missing/recycled window, ownership mismatch or rejected Windows request throws. Validation reduces cross-session mistakes but cannot eliminate handle recycling or external UI races after the checks. Unlike ShowApplication, this uses Windows show-state control rather than a native visibility request; it does not launch STAAD, change the active model, save, analyze, refresh cached metadata, poll or retry.</para>
        /// <para>Runs on the wrapper's constructing COM thread under the shared Root operation lease, using the captured session only. Reacquire wrapper.Root after RefreshComObjects. Disposal, stale binding, wrong-thread and nested Root/refresh/disposal work are rejected before native/Windows access. Raw-object calls and external UI changes bypass the guard. Native COM queries can block; no worker dispatch, automatic rollback or alternate-instance fallback occurs. A native/Windows exception can follow partial effects.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation state, live handle, PID or window ownership is invalid, or Windows rejects the request.</exception>
        /// <exception cref="PlatformNotSupportedException">The default window transport is invoked outside Windows.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">A live native handle or process query fails; its original HRESULT propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks GetMainWindowHandle or GetProcessId.</exception>
        /// <exception cref="System.DllNotFoundException">The required Windows window-management library is unavailable.</exception>
        /// <exception cref="EntryPointNotFoundException">A required Windows window-management function is unavailable.</exception>
        /// <example>With an owned connected STAAD session on its constructing STA thread:<code>
        /// bool changeWindowState = false; // Enable only when this window change is intended.
        /// if (changeWindowState) wrapper.Root.MaximizeStaadMainWindow();
        /// </code></example>
        void MaximizeStaadMainWindow();

        /// <summary>Requests the restored state for the main window reported by this bound STAAD root.</summary>
        /// <remarks>
        /// <para>Unlike cached GetMainWindowHandle, this reads the bound root's live window handle. After validating its pointer-sized shape, reads GetProcessId once and verifies the HWND with IsWindow and GetWindowThreadProcessId. Only a nonzero existing window owned by the reported positive native process ID receives ShowWindowAsync with command 9. This requests restoration from a minimized or maximized state. No foreground-focus guarantee or completed-state read-back is provided.</para>
        /// <para>This void request returns after Windows accepts the asynchronous show-state operation; acceptance does not certify that the state became visible. A zero/-1, malformed or out-of-range handle, invalid PID, missing/recycled window, ownership mismatch or rejected Windows request throws. Validation reduces cross-session mistakes but cannot eliminate handle recycling or external UI races after the checks. Unlike ShowApplication, this uses Windows show-state control rather than a native visibility request; it does not launch STAAD, change the active model, save, analyze, refresh cached metadata, poll or retry.</para>
        /// <para>Runs on the wrapper's constructing COM thread under the shared Root operation lease, using the captured session only. Reacquire wrapper.Root after RefreshComObjects. Disposal, stale binding, wrong-thread and nested Root/refresh/disposal work are rejected before native/Windows access. Raw-object calls and external UI changes bypass the guard. Native COM queries can block; no worker dispatch, automatic rollback or alternate-instance fallback occurs. A native/Windows exception can follow partial effects.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding/thread/operation state, live handle, PID or window ownership is invalid, or Windows rejects the request.</exception>
        /// <exception cref="PlatformNotSupportedException">The default window transport is invoked outside Windows.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">A live native handle or process query fails; its original HRESULT propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks GetMainWindowHandle or GetProcessId.</exception>
        /// <exception cref="System.DllNotFoundException">The required Windows window-management library is unavailable.</exception>
        /// <exception cref="EntryPointNotFoundException">A required Windows window-management function is unavailable.</exception>
        /// <example>With an owned connected STAAD session on its constructing STA thread:<code>
        /// bool changeWindowState = false; // Enable only when this window change is intended.
        /// if (changeWindowState) wrapper.Root.RestoreStaadMainWindow();
        /// </code></example>
        void RestoreStaadMainWindow();

        /// <summary>Sets the current model's input length and force units using existing enum codes.</summary>
        /// <param name="lengthUnit">Defined LengthInputUnit value: Inch=0, Feet=2, CentiMeter=3, Meter=4, MilliMeter=5, DeciMeter=6 or KiloMeter=7.</param>
        /// <param name="forceUnit">Defined ForceInputUnit value 0 through 7: KiloPound, Pound, KiloGram, MetricTon, Newton, KiloNewton, MegaNewton or DecaNewton.</param>
        /// <returns>This root adapter after the native void call returns without an exception.</returns>
        /// <remarks>
        /// <para>Validates both enums before one SetInputUnits call, sending integer length then
        /// force codes unchanged. The native API is void; no Boolean/numeric status is interpreted
        /// and no read-back is performed. Undefined enum values, including the unnamed length code 1,
        /// are rejected even though the installed vendor documentation lists 1 as another Feet code.</para>
        /// <para>This changes native input-unit state used by subsequent input operations. It does
        /// not rescale existing managed values, convert already-stored results, change the base
        /// unit system, save the model or restore previous units. A native failure may have partial
        /// effects; no rollback is supplied. Coordinate session access on the appropriate COM thread.</para>
        /// <para>Runs synchronously on the wrapper's constructing COM thread under one Root operation lease,
        /// including every poll, sleep and file operation. Reacquire wrapper.Root after RefreshComObjects.
        /// Disposed, replaced, wrong-thread and reentrant bindings are rejected before native or filesystem work.
        /// Does not refresh cached units or domain bindings; after a model transition, refresh explicitly outside
        /// this operation and reacquire adapters. The guard does not coordinate raw-object or external UI access.</para>
        /// <para>Wrapper unit properties and cached Root unit getters retain their prior snapshots. After this method
        /// returns, call wrapper.RefreshUnits() explicitly to reread units without replacing Root. The method
        /// returns this same adapter and never infers native success from a return value.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is in progress.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Either unit value is not defined by its public enum; rejected before native access.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native unit change fails; the original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The installed root lacks a compatible SetInputUnits signature.</exception>
        /// <example>
        /// With a connected wrapper on its constructing STA thread:
        /// <code>
        /// wrapper.Root.SetInputUnitsEx(StaadPro.Interop.Core.Enums.LengthInputUnit.Meter, StaadPro.Interop.Core.Enums.ForceInputUnit.KiloNewton);
        /// </code>
        /// </example>
        IOSRoot SetInputUnitsEx(LengthInputUnit lengthUnit, ForceInputUnit forceUnit);

        /// <summary>Polls this root until its reported full model path matches the requested path or the elapsed-time limit expires.</summary>
        /// <param name="fileFullPath">Non-null, non-whitespace Windows model path, normalized before polling; relative input uses the process working directory.</param>
        /// <param name="timeoutDurationInSeconds">Finite positive timeout in seconds within TimeSpan limits; default 60. Counts native polling time.</param>
        /// <param name="sleepIntervalInSeconds">Finite positive polling interval in seconds within TimeSpan limits; default 0.1. Sleeps are capped by the remaining budget.</param>
        /// <returns>True when a matching path is observed within the logical budget; false on timeout. This does not verify Geometry/Load readiness or analysis results.</returns>
        /// <remarks>
        /// <para>Checks immediately, then reads one native filename per poll with ordinal
        /// case-insensitive full-path comparison. An explicitly blank filename is a nonmatch.
        /// No open/save/close or application launch is performed. Native/read/path errors throw
        /// instead of being treated as a blank session or timeout.</para>
        /// <para>Uses a monotonic elapsed-time budget and synchronous waits on the caller's COM
        /// thread. It does not pump UI messages, offer cancellation or prevent native model changes
        /// after a match. A synchronous native call can exceed the timeout; the budget cannot abort it.
        /// Thread.Sleep resolution can exceed the last remaining interval. Serialize session access.</para>
        /// <para>Runs synchronously on the wrapper's constructing COM thread under one Root operation lease,
        /// including every poll, sleep and file operation. Reacquire wrapper.Root after RefreshComObjects.
        /// Disposed, replaced, wrong-thread and reentrant bindings are rejected before native or filesystem work.
        /// Does not refresh cached units or domain bindings; after a model transition, refresh explicitly outside
        /// this operation and reacquire adapters. The guard does not coordinate raw-object or external UI access.</para>
        /// <para>Captures the process working directory before dispatch and uses it for the requested path and every
        /// relative native filename throughout this workflow. Filename identity does not prove domain readiness
        /// or validate the .STD contents. No new process, provider acquisition or alternate-session fallback occurs.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// another Root/lifecycle operation is in progress, or a required native filename is malformed.</exception>
        /// <exception cref="ArgumentNullException">fileFullPath is null.</exception>
        /// <exception cref="ArgumentException">The requested path is empty/whitespace, or requested/native text has invalid Windows syntax.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A duration is nonpositive, NaN, infinite or outside supported limits; rejected before native access.</exception>
        /// <exception cref="NotSupportedException">A path format is unsupported.</exception>
        /// <exception cref="System.IO.PathTooLongException">A path exceeds runtime limits.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native polling fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible filename API.</exception>
        /// <example><code>
        /// bool identified = wrapper.Root.AwaitModelHandle(@"C:\Models\Frame.v2.STD", 1, 0.01);
        /// Console.WriteLine(identified);
        /// </code></example>
        bool AwaitModelHandle(string fileFullPath, double timeoutDurationInSeconds = 60, double sleepIntervalInSeconds = 0.1);

        /// <summary>Requests opening a model once, then waits for this root to report that exact normalized model path.</summary>
        /// <param name="staadFileFullPath">Non-null, non-whitespace Windows model path, normalized before the native request.</param>
        /// <param name="sleepInterval">Positive polling interval in milliseconds, capped by the remaining budget.</param>
        /// <param name="maxWaitTime">Positive total logical limit in seconds, including the native open call; default 600.</param>
        /// <returns>True when the exact normalized path is observed within the budget; false on timeout. A false result does not undo the open request or prove that the model stayed closed.</returns>
        /// <remarks>
        /// <para>Validates all input before one OpenSTAADFile request, then checks immediately and
        /// polls native full paths with ordinal case-insensitive comparison. Uses elapsed seconds rather than an iteration count. A similarly named model in
        /// another folder is not a match. It requests opening even if the target was already open.</para>
        /// <para>Model switching can affect unsaved work and captured interfaces; native dialogs may
        /// block. No auto-save, rollback, new process creation or Geometry/Load readiness check occurs.
        /// Uses synchronous caller-thread sleeps without UI pumping or cancellation. A monotonic
        /// budget cannot interrupt a blocked COM call. Native and path failures propagate unchanged.</para>
        /// <para>Runs synchronously on the wrapper's constructing COM thread under one Root operation lease,
        /// including every poll, sleep and file operation. Reacquire wrapper.Root after RefreshComObjects.
        /// Disposed, replaced, wrong-thread and reentrant bindings are rejected before native or filesystem work.
        /// Does not refresh cached units or domain bindings; after a model transition, refresh explicitly outside
        /// this operation and reacquire adapters. The guard does not coordinate raw-object or external UI access.</para>
        /// <para>Captures the process working directory before dispatch and uses it for the requested path and every
        /// relative native filename throughout this workflow. Filename identity does not prove domain readiness
        /// or validate the .STD contents. No new process, provider acquisition or alternate-session fallback occurs.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// another Root/lifecycle operation is in progress, or a required native filename is malformed.</exception>
        /// <exception cref="ArgumentNullException">staadFileFullPath is null.</exception>
        /// <exception cref="ArgumentException">The requested path is empty/whitespace, or requested/native text has invalid Windows syntax.</exception>
        /// <exception cref="ArgumentOutOfRangeException">sleepInterval or maxWaitTime is nonpositive; no native call occurs.</exception>
        /// <exception cref="NotSupportedException">A path format is unsupported.</exception>
        /// <exception cref="System.IO.PathTooLongException">A path exceeds runtime limits.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native open or polling fails; the original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible open or filename API.</exception>
        /// <example><code>
        /// bool identified = wrapper.Root.OpenStaadModel(@"C:\Models\Frame.v2.STD", 10, 1);
        /// Console.WriteLine(identified);
        /// </code></example>
        bool OpenStaadModel(string staadFileFullPath, int sleepInterval, int maxWaitTime = 600);

        /// <summary>Requests a silent close only when this root reports a non-whitespace current model filename.</summary>
        /// <remarks>
        /// <para>Reads one full native filename. Explicit empty/whitespace text causes no mutation.
        /// Otherwise calls native SetSilentMode with integer 1, then CloseSTAADFile once. The previous silent setting is ignored and not restored.</para>
        /// <para>Does not explicitly save unsaved work, verify closing, terminate STAAD or dispose
        /// the wrapper. Native dialogs can block despite silent mode, and captured model interfaces
        /// can become unusable after closing. A filename can change after the read; callers must
        /// coordinate UI/session access on the correct COM thread. Native failures propagate,
        /// and a failure after setting silent mode can leave that setting changed.</para>
        /// <para>Runs synchronously on the wrapper's constructing COM thread under one Root operation lease,
        /// including every poll, sleep and file operation. Reacquire wrapper.Root after RefreshComObjects.
        /// Disposed, replaced, wrong-thread and reentrant bindings are rejected before native or filesystem work.
        /// Does not refresh cached units or domain bindings; after a model transition, refresh explicitly outside
        /// this operation and reacquire adapters. The guard does not coordinate raw-object or external UI access.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// another Root/lifecycle operation is in progress, or a required native filename is malformed.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Filename, silent-mode or close dispatch fails; the original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible filename, silent-mode or close API.</exception>
        /// <example><code>
        /// wrapper.Root.CloseCurrentStaadModel(); // explicitly requests closing; does not auto-save
        /// </code></example>
        void CloseCurrentStaadModel();

        /// <summary>Closes the matching current model, replaces its existing file contents and requests reopening after bounded close/lock-release waiting.</summary>
        /// <param name="filePath">Non-null, non-whitespace Windows path to the existing current model; normalized before dispatch.</param>
        /// <param name="fileContents">Non-null list snapshotted before mutation. Lines are written as UTF-8 without BOM using platform newlines; null entries become empty lines.</param>
        /// <param name="sleepTimeMilliseconds">Positive retry interval in milliseconds; capped by the remaining close/lock-release budget. Default 200.</param>
        /// <param name="maxTimeMilliseconds">Nonnegative close/lock-release wait limit in milliseconds after the close request. Default 5000; zero permits only an immediate check.</param>
        /// <returns>True when replacement completed and the target was identified or its reopen request returned; false for a nonmatching model, wait expiry or an intervening model switch. False can follow native side effects or a completed file replacement.</returns>
        /// <remarks>
        /// <para>Validates input, snapshots lines and requires the current normalized path to match
        /// the target before closing. Rejects missing, directory or read-only targets before mutation.
        /// Sets native silent mode to 1 without restoring it, requests closing, then waits for an
        /// explicitly empty filename and exclusive read/write availability. Only Windows sharing/
        /// lock violations are retried; other filesystem errors propagate.</para>
        /// <para>Publishes a complete exclusively created sibling temporary file using File.Replace,
        /// without truncation fallback. Only owned temporary cleanup is attempted. Rechecks blank
        /// identity before writing and avoids reopening over a different intervening model. The
        /// exclusive probe is a snapshot, not a transaction with external writers or the STAAD UI.
        /// Native and filesystem races remain possible; replacement does not preserve original encoding.</para>
        /// <para>The budget does not bound close/write/open native calls or force-abort COM. No
        /// explicit save, backup, rollback, directory creation, process launch or reopen-readiness
        /// verification occurs. Errors propagate; a failure can leave silent mode enabled, the model
        /// closed or the replacement published. Serialize access on the correct session COM thread.</para>
        /// <para>Runs synchronously on the wrapper's constructing COM thread under one Root operation lease,
        /// including every poll, sleep and file operation. Reacquire wrapper.Root after RefreshComObjects.
        /// Disposed, replaced, wrong-thread and reentrant bindings are rejected before native or filesystem work.
        /// Does not refresh cached units or domain bindings; after a model transition, refresh explicitly outside
        /// this operation and reacquire adapters. The guard does not coordinate raw-object or external UI access.</para>
        /// <para>Captures the process working directory before dispatch and uses it for the requested path and every
        /// relative native filename throughout this workflow. Filename identity does not prove domain readiness
        /// or validate the .STD contents. No new process, provider acquisition or alternate-session fallback occurs.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// another Root/lifecycle operation is in progress, or a required native filename is malformed.</exception>
        /// <exception cref="ArgumentNullException">filePath or fileContents is null.</exception>
        /// <exception cref="ArgumentException">A requested path is empty/whitespace, or requested/native path syntax is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">sleepTimeMilliseconds is nonpositive or maxTimeMilliseconds is negative.</exception>
        /// <exception cref="NotSupportedException">A path or filesystem operation is unsupported.</exception>
        /// <exception cref="System.IO.IOException">The target is missing/a directory or a non-lock filesystem operation fails; original failures propagate.</exception>
        /// <exception cref="UnauthorizedAccessException">The target is read-only or permissions prevent required access.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native identity, silent-mode, close or open dispatch fails.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a required native signature.</exception>
        /// <example><code>
        /// var editedLines = new System.Collections.Generic.List&lt;string&gt; { "STAAD SPACE", "UNIT METER KN", "FINISH" };
        /// bool requested = wrapper.Root.CloseWriteReopenStaadFile(@"C:\Models\Frame.v2.STD", editedLines);
        /// Console.WriteLine(requested); // explicit file replacement; no reopen-readiness guarantee
        /// </code></example>
        bool CloseWriteReopenStaadFile(string filePath, List<string> fileContents, int sleepTimeMilliseconds = 200, int maxTimeMilliseconds = 5000);

        /// <summary>Requests analysis of the currently opened STAAD model with explicit engine-dialog and waiting options.</summary>
        /// <param name="silentMode">Defined SilentMode code 0/1 controlling supported engine warnings; no separate application SetSilentMode call is made.</param>
        /// <param name="hiddenMode">Defined HiddenMode code 0/1 controlling the analysis-engine dialog.</param>
        /// <param name="waitMode">Defined WaitAnalysisMode: Return=0 requests immediate return; Wait=1 requests native completion waiting.</param>
        /// <returns>The native integer analysis status as AnalysisResult: -1 terminated, 0 general error, 1 in progress, 2 completed successfully, 3 warnings, 4 errors or 5 not performed. Unrecognized Int32 codes are preserved as undefined enum values.</returns>
        /// <remarks>
        /// <para>Validates all flags before one AnalyzeEx call with integer silent/hidden/wait codes
        /// in that order. Accepts only supported CLR integer status values within Int32 range; null,
        /// Boolean, floating-point, decimal and string responses throw rather than being coerced
        /// into success. Defined native failure/in-progress codes are returned, not converted to exceptions.</para>
        /// <para>This starts native analysis and may generate/change analysis output files and display
        /// dialogs. No managed pre-save, target/readiness check, extra SetSilentMode call, result-file
        /// retrieval, retry, cancellation, timeout or rollback is supplied. Return mode does not mean
        /// completion; wait mode can block. After Return mode, the engine may continue running outside the managed lease.
        /// Silent/hidden flags do not guarantee unattended execution.
        /// Serialize calls on the correct COM thread and explicitly inspect the returned status.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one shared Root operation lease. Reacquire
        /// wrapper.Root after RefreshComObjects. Disposed, replaced, wrong-thread and reentrant Root/refresh/disposal
        /// access is rejected before native dispatch. The lease ends when this operation returns.
        /// Cached units, metadata and domain bindings are not refreshed. Raw-object and external UI access bypass
        /// this guard; it does not serialize a different wrapper or prevent later native state changes.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// another Root/lifecycle operation is active, or required native output is malformed or unavailable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Any flag is undefined; rejected before analysis dispatch.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native analysis dispatch fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible AnalyzeEx signature.</exception>
        /// <example><code>
        /// bool runAnalysis = false; // Enable only when this model should be analyzed.
        /// if (runAnalysis)
        /// {
        ///     var status = wrapper.Root.AnalyzeEx(StaadPro.Interop.Core.Enums.SilentMode.Enable,
        ///         StaadPro.Interop.Core.Enums.HiddenMode.Enable, StaadPro.Interop.Core.Enums.WaitAnalysisMode.Return);
        ///     Console.WriteLine(status); // Inspect status; Return does not mean completion.
        /// }
        /// </code></example>
        AnalysisResult AnalyzeEx(SilentMode silentMode, HiddenMode hiddenMode, WaitAnalysisMode waitMode);

        /// <summary>Requests analysis through the native Analyze entry point for the current model.</summary>
        /// <remarks><para>Calls native Analyze once with no arguments. This is the documented UI-equivalent analysis request;
        /// no analysis flags are supplied and no alternative entry point is substituted.
        /// Application silent-mode, license and native dialog behavior still apply.</para>
        /// <para>The native return is void. Normal return does not report analysis success, completion, result availability or persistence.
        /// Use IsAnalyzing/GetAnalysisStatus explicitly to observe state, or AnalyzeEx when its documented controls are needed.
        /// No save, polling, timeout, wrapper refresh, retry or rollback is added. The call may block or display UI.
        /// Use the acquired session's COM thread and serialize analysis and edits; an exception may follow a started analysis.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one shared Root operation lease. Reacquire
        /// wrapper.Root after RefreshComObjects. Disposed, replaced, wrong-thread and reentrant Root/refresh/disposal
        /// access is rejected before native dispatch. The lease ends when this operation returns.
        /// Cached units, metadata and domain bindings are not refreshed. Raw-object and external UI access bypass
        /// this guard; it does not serialize a different wrapper or prevent later native state changes.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// another Root/lifecycle operation is active, or required native output is malformed or unavailable.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native analysis request fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible Analyze member.</exception>
        /// <exception cref="System.Runtime.InteropServices.InvalidComObjectException">The captured COM wrapper is no longer associated with a usable native object.</exception>
        /// <example><code>
        /// bool runAnalysis = false; // enable only when analysis should actually be requested
        /// if (runAnalysis) wrapper.Root.Analyze();
        /// </code></example>
        void Analyze();

        /// <summary>Reads whether analysis is currently running in the bound STAAD application.</summary>
        /// <returns>True for native Boolean true or integral one; false for Boolean false or integral zero.</returns>
        /// <remarks><para>Calls native IsAnalyzing once. Other integers, null, strings, fractional numbers and unsupported shapes
        /// are rejected rather than interpreted as flags. A false value does not mean an analysis completed or succeeded;
        /// use GetAnalysisStatus to obtain the state of a particular model.</para>
        /// <para>This live predicate is separate from the wrapper metadata cache. It does not start or wait for analysis,
        /// query a filename, read result files, save, change silent mode or launch STAAD. Use the bound session's COM thread
        /// and coordinate shared access. The returned value is a snapshot; a native call may block.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one shared Root operation lease. Reacquire
        /// wrapper.Root after RefreshComObjects. Disposed, replaced, wrong-thread and reentrant Root/refresh/disposal
        /// access is rejected before native dispatch. The lease ends when this operation returns.
        /// Cached units, metadata and domain bindings are not refreshed. Raw-object and external UI access bypass
        /// this guard; it does not serialize a different wrapper or prevent later native state changes.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// another Root/lifecycle operation is active, or required native output is malformed or unavailable.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native predicate fails; the original exception propagates without retry.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The captured root lacks a compatible IsAnalyzing member.</exception>
        /// <example><code>
        /// bool isRunning = wrapper.Root.IsAnalyzing();
        /// Console.WriteLine(isRunning);
        /// </code></example>
        bool IsAnalyzing();

        /// <summary>Reads native analysis state, diagnostic counts and CPU time for the specified model.</summary>
        /// <param name="modelFilePath">Non-null, non-whitespace model path. Relative paths are resolved against the caller process's current directory before native access. No file existence or extension check is performed.</param>
        /// <returns>An immutable result containing the absolute queried path, exact native status, nullable recognized status,
        /// query success, completion state, and available warning count, error count and CPU time in seconds.</returns>
        /// <remarks><para>Calls GetAnalysisStatus once on the captured root, using internal Int32 reference buffers for warning/error
        /// counts and Double for CPU time. No reference parameters or native output buffers are exposed to callers.
        /// Native codes are -2 invalid path, -1 terminated, 0 general error, 1 running, 2 completed cleanly,
        /// 3 completed with warnings, 4 completed with errors and 5 not performed. Unknown signed Int32 codes remain inspectable.</para>
        /// <para>IsSuccessful means a recognized analysis state was retrieved (-1 or 1 through 5), not that analysis succeeded.
        /// Invalid-path, general-error and unknown-status results expose no output data. Counts and CPU time are required,
        /// nonnegative and finite for completed states 2 through 4. In other recognized states, written valid fields are retained
        /// independently; initial Int32.MinValue count buffers and a NaN time buffer represent unavailable fields. An unwritten
        /// time and an explicitly written NaN cannot be distinguished for these noncompleted states. Other malformed outputs throw.
        /// Numeric zero is valid data. Count/status consistency beyond these shape/range checks is not inferred.</para>
        /// <para>The model need not be the currently open model. No filename read, file opening, analysis, polling, save,
        /// result-file parsing or filesystem mutation occurs. Use the session's COM thread; serialize shared access.
        /// This is a live, sequential native observation, not a transaction or proof of current result freshness. No automatic retry
        /// or time limit is imposed; a native call may block.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one shared Root operation lease. Reacquire
        /// wrapper.Root after RefreshComObjects. Disposed, replaced, wrong-thread and reentrant Root/refresh/disposal
        /// access is rejected before native dispatch. The lease ends when this operation returns.
        /// Cached units, metadata and domain bindings are not refreshed. Raw-object and external UI access bypass
        /// this guard; it does not serialize a different wrapper or prevent later native state changes.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// another Root/lifecycle operation is active, or required native output is malformed or unavailable.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="modelFilePath"/> is null; rejected before native access.</exception>
        /// <exception cref="ArgumentException"><paramref name="modelFilePath"/> is blank or contains invalid Windows path characters.</exception>
        /// <exception cref="NotSupportedException">The requested path format is unsupported by the Windows Path API.</exception>
        /// <exception cref="System.IO.PathTooLongException">The requested path exceeds the limits enforced by the runtime.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native query fails; the original exception propagates without retry.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible native signature, including the required reference argument types.</exception>
        /// <example><code>
        /// var status = wrapper.Root.GetAnalysisStatus(@"C:\Models\Frame.STD");
        /// Console.WriteLine(status.NativeStatus);
        /// if (status.IsCompleted) Console.WriteLine(status.ErrorCount);
        /// </code></example>
        AnalysisStatusResult GetAnalysisStatus(string modelFilePath);

        /// <summary>Reads analysis status for the model path currently reported by this root session.</summary>
        /// <returns>The same immutable status/data contract as GetAnalysisStatus(string), for the path captured by one native filename read.</returns>
        /// <remarks><para>Reads the native full filename once, rejects an empty or whitespace-only filename, then queries
        /// that normalized path under the same lease. An unnamed session throws before any analysis-status query; use the explicit-path overload
        /// to query a known model without relying on an open filename. Relative native paths use the working directory captured before the filename read.</para>
        /// <para>Uses live native reads rather than the wrapper metadata cache. The model can change between the filename and status
        /// reads; the result identifies the path actually queried and does not guarantee that it remains the open model.
        /// Does not start analysis, open/save/close models, poll or launch STAAD. Use the bound session's COM thread and serialize
        /// shared access. No retries or bounded native-call completion are provided. IsSuccessful indicates state retrieval,
        /// including terminated, running, completed-with-errors and not-performed states; inspect Status for the analysis outcome.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one shared Root operation lease. Reacquire
        /// wrapper.Root after RefreshComObjects. Disposed, replaced, wrong-thread and reentrant Root/refresh/disposal
        /// access is rejected before native dispatch. The lease ends when this operation returns.
        /// Cached units, metadata and domain bindings are not refreshed. Raw-object and external UI access bypass
        /// this guard; it does not serialize a different wrapper or prevent later native state changes.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// another Root/lifecycle operation is active, or required native output is malformed or unavailable.</exception>
        /// <exception cref="ArgumentException">The reported model path contains invalid Windows path characters.</exception>
        /// <exception cref="NotSupportedException">The reported model path format is unsupported by the Windows Path API.</exception>
        /// <exception cref="System.IO.PathTooLongException">The reported path exceeds the limits enforced by the runtime.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">A native filename or status query fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The captured root lacks a compatible filename or analysis-status member.</exception>
        /// <example><code>
        /// var status = wrapper.Root.GetAnalysisStatus();
        /// Console.WriteLine(status.ModelFilePath);
        /// Console.WriteLine(status.Status);
        /// </code></example>
        AnalysisStatusResult GetAnalysisStatus();

        /// <summary>Requests changing only the current model's input length unit.</summary>
        /// <param name="lengthUnit">LengthInputUnit identifying native code 0 through 7. Named values are Inch=0, Feet=2, CentiMeter=3, Meter=4, MilliMeter=5, DeciMeter=6 and KiloMeter=7. The documented feet alias (LengthInputUnit)1 is also accepted and dispatched unchanged.</param>
        /// <remarks><para>Validates the complete unit argument before one native SetInputUnitForLength call with an Int32 code.
        /// Does not route through the combined unit setter, read or set force units, or change the managed unit cache.
        /// Unlike this method, the existing SetInputUnitsEx retains its named-enum validation and still rejects unnamed code 1.</para>
        /// <para>The native operation is void: normal return means dispatch completed, not verified read-back or persistence.
        /// No save, analysis, structure update, value rescaling, base-system change or rollback is promised or added by this library.
        /// On a wrapper-owned connection, explicitly call wrapper.RefreshUnits when fresh cached unit values are needed.
        /// Use the session's COM thread and serialize native mutations and refresh. No automatic retry occurs after an uncertain failure;
        /// the request may have taken effect before an exception. Native calls may block or display server UI.</para>
        /// <para>Uses the wrapper's constructing COM thread and current Root binding under one operation lease,
        /// including validation and any job-field reads. Reacquire wrapper.Root after RefreshComObjects.
        /// Disposal, stale/manual-noncurrent bindings, another thread and reentrant Root/refresh/disposal calls
        /// are rejected before native work. This guard does not coordinate another wrapper, raw-object access
        /// or external UI. Units, domain bindings and other cached metadata are not automatically refreshed.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is active.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lengthUnit"/> is outside native codes 0 through 7; rejected before native access.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native unit request fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible SetInputUnitForLength member.</exception>
        /// <example><code>
        /// bool changeLengthUnit = false; // enable only when the model should be changed
        /// if (changeLengthUnit) wrapper.Root.SetInputUnitForLength(StaadPro.Interop.Core.Enums.LengthInputUnit.Meter);
        /// </code></example>
        void SetInputUnitForLength(LengthInputUnit lengthUnit);

        /// <summary>Requests changing only the current model's input force unit.</summary>
        /// <param name="forceUnit">Defined ForceInputUnit: KiloPound=0, Pound=1, KiloGram=2, MetricTon=3, Newton=4, KiloNewton=5, MegaNewton=6 or DecaNewton=7.</param>
        /// <remarks><para>Validates the enum before one native SetInputUnitForForce call with its Int32 code.
        /// Does not read or set length units, invoke the combined unit setter or refresh cached wrapper properties.
        /// Explicitly call wrapper.RefreshUnits after the request when fresh cached unit information is needed.</para>
        /// <para>Native void completion does not verify the resulting unit or file persistence. The library adds no save,
        /// analysis, structure update, value rescaling, base-system change or transaction. Native failures may follow partial effects;
        /// no retry or rollback occurs. Use the session's COM thread and coordinate mutations and refresh.
        /// Native calls may block or display server UI.</para>
        /// <para>Uses the wrapper's constructing COM thread and current Root binding under one operation lease,
        /// including validation and any job-field reads. Reacquire wrapper.Root after RefreshComObjects.
        /// Disposal, stale/manual-noncurrent bindings, another thread and reentrant Root/refresh/disposal calls
        /// are rejected before native work. This guard does not coordinate another wrapper, raw-object access
        /// or external UI. Units, domain bindings and other cached metadata are not automatically refreshed.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is active.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="forceUnit"/> is not defined; rejected before native access.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native unit request fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible SetInputUnitForForce member.</exception>
        /// <example><code>
        /// bool changeForceUnit = false; // enable only when the model should be changed
        /// if (changeForceUnit) wrapper.Root.SetInputUnitForForce(StaadPro.Interop.Core.Enums.ForceInputUnit.KiloNewton);
        /// </code></example>
        void SetInputUnitForForce(ForceInputUnit forceUnit);

        /// <summary>Requests updating the current model's job name, client and originating engineer.</summary>
        /// <param name="jobName">Non-null job name text, sent unchanged. Empty or whitespace text is permitted.</param>
        /// <param name="jobClient">Non-null client text, sent unchanged. Empty or whitespace text is permitted.</param>
        /// <param name="engineerName">Non-null originating engineer name, sent unchanged in the native engineer position.</param>
        /// <remarks><para>Validates all three inputs before one native SetShortJobInfo call in job-name, client, engineer order.
        /// Preserves supplied Unicode, spaces and line breaks; native text limits and interpretation still apply.
        /// Empty input is sent as supplied, not replaced with another value. This does not establish the server's clearing or storage behavior.</para>
        /// <para>The native function is void: return indicates dispatch completion, not verified storage or persistence.
        /// No full-job metadata update, save, model opening/closing, analysis or read-back is added. Use GetShortJobInfo explicitly
        /// when read-back is required. Serialize shared model access on the session's COM thread.
        /// Native calls may block or display UI. Exceptions propagate without retry or rollback; side effects may already have occurred.</para>
        /// <para>Uses the wrapper's constructing COM thread and current Root binding under one operation lease,
        /// including validation and any job-field reads. Reacquire wrapper.Root after RefreshComObjects.
        /// Disposal, stale/manual-noncurrent bindings, another thread and reentrant Root/refresh/disposal calls
        /// are rejected before native work. This guard does not coordinate another wrapper, raw-object access
        /// or external UI. Units, domain bindings and other cached metadata are not automatically refreshed.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is active.</exception>
        /// <exception cref="ArgumentNullException">One of the three strings is null; all input validation precedes native access.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native metadata request fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible three-input SetShortJobInfo member.</exception>
        /// <example><code>
        /// bool updateJobMetadata = false; // enable only when the model should be changed
        /// if (updateJobMetadata) wrapper.Root.SetShortJobInfo("Frame study", "Client", "Engineer");
        /// </code></example>
        void SetShortJobInfo(string jobName, string jobClient, string engineerName);

        /// <summary>Requests updating short job metadata from an existing managed job-information object.</summary>
        /// <param name="jobInfo">Non-null ShortJobInfo with non-null Name, Client and Originator. Empty strings are permitted. A FullJobInfo is accepted through its inherited short fields; its other fields are not submitted.</param>
        /// <remarks><para>Reads each of the three mutable properties once into local string snapshots, validates them before native access,
        /// then submits one native three-string call within the same wrapper operation lease. Reuses the existing model class without creating a parallel write-options type.
        /// Editing the managed object alone does not update STAAD. Coordinate concurrent edits to obtain a coherent snapshot;
        /// these property reads are not an atomic transaction.</para>
        /// <para>Preserves text and native argument order. Normal void return indicates native dispatch completion only.
        /// No full-job update, date conversion, save, read-back, structure update or analysis occurs. Use the appropriate session COM thread;
        /// native failures propagate without automatic retry or rollback and may follow native side effects.</para>
        /// <para>Uses the wrapper's constructing COM thread and current Root binding under one operation lease,
        /// including validation and any job-field reads. Reacquire wrapper.Root after RefreshComObjects.
        /// Disposal, stale/manual-noncurrent bindings, another thread and reentrant Root/refresh/disposal calls
        /// are rejected before native work. This guard does not coordinate another wrapper, raw-object access
        /// or external UI. Units, domain bindings and other cached metadata are not automatically refreshed.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is active.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="jobInfo"/> is null; rejected before native access.</exception>
        /// <exception cref="ArgumentException">Name, Client or Originator is null; the exception identifies jobInfo and no native call is made.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native metadata request fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible SetShortJobInfo member.</exception>
        /// <example><code>
        /// var jobInfo = ShortJobInfo.From("Frame study", "Client", "Engineer");
        /// bool applyJobInfo = false; // enable only when the model should be changed
        /// if (applyJobInfo) wrapper.Root.SetShortJobInfo(jobInfo);
        /// </code></example>
        void SetShortJobInfo(ShortJobInfo jobInfo);

        /// <summary>Requests a native update of the current STAAD structure.</summary>
        /// <remarks><para>Invokes native UpdateStructure once with no arguments. The vendor reference describes a structure update
        /// but specifies no returned status, completion/readiness protocol, save behavior or detailed rebuild guarantees.
        /// A normal void return establishes dispatch completion only; it does not certify that pending edits are persisted or results are current.</para>
        /// <para>The library adds no analysis, save, model opening/closing, wrapper refresh, unit refresh, wait or read-back.
        /// This explicit native mutation may affect the current model; use the connected session's COM thread and coordinate edits.
        /// Native UI or a blocking call remains possible. Failures propagate without retry or rollback and can follow partial native effects.</para>
        /// <para>Uses the wrapper's constructing COM thread and current Root binding under one operation lease,
        /// including validation and any job-field reads. Reacquire wrapper.Root after RefreshComObjects.
        /// Disposal, stale/manual-noncurrent bindings, another thread and reentrant Root/refresh/disposal calls
        /// are rejected before native work. This guard does not coordinate another wrapper, raw-object access
        /// or external UI. Units, domain bindings and other cached metadata are not automatically refreshed.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is active.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native update request fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible UpdateStructure member.</exception>
        /// <exception cref="System.Runtime.InteropServices.InvalidComObjectException">The captured COM wrapper is no longer associated with a usable native object.</exception>
        /// <example><code>
        /// bool updateCurrentStructure = false; // enable only when a native update should be requested
        /// if (updateCurrentStructure) wrapper.Root.UpdateStructure();
        /// </code></example>
        void UpdateStructure();

        /// <summary>Requests updating all thirteen job-information fields on the currently bound model.</summary>
        /// <param name="jobName">Non-null job name, sent unchanged; empty text is permitted.</param>
        /// <param name="jobClient">Non-null client text, sent unchanged; empty text is permitted.</param>
        /// <param name="engineerName">Non-null originating engineer name, sent in the native engineer-name position.</param>
        /// <param name="engineerDate">Non-null engineer date text in a format accepted by the connected STAAD instance. Sent literally; no DateTime conversion or parsing is performed.</param>
        /// <param name="jobNumber">Non-null job number text, sent unchanged.</param>
        /// <param name="revision">Non-null revision text, sent unchanged.</param>
        /// <param name="part">Non-null job-part text, sent unchanged.</param>
        /// <param name="reference">Non-null job-reference text, sent unchanged.</param>
        /// <param name="checkerName">Non-null checker name, sent unchanged.</param>
        /// <param name="checkerDate">Non-null checker date text accepted by the native server; empty text is sent literally.</param>
        /// <param name="approverName">Non-null approver name, sent unchanged.</param>
        /// <param name="approverDate">Non-null approval date text accepted by the native server; empty text is sent literally.</param>
        /// <param name="comments">Non-null comment text, preserving Unicode, whitespace and line breaks.</param>
        /// <remarks><para>Validates every input for null before one native SetFullJobInfo call in documented order:
        /// name, client, engineer, engineer date, number, revision, part, reference, checker, checker date, approver, approval date, comments.
        /// Empty and whitespace strings are permitted and sent unchanged; native field limits and clearing behavior still apply.</para>
        /// <para>All dates are native text, not DATE values. No culture-dependent formatting, date guessing or model-factory reordering occurs.
        /// The existing FullJobInfo DateTime-based model is not automatically serialized; callers provide native date strings explicitly.
        /// Native acceptance of a date format and storage/persistence remain the server's responsibility.</para>
        /// <para>The native function returns void: normal return means dispatch completed, not verified storage or save.
        /// No automatic SaveModel, read-back, analysis, refresh, retry or rollback is added. A native error can occur after side effects.
        /// Use the session's COM thread and serialize shared edits. Native calls can block or display dialogs.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-Root operation lease,
        /// covering validation, input capture, native dispatch and output qualification. Reacquire wrapper.Root
        /// after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/lifecycle
        /// calls are rejected before native access. The guard does not serialize raw access, another wrapper
        /// or external UI. Unit snapshots and domain bindings are not automatically refreshed.</para></remarks>
        /// <exception cref="ArgumentNullException">Any text input is null; identifies that parameter and prevents native access.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native metadata update fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible thirteen-input SetFullJobInfo member.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is active.</exception>
        /// <example><code>
        /// bool updateFullJob = false; // enable only for an intended native model change
        /// if (updateFullJob)
        ///     wrapper.Root.SetFullJobInfo("Frame", "Client", "Engineer", "", "J-1", "A", "Part", "Ref", "Checker", "", "Approver", "", "Notes");
        /// </code></example>
        void SetFullJobInfo(string jobName, string jobClient, string engineerName, string engineerDate, string jobNumber, string revision, string part, string reference, string checkerName, string checkerDate, string approverName, string approverDate, string comments);

        /// <summary>Requests analysis using the documented native engine selector.</summary>
        /// <param name="engine">AnalysisEngine.StaadPro, native code -1. It is the only documented supported value and the managed default; other values are rejected before dispatch.</param>
        /// <remarks><para>Invokes native AnalyzeModel once with an Int32 -1 engine code. The optional managed default still sends that
        /// argument explicitly. The native void signature is confirmed by installed 2025/2026 interop metadata despite a help example
        /// assigning a return value. No success code is fabricated or interpreted.</para>
        /// <para>This operation is distinct from Analyze and AnalyzeEx. Normal return does not guarantee analysis completion or valid results.
        /// No automatic save, silent-mode change, analysis-state query, polling, wrapper refresh, retry or rollback occurs.
        /// Calls may block or display native UI. Use the session's COM thread; coordinate analysis and edits.
        /// A failure can follow native effects, including a launched analysis.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-Root operation lease,
        /// covering validation, input capture, native dispatch and output qualification. Reacquire wrapper.Root
        /// after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/lifecycle
        /// calls are rejected before native access. The guard does not serialize raw access, another wrapper
        /// or external UI. Unit snapshots and domain bindings are not automatically refreshed.</para></remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="engine"/> is not the documented STAAD.Pro engine; rejected before native access.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native engine request fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible AnalyzeModel member.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is active.</exception>
        /// <example><code>
        /// bool runEngine = false; // enable only when the engine should actually be requested
        /// if (runEngine) wrapper.Root.AnalyzeModel(); // sends the documented -1 engine argument
        /// </code></example>
        void AnalyzeModel(AnalysisEngine engine = AnalysisEngine.StaadPro);

        /// <summary>Reads the native success flag and qualified CONNECTED project information from this wrapper.Root.</summary>
        /// <returns>An immutable result containing IsSuccessful and project data. Project is non-null only after native success and validated string outputs; native false returns a result with null Project.</returns>
        /// <remarks><para>Calls GetCONNECTEDProjectInfo once with two internal VARIANT references initialized as distinguishable string buffers.
        /// Native Boolean values or integral 0/1 flags are accepted. On success, both outputs must be written strings;
        /// text, whitespace, Unicode and explicit empty strings are preserved without GUID coercion. Other flag/output shapes throw.
        /// On native false, partial, malformed or unwritten output buffers are not exposed as project data.</para>
        /// <para>No public reference parameters, tuples, login prompts, project assignment, network retries or automatic save are added.
        /// Native false does not prove why retrieval failed or that no association exists. Native exceptions remain exceptions.
        /// Successful retrieval does not verify cloud synchronization, membership, account credentials or continued association.
        /// Use the captured root on its COM thread and coordinate shared operations; calls may block or show server UI.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-Root operation lease,
        /// covering validation, input capture, native dispatch and output qualification. Reacquire wrapper.Root
        /// after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/lifecycle
        /// calls are rejected before native access. The guard does not serialize raw access, another wrapper
        /// or external UI. Unit snapshots and domain bindings are not automatically refreshed.</para></remarks>
        /// <exception cref="InvalidOperationException">The native flag is malformed or a successful response contains unwritten/non-string outputs.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native project query fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible two-output GetCONNECTEDProjectInfo signature.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is active.</exception>
        /// <example><code>
        /// var projectResult = wrapper.Root.GetConnectedProjectInfo();
        /// if (projectResult.IsSuccessful) Console.WriteLine(projectResult.Project.Name);
        /// </code></example>
        ConnectedProjectResult GetConnectedProjectInfo();

        /// <summary>Requests assigning the supplied CONNECTED project identity to the bound model.</summary>
        /// <param name="projectId">Non-null, non-whitespace project identifier. Preserved exactly; no GUID parsing or trimming occurs.</param>
        /// <param name="projectName">Non-null, non-whitespace project name. Preserved exactly, including surrounding spaces and Unicode.</param>
        /// <returns>The documented native success flag: true for Boolean true/integral one, false for Boolean false/integral zero.</returns>
        /// <remarks><para>Validates both inputs before one SetCONNECTEDProjectInfo call in ID/name order. Native unknown or malformed
        /// flags throw rather than becoming a successful assignment. Empty/whitespace values are intentionally rejected because
        /// clearing/unassigning semantics are not established; this API does not provide an implicit clearing operation.</para>
        /// <para>A true result is the native response, not verified cloud synchronization or read-back. No project creation, login,
        /// membership change, SaveModel, project query, retry or rollback is added. Native credential/network/licensing behavior still applies;
        /// calls may block or show server UI. Exceptions can follow native effects. Use the session's COM thread and serialize edits.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-Root operation lease,
        /// covering validation, input capture, native dispatch and output qualification. Reacquire wrapper.Root
        /// after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/lifecycle
        /// calls are rejected before native access. The guard does not serialize raw access, another wrapper
        /// or external UI. Unit snapshots and domain bindings are not automatically refreshed.</para></remarks>
        /// <exception cref="ArgumentNullException">Either input is null; identifies the parameter and prevents native access.</exception>
        /// <exception cref="ArgumentException">Either input is empty or whitespace-only; rejected before native access.</exception>
        /// <exception cref="InvalidOperationException">The native return is not a Boolean or integral zero/one flag.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native assignment fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible SetCONNECTEDProjectInfo member.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is active.</exception>
        /// <example><code>
        /// bool assignProject = false; // enable only with an intended project identity
        /// if (assignProject) Console.WriteLine(wrapper.Root.SetConnectedProjectInfo("project-id", "Project name"));
        /// </code></example>
        bool SetConnectedProjectInfo(string projectId, string projectName);

        /// <summary>Requests assigning CONNECTED project information from an immutable managed value.</summary>
        /// <param name="projectInfo">Non-null ConnectedProjectInfo whose ProjectId and Name contain non-whitespace text. Values obtained from a successful query can be reused after validation.</param>
        /// <returns>The native assignment flag, with the same meaning as the two-string overload.</returns>
        /// <remarks><para>Reuses the same data model returned by GetConnectedProjectInfo. Checks the object and fields before native access,
        /// then uses the shared internal assignment implementation under the same lease. The model itself permits empty query snapshots, but assignment rejects those fields.
        /// Does not mutate the supplied value, trim text, force GUID syntax or infer an unassignment.</para>
        /// <para>The native response does not certify cloud synchronization or persistence. No save, read-back, login, retry or rollback is added.
        /// Coordinate access on the session's COM thread; native calls may block, show UI or fail after effects.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-Root operation lease,
        /// covering validation, input capture, native dispatch and output qualification. Reacquire wrapper.Root
        /// after RefreshComObjects. Disposed, stale/manual-noncurrent, other-thread and reentrant Root/lifecycle
        /// calls are rejected before native access. The guard does not serialize raw access, another wrapper
        /// or external UI. Unit snapshots and domain bindings are not automatically refreshed.</para></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="projectInfo"/> is null.</exception>
        /// <exception cref="ArgumentException">The supplied identity or name is blank; identifies projectInfo and prevents native access.</exception>
        /// <exception cref="InvalidOperationException">The native return flag has an unsupported shape or value.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native assignment fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible native assignment member.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread,
        /// or another Root/lifecycle operation is active.</exception>
        /// <example><code>
        /// var projectInfo = wrapper.Root.GetConnectedProjectInfo().Project;
        /// bool reuseProject = false; // enable only when reassignment is intended
        /// if (reuseProject &amp;&amp; projectInfo != null) Console.WriteLine(wrapper.Root.SetConnectedProjectInfo(projectInfo));
        /// </code></example>
        bool SetConnectedProjectInfo(ConnectedProjectInfo projectInfo);
        /// <summary>Reads an opaque numeric process-handle value from the acquired OpenSTAAD root.</summary>
        /// <returns>A fresh immutable NativeProcessHandleValue containing the native bit pattern, without resource ownership or a Windows-handle usability guarantee.</returns>
        /// <remarks><para>Calls native GetProcessHandle once on this captured root. Accepts integral CLR values and IntPtr/UIntPtr
        /// representations only. Unsigned values are retained; signed negatives are sign-extended to an unsigned 64-bit bit pattern.
        /// Zero and all-one values are preserved without inventing validity or pseudo-handle semantics. The representation
        /// does not establish the server's actual handle width, lifetime, access rights or process context.</para>
        /// <para>This is diagnostic data, not an owning SafeHandle, PID, main-window handle or verified caller-process HANDLE.
        /// Do not close it or pass its numeric value to operating-system handle APIs. No process is opened, duplicated,
        /// enumerated or terminated. Use GetProcessId for process identification. Use the acquired COM thread and coordinate
        /// access; the snapshot does not prove connection liveness. There is no retry or global active-object lookup.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-Root operation lease,
        /// covering validation, input capture and native access. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/lifecycle calls are rejected before native access. Reacquire wrapper.Root after
        /// RefreshComObjects. This guard does not serialize raw native access, other wrappers or external UI.
        /// No metadata, unit snapshot or domain binding is automatically refreshed.</para></remarks>
        /// <exception cref="InvalidOperationException">The native response is null or has a nonintegral, Boolean, string, fractional or unsupported representation.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native query fails; the original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible GetProcessHandle member.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread, or another Root/lifecycle operation is active.</exception>
        /// <example><code>
        /// var handleValue = wrapper.Root.GetProcessHandle();
        /// Console.WriteLine(handleValue.RawValue); // diagnostic value only; do not close or use as a Windows HANDLE
        /// </code></example>
        NativeProcessHandleValue GetProcessHandle();

        /// <summary>Requests native creation of a new STD model in an unnamed STAAD session.</summary>
        /// <param name="filePath">Non-null, non-whitespace new .std model path. Relative input is resolved against the process working directory. The parent directory must exist; an existing file or directory at the target is rejected.</param>
        /// <param name="lengthUnit">Native length code 0 through 7, including the documented feet alias (LengthInputUnit)1. Codes are sent unchanged.</param>
        /// <param name="forceUnit">Defined ForceInputUnit value 0 through 7. Validated before filesystem or native access.</param>
        /// <remarks><para>Normalizes the path, validates units and the .std filename, and checks the existing parent and absent target.
        /// Rejects Windows device names. Reads GetSTAADFile once and requires empty/whitespace filename text, then rechecks
        /// target absence before one NewSTAADFile call with the absolute path and Int32 length/force codes. Access and path
        /// errors propagate; the library does not hide them behind File.Exists. A blank filename is not proof that the
        /// application has no unsaved work or is ready for creation.</para>
        /// <para>Prechecks are not atomic with the native creation request. Another process can create the target or switch the
        /// session after validation; native overwrite and loaded-model behavior are unspecified. Coordinate exclusive session
        /// and destination use. This method is not an atomic no-overwrite guarantee. It creates no placeholder file, parent
        /// directory, process or managed STD contents, and does not close or save another model.</para>
        /// <para>The native function returns void. Normal return means dispatch completed, not file persistence, requested units,
        /// active-model identity or readiness. No polling, read-back, cache refresh, save, retry or rollback is added.
        /// Refresh wrapper metadata explicitly when needed. Call on the session's COM thread; native UI may block the request.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-Root operation lease,
        /// covering validation, input capture and native access. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/lifecycle calls are rejected before native access. Reacquire wrapper.Root after
        /// RefreshComObjects. This guard does not serialize raw native access, other wrappers or external UI.
        /// No metadata, unit snapshot or domain binding is automatically refreshed.</para></remarks>
        /// <exception cref="ArgumentNullException">filePath is null; rejected before native access.</exception>
        /// <exception cref="ArgumentException">The path is blank, has invalid syntax, lacks a .std extension or names a Windows device.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A unit code is unsupported; rejected before filesystem or native access.</exception>
        /// <exception cref="InvalidOperationException">The bound session reports a nonblank model path or malformed/unwritten filename output.</exception>
        /// <exception cref="System.IO.IOException">The target exists, its parent is unavailable or a filesystem/path check fails.</exception>
        /// <exception cref="UnauthorizedAccessException">The caller cannot inspect the destination or its parent.</exception>
        /// <exception cref="NotSupportedException">The runtime rejects the path format.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The filename query or native creation request fails; partial native effects are possible.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible filename or three-input NewSTAADFile member.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread, or another Root/lifecycle operation is active.</exception>
        /// <example><code>
        /// bool createNewModel = false; // enable only in an intended unnamed session with an exclusively controlled destination
        /// if (createNewModel)
        ///     wrapper.Root.NewSTAADFile(@"C:\Models\NewFrame.std", StaadPro.Interop.Core.Enums.LengthInputUnit.Meter, StaadPro.Interop.Core.Enums.ForceInputUnit.KiloNewton);
        /// </code></example>
        void NewSTAADFile(string filePath, LengthInputUnit lengthUnit, ForceInputUnit forceUnit);

        /// <summary>Requests creating a named view using an explicitly supplied native creation flag.</summary>
        /// <param name="viewName">Non-null, non-whitespace view name, sent literally without trimming; Unicode and surrounding whitespace are retained.</param>
        /// <param name="creationFlag">Native Int32 creation flag whose meaning the caller has established for the connected build. All Int32 values are forwarded; the reviewed help does not define a valid domain, enum or default.</param>
        /// <remarks><para>Validates the name, then calls native CreateNamedView once with name and Int32 flag.
        /// Inspected managed interop declares two inputs and void return, unlike the help's additional nError
        /// parameter. This API follows that inspected signature and does not add an error buffer, guessed success result,
        /// default flag or retry with an alternate signature. Confirm the flag and signature on the actual target build.</para>
        /// <para>Normal return establishes dispatch completion only. Duplicate-name, overwrite, view contents, empty-model,
        /// persistence and flag semantics remain native decisions. No geometry is explicitly edited and no separate SaveModel,
        /// selection change, Create/Save substitution, refresh or rollback is added. Use the acquired session's COM thread;
        /// native calls may block, display UI or fail after effects. A raw flag is exposed because authoritative meanings
        /// are unavailable; no enum values or supported range are invented.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-Root operation lease,
        /// covering validation, input capture and native access. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/lifecycle calls are rejected before native access. Reacquire wrapper.Root after
        /// RefreshComObjects. This guard does not serialize raw native access, other wrappers or external UI.
        /// No metadata, unit snapshot or domain binding is automatically refreshed.</para></remarks>
        /// <exception cref="ArgumentNullException">viewName is null; rejected before native access.</exception>
        /// <exception cref="ArgumentException">viewName is empty or whitespace-only; rejected before native access.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native creation fails; the original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible two-input CreateNamedView member.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread, or another Root/lifecycle operation is active.</exception>
        /// <example><code>
        /// bool createView = false; // enable only after establishing the creation flag for this build
        /// int confirmedCreationFlag = 0; // replace with the verified flag; zero is not asserted to be a valid/default mode
        /// if (createView) wrapper.Root.CreateNamedView("Frame view", confirmedCreationFlag);
        /// </code></example>
        void CreateNamedView(string viewName, int creationFlag);

        /// <summary>Requests saving the current view under the supplied name through the native SaveNamedView entry point.</summary>
        /// <param name="viewName">Non-null, non-whitespace name, preserved exactly including Unicode and surrounding whitespace.</param>
        /// <remarks><para>Validates the name and calls native SaveNamedView once with that string. Installed 2025/2026
        /// managed interop declares a single input and void return; the help's additional nError parameter is inconsistent.
        /// This API follows the inspected one-input signature, without an error buffer, inferred success flag,
        /// alternate-signature retry or substitution through CreateNamedView. Verify actual native behavior per target build.</para>
        /// <para>Normal return does not certify that a view was saved, persisted to disk or had particular contents.
        /// Existing-name/overwrite, no-model and view-state behavior remain native decisions. This method is not SaveModel
        /// and adds no model-file save, selection changes, refresh, read-back or rollback. Use the bound COM thread
        /// and coordinate shared view/model operations; native calls may block, show UI or fail after effects.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-Root operation lease,
        /// covering validation, input capture and native access. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/lifecycle calls are rejected before native access. Reacquire wrapper.Root after
        /// RefreshComObjects. This guard does not serialize raw native access, other wrappers or external UI.
        /// No metadata, unit snapshot or domain binding is automatically refreshed.</para></remarks>
        /// <exception cref="ArgumentNullException">viewName is null; rejected before native access.</exception>
        /// <exception cref="ArgumentException">viewName is empty or whitespace-only; rejected before native access.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">Native view saving fails; the original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible one-input SaveNamedView member.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread, or another Root/lifecycle operation is active.</exception>
        /// <example><code>
        /// bool saveCurrentView = false; // enable only when this native view change is intended
        /// if (saveCurrentView) wrapper.Root.SaveNamedView("Frame view");
        /// </code></example>
        void SaveNamedView(string viewName);

        /// <summary>Explicitly requests termination of the STAAD application represented by this acquired root.</summary>
        /// <remarks><para>Calls native Quit once with no arguments. This targets the captured application instance and
        /// can affect unsaved work, every model/interface hosted by that instance and other clients sharing it.
        /// Save or discard work explicitly under the caller's policy before invoking this method. No silent-mode change,
        /// pre-save, model close, process kill, global lookup or other application-instance shutdown is added.</para>
        /// <para>The native return is void: normal return does not verify process exit. Native prompts or running analysis
        /// can delay or prevent termination. An exception can follow effects; no retry or rollback is attempted.
        /// Captured native interfaces may become unusable, but the managed wrapper is not automatically disposed or retargeted.
        /// OpenStaadWrapper.Dispose does not call this operation. Use the bound COM thread and coordinate all clients.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-Root operation lease,
        /// covering validation, input capture and native access. Disposed, stale/manual-noncurrent, other-thread
        /// and reentrant Root/lifecycle calls are rejected before native access. Reacquire wrapper.Root after
        /// RefreshComObjects. This guard does not serialize raw native access, other wrappers or external UI.
        /// No metadata, unit snapshot or domain binding is automatically refreshed.</para></remarks>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native shutdown request fails; the original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible no-argument Quit member.</exception>
        /// <exception cref="System.Runtime.InteropServices.InvalidComObjectException">The captured COM wrapper no longer represents a usable native object.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding is stale, the caller is on another thread, or another Root/lifecycle operation is active.</exception>
        /// <example><code>
        /// bool quitOwnedApplication = false; // enable only for an application whose shutdown the caller intends
        /// if (quitOwnedApplication) wrapper.Root.Quit();
        /// </code></example>
        void Quit();


        /// <summary>Requests removal of a named view through the inspected native entry point.</summary>
        /// <param name="viewName">Non-null, non-whitespace view name, preserved exactly without trimming.</param>
        /// <remarks><para>Validates the name, then calls RemoveNamedView once with the name and an Int32 zero in the
        /// required nError position. Installed 2025/2026 interop passes that parameter by value, unlike the help's reference
        /// output. Zero initializes this native slot; it is not interpreted as success. No output error code is observable
        /// through the inspected signature, so this managed method returns void rather than fabricating a result.</para>
        /// <para>Normal return means dispatch completed, not verified removal or disk persistence. Missing-name, current-view,
        /// prompts and native error behavior remain server decisions. No geometry deletion, SaveModel, read-back, alternate
        /// signature, retry or rollback is added. Use the bound COM thread and coordinate changes; effects may precede exceptions.
        /// The actual 2024 signature and native view behavior still require controlled verification.</para><para>Uses the wrapper's constructing COM thread and current Root operation lease throughout input
        /// validation, enumeration, native dispatch and output qualification. Disposal, stale/manual-noncurrent
        /// adapters, other threads and reentrant Root/refresh/disposal operations are rejected before native work.
        /// Reacquire wrapper.Root and its capabilities after RefreshComObjects, even when the native version
        /// is unchanged. No automatic unit or domain refresh occurs. Raw calls, other wrappers and external UI
        /// are outside this guard.</para></remarks>
        /// <exception cref="ArgumentNullException">viewName is null; rejected before dispatch.</exception>
        /// <exception cref="ArgumentException">viewName is empty or whitespace-only; rejected before dispatch.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native request fails; the original failure propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible two-input RemoveNamedView member.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The Root binding is stale, another thread is calling,
        /// or a Root/lifecycle operation is already in progress.</exception>
        /// <example><code>
        /// bool removeView = false; // enable only for an intended native view removal
        /// if (removeView) wrapper.Root.RemoveNamedView("Frame view");
        /// </code></example>
        void RemoveNamedView(string viewName);

        /// <summary>Requests modifying named-view membership using an owned snapshot of entity identifiers.</summary>
        /// <param name="viewName">Non-null, non-whitespace view name; text is preserved exactly.</param>
        /// <param name="entityIds">Non-null sequence of positive Int32 entity identifiers, enumerated once into a private zero-based Int32 array before native mutation. Order and duplicates are preserved; an empty array is forwarded rather than silently treated as a no-op.</param>
        /// <param name="entityType">Documented qualifier: Node=0, Beam=1, Plate=2, Solid=3 or Surface=4. Undefined values are rejected before enumeration/native access.</param>
        /// <param name="modificationFlag">Required native Int32 mode established by the caller for this build. The reviewed help supplies no meanings or valid range; every Int32 is sent unchanged, without a guessed enum or default.</param>
        /// <remarks><para>Validates name, sequence, qualifier and every identifier before one ModifyNamedView call.
        /// The inspected 2025/2026 signature has four inputs: name, Int32 array, qualifier and flag. It has no separate count
        /// or reference error output, unlike the help. The array carries its own length; no inconsistent caller count is accepted.
        /// Native return is void and no error/success result is fabricated. Input enumerator failures propagate before dispatch.</para>
        /// <para>The caller must establish mode and empty-array behavior on the actual build. Native entity existence,
        /// duplicate handling, membership rules and persistence are not inferred. No geometry mutation is explicitly requested,
        /// selection changes, SaveModel, read-back, rollback, signature fallback or retries are added. Native view effects may
        /// precede exceptions. Use the session's COM thread and coordinate changes; calls may block or display UI.
        /// Array marshaling, flag semantics and the 2024 signature still require controlled native validation.</para><para>Uses the wrapper's constructing COM thread and current Root operation lease throughout input
        /// validation, enumeration, native dispatch and output qualification. Disposal, stale/manual-noncurrent
        /// adapters, other threads and reentrant Root/refresh/disposal operations are rejected before native work.
        /// Reacquire wrapper.Root and its capabilities after RefreshComObjects, even when the native version
        /// is unchanged. No automatic unit or domain refresh occurs. Raw calls, other wrappers and external UI
        /// are outside this guard.</para></remarks>
        /// <exception cref="ArgumentNullException">viewName or entityIds is null.</exception>
        /// <exception cref="ArgumentException">The name is blank or a supplied identifier is nonpositive; rejected before mutation.</exception>
        /// <exception cref="ArgumentOutOfRangeException">entityType is undefined; rejected before enumeration or native access.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The native request fails; its original exception propagates.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The root lacks a compatible four-input ModifyNamedView member.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The Root binding is stale, another thread is calling,
        /// or a Root/lifecycle operation is already in progress.</exception>
        /// <example><code>
        /// bool modifyView = false; // enable only after establishing mode and array semantics for the actual build
        /// int confirmedMode = 0; // replace with a verified value; zero is not asserted to be a supported default
        /// if (modifyView) wrapper.Root.ModifyNamedView("Frame view", new[] { 1, 2 },
        ///     StaadPro.Interop.Core.Enums.Root.NamedViewEntityType.Beam, confirmedMode);
        /// </code></example>
        void ModifyNamedView(string viewName, IEnumerable<int> entityIds, NamedViewEntityType entityType, int modificationFlag);

        /// <summary>Gets the guarded gateway for acquiring optional Root operation interfaces.</summary>
        /// <value>A gateway bound to this exact Root adapter; access itself executes no native inspection or operation.</value>
        /// <remarks>Acquisition requires a reviewed exact native build and compatible member metadata.
        /// A grant does not certify runtime compatibility, model readiness, licensing or operation success.
        /// Gateway and granted interfaces are invalid after wrapper refresh or disposal; use the constructing
        /// COM thread outside another Root/lifecycle operation. Optional operations cannot be obtained by
        /// casting the common Root adapter. Raw access remains outside these managed guards.</remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The Root adapter is stale, another thread is calling,
        /// or a Root/lifecycle operation is already active.</exception>
        /// <example><code>
        /// var grant = wrapper.Root.Capabilities.GetAdinaExport();
        /// Console.WriteLine(grant.Availability);
        /// </code></example>
        IOSRootCapabilities Capabilities { get; }

        /// <summary>Retrieves local forces at both ends of the requested member/load-case pairs.</summary>
        /// <param name="beams">Non-null positive member IDs, enumerated once and deduplicated in first-seen order.</param>
        /// <param name="loadCases">Non-null positive load-case IDs, enumerated once and deduplicated in first-seen order.</param>
        /// <returns>A named batch containing nullable result availability, ordered per-end outcomes and qualified force records.
        /// Native false retains query identity with null force data; distinct identities are never collapsed by component equality.</returns>
        /// <remarks><para>Validates both inputs before native access and limits the Cartesian batch to 100,000 end queries.
        /// An empty input returns a successful empty batch with null availability and performs no native work.
        /// Otherwise reads the captured Output availability once; false returns an unsuccessful empty batch.
        /// Reads each distinct case title once from the captured Load binding, then queries load-case/member/start/end
        /// order with local-axis flag zero. Results are sequential observations, not a transaction.
        /// Every successful force response must supply six written finite Double values in Fx,Fy,Fz,Mx,My,Mz order.
        /// Values retain native result units; cached input units do not certify output units. No offset transfer is applied.
        /// The returned force models are editable detached data. Exceptions return no partial batch; ordinary false
        /// flags are represented per query and do not prevent later queries.</para><para>Runs on the wrapper's constructing COM thread under one current-binding operation lease, including
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

        /// <exception cref="ArgumentNullException">Either sequence is null.</exception>
        /// <exception cref="ArgumentException">An ID is nonpositive or the request exceeds the query budget.</exception>
        /// <example><code>
        /// var result = wrapper.Root.RetrieveMemberForces(new[] { 1, 2 }, new[] { 1 });
        /// foreach (var query in result.Queries)
        ///     if (query.IsSuccessful) Console.WriteLine(query.Forces);
        /// </code></example>
        MemberForcesResult RetrieveMemberForces(IEnumerable<int> beams, IEnumerable<int> loadCases);
    }
}
