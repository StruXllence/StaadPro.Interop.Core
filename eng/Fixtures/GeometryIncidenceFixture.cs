using System;
using System.Collections.Generic;
using System.Linq;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

public sealed class GeometryIncidenceNative
{
    public List<string> Calls { get; } = new();
    public List<int> Threads { get; } = new();
    public object MemberStatus { get; set; } = 0;
    public object PlateStatus { get; set; } = 0;
    public object[] MemberIds { get; set; } = [11, 12];
    public object[] PlateIds { get; set; } = [11, 12, 13, 0];
    public int? IncompleteNodeId { get; set; }
    public Action<string> OnCall { get; set; }

    public object GetMemberIncidence(int beamId, ref object start, ref object end)
    {
        Note("GetMemberIncidence:" + beamId);
        if (MemberIds.Length > 0) start = MemberIds[0];
        if (MemberIds.Length > 1) end = MemberIds[1];
        return MemberStatus;
    }

    public object GetPlateIncidence(int plateId, ref object a, ref object b, ref object c, ref object d)
    {
        Note("GetPlateIncidence:" + plateId);
        if (PlateIds.Length > 0) a = PlateIds[0];
        if (PlateIds.Length > 1) b = PlateIds[1];
        if (PlateIds.Length > 2) c = PlateIds[2];
        if (PlateIds.Length > 3) d = PlateIds[3];
        return PlateStatus;
    }

    public void GetNodeCoordinates(int nodeId, ref object x, ref object y, ref object z)
    {
        Note("GetNodeCoordinates:" + nodeId);
        x = (double)nodeId;
        y = -0.5;
        if (nodeId != IncompleteNodeId) z = 3d;
    }

    private void Note(string call)
    {
        Calls.Add(call);
        Threads.Add(Environment.CurrentManagedThreadId);
        OnCall?.Invoke(call);
    }
}

public sealed class GeometryIncidenceRoot
{
    public GeometryIncidenceRoot(GeometryIncidenceNative geometry) { Geometry = geometry; }
    public GeometryIncidenceNative Geometry { get; }

    public object GetApplicationVersion(ref object major, ref object update, ref object revision, ref object build)
    {
        major = 26;
        update = 0;
        revision = 1;
        build = 491;
        return "26.0.1.491";
    }

    public int GetBaseUnit() => 2;
    public int GetInputUnitForForce(ref object text) { text = "kN"; return 5; }
    public int GetInputUnitForLength(ref object text) { text = "m"; return 4; }
    public int GetMainWindowHandle() => 12345;
}

internal static class GeometryIncidenceFixture
{
    internal static void Verify()
    {
        var native = new GeometryIncidenceNative { PlateIds = [11, 12, 13, 11] };
        using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));
        var geometry = wrapper.Geometry;
        var beam = geometry.GetBeam(beamNumber: 42);
        if (beam.Id != 42 || beam.StartNode.Id != 11 || beam.EndNode.Id != 12 || beam.EndNode.X != 12)
            throw new InvalidOperationException("Installed member incidence or coordinates changed.");
        var plate = geometry.GetPlate(pId: 7);
        if (plate.Id != 7 || plate.C.Id != 13 || !ReferenceEquals(plate.A, plate.D))
            throw new InvalidOperationException("Installed plate incidence slots changed.");
        var corners = geometry.GetPlateIncidence(pId: 7);
        int dispatchCount = native.Calls.Count;
        if (!corners.Select(corner => corner.Id).SequenceEqual(new[] { 11, 12, 13 }) ||
            native.Calls.Count != dispatchCount)
            throw new InvalidOperationException("Installed plate incidence was reordered or used deferred native reads.");
        var first = new Node(11, 90, 0, 0);
        var supplied = new[] { first, new Node(11, 99, 0, 0) };
        var projectedBeam = geometry.GetBeam(bId: 42, fromModelNodes: supplied);
        var projectedPlate = geometry.GetPlate(pId: 7, fromModelNodes: supplied);
        if (!ReferenceEquals(projectedBeam.StartNode, first) || projectedBeam.EndNode != null ||
            !ReferenceEquals(projectedPlate.A, first) || !ReferenceEquals(projectedPlate.D, first) ||
            projectedPlate.B != null || projectedPlate.C != null)
            throw new InvalidOperationException("Installed incidence projection changed supplied node identity or missing slots.");
        if (!native.Calls.Skip(dispatchCount).SequenceEqual(new[] { "GetMemberIncidence:42", "GetPlateIncidence:7" }) ||
            native.Threads.Any(thread => thread != Environment.CurrentManagedThreadId))
            throw new InvalidOperationException("Installed incidence projection made unexpected native calls.");
    }
}
