using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

public sealed class PlateCompositeNative
{
    public readonly List<string> Calls = new();
    public bool Success = true;
    private bool Record(string name, int plate, int load)
    { if (plate != 17 || load != 1) throw new Exception("Composite identity/order changed."); Calls.Add(name); return Success; }
    public object GetAllPlateCenterForces(int plate, int load, ref object values)
    { bool s = Record("forces", plate, load); values = new[] { 1d, -2d, 3d, -4d, 5d }; return s; }
    public object GetAllPlateCenterMoments(int plate, int load, ref object values)
    { bool s = Record("moments", plate, load); values = new[] { 6d, -7d, 8d }; return s; }
    public object GetPlateCenterVonMisesStresses(int plate, int load, ref object top, ref object bottom)
    { bool s = Record("von", plate, load); top = 9d; bottom = -10d; return s; }
    public object GetPlateCenterNormalPrincipalStresses(int plate, int load, ref object maxTop, ref object minTop,
        ref object maxBottom, ref object minBottom)
    { bool s = Record("principal", plate, load); maxTop = 11d; minTop = -12d; maxBottom = 13d; minBottom = -14d; return s; }
}
public sealed class PlateCompositeRoot : NativeRoot { public PlateCompositeNative Output { get; } = new(); }
internal static class PlateCompositeFixture
{
    internal static void Verify(XDocument docs)
    {
        var root = new PlateCompositeRoot(); using var wrapper = new OpenStaadWrapper(root);
        var lc = new LoadCase(1, "Case"); var plate = new Plate { Id = 17 };
        var one = wrapper.Output.GetPlateCenterResults(lc, plate);
        if (!root.Output.Calls.SequenceEqual(new[] { "forces", "moments", "von", "principal" }) ||
            one.Forces.Sx != 3 || one.Moments.Mxy != 8 || one.VonMises.Bottom != -10 || one.Principal.SminBottom != -14 ||
            !ReferenceEquals(one.LoadCase, lc) || !ReferenceEquals(one.Plate, plate)) throw new Exception("Composite mapping/order/context changed.");
        var task = wrapper.Output.GetPlateCenterResultsAsync(lc, plate);
        if (!task.IsCompletedSuccessfully || task.Result.Moments.Mx != 6) throw new Exception("Single settled task changed.");
        var rows = wrapper.Output.GetPlateCenterResults(new[] { lc, lc }, new[] { plate });
        var list = rows.ToArray(); if (list.Length != 2 || !((ICollection<PlateCenterResults>)rows).IsReadOnly ||
            ReferenceEquals(list[0].Moments, list[1].Moments)) throw new Exception("Batch ownership changed.");
        int before = root.Output.Calls.Count; rows.ToArray(); if (before != root.Output.Calls.Count) throw new Exception("Batch requery.");
        var batch = wrapper.Output.GetPlateCenterResultsAsync(new[] { lc }, new[] { plate });
        if (!batch.IsCompletedSuccessfully || batch.Result.Single().Forces.Sqx != 1) throw new Exception("Task generic result changed.");
        var principal = wrapper.Output.GetPlatePrincipalStressesAsync(lc, plate);
        if (!principal.IsCompletedSuccessfully || principal.Result.SmaxTop != 11) throw new Exception("Principal task changed.");
        root.Output.Success = false;
        if (!wrapper.Output.GetPlateCenterResultsAsync(lc, plate).IsFaulted ||
            !wrapper.Output.GetPlateCenterResultsAsync(new[] { lc }, new[] { plate }).IsFaulted ||
            !wrapper.Output.GetPlatePrincipalStressesAsync(lc, plate).IsFaulted) throw new Exception("Native failure lost.");
        foreach (var m in typeof(IOSOutput).GetMethods().Where(m => new[] { "GetPlateCenterResults", "GetPlateCenterResultsAsync", "GetPlatePrincipalStressesAsync" }.Contains(m.Name) && m.GetParameters().Length == 2))
        {
            bool many = m.GetParameters()[0].ParameterType.IsGenericType;
            Type result = m.Name.Contains("Principal") ? typeof(PlatePrincipalStresses) : many ? typeof(IEnumerable<PlateCenterResults>) : typeof(PlateCenterResults);
            if (m.ReturnType != (m.Name.EndsWith("Async") ? typeof(Task<>).MakeGenericType(result) : result) ||
                !m.GetParameters().Select(p => p.Name).SequenceEqual(many ? new[] { "loadCases", "plates" } : new[] { "loadCase", "plate" }) ||
                !m.GetParameters().Select(p => p.ParameterType).SequenceEqual(many ? new[] { typeof(IEnumerable<ILoadCase>), typeof(IEnumerable<Plate>) } : new[] { typeof(ILoadCase), typeof(Plate) }) ||
                m.GetParameters().Any(p => p.IsOptional || p.ParameterType.IsByRef)) throw new Exception("Composite source signature changed.");
        }
        foreach (var p in typeof(PlateCenterResults).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (!docs.Descendants("member").Any(x => (string)x.Attribute("name") == "P:" + typeof(PlateCenterResults).FullName + "." + p.Name && x.Element("summary") != null && x.Element("value") != null))
                throw new Exception("Composite property documentation missing.");
        var reused = PlateCenterResults.Create(one.Forces, one.Moments, one.VonMises, one.Principal);
        if (!ReferenceEquals(reused.Forces, one.Forces)) throw new Exception("Manual container cloned components.");
    }
}
