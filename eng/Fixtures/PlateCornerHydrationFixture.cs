using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;

public sealed class PlateCornerHydrationRoot
{
    public PlateCornerHydrationRoot(PlateCornerHydrationNative native)
    { Geometry = native; Load = native; Output = native; }
    public object Geometry { get; set; }
    public object Load { get; set; }
    public object Output { get; set; }
    public object GetApplicationVersion(ref object major, ref object update, ref object revision, ref object build)
    { major = 26; update = 0; revision = 1; build = 491; return "26.0.1.491"; }
    public int GetBaseUnit() => 2;
    public int GetInputUnitForForce(ref object text) { text = "kN"; return 5; }
    public int GetInputUnitForLength(ref object text) { text = "m"; return 4; }
    public int GetMainWindowHandle() => 12345;
}
public sealed class PlateCornerHydrationNative
{
    public readonly List<string> Calls = new();
    public readonly List<int> Threads = new();
    public readonly Dictionary<string, object> Counts = new() { ["p"] = 1, ["c"] = 1, ["r"] = 1 };
    public readonly Dictionary<string, object> Ids = new() { ["p"] = new[] { 1 }, ["c"] = new[] { 2 }, ["r"] = new[] { 3 } };
    public readonly Dictionary<string, object> Responses = new() { ["p"] = true, ["c"] = true, ["r"] = 1 };
    public readonly Dictionary<string, int> CountCalls = new();
    public object A = 11, B = 12, C = 13, D = 0, IncidenceStatus = 0;
    public object Title = "  case\r\n", Type = 23, ForceStatus = true;
    public object ForceValues = new[] { 1d, -2d, 3d, -4d, 5d, -6d };
    public bool WriteIncidence = true, WriteCoordinates = true, WriteIds = true, WriteForces = true;
    public Action<string> Callback;
    public Exception Failure;
    public string FailureAt;
    public Func<string, int, object> CountAt;
    public Func<int, object[]> Coordinates;
    private void Step(string operation)
    {
        Calls.Add(operation); Threads.Add(Thread.CurrentThread.ManagedThreadId); Callback?.Invoke(operation);
        if (Failure != null && (FailureAt == null || operation.StartsWith(FailureAt, StringComparison.Ordinal))) throw Failure;
    }
    public object GetPlateIncidence(int plateId, ref object a, ref object b, ref object c, ref object d)
    {
        Step("incidence:" + plateId);
        if (!(a is int x && x == int.MinValue && b is int y && y == int.MinValue &&
            c is int z && z == int.MinValue && d is int w && w == int.MinValue))
            throw new InvalidOperationException("Expected unwritten Int32 incidence sentinels.");
        if (WriteIncidence) { a = A; b = B; c = C; d = D; }
        return IncidenceStatus;
    }
    public void GetNodeCoordinates(int nodeId, ref object x, ref object y, ref object z)
    {
        Step("coordinate:" + nodeId);
        if (!(x is double dx && double.IsNaN(dx) && y is double dy && double.IsNaN(dy) && z is double dz && double.IsNaN(dz)))
            throw new InvalidOperationException("Expected coordinate NaN sentinels.");
        if (WriteCoordinates)
        {
            object[] values = Coordinates?.Invoke(nodeId) ?? new object[] { (double)nodeId, 0d, 1d };
            x = values[0]; y = values[1]; z = values[2];
        }
    }
    private object Count(string family)
    {
        Step(family + "Count");
        int number = CountCalls.TryGetValue(family, out int known) ? known + 1 : 1;
        CountCalls[family] = number;
        return CountAt == null ? Counts[family] : CountAt(family, number);
    }
    private object Numbers(string family, ref object buffer)
    {
        Step(family + "Numbers");
        if (buffer is not int[] values || values.Any(i => i != int.MinValue))
            throw new InvalidOperationException("Expected Int32 case-ID sentinels.");
        if (WriteIds) buffer = Ids[family];
        return Responses[family];
    }
    public object GetPrimaryLoadCaseCount() => Count("p");
    public object GetLoadCombinationCaseCount() => Count("c");
    public object GetReferenceLoadCaseCount() => Count("r");
    public object GetPrimaryLoadCaseNumbers(ref object buffer) => Numbers("p", ref buffer);
    public object GetLoadCombinationCaseNumbers(ref object buffer) => Numbers("c", ref buffer);
    public object GetReferenceLoadCaseNumbers(ref object buffer) => Numbers("r", ref buffer);
    public object GetLoadCaseTitle(int id) { Step("title:" + id); return Title; }
    public object GetReferenceLoadCaseTitle(int id) { Step("referenceTitle:" + id); return Title; }
    public object GetLoadType(int id) { Step("type:" + id); return Type; }
    public object GetReferenceLoadType(int id) { Step("referenceType:" + id); return Type; }
    public object GetPlateCornerForces(int plate, int node, int load, ref object buffer)
    {
        Step("force:" + plate + ":" + node + ":" + load);
        if (buffer is not double[] values || values.Length != 6 || values.Any(v => !double.IsNaN(v)))
            throw new InvalidOperationException("Expected six force NaN sentinels.");
        if (WriteForces) buffer = ForceValues;
        return ForceStatus;
    }
}
internal static class PlateCornerHydrationFixture
{
    internal static void Verify()
    {
        var native = new PlateCornerHydrationNative();
        using var wrapper = new OpenStaadWrapper(new PlateCornerHydrationRoot(native));
        PlateCornerForces[] observations = {
            wrapper.Output.GetPlateCornerForces(1,17,11),
            wrapper.Output.GetPlateCornerForcesAsync(1,17,11).GetAwaiter().GetResult(),
            wrapper.Output.GetPlateCornerForces(1,17,11,wrapper.Geometry,wrapper.Load),
            wrapper.Output.GetPlateCornerForcesAsync(1,17,11,wrapper.Geometry,wrapper.Load).GetAwaiter().GetResult(),
            wrapper.Output.GetPlateCornerForces(2,17,11,LoadCaseType.LoadCombination,wrapper.Geometry,wrapper.Load)
        };
        if (observations.Any(r => r.Fx != 1 || r.Mz != -6 || r.Plate.Id != 17 || r.CornerNode.Id != 11 ||
            !ReferenceEquals(r.CornerNode,r.Plate.A)) || observations[4].LoadCase.CaseType != LoadCaseType.LoadCombination ||
            native.Calls.Count(s => s.StartsWith("force:")) != 5 || native.Threads.Any(t => t != Environment.CurrentManagedThreadId))
            throw new Exception("Installed ID hydration workflow contract changed.");
        native.ForceStatus = false;
        if (!wrapper.Output.GetPlateCornerForcesAsync(1,17,11).IsFaulted ||
            !wrapper.Output.GetPlateCornerForcesAsync(1,17,11,wrapper.Geometry,wrapper.Load).IsFaulted)
            throw new Exception("Installed ID hydration task failure lost.");
    }
    internal static void VerifyBatchesAndAliases()
    {
        var native = new PlateCornerHydrationNative();
        using var wrapper = new OpenStaadWrapper(new PlateCornerHydrationRoot(native));
        var family = wrapper.Output.GetPlateCornerForcesAsync(2,17,11,LoadCaseType.LoadCombination,wrapper.Geometry,wrapper.Load);
        var batch = wrapper.Output.GetPlateCornerForces(new[]{3,1,2,3},17,11,wrapper.Geometry,wrapper.Load);
        var batchTask = wrapper.Output.GetPlateCornerForcesAsync(new[]{3,1,2},17,11,wrapper.Geometry,wrapper.Load);
        var loadCase = new LoadCase(1,"Case");
        var p = new Plate(new Node(11,0,0,0),new Node(12,0,1,0),new Node(13,0,0,1)){Id=17};
        var global = wrapper.Output.GetPlateCornerForcesInGlobalCoordinates(loadCase,p,p.A);
        var globalTask = wrapper.Output.GetPlateCornerForcesInGlobalCoordinatesAsync(loadCase,p,p.A);
        if (!family.IsCompletedSuccessfully || family.Result.LoadCase.CaseType != LoadCaseType.LoadCombination ||
            !batch.Results.Select(r=>r.LoadCaseId).SequenceEqual(new[]{3,1,2}) ||
            !batchTask.IsCompletedSuccessfully || batchTask.Result.Count != 3 ||
            !ReferenceEquals(batch[3,17,11].Plate,batch[1,17,11].Plate) ||
            global.Fx != 1 || global.Mz != -6 || !ReferenceEquals(global.Plate,p) ||
            !globalTask.IsCompletedSuccessfully || globalTask.Result.Fy != -2)
            throw new Exception("Installed family/batch/global alias contracts changed.");
        native.ForceStatus=false;
        if (!wrapper.Output.GetPlateCornerForcesAsync(2,17,11,LoadCaseType.LoadCombination,wrapper.Geometry,wrapper.Load).IsFaulted ||
            !wrapper.Output.GetPlateCornerForcesAsync(new[]{1},17,11,wrapper.Geometry,wrapper.Load).IsFaulted ||
            !wrapper.Output.GetPlateCornerForcesInGlobalCoordinatesAsync(loadCase,p,p.A).IsFaulted)
            throw new Exception("Installed family/batch/alias faults lost.");
    }
}
