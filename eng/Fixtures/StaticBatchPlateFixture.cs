using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Base;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Interfaces;
using StaadPro.Interop.Core.Models;

public sealed class StaticBatchPlateNative
{
    public readonly List<string> Calls = new();
    public readonly List<int> Threads = new();
    public object Status = true;
    public object PlateValues = new double[] { 1, -2, 3, -4, 5 };
    private void Record(string name) { Calls.Add(name); Threads.Add(Environment.CurrentManagedThreadId); }
    public object GetStaticCheckResult(int load, ref object forces, ref object reactions)
    {
        if (forces is not double[] f || reactions is not double[] r || f.Length != 6 || r.Length != 6 ||
            !f.All(double.IsNaN) || !r.All(double.IsNaN) || ReferenceEquals(f, r))
            throw new Exception("Static batch buffer contract changed.");
        Record("static:" + load);
        forces = new double[] { 11, -12, 13, -14, 15, -16 };
        reactions = new double[] { 1, -2, 3, -4, 5, -6 };
        return Status;
    }
    public object GetAllPlateCenterForces(object plate, object load, ref object values)
    {
        if (plate is not int || load is not int || values is not double[] a || a.Length != 5 || !a.All(double.IsNaN))
            throw new Exception("Plate query identity/buffer contract changed.");
        Record("plate:" + plate + ":" + load);
        values = PlateValues; return Status;
    }
}
public sealed class StaticBatchPlateRoot : NativeRoot
{
    public StaticBatchPlateNative Output { get; } = new();
}
internal static class StaticBatchPlateFixture
{
    internal static void Verify(XDocument docs)
    {
        var native = new StaticBatchPlateRoot(); using var wrapper = new OpenStaadWrapper(native);
        var output = wrapper.Output;
        var batch = output.GetStaticCheckResults(new[] { 7, 3, 7 });
        if (batch.Count != 3 || batch.Count(r => r.LoadCaseId == 7) != 2 ||
            !native.Output.Calls.SequenceEqual(new[] { "static:7", "static:3", "static:7" }))
            throw new Exception("Batch occurrence/order contract changed.");
        var copies = batch.ToArray(); copies[0].Forces.Fx = 999;
        if (copies.Skip(1).Any(r => r.Forces.Fx != 11) || copies.Any(r => ReferenceEquals(r.Forces, r.Reactions)))
            throw new Exception("Static-check records alias component storage.");
        if (output.GetStaticCheckResults(new[] { 7 }, 512).Count != 1 ||
            !output.GetStaticCheckResultsAsync(new[] { 7 }).IsCompletedSuccessfully)
            throw new Exception("Sequential compatibility/task behavior changed.");
        var loadCase = new LoadCase(7, "Dead"); var plate = new Plate { Id = 17 };
        var stresses = output.GetPlateCenterForces(loadCase, plate);
        if (!new[] { stresses.Sqx, stresses.Sqy, stresses.Sx, stresses.Sy, stresses.Sxy }.SequenceEqual(new double[] { 1, -2, 3, -4, 5 }) ||
            !ReferenceEquals(stresses.LoadCase, loadCase) || !ReferenceEquals(stresses.Plate, plate) ||
            stresses is not PlateStress || stresses is not IPlateStress)
            throw new Exception("Plate stress/context mapping changed.");
        ((double[])native.Output.PlateValues)[0] = 999;
        if (stresses.Sqx != 1) throw new Exception("Plate result retained native storage.");
        var task = output.GetPlateCenterForcesAsync(loadCase, plate);
        if (!task.IsCompletedSuccessfully || native.Output.Threads.Any(t => t != Environment.CurrentManagedThreadId))
            throw new Exception("Plate task left owner thread or did not settle synchronously.");
        native.Output.Status = false;
        if (!output.GetStaticCheckResultsAsync(new[] { 7 }).IsFaulted ||
            !output.GetPlateCenterForcesAsync(loadCase, plate).IsFaulted)
            throw new Exception("Native failure did not fault task.");
        native.Output.Status = true;
        Action[] invalid = {
            () => output.GetStaticCheckResults(new[] { 7, 0 }),
            () => output.GetStaticCheckResults(Array.Empty<int>(), 513),
            () => output.GetPlateCenterForces(new LoadCase(0, ""), plate)
        };
        int before = native.Output.Calls.Count;
        foreach (Action call in invalid)
        {
            bool rejected = false; try { call(); } catch (ArgumentException) { rejected = true; }
            if (!rejected || native.Output.Calls.Count != before) throw new Exception("Invalid input dispatched native work.");
        }
        Action<IOSOutput>[] queries = {
            o => o.GetStaticCheckResults(new[] { 7 }), o => o.GetStaticCheckResults(new[] { 7 }, 2),
            o => o.GetStaticCheckResultsAsync(new[] { 7 }).GetAwaiter().GetResult(),
            o => o.GetPlateCenterForces(loadCase, plate), o => o.GetPlateCenterForcesAsync(loadCase, plate).GetAwaiter().GetResult()
        };
        wrapper.RefreshComObjects(); before = native.Output.Calls.Count;
        foreach (var query in queries)
        {
            bool rejected = false; try { query(output); } catch (InvalidOperationException) { rejected = true; }
            if (!rejected || native.Output.Calls.Count != before) throw new Exception("Stale Output remained callable.");
        }
        Type[] owners = { typeof(IOSOutput), typeof(OSOutputAdapter) };
        string[] names = { "GetStaticCheckResults", "GetStaticCheckResults", "GetStaticCheckResultsAsync", "GetPlateCenterForces", "GetPlateCenterForcesAsync" };
        for (int i = 0; i < names.Length; i++)
        {
            Type[] parameters = i >= 3 ? new[] { typeof(ILoadCase), typeof(Plate) } :
                i == 1 ? new[] { typeof(IEnumerable<int>), typeof(int) } : new[] { typeof(IEnumerable<int>) };
            string[] parameterNames = i >= 3 ? new[] { "loadCase", "plate" } :
                i == 1 ? new[] { "loadCaseIds", "nThreads" } : new[] { "loadCaseIds" };
            Type result = i >= 3 ? typeof(PlateCenterForces) : typeof(HashSet<StaticCheckResult>);
            if (i == 2 || i == 4) result = typeof(Task<>).MakeGenericType(result);
            XElement previous = null;
            foreach (Type owner in owners)
            {
                var method = owner.GetMethod(names[i], parameters);
                if (method == null || method.ReturnType != result ||
                    !method.GetParameters().Select(p => p.Name).SequenceEqual(parameterNames) ||
                    method.GetParameters().Any(p => p.IsOptional || p.ParameterType.IsByRef))
                    throw new Exception("New Output signature changed.");
                string suffix = i >= 3 ? "(StaadPro.Interop.Core.Entities.ILoadCase,StaadPro.Interop.Core.Entities.Plate)" :
                    i == 1 ? "(System.Collections.Generic.IEnumerable{System.Int32},System.Int32)" : "(System.Collections.Generic.IEnumerable{System.Int32})";
                var doc = docs.Descendants("member").Single(e => (string)e.Attribute("name") == "M:" + owner.FullName + "." + names[i] + suffix);
                foreach (string tag in new[] { "summary", "returns", "remarks", "exception", "example" })
                    if (doc.Element(tag) == null) throw new Exception("New Output IntelliSense missing.");
                if (previous != null && !doc.Elements().Select(e => e.ToString()).SequenceEqual(previous.Elements().Select(e => e.ToString())))
                    throw new Exception("New interface/concrete documentation differ.");
                previous = doc;
            }
        }
        foreach (Type model in new[] { typeof(Plate), typeof(IPlateStress), typeof(PlateStress), typeof(PlateCenterForces) })
        {
            if (!docs.Descendants("member").Any(e => (string)e.Attribute("name") == "T:" + model.FullName && e.Element("summary") != null))
                throw new Exception("Plate model IntelliSense missing.");
            foreach (var property in model.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (!docs.Descendants("member").Any(e => (string)e.Attribute("name") == "P:" + model.FullName + "." + property.Name && e.Element("summary") != null && e.Element("value") != null))
                    throw new Exception("Plate property IntelliSense missing.");
        }
        if (typeof(Plate).GetProperty("A") != null) throw new Exception("Identity support unexpectedly hydrated plate geometry.");
    }
}
