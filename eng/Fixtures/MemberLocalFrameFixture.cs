using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Output;

public sealed class MemberLocalFrameRoot : NativeRoot
{
    // Intentionally has no dispatchable members. These projections use only supplied managed data.
    public object Output { get; } = new();
}

internal static class MemberLocalFrameFixture
{
    internal static void Verify(XDocument docs)
    {
        var root = new MemberLocalFrameRoot();
        using var wrapper = new OpenStaadWrapper(root);
        IOSOutput output = wrapper.Output;
        int initialCalls = root.Calls;
        var corner = new Node(11, 0, 0, 0);
        var start = new Node(12, -2, 0, 0);
        var up = new Node(13, 0, 4, 0);
        var plate = new Plate(corner, start, up) { Id = 17 };
        var global = new PlateCornerForces(new LoadCase(1, "Dead"), plate, corner, 1, -2, 3, -4, 5, -6);
        var member = new Beam(21, start, corner) { BetaAngle = 90 };

        var conceptual = output.TransformPlateCornerForcesToMemberLocal(global, up);
        CheckForces(conceptual, [-2, -1, 3, 5, 4, -6], 0, MemberEnd.Start);
        var actual = output.TransformPlateCornerForcesToMemberLocal(global, member);
        CheckForces(actual, [1, 3, 2, -4, -6, -5], 21, MemberEnd.End);

        Task<MemberForces> conceptualTask = output.TransformPlateCornerForcesToMemberLocalAsync(global, up);
        Task<MemberForces> actualTask = output.TransformPlateCornerForcesToMemberLocalAsync(global, member);
        if (!conceptualTask.IsCompletedSuccessfully || !actualTask.IsCompletedSuccessfully)
            throw new Exception("Member projection tasks must settle during invocation.");
        CheckForces(conceptualTask.Result, [-2, -1, 3, 5, 4, -6], 0, MemberEnd.Start);
        CheckForces(actualTask.Result, [1, 3, 2, -4, -6, -5], 21, MemberEnd.End);
        if (!output.TransformPlateCornerForcesToMemberLocalAsync(global, corner).IsFaulted)
            throw new Exception("Coincident projection direction must fault.");

        LocalCS frame = output.GetMemberLocalCoordinateSystem(member);
        if (frame.OriginNodeId != 12) throw new Exception("Member frame origin ID changed.");
        Check(frame.Origin.X, -2); Check(frame.Origin.Y, 0); Check(frame.Origin.Z, 0);
        Check(frame.U.X, 1); Check(frame.U.Y, 0); Check(frame.U.Z, 0);
        Check(frame.V.X, 0); Check(frame.V.Y, 0); Check(frame.V.Z, 1);
        Check(frame.W.X, 0); Check(frame.W.Y, -1); Check(frame.W.Z, 0);
        start.X = -3;
        start.Id = 99;
        member.BetaAngle = 0;
        Check(frame.Origin.X, -2);
        Check(frame.V.Z, 1);
        if (frame.OriginNodeId != 12 || root.Calls != initialCalls || global.Fx != 1 || global.Mz != -6)
            throw new Exception("Member projection changed source data or accessed native state.");

        foreach (Type geometryType in new[] { typeof(Node), typeof(Beam) })
        {
            string parameter = geometryType == typeof(Node) ? "otherMemberNode" : "member";
            Type[] types = [typeof(PlateCornerForces), geometryType];
            string[] names = ["plateCornerForces", parameter];
            VerifyContract(docs, "TransformPlateCornerForcesToMemberLocal", typeof(MemberForces), types, names);
            VerifyContract(docs, "TransformPlateCornerForcesToMemberLocalAsync", typeof(Task<MemberForces>), types, names);
        }
        VerifyContract(docs, "GetMemberLocalCoordinateSystem", typeof(LocalCS), [typeof(Beam)], ["member"]);
        VerifyLiveQueries(docs);
    }

