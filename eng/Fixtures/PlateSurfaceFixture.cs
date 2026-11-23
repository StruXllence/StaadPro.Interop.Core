using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

public sealed class PlateSurfaceNative
{
    public object Status = true;
    public int Calls;
    private static void Check(object value)
    {
        if (value is not double number || !double.IsNaN(number)) throw new Exception("Surface sentinel changed.");
    }
    private void Record(int plate, int load)
    {
        if (plate != 17 || load != 1) throw new Exception("Plate/case native order changed.");
        Calls++;
    }
    public object GetAllPlateCenterMoments(int plate, int load, ref object values)
    {
        if (values is not double[] a || a.Length != 3 || !a.All(double.IsNaN)) throw new Exception("Moment buffer changed.");
        Record(plate, load); values = new[] { 11d, -12d, 13d }; return Status;
    }
    public object GetPlateCenterVonMisesStresses(int plate, int load, ref object top, ref object bottom)
    {
        Check(top); Check(bottom); Record(plate, load); top = 17d; bottom = -19d; return Status;
    }
    public object GetPlateCenterNormalPrincipalStresses(int plate, int load, ref object maxTop, ref object minTop,
        ref object maxBottom, ref object minBottom)
    {
        Check(maxTop); Check(minTop); Check(maxBottom); Check(minBottom); Record(plate, load);
        maxTop = 23d; minTop = -29d; maxBottom = 31d; minBottom = -37d; return Status;
    }
}
public sealed class PlateSurfaceRoot : NativeRoot
{
    public PlateSurfaceNative Output { get; } = new();
}
internal static class PlateSurfaceFixture
{
    internal static void Verify(XDocument docs)
    {
        var root = new PlateSurfaceRoot(); using var wrapper = new OpenStaadWrapper(root);
        var lc = new LoadCase(1, "Case"); var plate = new Plate { Id = 17 };
        var moments = wrapper.Output.GetPlateCenterMoments(lc, plate);
        if (moments.Mx != 11 || moments.My != -12 || moments.Mxy != 13 || !ReferenceEquals(moments.LoadCase, lc) || !ReferenceEquals(moments.Plate, plate))
            throw new Exception("Moment mapping/context changed.");
        var momentTask = wrapper.Output.GetPlateCenterMomentsAsync(lc, plate);
        if (!momentTask.IsCompletedSuccessfully || momentTask.Result.Mxy != 13) throw new Exception("Moment task semantics changed.");
        moments.Mx = 900; if (momentTask.Result.Mx != 11) throw new Exception("Moment result storage is shared.");
        var von = wrapper.Output.GetPlateCenterVonMises(lc, plate);
        if (von.Top != 17 || von.Bottom != -19 || von.AbsoluteMaximum != 19) throw new Exception("Von Mises mapping changed.");
        var vonTask = wrapper.Output.GetPlateCenterVonMisesAsync(lc, plate);
        if (!vonTask.IsCompletedSuccessfully || vonTask.Result.Bottom != -19) throw new Exception("Von Mises task semantics changed.");
        var principal = wrapper.Output.GetPlatePrincipalStresses(lc, plate);
        if (principal.SmaxTop != 23 || principal.SminTop != -29 || principal.SmaxBottom != 31 || principal.SminBottom != -37 || root.Output.Calls != 5)
            throw new Exception("Principal surface/order mapping changed.");
        root.Output.Status = false;
        bool failed = false; try { wrapper.Output.GetPlatePrincipalStresses(lc, plate); } catch (InvalidOperationException) { failed = true; }
        if (!failed || !wrapper.Output.GetPlateCenterMomentsAsync(lc, plate).IsFaulted || !wrapper.Output.GetPlateCenterVonMisesAsync(lc, plate).IsFaulted)
            throw new Exception("Failed native reads returned apparent results.");
        foreach (string name in new[] { "GetPlateCenterMoments", "GetPlateCenterMomentsAsync", "GetPlateCenterVonMises", "GetPlateCenterVonMisesAsync", "GetPlatePrincipalStresses" })
        {
            var method = typeof(IOSOutput).GetMethod(name);
            Type model = name.Contains("Moments") ? typeof(PlateCenterMoments) : name.Contains("VonMises") ? typeof(PlateCenterVonMises) : typeof(PlatePrincipalStresses);
            Type result = name.EndsWith("Async", StringComparison.Ordinal) ? typeof(Task<>).MakeGenericType(model) : model;
            if (method == null || method.ReturnType != result || !method.GetParameters().Select(p => p.Name).SequenceEqual(new[] { "loadCase", "plate" }) ||
                !method.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(ILoadCase), typeof(Plate) }) || method.GetParameters().Any(p => p.IsOptional))
                throw new Exception("Surface query signature changed.");
        }
        foreach (Type type in new[] { typeof(PlateCenterMoments), typeof(PlateCenterVonMises), typeof(PlatePrincipalStresses) })
        {
            if (!docs.Descendants("member").Any(x => (string)x.Attribute("name") == "T:" + type.FullName && x.Element("summary") != null))
                throw new Exception("Surface model IntelliSense missing.");
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (!docs.Descendants("member").Any(x => (string)x.Attribute("name") == "P:" + type.FullName + "." + p.Name && x.Element("summary") != null && x.Element("value") != null))
                    throw new Exception("Surface property IntelliSense missing.");
        }
    }
}
