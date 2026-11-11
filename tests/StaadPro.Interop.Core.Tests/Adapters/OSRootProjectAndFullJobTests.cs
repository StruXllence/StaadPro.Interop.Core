using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Enums.Root;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Root;
using MetadataRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class OSRootProjectAndFullJobTests
    {
        private static readonly string[] Operations = ["full", "analyze", "get", "set", "setObject"];
        private static readonly string[] FieldNames = ["jobName", "jobClient", "engineerName", "engineerDate", "jobNumber", "revision", "part", "reference", "checkerName", "checkerDate", "approverName", "approverDate", "comments"];
        private static readonly object[] TrueFlags = [true, (sbyte)1, (byte)1, (short)1, (ushort)1, 1, 1u, 1L, 1UL];
        private static readonly object[] FalseFlags = [false, (sbyte)0, (byte)0, (short)0, (ushort)0, 0, 0u, 0L, 0UL];
        private static readonly object[] InvalidFlags = [null, "true", "1", -1, 2, 1d, 1f, 1m, ulong.MaxValue, DBNull.Value, DayOfWeek.Monday];

        public sealed class NativeRoot : MetadataRoot
        {
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];
            public string[] FullFields, Assigned;
            public object Engine, QueryFlag = true, SetFlag = true, ProjectId = "ID", ProjectName = "Project";
            public bool WriteId = true, WriteName = true, SwapSentinels;
            public Exception Failure;
            public Action<string> OnCall;
            private void Step(string operation)
            {
                Calls.Add(operation); Threads.Add(Environment.CurrentManagedThreadId);
                OnCall?.Invoke(operation);
                if (Failure != null) throw Failure;
            }
            public void SetFullJobInfo(object a, object b, object c, object d, object e, object f, object g, object h, object i, object j, object k, object l, object m)
            {
                object[] fields = [a,b,c,d,e,f,g,h,i,j,k,l,m];
                Assert.That(fields, Is.All.TypeOf<string>());
                FullFields = fields.Cast<string>().ToArray(); Step("full");
            }
            public void AnalyzeModel(object engine) { Engine = engine; Step("analyze"); }
            public object GetCONNECTEDProjectInfo(ref object id, ref object name)
            {
                Assert.That(id, Is.TypeOf<string>()); Assert.That(name, Is.TypeOf<string>());
                Assert.That(id, Is.Not.EqualTo(name));
                Step("get");
                if (SwapSentinels) (id, name) = (name, id);
                else { if (WriteId) id = ProjectId; if (WriteName) name = ProjectName; }
                return QueryFlag;
            }
            public object SetCONNECTEDProjectInfo(object id, object name)
            {
                Assert.That(id, Is.TypeOf<string>()); Assert.That(name, Is.TypeOf<string>());
                Assigned = [(string)id, (string)name]; Step("set"); return SetFlag;
            }
        }

        private static void Full(IOSRoot root, string[] fields) => root.SetFullJobInfo(fields[0],fields[1],fields[2],fields[3],fields[4],fields[5],fields[6],fields[7],fields[8],fields[9],fields[10],fields[11],fields[12]);
        private static string[] Fields() => Enumerable.Range(0,13).Select(i => " field " + i + "\r\n\u03a9 ").ToArray();
        private static void Request(IOSRoot root, string operation)
        {
            switch (operation)
            {
                case "full": Full(root, Fields()); break;
                case "analyze": root.AnalyzeModel(); break;
                case "get": root.GetConnectedProjectInfo(); break;
                case "set": root.SetConnectedProjectInfo("id", "name"); break;
                case "setObject": root.SetConnectedProjectInfo(new ConnectedProjectInfo("id", "name")); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }

        [TestCaseSource(nameof(Operations))]
        public void UsesOnlyMatchingNativeMemberOnOwnerThreadWithoutRefreshingOrQueryingMetadata(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root; object geometry = wrapper.RawGeometry; var units = wrapper.InputLengthUnit; native.Queries.Clear();
            Request(root, operation);
            Assert.That(native.Calls, Is.EqualTo(new[] { operation == "setObject" ? "set" : operation }));
            Assert.That(native.Threads, Is.EqualTo(new[] { Environment.CurrentManagedThreadId }));
            Assert.That(native.Queries, Is.Empty); Assert.That(wrapper.Root, Is.SameAs(root));
            Assert.That((object)wrapper.RawGeometry, Is.SameAs(geometry)); Assert.That(wrapper.InputLengthUnit, Is.EqualTo(units));
        }

        [Test]
        public void FullJobSendsThirteenLiteralFieldsInNativeOrderIncludingUnparsedDates()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            string[] fields = Fields(); fields[3] = "not-a-date"; fields[9] = ""; fields[11] = "31/02/26";
            Full(wrapper.Root, fields); Assert.That(native.FullFields, Is.EqualTo(fields));
            Full(wrapper.Root, Enumerable.Repeat("",13).ToArray()); Assert.That(native.FullFields, Is.All.Empty);
        }

        [TestCaseSource(nameof(FieldNames))]
        public void EveryNullFullJobFieldRejectsBeforeNativeWorkAndNamesTheParameter(string field)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            string[] fields = Fields(); fields[Array.IndexOf(FieldNames,field)] = null;
            Assert.That(Assert.Throws<ArgumentNullException>(() => Full(wrapper.Root,fields)).ParamName, Is.EqualTo(field));
            Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void AnalysisDefaultAndExplicitEngineSendInt32MinusOneAndReturnVoid()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            wrapper.Root.AnalyzeModel(); Assert.That(native.Engine, Is.TypeOf<int>().And.EqualTo(-1));
            wrapper.Root.AnalyzeModel(AnalysisEngine.StaadPro); Assert.That(native.Engine, Is.TypeOf<int>().And.EqualTo(-1));
            Assert.That(native.Calls, Is.EqualTo(new[] { "analyze", "analyze" }));
        }

        [TestCase(int.MinValue)] [TestCase(-2)] [TestCase(0)] [TestCase(1)] [TestCase(int.MaxValue)]
        public void UndocumentedEngineCodesRejectWithoutDispatch(int code)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Root.AnalyzeModel((AnalysisEngine)code)).ParamName, Is.EqualTo("engine"));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(TrueFlags))]
        public void SuccessfulProjectQueryPreservesStringsAndReturnsIndependentImmutableSnapshots(object flag)
        {
            var native = new NativeRoot { QueryFlag = flag, ProjectId = " id \r\n\u03a9 ", ProjectName = " Name \t" };
            using var wrapper = new OpenStaadWrapper(native);
            var result = wrapper.Root.GetConnectedProjectInfo(); var next = wrapper.Root.GetConnectedProjectInfo();
            Assert.That(result.IsSuccessful, Is.True); Assert.That(result.Project.ProjectId, Is.EqualTo(native.ProjectId));
            Assert.That(result.Project.Name, Is.EqualTo(native.ProjectName)); Assert.That(next, Is.Not.SameAs(result));
            Assert.That(next.Project, Is.Not.SameAs(result.Project));
            native.ProjectId = "changed"; Assert.That(result.Project.ProjectId, Is.EqualTo(" id \r\n\u03a9 "));
        }

        [TestCaseSource(nameof(FalseFlags))]
        public void NativeFalseDiscardsEvenPartialMalformedOrUnwrittenProjectOutputs(object flag)
        {
            var native = new NativeRoot { QueryFlag = flag, ProjectId = 123, WriteName = false };
            using var wrapper = new OpenStaadWrapper(native);
            var result = wrapper.Root.GetConnectedProjectInfo();
            Assert.That(result.IsSuccessful, Is.False); Assert.That(result.Project, Is.Null);
        }

        [TestCaseSource(nameof(InvalidFlags))]
        public void InvalidFlagsThrowForQueriesAndBothAssignmentOverloads(object flag)
        {
            var native = new NativeRoot { QueryFlag = flag, SetFlag = flag }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetConnectedProjectInfo());
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.SetConnectedProjectInfo("id","name"));
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.SetConnectedProjectInfo(new ConnectedProjectInfo("id","name")));
            Assert.That(native.Calls, Is.EqualTo(new[] { "get", "set", "set" }));
        }

        [TestCase(true,false)] [TestCase(false,true)] [TestCase(false,false)]
        public void NativeSuccessRequiresBothOutputsToHaveBeenWritten(bool id, bool name)
        {
            var native = new NativeRoot { WriteId = id, WriteName = name }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetConnectedProjectInfo());
        }

        [Test]
        public void SwappedUnwrittenMarkersAreNeverExposedAsSuccessfulProjectData()
        {
            using var wrapper = new OpenStaadWrapper(new NativeRoot { SwapSentinels = true });
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetConnectedProjectInfo());
        }

        [TestCase(null)] [TestCase(12)] [TestCase(true)]
        public void NativeSuccessRequiresStringOutputs(object malformed)
        {
            var native = new NativeRoot { ProjectId = malformed }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetConnectedProjectInfo());
            native.ProjectId = "ID"; native.ProjectName = malformed;
            Assert.Throws<InvalidOperationException>(() => wrapper.Root.GetConnectedProjectInfo());
        }

        [Test]
        public void SuccessfulExplicitEmptyTextIsAQuerySnapshotButCannotBeAssigned()
        {
            var native = new NativeRoot { ProjectId = "", ProjectName = "" }; using var wrapper = new OpenStaadWrapper(native);
            var result = wrapper.Root.GetConnectedProjectInfo(); Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Project.ProjectId, Is.Empty); Assert.That(result.Project.Name, Is.Empty);
            Assert.That(Assert.Throws<ArgumentException>(() => wrapper.Root.SetConnectedProjectInfo(result.Project)).ParamName, Is.EqualTo("projectInfo"));
            Assert.That(native.Calls, Is.EqualTo(new[] { "get" }));
        }

        [TestCaseSource(nameof(TrueFlags))] [TestCaseSource(nameof(FalseFlags))]
        public void BothAssignmentOverloadsPreserveExactNonblankTextAndNativeFlags(object flag)
        {
            var native = new NativeRoot { SetFlag = flag }; using var wrapper = new OpenStaadWrapper(native);
            var info = new ConnectedProjectInfo(" non-guid \u03a9 ", " Name \r\n ");
            bool expected = flag.Equals(true) || Convert.ToInt64(flag) == 1;
            Assert.That(wrapper.Root.SetConnectedProjectInfo(info.ProjectId,info.Name), Is.EqualTo(expected));
            Assert.That(native.Assigned, Is.EqualTo(new[] { info.ProjectId, info.Name }));
            Assert.That(wrapper.Root.SetConnectedProjectInfo(info), Is.EqualTo(expected));
            Assert.That(native.Assigned, Is.EqualTo(new[] { info.ProjectId, info.Name }));
            Assert.That(native.Calls, Is.EqualTo(new[] { "set", "set" }));
        }

        [TestCase(null,"name","projectId")] [TestCase("id",null,"projectName")]
        [TestCase("","name","projectId")] [TestCase("id","","projectName")]
        [TestCase(" \t","name","projectId")] [TestCase("id","\r\n","projectName")]
        public void InvalidAssignmentInputsIdentifyTheFieldBeforeNativeWork(string id, string name, string parameter)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var error = Assert.Catch<ArgumentException>(() => wrapper.Root.SetConnectedProjectInfo(id,name));
            Assert.That(error.ParamName, Is.EqualTo(parameter));
            if (id == null || name == null) Assert.That(error, Is.TypeOf<ArgumentNullException>());
            else Assert.That(Assert.Throws<ArgumentException>(() => wrapper.Root.SetConnectedProjectInfo(new ConnectedProjectInfo(id,name))).ParamName, Is.EqualTo("projectInfo"));
            Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void ProjectModelsRejectNullAndExposeNoMutationOrPublicResultConstructor()
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => new ConnectedProjectInfo(null,"n")).ParamName, Is.EqualTo("projectId"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => new ConnectedProjectInfo("i",null)).ParamName, Is.EqualTo("projectName"));
            using var wrapper = new OpenStaadWrapper(new NativeRoot());
            Assert.That(Assert.Throws<ArgumentNullException>(() => wrapper.Root.SetConnectedProjectInfo((ConnectedProjectInfo)null)).ParamName, Is.EqualTo("projectInfo"));
            foreach (Type type in new[] { typeof(ConnectedProjectInfo),typeof(ConnectedProjectResult) })
            {
                Assert.That(type.IsSealed, Is.True); Assert.That(type.GetProperties().Any(p=>p.GetSetMethod(true)!=null), Is.False);
            }
            Assert.That(typeof(ConnectedProjectResult).GetConstructors(), Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void OriginalNativeErrorsPropagateAfterPossibleEffectsAndLeaseIsReleased(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var error = new COMException("native failure after effects",unchecked((int)0x80004005)); native.Failure = error;
            Assert.That(Assert.Throws<COMException>(() => Request(wrapper.Root,operation)), Is.SameAs(error));
            Assert.That(native.Calls, Has.Count.EqualTo(1));
            native.Failure = null; Assert.DoesNotThrow(() => Request(wrapper.Root,operation));
        }

        [TestCaseSource(nameof(Operations))]
        public void MissingNativeMembersPropagateWithoutFallback(string operation)
        {
            using var wrapper = new OpenStaadWrapper(new MetadataRoot());
            Assert.Throws<RuntimeBinderException>(() => Request(wrapper.Root,operation));
            Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void DisposedStaleManualNoncurrentAndOtherThreadCallsNeverReachNative(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            var root = wrapper.Root;
            Assert.Throws<InvalidOperationException>(() => Request(new OSRootAdapter(wrapper),operation));
            Exception error = null;
            var worker = new Thread(() => { try { Request(root,operation); } catch(Exception e) { error=e; } });
            worker.Start(); Assert.That(worker.Join(5000), Is.True); Assert.That(error, Is.TypeOf<InvalidOperationException>());
            wrapper.RefreshComObjects(); Assert.Throws<InvalidOperationException>(() => Request(root,operation));
            root = wrapper.Root; wrapper.Dispose(); Assert.Throws<ObjectDisposedException>(() => Request(root,operation));
            Assert.That(native.Calls, Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void OneLeaseRejectsReentrantRootRefreshAndDisposeWork(string operation)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.OnCall = _ =>
            {
                foreach (string nested in Operations) Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root,nested));
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshUnits());
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(() => wrapper.Dispose());
            };
            Request(wrapper.Root,operation); Assert.That(native.Calls, Has.Count.EqualTo(1));
            native.OnCall = null; Assert.DoesNotThrow(() => wrapper.Root.GetBaseUnit());
        }

        [TestCaseSource(nameof(Operations))]
        public void ExactSignaturesDefaultsAndExpandedIntelliSenseMatchOnInterfaceAndAdapter(string operation)
        {
            string name = operation == "full" ? "SetFullJobInfo" : operation == "analyze" ? "AnalyzeModel" : operation == "get" ? "GetConnectedProjectInfo" : "SetConnectedProjectInfo";
            Type[] types = operation == "full" ? Enumerable.Repeat(typeof(string),13).ToArray() : operation == "analyze" ? [typeof(AnalysisEngine)] : operation == "get" ? [] : operation == "set" ? [typeof(string),typeof(string)] : [typeof(ConnectedProjectInfo)];
            string[] names = operation == "full" ? FieldNames : operation == "analyze" ? ["engine"] : operation == "get" ? [] : operation == "set" ? ["projectId","projectName"] : ["projectInfo"];
            Type result = operation == "full" || operation == "analyze" ? typeof(void) : operation == "get" ? typeof(ConnectedProjectResult) : typeof(bool);
            foreach(Type owner in new[] {typeof(IOSRoot),typeof(OSRootAdapter)})
            {
                var method = owner.GetMethod(name,types); Assert.That(method, Is.Not.Null); Assert.That(method.ReturnType, Is.EqualTo(result));
                Assert.That(method.GetParameters().Select(p=>p.Name), Is.EqualTo(names));
                Assert.That(method.GetParameters().Any(p=>p.IsOut||p.ParameterType.IsByRef), Is.False);
                if(operation=="analyze") { Assert.That(method.GetParameters()[0].IsOptional, Is.True); Assert.That(method.GetParameters()[0].DefaultValue, Is.EqualTo(AnalysisEngine.StaadPro)); }
                else Assert.That(method.GetParameters().Any(p=>p.IsOptional), Is.False);
            }
            string suffix=name+(types.Length==0?"":"("+string.Join(",",types.Select(t=>t.FullName))+")");
            var docs=XDocument.Load(Path.ChangeExtension(typeof(IOSRoot).Assembly.Location,".xml"));
            var contract=docs.Descendants("member").Single(e=>(string)e.Attribute("name")=="M:"+typeof(IOSRoot).FullName+"."+suffix);
            var concrete=docs.Descendants("member").Single(e=>(string)e.Attribute("name")=="M:"+typeof(OSRootAdapter).FullName+"."+suffix);
            Assert.That(contract.Elements().Select(e=>e.ToString()), Is.EqualTo(concrete.Elements().Select(e=>e.ToString())));
            foreach(string tag in new[]{"summary","remarks","exception","example"}) Assert.That(contract.Element(tag)?.Value, Is.Not.Null.And.Not.Empty);
            Assert.That(contract.Elements("param").Select(e=>(string)e.Attribute("name")), Is.EqualTo(names));
            if(result!=typeof(void)) Assert.That(contract.Element("returns")?.Value, Is.Not.Null.And.Not.Empty);
            Assert.That(contract.Descendants().Any(e=>e.Name=="include"||e.Name=="inheritdoc"), Is.False);
        }
    }
}
