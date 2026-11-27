using System;
using System.Collections.Generic;
using System.Linq;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;

public sealed class OutputStressOffsetNative
{
    public List<string> Calls { get; } = new();
    public List<int> Threads { get; } = new();
    public object StressStatus = true;
    public object StressValues = Enumerable.Range(0, 32).Select(i => i + 0.25).ToArray();
    public bool WriteStress = true;
    public object OffsetStatus = 0;
    public object[] Offset = [1d, 2d, 3d];
    public object IncidenceStatus = 0;
    public object StartId = 11, EndId = 12;
    public object[] Joint = [4d, 5d, 6d];
    public object ForceStatus = true;
    public object ForceValues = new[] { 5d, -7d, 11d, 13d, 17d, 19d };
    public double[] StressInput, FacingInput;
    public Action<string> OnCall;

    private void Note(string operation)
    {
        Calls.Add(operation);
        Threads.Add(Environment.CurrentManagedThreadId);
        OnCall?.Invoke(operation);
    }

    public object GetPlateStressAtPoint(int plate, int loadCase, ref object point, ref object facing, ref object output)
    {
        Note($"stress:{plate}:{loadCase}");
        StressInput = (double[])point;
        FacingInput = (double[])facing;
        if (output is not double[] reserved || reserved.Length != 32 || reserved.Any(x => !double.IsNaN(x)))
            throw new InvalidOperationException("Expected thirty-two unwritten Doubles.");
        if (WriteStress) output = StressValues;
        return StressStatus;
    }

    public object GetMemberIncidence(int beam, ref object start, ref object end)
    {
        Note($"incidence:{beam}");
        if (!Equals(start, int.MinValue) || !Equals(end, int.MinValue))
            throw new InvalidOperationException("Expected unwritten incidence.");
        start = StartId;
        end = EndId;
        return IncidenceStatus;
    }

    public void GetNodeCoordinates(int node, ref object x, ref object y, ref object z)
    {
        Note($"coordinate:{node}");
        CheckVector(x, y, z);
        x = Joint[0];
        y = Joint[1];
        z = Joint[2];
    }

    public object GetMemberGlobalOffSet(int beam, int end, ref object x, ref object y, ref object z)
    {
        Note($"offset:{beam}:{end}");
        CheckVector(x, y, z);
        x = Offset[0];
        y = Offset[1];
        z = Offset[2];
        return OffsetStatus;
    }

    public object GetMemberEndForces(int beam, int end, int loadCase, ref object result, int axes)
    {
        Note($"forces:{beam}:{end}:{loadCase}:{axes}");
        if (result is not double[] reserved || reserved.Length != 6 || reserved.Any(x => !double.IsNaN(x)))
            throw new InvalidOperationException("Expected six unwritten Doubles.");
        result = ForceValues;
        return ForceStatus;
    }

    private static void CheckVector(object x, object y, object z)
    {
        if (x is not double a || y is not double b || z is not double c ||
            !double.IsNaN(a) || !double.IsNaN(b) || !double.IsNaN(c))
            throw new InvalidOperationException("Expected three unwritten Double components.");
    }
}

internal static class OutputStressOffsetFixture
{
    internal static OpenStaadWrapper Wrap(OutputStressOffsetNative native) =>
        new(new OutputStressOffsetRoot(native));

    internal static void Verify()
    {
        var native = new OutputStressOffsetNative();
        using var wrapper = Wrap(native);
        var output = wrapper.Output;
        var stress = output.GetPlateStressAtPoint(17, 3, new Node(0, 2, 4, 6), new Node(0, 8, 10, 12));
        if (stress.PlateId != 17 || stress.LoadCaseId != 3 || stress.TopMax != 2.25 || stress.CombXYBot != 31.25)
            throw new Exception("Installed point stress identity or named components changed.");
        var copy = stress.Raw;
        copy[2] = 100;
        if (stress.TopMax != 2.25 || !native.StressInput.SequenceEqual(new[] { 2d, 4d, 6d }))
            throw new Exception("Installed point stress did not preserve captured inputs or packet ownership.");

        var beam = new Beam(17, new Node(11, 1, 2, 3), new Node(12, 4, 5, 6));
        var fromBeam = output.GetMemberEndForcesConsideringOffsets("Dead", beam, 3, MemberEnd.End);
        var fromId = output.GetMemberEndForcesConsideringOffsets("Dead", 17, 3, MemberEnd.End);
        foreach (var off in new[] { fromBeam, fromId })
        {
            if (!off.HasOffsetSpecification || off.PhysicalJoint.Id != 12 || off.MemberFace.Z != 9 ||
                off.MemberFaceForces.Mx != 13 || off.PhysicalJointForces.Mx != 56 || off.PhysicalJointForces.Mz != 2)
                throw new Exception("Installed offset and moment transfer changed.");
        }

        if (output.GetMemberEndForcesAtPhysicalJoint("Dead", beam, 3, MemberEnd.End).My != 21 ||
            output.GetMemberEndForcesAtPhysicalJoint("Dead", 17, 3, MemberEnd.End).My != 21 ||
            native.Threads.Any(thread => thread != Environment.CurrentManagedThreadId) ||
            native.Calls.Count(call => call == "forces:17:1:3:1") != 4)
            throw new Exception("Installed physical-joint projection or owning-thread execution changed.");
    }
}

public sealed class OutputStressOffsetRoot
{
    public OutputStressOffsetRoot(OutputStressOffsetNative native) { Geometry = native; Property = native; Output = native; }
    public object Geometry { get; set; }
    public object Property { get; set; }
    public object Output { get; set; }
    public object GetApplicationVersion(ref object major, ref object update, ref object revision, ref object build)
    { major = 26; update = 0; revision = 1; build = 491; return "26.0.1.491"; }
    public int GetBaseUnit() => 2;
    public int GetInputUnitForForce(ref object text) { text = "kN"; return 5; }
    public int GetInputUnitForLength(ref object text) { text = "m"; return 4; }
    public int GetMainWindowHandle() => 12345;
}
