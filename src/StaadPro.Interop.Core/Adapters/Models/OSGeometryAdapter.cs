using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Geometry;

namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Provides guarded read access to model geometry IDs, node coordinates and selected nodes.</summary>
    public sealed class OSGeometryAdapter : OSBaseAdapter, IOSGeometry
    {
        /// <summary>Captures the native Geometry object from the supplied wrapper.</summary>
        /// <param name="wrapper">The wrapper exposing the required native binding.</param>
        /// <exception cref="ArgumentNullException">The wrapper is null.</exception>
        /// <exception cref="ArgumentException">The native Geometry binding is unavailable.</exception>
        public OSGeometryAdapter(OpenStaadWrapper wrapper) : base(wrapper)
        {
            ComObject = (object)wrapper.RawGeometry ?? throw new ArgumentException("The Geometry binding is unavailable.", nameof(wrapper));
        }

        /// <summary>Gets the captured native Geometry object for advanced, apartment-aware interoperation.</summary>
        /// <remarks>Raw access is outside managed operation validation. Do not use a captured binding after wrapper refresh/disposal.</remarks>
        public dynamic ComObject { get; }

        /// <summary>Reads the complete analytical member ID inventory of the current native model.</summary>
        /// <returns>A detached sequence of unique positive analytical member IDs in native order, or an empty sequence when the native count is zero. Re-enumerating the sequence makes no native calls.</returns>
        /// <remarks>
        /// <para>Reads <c>GetMemberCount</c> and accepts an integral count from zero to 1,000,000. For a positive count, supplies an Int32 SAFEARRAY to the void <c>GetBeamList</c> call. The output must contain exactly the reported number of positive, unique IDs. A replacement array or a nonzero lower bound is supported. Reads the count again after filling the list and throws if it changed.</para>
        /// <para>The inventory covers the entire model, irrespective of selection. It performs no entity hydration, sorting, retries or model changes. Count rechecking detects changes in size during the read. Edits that keep the same count can still affect the snapshot.</para>
        /// <para>Runs eagerly on the wrapper's constructing COM thread under one current-binding operation lease. Reacquire Geometry after refresh. The guard cannot coordinate raw COM access, other wrappers or external model edits.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter is stale or manually constructed, the caller is on another thread or reentrant, or native output is invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native error propagates, including its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// foreach (int id in wrapper.Geometry.GetAllBeamsList()) Console.WriteLine(id);
        /// </code></example>
        public IEnumerable<int> GetAllBeamsList() =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadIds(wrapper.RawGeometry, GeometryReader.Entity.Beams));

        /// <summary>Reads the complete node ID inventory of the current native model.</summary>
        /// <returns>A detached sequence of unique positive node IDs in native order, or an empty sequence when the native count is zero. Re-enumerating the sequence makes no native calls.</returns>
        /// <remarks>
        /// <para>Reads <c>GetNodeCount</c> and accepts an integral count from zero to 1,000,000. For a positive count, supplies an Int32 SAFEARRAY to the void <c>GetNodeList</c> call. The output must contain exactly the reported number of positive, unique IDs. A replacement array or a nonzero lower bound is supported. Reads the count again after filling the list and throws if it changed.</para>
        /// <para>The inventory covers the entire model, irrespective of selection. It performs no entity hydration, sorting, retries or model changes. Count rechecking detects changes in size during the read. Edits that keep the same count can still affect the snapshot.</para>
        /// <para>Runs eagerly on the wrapper's constructing COM thread under one current-binding operation lease. Reacquire Geometry after refresh. The guard cannot coordinate raw COM access, other wrappers or external model edits.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter is stale or manually constructed, the caller is on another thread or reentrant, or native output is invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native error propagates, including its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// foreach (int id in wrapper.Geometry.GetAllNodesList()) Console.WriteLine(id);
        /// </code></example>
        public IEnumerable<int> GetAllNodesList() =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadIds(wrapper.RawGeometry, GeometryReader.Entity.Nodes));

        /// <summary>Reads the complete plate ID inventory of the current native model.</summary>
        /// <returns>A detached sequence of unique positive plate IDs in native order, or an empty sequence when the native count is zero. Re-enumerating the sequence makes no native calls.</returns>
        /// <remarks>
        /// <para>Reads <c>GetPlateCount</c> and accepts an integral count from zero to 1,000,000. For a positive count, supplies an Int32 SAFEARRAY to the void <c>GetPlateList</c> call. The output must contain exactly the reported number of positive, unique IDs. A replacement array or a nonzero lower bound is supported. Reads the count again after filling the list and throws if it changed.</para>
        /// <para>The inventory covers the entire model, irrespective of selection. It performs no entity hydration, sorting, retries or model changes. Count rechecking detects changes in size during the read. Edits that keep the same count can still affect the snapshot.</para>
        /// <para>Runs eagerly on the wrapper's constructing COM thread under one current-binding operation lease. Reacquire Geometry after refresh. The guard cannot coordinate raw COM access, other wrappers or external model edits.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter is stale or manually constructed, the caller is on another thread or reentrant, or native output is invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native error propagates, including its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// foreach (int id in wrapper.Geometry.GetAllPlatesList()) Console.WriteLine(id);
        /// </code></example>
        public IEnumerable<int> GetAllPlatesList() =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadIds(wrapper.RawGeometry, GeometryReader.Entity.Plates));

        /// <summary>Reads the complete solid ID inventory of the current native model.</summary>
        /// <returns>A detached sequence of unique positive solid IDs in native order, or an empty sequence when the native count is zero. Re-enumerating the sequence makes no native calls.</returns>
        /// <remarks>
        /// <para>Reads <c>GetSolidCount</c> and accepts an integral count from zero to 1,000,000. For a positive count, supplies an Int32 SAFEARRAY to the void <c>GetSolidList</c> call. The output must contain exactly the reported number of positive, unique IDs. A replacement array or a nonzero lower bound is supported. Reads the count again after filling the list and throws if it changed.</para>
        /// <para>The inventory covers the entire model, irrespective of selection. It performs no entity hydration, sorting, retries or model changes. Count rechecking detects changes in size during the read. Edits that keep the same count can still affect the snapshot.</para>
        /// <para>Runs eagerly on the wrapper's constructing COM thread under one current-binding operation lease. Reacquire Geometry after refresh. The guard cannot coordinate raw COM access, other wrappers or external model edits.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter is stale or manually constructed, the caller is on another thread or reentrant, or native output is invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native error propagates, including its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// foreach (int id in wrapper.Geometry.GetAllSolidsList()) Console.WriteLine(id);
        /// </code></example>
        public IEnumerable<int> GetAllSolidsList() =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadIds(wrapper.RawGeometry, GeometryReader.Entity.Solids));

        /// <summary>Reads one node's global coordinates from the current native model.</summary>
        /// <param name="nId">The positive analytical node number. Zero represents an absent node and skips native dispatch.</param>
        /// <returns>A new detached <see cref="Node"/> with the requested ID and three finite coordinates in the length units supplied by OpenSTAAD, or <see langword="null"/> for zero. Editing the returned node changes managed metadata only.</returns>
        /// <remarks>
        /// <para>Uses the void <c>GetNodeCoordinates</c> call and requires it to write three finite Double values. Zero and negative coordinate components are valid. If a positive node number cannot be read, the native error or the invalid-output exception propagates. No coordinate conversion, enumeration, separate existence query or model mutation occurs.</para>
        /// <para>Runs eagerly on the wrapper's constructing COM thread under one current-binding operation lease. Reacquire Geometry after refresh. The guard cannot coordinate raw COM access, other wrappers or external model edits.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="nId"/> is negative.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter is stale or manually constructed, the caller is on another thread or reentrant, or native output is invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native error propagates, including its HRESULT.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// var node = wrapper.Geometry.GetNode(1);
        /// Console.WriteLine($"{node.Id}: ({node.X}, {node.Y}, {node.Z})");
        /// </code></example>
        public Node GetNode(int nId) =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadNode(wrapper.RawGeometry, nId));

        /// <summary>Finds the first supplied node whose ID matches the requested number.</summary>
        /// <param name="nId">The exact ID to match in the supplied sequence. Zero and negative IDs can match caller-supplied metadata.</param>
        /// <param name="fromModelNodes">A non-null sequence of supplied nodes. Enumeration stops at the first match.</param>
        /// <returns>The matching supplied <see cref="Node"/> instance, or <see langword="null"/> if no ID matches. The method returns its coordinates and identity unchanged.</returns>
        /// <remarks>
        /// <para>Compares the stored IDs in sequence order and preserves the first matching reference even when IDs repeat. Performs a managed lookup without calling OpenSTAAD or converting coordinates.</para>
        /// <para>Runs under one current Geometry binding operation on the wrapper's constructing COM thread. Reacquire the adapter after refresh. Raw native access, other wrappers and external UI are outside the guard.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">The supplied sequence is null.</exception>
        /// <exception cref="ArgumentException">A visited sequence element is null.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The adapter is stale or manually constructed, or the caller is on another thread or reentrant.</exception>
        /// <example><code>
        /// var nodes = new[] { new StaadPro.Interop.Core.Entities.Node(3, 2, 0, 0) };
        /// var node = wrapper.Geometry.GetNode(3, nodes);
        /// if (node != null) Console.WriteLine(node.X);
        /// </code></example>
        public Node GetNode(int nId, IEnumerable<Node> fromModelNodes) =>
            Wrapper.UseGeometryBinding(this, _ => GeometryReader.FindNode(nId, fromModelNodes));

        /// <summary>Reads the IDs of nodes currently selected in the native model.</summary>
        /// <returns>A fresh set of unique positive selected-node IDs. An empty selection returns an empty set. Set iteration order is unspecified.</returns>
        /// <remarks>
        /// <para>Reads an integral GetNoOfSelectedNodes count from zero through 1,000,000. A positive count uses void GetSelectedNodes with an Int32 SAFEARRAY and integer zero for native selection order. The array must contain the expected number of unique positive Int32 IDs. Accepts a nonzero lower bound or replacement array and rechecks the count before publishing results. The count check can detect a size change. It cannot certify an atomic snapshot of the selection.</para>
        /// <para>Runs under one current Geometry binding operation on the wrapper's constructing COM thread. Reacquire the adapter after refresh. Raw native access, other wrappers and external UI are outside the guard.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, or native count, array or coordinate output is malformed or inconsistent.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native exception and HRESULT are preserved.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// foreach (int id in wrapper.Geometry.GetSelectedNodesIds()) Console.WriteLine(id);
        /// </code></example>
        public HashSet<int> GetSelectedNodesIds() =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadIds((object)wrapper.RawGeometry, GeometryReader.Entity.SelectedNodes).ToHashSet());

        /// <summary>Reads the native node selection and retrieves each selected node's coordinates.</summary>
        /// <returns>A fresh set of detached <see cref="Node"/> instances with positive IDs and finite global XYZ coordinates. An empty selection returns an empty set. Set iteration order is unspecified.</returns>
        /// <remarks>
        /// <para>Reads an integral GetNoOfSelectedNodes count from zero through 1,000,000. A positive count uses void GetSelectedNodes with an Int32 SAFEARRAY and integer zero for native selection order. The array must contain the expected number of unique positive Int32 IDs. Accepts a nonzero lower bound or replacement array and rechecks the count before publishing results. The count check can detect a size change. It cannot certify an atomic snapshot of the selection.</para>
        /// <para>Uses the captured ID list and reads each node exactly once through the same finite-Double coordinate check as GetNode(int). Coordinates retain OpenSTAAD length units and editing the returned nodes changes only managed metadata. A failed coordinate read throws before a set is returned.</para>
        /// <para>Runs under one current Geometry binding operation on the wrapper's constructing COM thread. Reacquire the adapter after refresh. Raw native access, other wrappers and external UI are outside the guard.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, or native count, array or coordinate output is malformed or inconsistent.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native exception and HRESULT are preserved.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// foreach (var node in wrapper.Geometry.GetSelectedNodesExt())
        ///     Console.WriteLine($"{node.Id}: ({node.X}, {node.Y}, {node.Z})");
        /// </code></example>
        public HashSet<Node> GetSelectedNodesExt() =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadSelectedNodes((object)wrapper.RawGeometry));

        /// <summary>Maps the current native node selection to caller-supplied node objects.</summary>
        /// <param name="fromModelNodes">A non-null sequence of nodes to match by ID. Each selected ID uses the first supplied match.</param>
        /// <returns>A fresh set of the matching supplied <see cref="Node"/> instances. If any selected ID is missing from the supplied sequence, the set contains one <see langword="null"/> marker. Empty selection returns an empty set. Set iteration order is unspecified.</returns>
        /// <remarks>
        /// <para>Reads an integral GetNoOfSelectedNodes count from zero through 1,000,000. A positive count uses void GetSelectedNodes with an Int32 SAFEARRAY and integer zero for native selection order. The array must contain the expected number of unique positive Int32 IDs. Accepts a nonzero lower bound or replacement array and rechecks the count before publishing results. The count check can detect a size change. It cannot certify an atomic snapshot of the selection.</para>
        /// <para>Enumerates the supplied sequence at most once and stops when every selected ID has been matched. A missing ID uses the source contract's null marker. The result keeps the supplied node references and coordinates. The only native reads are for selected IDs and selection counts.</para>
        /// <para>Runs under one current Geometry binding operation on the wrapper's constructing COM thread. Reacquire the adapter after refresh. Raw native access, other wrappers and external UI are outside the guard.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">The supplied sequence is null.</exception>
        /// <exception cref="ArgumentException">A visited sequence element is null.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, or native count, array or coordinate output is malformed or inconsistent.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native exception and HRESULT are preserved.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// var nodes = new[] { new StaadPro.Interop.Core.Entities.Node(3, 2, 0, 0) };
        /// foreach (var node in wrapper.Geometry.GetSelectedNodesExt(nodes))
        ///     if (node != null) Console.WriteLine(node.Id);
        /// </code></example>
        public HashSet<Node> GetSelectedNodesExt(IEnumerable<Node> fromModelNodes) =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadSuppliedSelection((object)wrapper.RawGeometry,
                GeometryReader.Entity.SelectedNodes, fromModelNodes, nameof(fromModelNodes)));

        /// <summary>Reads the selected node IDs in native selection order and returns a settled task.</summary>
        /// <returns>A completed task with a caller-owned array in native order, including an empty array for zero selection, or a faulted task with the original validation or native exception.</returns>
        /// <remarks>
        /// <para>Reads an integral GetNoOfSelectedNodes count from zero through 1,000,000. A positive count uses void GetSelectedNodes with an Int32 SAFEARRAY and integer zero for native selection order. The array must contain the expected number of unique positive Int32 IDs. Accepts a nonzero lower bound or replacement array and rechecks the count before publishing results. The count check can detect a size change. It cannot certify an atomic snapshot of the selection.</para>
        /// <para>Native calls and validation complete synchronously before the task is returned. Invocation may block. Awaiting the returned task does not move COM work to a background thread.</para>
        /// <para>Runs under one current Geometry binding operation on the wrapper's constructing COM thread. Reacquire the adapter after refresh. Raw native access, other wrappers and external UI are outside the guard.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed. The task faults.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, or native count, array or coordinate output is malformed or inconsistent. The task faults.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native exception and HRESULT are preserved. The task faults.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature. The task faults.</exception>
        /// <example><code>
        /// var ids = wrapper.Geometry.GetSelectedNodesListAsync().GetAwaiter().GetResult();
        /// Console.WriteLine(ids.Length);
        /// </code></example>
        public Task<int[]> GetSelectedNodesListAsync() =>
            ReadSelectedIdsTask(GeometryReader.Entity.SelectedNodes);

        /// <summary>Reads the IDs of analytical members currently selected in the native model.</summary>
        /// <returns>A fresh set of unique positive IDs. No selection returns an empty set. Set iteration order is unspecified.</returns>
        /// <remarks>
        /// <para>Reads an integral GetNoOfSelectedBeams count from zero through 1,000,000. A positive count calls void GetSelectedBeams with an Int32 SAFEARRAY and integer zero for native selection order. The buffer must contain the reported number of unique positive IDs. A replacement array or nonzero lower bound is supported. The count is checked again before returning the result.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-binding operation. Reacquire the adapter after refresh. External edits that leave the count unchanged can affect the read. These calls observe the selection without changing it.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, or the native count or ID array is malformed or inconsistent.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native exception and HRESULT are preserved.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// var ids = wrapper.Geometry.GetSelectedBeamsIds();
        /// foreach (int id in ids) Console.WriteLine(id);
        /// </code></example>
        public HashSet<int> GetSelectedBeamsIds() =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadIds((object)wrapper.RawGeometry, GeometryReader.Entity.SelectedBeams).ToHashSet());

        /// <summary>Reads selected analytical member IDs in native selection order and returns a settled task.</summary>
        /// <returns>A completed task with a detached Int32 array in native order, including an empty array for no selection, or a faulted task.</returns>
        /// <remarks>
        /// <para>Reads an integral GetNoOfSelectedBeams count from zero through 1,000,000. A positive count calls void GetSelectedBeams with an Int32 SAFEARRAY and integer zero for native selection order. The buffer must contain the reported number of unique positive IDs. A replacement array or nonzero lower bound is supported. The count is checked again before returning the result.</para>
        /// <para>Native reads and validation finish synchronously before the task is returned. Invocation can block. The task preserves the original validation or native exception.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-binding operation. Reacquire the adapter after refresh. External edits that leave the count unchanged can affect the read. These calls observe the selection without changing it.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed. The task faults.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, or the native count or ID array is malformed or inconsistent. The task faults.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native exception and HRESULT are preserved. The task faults.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature. The task faults.</exception>
        /// <example><code>
        /// var ids = wrapper.Geometry.GetSelectedBeamsListAsync().GetAwaiter().GetResult();
        /// foreach (int id in ids) Console.WriteLine(id);
        /// </code></example>
        public Task<int[]> GetSelectedBeamsListAsync() =>
            ReadSelectedIdsTask(GeometryReader.Entity.SelectedBeams);

        /// <summary>Reads the IDs of plate elements currently selected in the native model.</summary>
        /// <returns>A fresh set of unique positive IDs. No selection returns an empty set. Set iteration order is unspecified.</returns>
        /// <remarks>
        /// <para>Reads an integral GetNoOfSelectedPlates count from zero through 1,000,000. A positive count calls void GetSelectedPlates with an Int32 SAFEARRAY and integer zero for native selection order. The buffer must contain the reported number of unique positive IDs. A replacement array or nonzero lower bound is supported. The count is checked again before returning the result.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-binding operation. Reacquire the adapter after refresh. External edits that leave the count unchanged can affect the read. These calls observe the selection without changing it.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, or the native count or ID array is malformed or inconsistent.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native exception and HRESULT are preserved.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// var ids = wrapper.Geometry.GetSelectedPlatesIds();
        /// foreach (int id in ids) Console.WriteLine(id);
        /// </code></example>
        public HashSet<int> GetSelectedPlatesIds() =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadIds((object)wrapper.RawGeometry, GeometryReader.Entity.SelectedPlates).ToHashSet());

        /// <summary>Reads selected plate element IDs in native selection order and returns a settled task.</summary>
        /// <returns>A completed task with a detached Int32 array in native order, including an empty array for no selection, or a faulted task.</returns>
        /// <remarks>
        /// <para>Reads an integral GetNoOfSelectedPlates count from zero through 1,000,000. A positive count calls void GetSelectedPlates with an Int32 SAFEARRAY and integer zero for native selection order. The buffer must contain the reported number of unique positive IDs. A replacement array or nonzero lower bound is supported. The count is checked again before returning the result.</para>
        /// <para>Native reads and validation finish synchronously before the task is returned. Invocation can block. The task preserves the original validation or native exception.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-binding operation. Reacquire the adapter after refresh. External edits that leave the count unchanged can affect the read. These calls observe the selection without changing it.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed. The task faults.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, or the native count or ID array is malformed or inconsistent. The task faults.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native exception and HRESULT are preserved. The task faults.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature. The task faults.</exception>
        /// <example><code>
        /// var ids = wrapper.Geometry.GetSelectedPlatesListAsync().GetAwaiter().GetResult();
        /// foreach (int id in ids) Console.WriteLine(id);
        /// </code></example>
        public Task<int[]> GetSelectedPlatesListAsync() =>
            ReadSelectedIdsTask(GeometryReader.Entity.SelectedPlates);

        /// <summary>Maps selected analytical member numbers to caller-supplied beam objects.</summary>
        /// <param name="fromModelBeams">A non-null beam sequence. Each selected ID uses the first supplied beam with that ID.</param>
        /// <returns>A fresh set containing the matching supplied <see cref="Beam"/> references and one <see langword="null"/> entry if any selected ID is missing. An empty selection returns an empty set. Set iteration order is unspecified.</returns>
        /// <remarks>
        /// <para>Reads an integral GetNoOfSelectedBeams count from zero through 1,000,000. A positive count calls void GetSelectedBeams with an Int32 SAFEARRAY and integer zero for native selection order. The buffer must contain the reported number of unique positive IDs. A replacement array or nonzero lower bound is supported. The count is checked again before returning the result.</para>
        /// <para>Enumerates the supplied sequence at most once and stops when every selected ID has a match. The only native reads are for the selected IDs and their counts. Supplied incidence and other metadata remain as provided.</para>
        /// <para>The set uses the Beam ID equality contract. Remove a beam from the set before changing its ID and reinsert it after the change.</para>
        /// <para>Runs on the wrapper's constructing COM thread under one current-binding operation. Reacquire the adapter after refresh. External edits that leave the count unchanged can affect the read. These calls observe the selection without changing it.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">The supplied beam sequence is null.</exception>
        /// <exception cref="ArgumentException">An inspected sequence element is null.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, or the native count or ID array is malformed or inconsistent.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native exception and HRESULT are preserved.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The connected Geometry binding lacks a required native signature.</exception>
        /// <example><code>
        /// var beams = new[] { new StaadPro.Interop.Core.Entities.Beam { Id = 3 } };
        /// foreach (var beam in wrapper.Geometry.GetSelectedBeamsExt(beams))
        ///     if (beam != null) Console.WriteLine(beam.Id);
        /// </code></example>
        public HashSet<Beam> GetSelectedBeamsExt(IEnumerable<Beam> fromModelBeams) =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadSuppliedSelection((object)wrapper.RawGeometry,
                GeometryReader.Entity.SelectedBeams, fromModelBeams, nameof(fromModelBeams)));

        /// <summary>Reads a detached analytical member with its native start and end nodes.</summary>
        /// <param name="beamNumber">Positive analytical member number. Zero represents no member and skips native calls.</param>
        /// <returns>A new beam with the requested ID and two detached endpoints, or <see langword="null"/> for zero.</returns>
        /// <remarks>
        /// <para>Reads member incidence and then the two finite global XYZ positions in OpenSTAAD length units. Preserves native start and end order. Beta angle and design metadata are not queried.</para>
        /// <para>OpenSTAAD must report integral zero success from <c>GetMemberIncidence</c>. Failed status and malformed outputs stop the read without exposing a partial native result.</para>
        /// <para>Runs under one current Geometry binding operation on the wrapper's constructing COM thread. Reacquire Geometry after refresh. The call observes the model without changing it. Coordinate external edits with the owning thread.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="beamNumber"/> is negative.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, native incidence failed, or returned values are unwritten or invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The native binding lacks a required signature.</exception>
        /// <example><code>
        /// var beam = wrapper.Geometry.GetBeam(17);
        /// if (beam != null) Console.WriteLine(beam.StartNode.Id);
        /// </code></example>
        public Beam GetBeam(int beamNumber) =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadBeam((object)wrapper.RawGeometry, beamNumber));

        /// <summary>Reads member incidence and resolves its ends against supplied nodes.</summary>
        /// <param name="bId">Positive analytical member number. Zero returns null without reading incidence or enumerating the supplied nodes.</param>
        /// <param name="fromModelNodes">Non-null node sequence for a positive member number. The first matching object for each node ID is used.</param>
        /// <returns>A new beam with the requested ID and the matching supplied references. A missing endpoint is null. A zero member number returns null.</returns>
        /// <remarks>
        /// <para>Reads only native member incidence. Enumerates supplied nodes at most once and stops when both endpoints are found. It performs no coordinate queries or conversion and leaves supplied objects unchanged. Beta and design metadata are not queried.</para>
        /// <para>OpenSTAAD must report integral zero success from <c>GetMemberIncidence</c>. Failed status and malformed outputs stop the read without exposing a partial native result.</para>
        /// <para>Runs under one current Geometry binding operation on the wrapper's constructing COM thread. Reacquire Geometry after refresh. The call observes the model without changing it. Coordinate external edits with the owning thread.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bId"/> is negative.</exception>
        /// <exception cref="ArgumentNullException">A required supplied sequence is null.</exception>
        /// <exception cref="ArgumentException">An inspected supplied node is null.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, native incidence failed, or returned values are unwritten or invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The native binding lacks a required signature.</exception>
        /// <example><code>
        /// var nodes = new[] { new StaadPro.Interop.Core.Entities.Node(11, 0, 0, 0) };
        /// var beam = wrapper.Geometry.GetBeam(17, nodes);
        /// if (beam?.StartNode != null) Console.WriteLine(beam.StartNode.Id);
        /// </code></example>
        public Beam GetBeam(int bId, IEnumerable<Node> fromModelNodes) =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadBeam((object)wrapper.RawGeometry, bId, fromModelNodes));

        /// <summary>Reads a detached plate and its native incidence coordinates.</summary>
        /// <param name="pId">Positive analytical plate element number.</param>
        /// <returns>A new plate with the requested ID and detached A/B/C/D nodes. D is null for an absent fourth node. A repeated D shares the corresponding earlier node reference.</returns>
        /// <remarks>
        /// <para>The first three node IDs must be distinct and positive. D can be zero or a positive ID, including a repeated closing node. Queries each unique positive node ID once. Coordinates are written finite global XYZ values in OpenSTAAD length units.</para>
        /// <para>OpenSTAAD must report integral zero success from <c>GetPlateIncidence</c>. Failed status and malformed outputs stop the read without exposing a partial native result.</para>
        /// <para>Runs under one current Geometry binding operation on the wrapper's constructing COM thread. Reacquire Geometry after refresh. The call observes the model without changing it. Coordinate external edits with the owning thread.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pId"/> is nonpositive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, native incidence failed, or returned values are unwritten or invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The native binding lacks a required signature.</exception>
        /// <example><code>
        /// var plate = wrapper.Geometry.GetPlate(17);
        /// Console.WriteLine(plate.A.Id);
        /// </code></example>
        public Plate GetPlate(int pId) =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadPlate((object)wrapper.RawGeometry, pId));

        /// <summary>Reads native plate incidence and resolves its corners against supplied nodes.</summary>
        /// <param name="pId">Positive analytical plate element number.</param>
        /// <param name="fromModelNodes">Non-null node sequence. The first matching object for each positive incidence ID is used.</param>
        /// <returns>A new plate with matching supplied references in A/B/C/D order. Unmatched nodes and a zero D use null. A repeated D shares its earlier matching reference.</returns>
        /// <remarks>
        /// <para>Reads and validates native incidence before matching supplied nodes. Enumerates the sequence at most once and stops once all required IDs match. No coordinate read or conversion occurs. Supplied nodes are left unchanged.</para>
        /// <para>OpenSTAAD must report integral zero success from <c>GetPlateIncidence</c>. Failed status and malformed outputs stop the read without exposing a partial native result.</para>
        /// <para>Runs under one current Geometry binding operation on the wrapper's constructing COM thread. Reacquire Geometry after refresh. The call observes the model without changing it. Coordinate external edits with the owning thread.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pId"/> is nonpositive.</exception>
        /// <exception cref="ArgumentNullException">A required supplied sequence is null.</exception>
        /// <exception cref="ArgumentException">An inspected supplied node is null.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, native incidence failed, or returned values are unwritten or invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The native binding lacks a required signature.</exception>
        /// <example><code>
        /// var nodes = new[] { new StaadPro.Interop.Core.Entities.Node(11, 0, 0, 0) };
        /// var plate = wrapper.Geometry.GetPlate(17, nodes);
        /// if (plate.A != null) Console.WriteLine(plate.A.Id);
        /// </code></example>
        public Plate GetPlate(int pId, IEnumerable<Node> fromModelNodes) =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadPlate((object)wrapper.RawGeometry, pId, fromModelNodes));

        /// <summary>Reads an eager, ordered snapshot of the unique native plate corners.</summary>
        /// <param name="pId">Positive analytical plate element number.</param>
        /// <returns>A detached sequence of three or four nodes in first-seen A/B/C/D order. A zero or repeated D adds no entry. Re-enumerating the result makes no native calls.</returns>
        /// <remarks>
        /// <para>Requires three distinct positive A/B/C IDs. Each distinct corner ID is queried once and must yield finite global XYZ coordinates in OpenSTAAD length units.</para>
        /// <para>OpenSTAAD must report integral zero success from <c>GetPlateIncidence</c>. Failed status and malformed outputs stop the read without exposing a partial native result.</para>
        /// <para>Runs under one current Geometry binding operation on the wrapper's constructing COM thread. Reacquire Geometry after refresh. The call observes the model without changing it. Coordinate external edits with the owning thread.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pId"/> is nonpositive.</exception>
        /// <exception cref="ObjectDisposedException">The owning wrapper is disposed.</exception>
        /// <exception cref="InvalidOperationException">The binding or calling context is invalid, native incidence failed, or returned values are unwritten or invalid.</exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">The original native failure and HRESULT propagate.</exception>
        /// <exception cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException">The native binding lacks a required signature.</exception>
        /// <example><code>
        /// foreach (var node in wrapper.Geometry.GetPlateIncidence(17))
        ///     Console.WriteLine($"{node.Id}: ({node.X}, {node.Y}, {node.Z})");
        /// </code></example>
        public IEnumerable<Node> GetPlateIncidence(int pId) =>
            Wrapper.UseGeometryBinding(this, wrapper => GeometryReader.ReadPlateCorners((object)wrapper.RawGeometry, pId));

        private Task<int[]> ReadSelectedIdsTask(GeometryReader.Entity entity)
        {
            try
            {
                return Task.FromResult(Wrapper.UseGeometryBinding(this,
                    wrapper => GeometryReader.ReadIds((object)wrapper.RawGeometry, entity)));
            }
            catch (Exception error)
            {
                return Task.FromException<int[]>(error);
            }
        }
    }
}
