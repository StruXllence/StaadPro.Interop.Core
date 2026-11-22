using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

public sealed class SupportResultNative
{
    public object Status = true;
    public int Calls;
    private static void Check(object buffer)
    {
        if (buffer is not double[] values || values.Length != 6 || !values.All(double.IsNaN))
            throw new Exception("Support/static-check sentinels changed.");
    }
    public object GetSupportReactions(int node, int load, ref object values)
    {
        Check(values); Calls++;
        if (node != 2 || load != 1) throw new Exception("Support identity changed.");
        values = new[] { 1d, -2d, 3d, -4d, 5d, -6d }; return Status;
    }
    public object GetStaticCheckResult(int load, ref object forces, ref object reactions)
    {
        Check(forces); Check(reactions); Calls++;
        if (load != 1 || ReferenceEquals(forces, reactions)) throw new Exception("Static-check buffers/identity changed.");
        forces = new[] { 11d, -12d, 13d, -14d, 15d, -16d };
        reactions = new[] { 1d, -2d, 3d, -4d, 5d, -6d }; return Status;
    }
}
public sealed class SupportResultRoot : NativeRoot
{
    public SupportResultNative Output { get; } = new();
}
internal static class SupportResultFixture
{
    internal static void Verify(XDocument docs)
    {
        var root = new SupportResultRoot(); using var wrapper = new OpenStaadWrapper(root);
        var reactions = wrapper.Output.GetSupportReactions(2, 1);
        var forces = wrapper.Output.GetSupportForces(2, 1);
        if (reactions.Id != 1 || reactions.Forces.Fy != -2 || forces.Forces.Fy != 2 || forces.Forces.Mz != 6 || root.Output.Calls != 2)
            throw new Exception("Support identity, component order or opposite signs changed.");
        if (!wrapper.Output.GetSupportReactionsAsync(2, 1).IsCompletedSuccessfully || wrapper.Output.GetSupportForcesAsync(2, 1).Result.Forces.Mx != 4)
            throw new Exception("Support task behavior changed.");
        var task = wrapper.Output.GetStaticCheckResultAsync(1);
        if (!task.IsCompletedSuccessfully || task.Result.LoadCaseId != 1 || task.Result.Forces.Mz != -16 || task.Result.Reactions.Mz != -6 || task.Result.Differentials.Mz != -10)
            throw new Exception("Static-check component/difference mapping changed.");
        if (ReferenceEquals(task.Result.Forces, task.Result.Reactions)) throw new Exception("Static arrays were aliased.");
        root.Output.Status = false;
        bool rejected = false; try { wrapper.Output.GetSupportForces(2, 1); } catch (InvalidOperationException) { rejected = true; }
        if (!rejected || !wrapper.Output.GetStaticCheckResultAsync(1).IsFaulted) throw new Exception("Failed retrieval returned apparent force data.");
        foreach (string name in new[] { "GetSupportReactions", "GetSupportForces", "GetSupportReactionsAsync", "GetSupportForcesAsync", "GetStaticCheckResultAsync" })
        {
            bool isStatic = name == "GetStaticCheckResultAsync", isTask = name.EndsWith("Async", StringComparison.Ordinal);
            Type result = isStatic ? typeof(StaticCheckResult) : typeof(LoadCaseForces);
            if (isTask) result = typeof(Task<>).MakeGenericType(result);
            var method = typeof(IOSOutput).GetMethod(name);
            if (method == null || method.ReturnType != result || !method.GetParameters().Select(p => p.Name).SequenceEqual(isStatic ? new[] { "loadCaseId" } : new[] { "supportId", "loadCaseId" }) || method.GetParameters().Any(p => p.ParameterType != typeof(int) || p.IsOptional))
                throw new Exception("Support/static-check signature changed.");
        }
        foreach (Type type in new[] { typeof(LoadCaseForces), typeof(Forces), typeof(StaticCheckResult) })
        {
            if (!docs.Descendants("member").Any(x => (string)x.Attribute("name") == "T:" + type.FullName && x.Element("summary") != null))
                throw new Exception("Support result model IntelliSense missing.");
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (!docs.Descendants("member").Any(x => (string)x.Attribute("name") == "P:" + type.FullName + "." + property.Name && x.Element("summary") != null && x.Element("value") != null))
                    throw new Exception("Support result property IntelliSense missing.");
        }
    }
}
