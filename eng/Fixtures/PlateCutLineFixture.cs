using System;
using System.Linq;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

public sealed class PlateCutLineNative
{
    public object Status = true;
    public bool WriteResult = true;
    public object Buffer = new double[,] { { 1, -2, 3, -4, 5, -6 } };
    public int Calls;
    public int Thread;
    public int GlobalFlag;
    public int[] Plates;
    public int[] Case;
    public double[] Start;
    public double[] End;
    public Exception Failure;
    public Action Callback;

    public object GetResultantForceAlongLineForPlateList(ref object plates, int count, ref object cases,
        ref object start, ref object end, int globalFlag, int firstNode, int secondNode, int thirdNode,
        ref object output)
    {
        if (plates is not int[] ids || ids.Length != count ||
            cases is not int[] { Length: 1 } loadCase ||
            start is not double[] { Length: 3 } from || end is not double[] { Length: 3 } to ||
            firstNode != -1 || secondNode != -1 || thirdNode != -1 ||
            output is not double[,] initial || initial.GetLength(0) != 1 ||
            initial.GetLength(1) != 6 || !initial.Cast<double>().All(double.IsNaN))
            throw new InvalidOperationException("Unexpected cut-line dispatch arguments or initial buffer.");

        Calls++;
        Thread = Environment.CurrentManagedThreadId;
        GlobalFlag = globalFlag;
        Plates = (int[])ids.Clone();
        Case = (int[])loadCase.Clone();
        Start = (double[])from.Clone();
        End = (double[])to.Clone();
        ids[0] = -123;
        Callback?.Invoke();
        if (Failure != null) throw Failure;
        if (WriteResult) output = Buffer;
        return Status;
    }
}

internal static class PlateCutLineFixture
{
    internal static void Verify()
    {
        var native = new PlateCutLineNative();
        var root = new PlateCornerHydrationRoot(new PlateCornerHydrationNative()) { Output = native };
        using var wrapper = new OpenStaadWrapper(root);
        var plates = new[] { new Plate(null, null, null) { Id = 17 }, new Plate(null, null, null) { Id = 22 } };
        var start = new CartesianVector3(1, 2, 3);
        var end = new CartesianVector3(4, 5, 6);
        var status = wrapper.Output.QueryPlateCutLineResultant(plates, 8, start, end, true);
        if (!status.IsSuccessful || status.Forces.Mz != -6 || !status.PlateIds.SequenceEqual(new[] { 17, 22 }) ||
            !status.UsesGlobalAxes || status.LoadCaseId != 8 || native.GlobalFlag != 1 ||
            !native.Start.SequenceEqual(new[] { 1d, 2d, 3d }) || !native.End.SequenceEqual(new[] { 4d, 5d, 6d }))
            throw new InvalidOperationException("Installed cut-line observation did not preserve the request and results.");
        var direct = wrapper.Output.GetPlateCutLineResultant(plates, 8, start, end, false);
        var task = wrapper.Output.GetPlateCutLineResultantAsync(plates, 8, start, end, false);
        if (!task.IsCompletedSuccessfully || task.Result.Fy != -2 || direct.Fy != -2 || native.GlobalFlag != 0 ||
            native.Thread != Environment.CurrentManagedThreadId || native.Calls != 3)
            throw new InvalidOperationException("Installed cut-line task must perform one read on the owning thread.");
        native.Status = false;
        if (wrapper.Output.QueryPlateCutLineResultant(plates, 8, start, end, false).IsSuccessful ||
            !wrapper.Output.GetPlateCutLineResultantAsync(plates, 8, start, end, false).IsFaulted)
            throw new InvalidOperationException("Installed cut-line native rejection contract changed.");
    }
}
