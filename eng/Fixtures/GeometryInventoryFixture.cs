using System;
using System.Collections.Generic;
using System.Linq;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

public sealed class GeometryInventoryNative
{
    public List<string> Calls { get; } = new();
    public List<int> Threads { get; } = new();
    public Queue<object> Counts { get; } = new();
    public object Ids { get; set; } = new[] { 29, 3, 17 };
    public bool ReplaceBuffer { get; set; }
    public object[] Coordinates { get; set; } = [0d, -2.25d, 7d];
    public Action<string> OnCall { get; set; }

    public object GetMemberCount() => ReadCount(nameof(GetMemberCount));
    public object GetNodeCount() => ReadCount(nameof(GetNodeCount));
    public object GetPlateCount() => ReadCount(nameof(GetPlateCount));
    public object GetSolidCount() => ReadCount(nameof(GetSolidCount));
    public object GetNoOfSelectedNodes() => ReadCount(nameof(GetNoOfSelectedNodes));

    public void GetBeamList(ref object ids) => WriteIds(nameof(GetBeamList), ref ids);
    public void GetNodeList(ref object ids) => WriteIds(nameof(GetNodeList), ref ids);
    public void GetPlateList(ref object ids) => WriteIds(nameof(GetPlateList), ref ids);
    public void GetSolidList(ref object ids) => WriteIds(nameof(GetSolidList), ref ids);

    public void GetSelectedNodes(ref object ids, object nIsSorted)
    {
        if (nIsSorted is not int flag || flag != 0)
            throw new InvalidOperationException("Expected the Int32 selection-order flag zero.");
        WriteIds(nameof(GetSelectedNodes), ref ids);
    }

    public void GetNodeCoordinates(int nId, ref object x, ref object y, ref object z)
    {
        Note("GetNodeCoordinates:" + nId);
        if (x is not double sx || !double.IsNaN(sx) || y is not double sy || !double.IsNaN(sy) ||
            z is not double sz || !double.IsNaN(sz))
            throw new InvalidOperationException("Expected unwritten Double slots.");
        if (Coordinates.Length > 0) x = Coordinates[0];
        if (Coordinates.Length > 1) y = Coordinates[1];
        if (Coordinates.Length > 2) z = Coordinates[2];
    }

    private object ReadCount(string operation)
    {
        Note(operation);
        return Counts.TryDequeue(out object count) ? count : (Ids as Array)?.Length ?? 0;
    }

    private void WriteIds(string operation, ref object ids)
    {
        Note(operation);
        if (ids is not int[] reserved || reserved.Any(id => id != 0))
            throw new InvalidOperationException("Expected an unwritten Int32 inventory buffer.");
        if (ReplaceBuffer) ids = Ids;
        else Array.Copy((int[])Ids, reserved, reserved.Length);
    }

    private void Note(string operation)
    {
        Calls.Add(operation);
        Threads.Add(Environment.CurrentManagedThreadId);
        OnCall?.Invoke(operation);
    }
}

public sealed class GeometryInventoryRoot
{
    public GeometryInventoryRoot(GeometryInventoryNative geometry) { Geometry = geometry; }
    public GeometryInventoryNative Geometry { get; }
    public object GetApplicationVersion(ref object major, ref object update, ref object revision, ref object build)
    { major = 26; update = 0; revision = 1; build = 491; return "26.0.1.491"; }
    public int GetBaseUnit() => 2;
    public int GetInputUnitForForce(ref object text) { text = "kN"; return 5; }
    public int GetInputUnitForLength(ref object text) { text = "m"; return 4; }
    public int GetMainWindowHandle() => 12345;
}

internal static class GeometryInventoryFixture
{
    internal static void Verify()
    {
        var native = new GeometryInventoryNative();
        using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
        var geometry = wrapper.Geometry;
        var expected = new[] { 29, 3, 17 };
        if (!geometry.GetAllBeamsList().SequenceEqual(expected) ||
            !geometry.GetAllNodesList().SequenceEqual(expected) ||
            !geometry.GetAllPlatesList().SequenceEqual(expected) ||
            !geometry.GetAllSolidsList().SequenceEqual(expected))
            throw new InvalidOperationException("Installed Geometry inventories changed order or IDs.");
        var node = geometry.GetNode(nId: 3);
        if (node.Id != 3 || node.X != 0 || node.Y != -2.25 || node.Z != 7 || geometry.GetNode(0) != null)
            throw new InvalidOperationException("Installed Geometry node projection changed.");
        if (!geometry.GetSelectedNodesIds().SetEquals(expected) ||
            !geometry.GetSelectedNodesListAsync().GetAwaiter().GetResult().SequenceEqual(expected) ||
            !geometry.GetSelectedNodesExt().Select(selected => selected.Id).ToHashSet().SetEquals(expected))
            throw new InvalidOperationException("Installed Geometry selection order or membership changed.");
        var supplied = new[] { new Node(3, 5, 0, 0), new Node(3, 6, 0, 0) };
        var mapped = geometry.GetSelectedNodesExt(supplied);
        if (!ReferenceEquals(geometry.GetNode(3, supplied), supplied[0]) ||
            !mapped.SetEquals(new Node[] { supplied[0], null }))
            throw new InvalidOperationException("Installed Geometry supplied-node identity changed.");
        if (native.Threads.Any(thread => thread != Environment.CurrentManagedThreadId))
            throw new InvalidOperationException("Installed Geometry reads left the wrapper's owner thread.");
    }
}