    private static void VerifyLiveQueries(XDocument docs)
    {
        var native = new PlateCornerHydrationNative();
        using var wrapper = new OpenStaadWrapper(new PlateCornerHydrationRoot(native));
        IOSOutput output = wrapper.Output;
        var corner = new Node(11, 0, 0, 0);
        var start = new Node(12, -2, 0, 0);
        var up = new Node(13, 0, 4, 0);
        var plate = new Plate(corner, start, up) { Id = 17 };
        var member = new Beam(21, start, corner) { BetaAngle = 90 };
        var load = new LoadCase(1, "Dead");

        var actions = output.GetPlateCornerForcesGlobal(plate, corner, 1);
        if (!actions.IsSuccessful || actions.Key != new PlateCornerForcesKey(1, 17, 11) ||
            actions.Forces.Fx != 1 || actions.Forces.Mz != -6)
            throw new Exception("Installed global corner observation did not preserve data and identity.");

        CheckForces(output.GetPlateCornerForces(load, plate, corner, up), [-2, -1, 3, 5, 4, -6], 0, MemberEnd.Start);
        CheckForces(output.GetPlateCornerForces(load, plate, corner, member), [1, 3, 2, -4, -6, -5], 21, MemberEnd.End);
        var nodeTask = output.GetPlateCornerForcesAsync(load, plate, corner, up);
        var memberTask = output.GetPlateCornerForcesAsync(load, plate, corner, member);
        if (!nodeTask.IsCompletedSuccessfully || !memberTask.IsCompletedSuccessfully ||
            native.Calls.Count != 5 || native.Calls.Any(call => call != "force:17:11:1") ||
            native.Threads.Any(thread => thread != Environment.CurrentManagedThreadId))
            throw new Exception("Installed corner composition must do one force read per call on the owning thread.");
        CheckForces(nodeTask.Result, [-2, -1, 3, 5, 4, -6], 0, MemberEnd.Start);
        CheckForces(memberTask.Result, [1, 3, 2, -4, -6, -5], 21, MemberEnd.End);

        var localPlate = output.GetPlateCornerForcesInMemberLocalCoordinates(load, plate, corner, up);
        var localPlateTask = output.GetPlateCornerForcesInMemberLocalCoordinatesAsync(load, plate, corner, up);
        if (!localPlate.IsMemberLocal || localPlate.Plate != plate || localPlate.CornerNode != corner ||
            !ReferenceEquals(localPlate.LoadCase, load) || localPlate.Fx != -2 || localPlate.Mx != 5 ||
            !localPlateTask.IsCompletedSuccessfully || localPlateTask.Result.Mx != 5 || native.Calls.Count != 7)
            throw new Exception("Installed member-local corner metadata or components changed.");

        native.ForceStatus = false;
        var failure = output.GetPlateCornerForcesGlobal(plate, corner, 1);
        if (failure.IsSuccessful || failure.Forces != null || failure.Key != actions.Key ||
            !output.GetPlateCornerForcesAsync(load, plate, corner, member).IsFaulted)
            throw new Exception("Installed corner failure contract changed.");

        VerifyContract(docs, "GetPlateCornerForcesGlobal", typeof(PlateCornerForcesQueryResult),
            [typeof(Plate), typeof(Node), typeof(int)], ["plate", "cornerNode", "loadCaseId"]);
        foreach (Type geometryType in new[] { typeof(Node), typeof(Beam) })
        {
            Type[] types = [typeof(ILoadCase), typeof(Plate), typeof(Node), geometryType];
            string[] names = ["loadCase", "plate", "cornerNode", geometryType == typeof(Node) ? "otherMemberNode" : "member"];
            VerifyContract(docs, "GetPlateCornerForces", typeof(MemberForces), types, names);
            VerifyContract(docs, "GetPlateCornerForcesAsync", typeof(Task<MemberForces>), types, names);
        }
    }

    private static void CheckForces(MemberForces result, double[] expected, int beamId, MemberEnd end)
    {
        if (result.BeamId != beamId || result.End != end || result.LoadCaseId != 1 || result.LoadCaseTitle != "Dead")
            throw new Exception("Member projection metadata changed.");
        double[] actual = [result.Fx, result.Fy, result.Fz, result.Mx, result.My, result.Mz];
        for (int i = 0; i < actual.Length; i++) Check(actual[i], expected[i]);
    }

    private static void Check(double actual, double expected)
    {
        if (!double.IsFinite(actual) || Math.Abs(actual - expected) > 1e-12)
            throw new Exception($"Expected projected component {expected}, got {actual}.");
    }

    private static void VerifyContract(XDocument docs, string name, Type result, Type[] inputTypes, string[] inputNames)
    {
        foreach (Type declaring in new[] { typeof(IOSOutput), typeof(OSOutputAdapter) })
        {
            MethodInfo method = declaring.GetMethod(name, inputTypes);
            if (method == null || method.ReturnType != result ||
                !method.GetParameters().Select(p => p.Name).SequenceEqual(inputNames) ||
                method.GetParameters().Any(p => p.IsOptional))
                throw new Exception("Member projection signature changed: " + name);
            string id = "M:" + declaring.FullName + "." + name + "(" + string.Join(",", inputTypes.Select(t => t.FullName)) + ")";
            var member = docs.Descendants("member").Single(e => (string)e.Attribute("name") == id);
            foreach (string tag in new[] { "summary", "param", "returns", "remarks", "exception", "example" })
                if (member.Element(tag) == null) throw new Exception("Missing member projection XML: " + id + " " + tag);
            if (!member.Elements("param").Select(e => (string)e.Attribute("name")).SequenceEqual(inputNames) ||
                member.Descendants().Any(e => e.Name == "include" || e.Name == "inheritdoc"))
                throw new Exception("Member projection XML signature or expansion changed: " + id);
        }
    }
}
