using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Interfaces;
using StaadPro.Interop.Core.Services;
using StaadPro.Interop.Core.Services.Wrapper;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Wraps a bound OpenSTAAD root, its domain interfaces and cached application/model information.</summary>
    /// <remarks>Ported from the main wrapper with late-bound native objects and explicitly documented domain placeholders.
    /// Use the connected session's COM thread and serialize refresh/use. Unit properties are snapshots; call RefreshUnits
    /// after unit changes and RefreshComObjects after reopening a model. Construction/refresh query native metadata.
    /// Refresh and live disposal enforce the constructing thread and reject nested lifecycle operations.
    /// These guards do not make raw objects or domain methods safe for arbitrary threads.
    /// This class neither launches STAAD nor owns or terminates its process.</remarks>
    /// <example>
    /// With an acquired root object named rawOpenStaad, on the session's COM thread:
    /// <code>
    /// using (var wrapper = new OpenStaadWrapper(rawOpenStaad))
    /// {
    ///     Console.WriteLine(wrapper.StaadVersion?.Display); // native STAAD version
    ///     Console.WriteLine(wrapper.ApplicationVersion.Display); // host version
    /// }
    /// </code>
    /// </example>
    public class OpenStaadWrapper : IDisposable
    {
        private readonly IStaadModelServices _staadModelServices;
        private readonly WrapperOperationContext operationContext;
        private ApplicationVersion staadVersion;
        private bool disposed;

        /// <summary>Captures the native root, binds available interfaces and queries cached metadata.</summary>
        /// <param name="openStaad">The non-null root from the intended STAAD instance.</param>
        /// <param name="isDedicated">A caller-supplied dedication flag; does not transfer process ownership.</param>
        /// <remarks>Uses the default noninteractive host service and a window service bound to this exact root.
        /// Missing optional members are tolerated; native metadata failures can propagate. Call on the appropriate COM thread.</remarks>
        /// <exception cref="ArgumentNullException">openStaad is null.</exception>
        /// <exception cref="InvalidOperationException">A provided native version/window response is malformed, or a COM root is constructed outside an STA thread.</exception>
        /// <exception cref="COMException">A native operation fails, other than a missing optional member or a unit query using its documented fallback.</exception>
        public OpenStaadWrapper(object openStaad, bool isDedicated = false)
            : this(openStaad, isDedicated, new BoundStaadModelServices(openStaad), StaadInteropHostServices.Interaction)
        {
        }

        /// <summary>Creates a wrapper with explicitly supplied model-window and host-interaction services.</summary>
        /// <param name="openStaad">The non-null bound OpenSTAAD root.</param>
        /// <param name="isDedicated">The caller's dedication flag.</param>
        /// <param name="staadModelServices">Service supplying this instance's window handle; its other main-project operations are outside this port.</param>
        /// <param name="staadInteropInteractionService">Host interaction implementation; captured without displaying UI.</param>
        /// <remarks>Does not own/dispose injected services. Initializes all available bindings and cached properties synchronously on the calling COM thread.</remarks>
        /// <exception cref="ArgumentNullException">The root or either injected service is null.</exception>
        /// <exception cref="InvalidOperationException">Native metadata is malformed or contradictory, or the caller violates the constructing-thread or non-reentrancy contract.</exception>
        /// <exception cref="COMException">A required native metadata operation fails.</exception>
        public OpenStaadWrapper(object openStaad, bool isDedicated, IStaadModelServices staadModelServices, IStaadInteropInteractionService staadInteropInteractionService)
        {
            RawOpenStaad = openStaad ?? throw new ArgumentNullException(nameof(openStaad));
            operationContext = new WrapperOperationContext(openStaad);
            _staadModelServices = staadModelServices ?? throw new ArgumentNullException(nameof(staadModelServices));
            StaadInteropInteractionService = staadInteropInteractionService ?? throw new ArgumentNullException(nameof(staadInteropInteractionService));
            IsDedicated = isDedicated;
            using (operationContext.Enter()) SetPrivateProperties();
        }

        /// <summary>Indicates whether the caller designated this STAAD instance as dedicated.</summary>
        /// <remarks>This flag does not transfer process ownership or cause disposal to close STAAD.</remarks>
        public bool IsDedicated { get; private set; }

        /// <summary>Gets the acquired root OpenSTAAD automation object.</summary>
        /// <remarks>The original bound object, or null after disposal; no Bentley interop assembly is required.</remarks>
        public object RawOpenStaad { get; private set; }

        /// <summary>Gets the managed root adapter bound to RawOpenStaad.</summary>
        /// <remarks>Exposes cached metadata and live model/process observations on this wrapper's constructing thread.
        /// RefreshComObjects replaces the adapter; reacquire this property afterward. Null after disposal.</remarks>
        public IOSRoot Root { get; private set; }

        /// <summary>Gets the native Geometry automation interface.</summary>
        /// <remarks>Preserves the existing direct-root dispatch fallback when Geometry is missing.
        /// Null for an explicitly null native property or after disposal; availability of native functions is not certified.</remarks>
        public dynamic RawGeometry { get; private set; }

        /// <summary>Gets the managed Geometry adapter.</summary>
        /// <remarks>A binding-only placeholder; domain functions are planned separately. Null when the native interface is unavailable.</remarks>
        public IOSGeometry Geometry { get; private set; }

        /// <summary>Gets the native Load automation interface.</summary>
        /// <remarks>Preserves the existing direct-root dispatch fallback when Load is missing.
        /// Null for an explicitly null native property or after disposal; availability of native functions is not certified.</remarks>
        public dynamic RawLoad { get; private set; }

        /// <summary>Gets the managed Load adapter.</summary>
        /// <remarks>A binding-only placeholder; domain functions are planned separately. Null when the native interface is unavailable.</remarks>
        public IOSLoad Load { get; private set; }

        /// <summary>Gets the native Output automation interface.</summary>
        /// <remarks>Null when the optional interface is unavailable or after disposal.</remarks>
        public dynamic RawOutput { get; private set; }

        /// <summary>Gets the managed Output adapter.</summary>
        /// <remarks>A binding-only placeholder; domain functions are planned separately. Null when the native interface is unavailable.</remarks>
        public IOSOutput Output { get; private set; }

        /// <summary>Gets the native Property automation interface.</summary>
        /// <remarks>Null when the optional interface is unavailable or after disposal.</remarks>
        public dynamic RawProperty { get; private set; }

        /// <summary>Gets the managed Property adapter.</summary>
        /// <remarks>A binding-only placeholder; domain functions are planned separately. Null when the native interface is unavailable.</remarks>
        public IOSProperty Property { get; private set; }

        /// <summary>Gets the native Support automation interface.</summary>
        /// <remarks>Null when the optional interface is unavailable or after disposal.</remarks>
        public dynamic RawSupport { get; private set; }

        /// <summary>Gets the managed Support adapter.</summary>
        /// <remarks>A binding-only placeholder; domain functions are planned separately. Null when the native interface is unavailable.</remarks>
        public IOSSupport Support { get; private set; }

        /// <summary>Gets the native Design automation interface.</summary>
        /// <remarks>Null when the optional interface is unavailable or after disposal.</remarks>
        public dynamic RawDesign { get; private set; }

        /// <summary>Gets the managed Design adapter.</summary>
        /// <remarks>A binding-only placeholder; domain functions are planned separately. Null when the native interface is unavailable.</remarks>
        public IOSDesign Design { get; private set; }

        /// <summary>Gets the native Commands automation interface.</summary>
        /// <remarks>Binds the native singular Command property; null when unavailable or after disposal.</remarks>
        public dynamic RawCommands { get; private set; }

        /// <summary>Gets the managed Commands adapter.</summary>
        /// <remarks>A binding-only placeholder; domain functions are planned separately. Null when the native interface is unavailable.</remarks>
        public IOSCommands Commands { get; private set; }

        /// <summary>Gets the native Table automation interface.</summary>
        /// <remarks>Null when the optional interface is unavailable or after disposal.</remarks>
        public dynamic RawTable { get; private set; }

        /// <summary>Gets the managed Table adapter.</summary>
        /// <remarks>A binding-only placeholder; Table operations are planned separately. Null when the native interface is unavailable.</remarks>
        public IOSTable Table { get; private set; }

        /// <summary>Gets the native View automation interface.</summary>
        /// <remarks>Null when the optional interface is unavailable or after disposal.</remarks>
        public dynamic RawView { get; private set; }

        /// <summary>Gets the managed View adapter.</summary>
        /// <remarks>A binding-only placeholder; View operations are planned separately. Null when the native interface is unavailable.</remarks>
        public IOSView View { get; private set; }

        /// <summary>Gets the reserved IDEA StatiCa integration binding.</summary>
        /// <remarks>A placeholder created only when Geometry, Load and Output exist; it performs no force transformation or export.</remarks>
        public IOpenStaadIdeaStatiCaAdapter IdeaStatiCa { get; private set; }

        /// <summary>Gets the cached native STAAD.Pro application version.</summary>
        /// <remarks>Null when the root lacks the version member. Each access returns an independent copy so editing
        /// the mutable version model cannot alter this wrapper's observed native version. Refresh with RefreshComObjects.</remarks>
        public ApplicationVersion StaadVersion
        {
            get => staadVersion == null ? null : new ApplicationVersion
            {
                Major = staadVersion.Major, Update = staadVersion.Update,
                Revision = staadVersion.Revision, Build = staadVersion.Build
            };
            private set => staadVersion = value;
        }

        /// <summary>Gets the cached version of the entry/calling application assembly.</summary>
        /// <remarks>This is the host/client version, not the native STAAD version. Use StaadVersion for OpenSTAAD version selection.</remarks>
        public ApplicationVersion ApplicationVersion { get; private set; }

        /// <summary>Gets the cached STAAD family classification.</summary>
        /// <remarks>None when native version is unavailable; V8i for native major &lt;= 20, otherwise Connect. It does not certify features.</remarks>
        public ApplicationEdition StaadEdition { get; private set; }

        /// <summary>Gets the cached non-owning STAAD main-window handle.</summary>
        /// <remarks>For Connect, the default service reads the bound root; zero when unavailable. RefreshComObjects rereads it. A cached handle is not a lifetime or readiness guarantee.</remarks>
        public IntPtr StaadApplicationWindowHandle { get; private set; }

        /// <summary>Gets the cached native base unit system.</summary>
        /// <remarks>RefreshUnits and RefreshComObjects update this value. Unknown means the native member is unavailable or returned no value.</remarks>
        public BaseUnitSystem BaseUnitSystem { get; private set; }

        /// <summary>Gets the cached model input force unit.</summary>
        /// <remarks>Reads reference output before a usable native return. If neither parses, uses Kip for Imperial base units and kN otherwise.
        /// This compatibility fallback is not proof of an observed input unit. RefreshUnits updates the snapshot.</remarks>
        public ForceInputUnit InputForceUnit { get; private set; }

        /// <summary>Gets the cached model input length unit.</summary>
        /// <remarks>Reads reference output before a usable native return. If neither parses, uses Feet for Imperial base units and Meter otherwise.
        /// This compatibility fallback is not proof of an observed input unit. RefreshUnits updates the snapshot.</remarks>
        public LengthInputUnit InputLengthUnit { get; private set; }

        /// <summary>Gets the host interaction service captured during construction.</summary>
        /// <remarks>No selections or notifications are requested by wrapper construction or refresh.</remarks>
        public IStaadInteropInteractionService StaadInteropInteractionService { get; }

        /// <summary>Indicates whether the undisposed wrapper retains its root and any managed domain binding.</summary>
        /// <remarks>This managed state check makes no native call and does not establish model readiness or server liveness.</remarks>
        public bool IsConnected => !disposed && RawOpenStaad != null && (Geometry != null || Load != null || Output != null || Property != null || Support != null || Design != null || Commands != null || Table != null || View != null);

        /// <summary>Rebinds all native/managed interfaces and refreshes cached application, unit and window information.</summary>
        /// <remarks>Call after the reopened model's native database is ready, on the session's COM thread.
        /// Does not wait, open/reopen a file, validate readiness or launch STAAD. Replaces adapter instances;
        /// callers must reacquire wrapper properties rather than use previously captured adapters.
        /// Refresh is in place, not transactional; a propagated native failure may leave partially refreshed state.</remarks>
        /// <exception cref="ObjectDisposedException">The wrapper has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Native metadata is malformed or contradictory.</exception>
        /// <exception cref="COMException">A native reference or metadata request fails.</exception>
        /// <example>
        /// After the caller has reopened a model and allowed its database to initialize:
        /// <code>
        /// wrapper.RefreshComObjects();
        /// var geometry = wrapper.Geometry; // reacquire the replacement adapter
        /// </code>
        /// </example>
        public void RefreshComObjects()
        {
            ThrowIfDisposed();
            using (operationContext.Enter()) SetPrivateProperties();
        }

        /// <summary>Rereads and caches the model's base, force-input and length-input units.</summary>
        /// <remarks>Does not rebind adapters, reread versions/handles, save or mutate the model. Unit dispatch failures
        /// tolerate a parseable written output; otherwise the source wrapper's base-system fallback applies. Serialize with model changes on the session's COM thread.</remarks>
        /// <exception cref="ObjectDisposedException">The wrapper has been disposed.</exception>
        /// <exception cref="COMException">The native base-unit request fails.</exception>
        /// <exception cref="InvalidOperationException">A native base-unit result has an unsupported shape, or refresh is called on another thread or during another refresh/disposal operation.</exception>
        /// <example>
        /// After changing the native model's input units, with a connected wrapper:
        /// <code>
        /// wrapper.RefreshUnits();
        /// Console.WriteLine(wrapper.InputForceUnit);
        /// Console.WriteLine(wrapper.InputLengthUnit);
        /// </code>
        /// </example>
        public void RefreshUnits()
        {
            ThrowIfDisposed();
            using (operationContext.Enter()) UpdateUnitCache();
        }

        // Root operations share the lifecycle gate to reject stale bindings and nested native/lifecycle work.
        internal T UseRootBinding<T>(OSRootAdapter adapter, Func<OpenStaadWrapper, T> read)
        {
            ThrowIfDisposed();
            using (operationContext.Enter())
            {
                if (!ReferenceEquals(Root, adapter))
                    throw new InvalidOperationException("This Root binding was replaced by RefreshComObjects; reacquire wrapper.Root.");
                return read(this);
            }
        }

        internal T UseLoadBinding<T>(OSLoadAdapter adapter, Func<OpenStaadWrapper,T> operation)
        {
            ThrowIfDisposed();
            using(operationContext.Enter())
            {
                if(!ReferenceEquals(Load,adapter))throw new InvalidOperationException("This Load binding is not current; reacquire wrapper.Load.");
                return operation(this);
            }
        }

        internal T UseOutputBinding<T>(OSOutputAdapter adapter, Func<OpenStaadWrapper,T> operation)
        {
            ThrowIfDisposed();
            using(operationContext.Enter())
            {
                if(!ReferenceEquals(Output,adapter))throw new InvalidOperationException("This Output binding is not current; reacquire wrapper.Output.");
                return operation(this);
            }
        }

        /// <summary>Clears this wrapper's native and managed bindings without closing STAAD or a model.</summary>
        /// <remarks>Idempotent. Leaves cached metadata and injected host services available. Does not call FinalReleaseComObject
        /// or dispose caller-owned services. An undisposed wrapper must be disposed on its constructing thread, outside a refresh.
        /// A repeated disposal after completion is harmless. Managed Root observations reject disposed bindings;
        /// previously captured raw objects remain caller-managed and native ownership remains external.</remarks>
        /// <exception cref="InvalidOperationException">An undisposed wrapper is disposed on another thread or during a native refresh.</exception>
        public void Dispose()
        {
            if (disposed) return;
            using (operationContext.Enter())
            {
                disposed = true;
                Root = null;
                Geometry = null; Load = null; Output = null; Property = null; Support = null;
                Design = null; Commands = null; Table = null; View = null; IdeaStatiCa = null;
                RawGeometry = null; RawLoad = null; RawOutput = null; RawProperty = null; RawSupport = null;
                RawDesign = null; RawCommands = null; RawTable = null; RawView = null; RawOpenStaad = null;
                if (_staadModelServices is BoundStaadModelServices bound) bound.Detach();
            }
        }

        private void UpdateUnitCache()
        {
            BaseUnitSystem baseUnits = WrapperMetadataReader.ReadBaseUnit(RawOpenStaad);
            ForceInputUnit forceUnits = WrapperMetadataReader.ReadInputForceUnit(RawOpenStaad, baseUnits, staadVersion);
            LengthInputUnit lengthUnits = WrapperMetadataReader.ReadInputLengthUnit(RawOpenStaad, baseUnits, staadVersion);
            BaseUnitSystem = baseUnits;
            InputForceUnit = forceUnits;
            InputLengthUnit = lengthUnits;
        }

        private void SetPrivateProperties()
        {
            SetUnmanagedObjects();
            SetManagedObjects();
            StaadVersion = WrapperMetadataReader.ReadApplicationVersion(RawOpenStaad);
            StaadEdition = GetStaadApplicationEdition();
            ApplicationVersion = GetApplicationVersion();
            UpdateUnitCache();
            StaadApplicationWindowHandle = StaadEdition == ApplicationEdition.Connect
                ? _staadModelServices.GetStaadApplicationWindowHandle() : IntPtr.Zero;
        }

        private object ReadInterface(Func<dynamic, object> read, bool allowRootFallback = false)
        {
            try { return read(RawOpenStaad); }
            catch (RuntimeBinderException) { return allowRootFallback ? RawOpenStaad : null; }
            catch (COMException ex) when (BoundStaadModelServices.IsMissingMember(ex))
            { return allowRootFallback ? RawOpenStaad : null; }
        }

        private void SetUnmanagedObjects()
        {
            // Preserve the existing direct-root Geometry/Load fallback for legacy/test roots.
            RawGeometry = ReadInterface(root => root.Geometry, true);
            RawLoad = ReadInterface(root => root.Load, true);
            RawOutput = ReadInterface(root => root.Output);
            RawProperty = ReadInterface(root => root.Property);
            RawSupport = ReadInterface(root => root.Support);
            RawDesign = ReadInterface(root => root.Design);
            RawCommands = ReadInterface(root => root.Command);
            RawTable = ReadInterface(root => root.Table);
            RawView = ReadInterface(root => root.View);
        }

        private void SetManagedObjects()
        {
            Root = new OSRootAdapter(this);
            Geometry = RawGeometry != null ? new OSGeometryAdapter(this) : null;
            Load = RawLoad != null ? new OSLoadAdapter(this) : null;
            Output = RawOutput != null ? new OSOutputAdapter(this) : null;
            Property = RawProperty != null ? new OSPropertyAdapter(this) : null;
            Support = RawSupport != null ? new OSSupportAdapter(this) : null;
            Design = RawDesign != null ? new OSDesignAdapter(this) : null;
            Commands = RawCommands != null ? new OSCommandsAdapter(this) : null;
            Table = RawTable != null ? new OSTableAdapter(this) : null;
            View = RawView != null ? new OSViewAdapter(this) : null;
            IdeaStatiCa = Geometry != null && Load != null && Output != null ? new OpenStaadIdeaStatiCaAdapter(this) : null;
        }

        private ApplicationVersion GetApplicationVersion()
        {
            Assembly host = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
            return Models.ApplicationVersion.Parse(host.GetName().Version);
        }

        private ApplicationEdition GetStaadApplicationEdition()
            => staadVersion == null ? ApplicationEdition.None : staadVersion.Major <= 20 ? ApplicationEdition.V8i : ApplicationEdition.Connect;

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(OpenStaadWrapper));
        }
    }
}
