using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

public sealed class PlateCornerNative
{
    public readonly List<string> Calls = new();
    public bool Success = true;
    public object GetPlateCornerForces(int plate, int node, int load, ref object buffer)
    {
        if (plate != 17 || load != 1 || node < 11 || node > 13 || buffer is not double[] values || values.Length != 6 || !values.All(double.IsNaN))
            throw new Exception("Corner signature/sentinels changed.");
        Calls.Add(plate + ":" + node + ":" + load); buffer = new[] { 1d, -2d, 3d, -4d, 5d, -6d }; return Success;
    }
}
public sealed class CornerGeometry
{
    public int Calls;
    public object GetPlateIncidence(int plate, ref object a, ref object b, ref object c, ref object d)
    { Calls++; a = 11; b = 12; c = 13; d = 0; return 0; }
    public void GetNodeCoordinates(int node, ref object x, ref object y, ref object z)
    { Calls++; x = 0d; y = 1d; z = 2d; }
}
public sealed class PlateCornerRoot : NativeRoot
{
    public PlateCornerNative Output { get; } = new();
    public new CornerGeometry Geometry { get; } = new();
}
internal static class PlateCornerFixture
{
    internal static void Verify(XDocument docs)
    {
        var root = new PlateCornerRoot(); using var wrapper = new OpenStaadWrapper(root);
        var lc = new LoadCase(1, "Case"); var a = new Node { Id = 11 }; var p = new Plate(a, new Node { Id = 12 }, new Node { Id = 13 }, new Node { Id = 11 }) { Id = 17 };
        var one = wrapper.Output.GetPlateCornerForces(lc, p, new Node { Id = 11 });
        if (one.Fx != 1 || one.Mz != -6 || !ReferenceEquals(one.CornerNode, a)) throw new Exception("Corner values/context changed.");
        var all = wrapper.Output.GetPlateCornerForces(lc, p);
        if (all.Count != 3 || all[1, 17, 11].Fy != -2 || !all.CornerNodeIds.SequenceEqual(new[] { 11, 12, 13 })) throw new Exception("Corner set indexing changed.");
        a.Id = 999;
        if (!ReferenceEquals(all.GetForPlateCorner(17, 11).Single(), all[1, 17, 11])) throw new Exception("Captured key changed with metadata.");
        a.Id = 11;
        if (!wrapper.Output.GetPlateCornerForcesAsync(lc, p, a).IsCompletedSuccessfully || !wrapper.Output.GetPlateCornerForcesAsync(lc, p).IsCompletedSuccessfully)
            throw new Exception("Corner settled task semantics changed.");
        var hydrated = wrapper.Output.GetPlateCornerForces(lc, new Plate { Id = 17 }, a);
        if (root.Geometry.Calls != 4 || hydrated.Plate.A.Id != 11 || hydrated.CornerNode.Y != 1) throw new Exception("Zero-success/void-output hydration changed.");
        root.Output.Success = false;
        if (!wrapper.Output.GetPlateCornerForcesAsync(lc, p).IsFaulted || !wrapper.Output.GetPlateCornerForcesAsync(lc, p, a).IsFaulted) throw new Exception("Native failure lost.");
        using var center = new OpenStaadWrapper(new PlateCompositeRoot());
        if (center.Output.GetPlateCenterResults(new[] { lc }, new[] { p }, 512).Count() != 1) throw new Exception("Thread-count compatibility changed.");
        var selected = typeof(IOSOutput).GetMethods().Where(m => m.Name.Contains("PlateCornerForces") || m.Name == "GetPlateCenterResults" && m.GetParameters().Length == 3).ToArray();
        if (selected.Length != 5) throw new Exception("Expected five new contracts.");
        foreach (var m in selected)
        {
            bool hint = m.Name == "GetPlateCenterResults", single = m.GetParameters().Length == 3 && !hint;
            Type type = hint ? typeof(IEnumerable<PlateCenterResults>) : single ? typeof(PlateCornerForces) : typeof(PlateCornerForcesResultSet);
            Type[] parameters = hint ? new[] { typeof(IEnumerable<ILoadCase>), typeof(IEnumerable<Plate>), typeof(int) } : single ? new[] { typeof(ILoadCase), typeof(Plate), typeof(Node) } : new[] { typeof(ILoadCase), typeof(Plate) };
            string[] names = hint ? new[] { "loadCases", "plates", "nThreads" } : single ? new[] { "loadCase", "plate", "cornerNode" } : new[] { "loadCase", "plate" };
            if (m.ReturnType != (m.Name.EndsWith("Async") ? typeof(Task<>).MakeGenericType(type) : type) || !m.GetParameters().Select(p => p.Name).SequenceEqual(names) ||
                !m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameters) || m.GetParameters().Any(p => p.IsOptional || p.ParameterType.IsByRef)) throw new Exception("Corner/hint contract changed.");
        }
        foreach (var type in new[] { typeof(PlateCornerForces), typeof(PlateCornerForcesKey), typeof(PlateCornerForcesResultSet), typeof(Plate) })
        {
            if (!docs.Descendants("member").Any(e => (string)e.Attribute("name") == "T:" + type.FullName && e.Element("summary") != null)) throw new Exception("Corner type documentation missing.");
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (!docs.Descendants("member").Any(e => ((string)e.Attribute("name")).StartsWith("P:" + type.FullName + "." + property.Name) && e.Element("summary") != null)) throw new Exception("Corner property documentation missing.");
        }
    }
}
