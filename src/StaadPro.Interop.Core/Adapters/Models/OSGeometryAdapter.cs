using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Geometry;

namespace StaadPro.Interop.Core.Adapters.Models
{
    /// <summary>Provides guarded read access to model geometry IDs and node coordinates.</summary>
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
    }
}
