using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using MetadataRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class StaticBatchAndPlateCenterTests
    {
        private static readonly string[] Operations = ["batch","threads","batchTask","plate","plateTask"];
        private static readonly object[] GoodStatuses = [true,(sbyte)1,(byte)1,(short)1,(ushort)1,1,1u,1L,1UL];
        private static readonly object[] BadStatuses = [false,0,null,-1,2,"true","1",1d,1f,1m,DayOfWeek.Monday,ulong.MaxValue,DBNull.Value];
        private static readonly object[] BadPlateArrays = [null,new double[0],new double[4],new double[6],
            new double[,]{{1,2,3,4,5}},new float[]{1,2,3,4,5},new int[]{1,2,3,4,5},
            new object[]{1d,2d,3d,4d,5d},new double[]{1,2,3,4,double.NaN},
            new double[]{1,2,3,4,double.PositiveInfinity},new double[]{1,2,3,4,double.NegativeInfinity}];

        public sealed class Native
        {
            public object Status=true, PlateValues=new double[]{1,-2,3,-4,5};
            public object Loads=new double[]{11,-12,13,-14,15,-16}, Reactions=new double[]{1,-2,3,-4,5,-6};
            public bool Write=true, InPlace, WriteReactions=true;
            public Exception Failure;
            public Action<string> OnCall;
            public readonly List<string> Calls=[];
            public readonly List<int> Threads=[];
            private void Step(string name)
            {
                Calls.Add(name);Threads.Add(Environment.CurrentManagedThreadId);OnCall?.Invoke(name);
                if(Failure!=null)throw Failure;
            }
            private static void Check(object buffer,int length)
            { Assert.That(buffer,Is.TypeOf<double[]>());Assert.That((double[])buffer,Has.Length.EqualTo(length));Assert.That((double[])buffer,Is.All.NaN); }
            public object GetStaticCheckResult(int id,ref object loads,ref object reactions)
            {
                Check(loads,6);Check(reactions,6);Assert.That(loads,Is.Not.SameAs(reactions));
                Step("static:"+id);
                if(Write)
                {
                    if(InPlace)Array.Copy((double[])Loads,(double[])loads,6);else loads=Loads;
                    if(WriteReactions){if(InPlace)Array.Copy((double[])Reactions,(double[])reactions,6);else reactions=Reactions;}
                }
                return Status;
            }
            public object GetAllPlateCenterForces(object plate,object load,ref object values)
            {
                Assert.That(plate,Is.TypeOf<int>());Assert.That(load,Is.TypeOf<int>());Check(values,5);
                Step("plate:"+plate+":"+load);
                if(Write){if(InPlace)Array.Copy((double[])PlateValues,(double[])values,5);else values=PlateValues;}
                return Status;
            }
        }
        private static OpenStaadWrapper Wrap(object n)=>new(new MetadataRoot{Output=n});
        private static HashSet<StaticCheckResult> Batch(IOSOutput o,string op,IEnumerable<int> ids)=>op switch
        { "threads"=>o.GetStaticCheckResults(ids,3),"batchTask"=>o.GetStaticCheckResultsAsync(ids).GetAwaiter().GetResult(),_=>o.GetStaticCheckResults(ids)};
        private static PlateCenterForces PlateRead(IOSOutput o,string op,ILoadCase lc,Plate p)=>
            op=="plateTask"?o.GetPlateCenterForcesAsync(lc,p).GetAwaiter().GetResult():o.GetPlateCenterForces(lc,p);
        private static object Invoke(IOSOutput o,string op)=>op.StartsWith("plate",StringComparison.Ordinal)?
            PlateRead(o,op,new LoadCase(7,"Dead"),new Plate{Id=17}):Batch(o,op,new[]{7,3,7});
        private static double[] Components(PlateCenterForces p)=>[p.Sqx,p.Sqy,p.Sx,p.Sy,p.Sxy];

        [TestCase("batch"),TestCase("threads"),TestCase("batchTask")]
        public void BatchCapturesOncePreservesEveryOccurrenceAndReturnsIndependentMaterializedRecords(string op)
        {
            var n=new Native();using var w=Wrap(n);var input=new List<int>{7,3,7};
            n.OnCall=_=>input.Clear();
            var result=Batch(w.Output,op,new Once<int>(input));
            Assert.That(n.Calls,Is.EqualTo(new[]{"static:7","static:3","static:7"}));
            Assert.That(result,Has.Count.EqualTo(3));Assert.That(result.Count(r=>r.LoadCaseId==7),Is.EqualTo(2));
            Assert.That(result.All(r=>r.Forces.Fx==11&&r.Reactions.My==5&&r.Differentials.Mz==-10),Is.True);
            var first=result.First();first.Forces.Fx=900;
            Assert.That(result.Count(r=>r.Forces.Fx==11),Is.EqualTo(2));
            Assert.That(result.All(r=>!ReferenceEquals(r.Forces,r.Reactions)),Is.True);
            ((double[])n.Loads)[0]=800;Assert.That(result.Count(r=>r.Forces.Fx==11),Is.EqualTo(2));
            Assert.That(result.ToArray(),Has.Length.EqualTo(3));Assert.That(n.Calls,Has.Count.EqualTo(3));
        }

        [TestCase("batch"),TestCase("threads"),TestCase("batchTask")]
        public void BatchNullInvalidOversizedAndFailingEnumeratorsMakeNoNativeCalls(string op)
        {
            var n=new Native();using var w=Wrap(n);
            Assert.That(Assert.Throws<ArgumentNullException>(()=>Batch(w.Output,op,null)).ParamName,Is.EqualTo("loadCaseIds"));
            foreach(int bad in new[]{0,-1,int.MinValue})
                Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>Batch(w.Output,op,new[]{7,bad})).ParamName,Is.EqualTo("loadCaseIds"));
            Assert.That(Assert.Throws<ArgumentException>(()=>Batch(w.Output,op,Enumerable.Repeat(7,100001))).ParamName,Is.EqualTo("loadCaseIds"));
            Assert.Throws<ApplicationException>(()=>Batch(w.Output,op,Broken()));Assert.That(n.Calls,Is.Empty);
        }

        [TestCase("batch"),TestCase("threads"),TestCase("batchTask")]
        public void BatchEmptyInputsReturnFreshSetsAndExactMaximumOccurrencesAreAccepted(string op)
        {
            var n=new Native{Status=false};using var w=Wrap(n);
            var a=Batch(w.Output,op,Array.Empty<int>());var b=Batch(w.Output,op,Array.Empty<int>());
            Assert.That(a,Is.Empty);Assert.That(b,Is.Empty);Assert.That(b,Is.Not.SameAs(a));Assert.That(n.Calls,Is.Empty);
            Assert.Throws<InvalidOperationException>(()=>Batch(w.Output,op,Enumerable.Repeat(7,100000)));
            Assert.That(n.Calls,Is.EqualTo(new[]{"static:7"})); // accepted input, then native failure
        }

        [TestCase(-1),TestCase(0),TestCase(513),TestCase(int.MaxValue)]
        public void CompatibilityThreadRangeRejectsBeforeEnumeration(int count)
        {
            var n=new Native();using var w=Wrap(n);
            var ids=new Once<int>(new[]{7},()=>Assert.Fail("Must validate count before enumeration."));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>w.Output.GetStaticCheckResults(ids,count)).ParamName,Is.EqualTo("nThreads"));
            Assert.That(n.Calls,Is.Empty);
        }

        [TestCase(1),TestCase(2),TestCase(512)]
        public void ThreadHintNeverDispatchesNativeWorkOffTheOwnerThread(int count)
        {
            var n=new Native();using var w=Wrap(n);
            var result=w.Output.GetStaticCheckResults(new[]{7,3,7},count);
            Assert.That(result,Has.Count.EqualTo(3));Assert.That(n.Threads,Is.All.EqualTo(Environment.CurrentManagedThreadId));
        }

        [TestCase("batch"),TestCase("threads"),TestCase("batchTask")]
        public void LaterBatchFailureStopsRemainingCallsWithoutReturningPartialSetAndReleasesLease(string op)
        {
            var n=new Native();using var w=Wrap(n);var failure=new COMException("later read",unchecked((int)0x80010108));
            n.OnCall=call=>{if(call=="static:3")throw failure;};
            Assert.That(Assert.Throws<COMException>(()=>Batch(w.Output,op,new[]{7,3,9})),Is.SameAs(failure));
            Assert.That(n.Calls,Is.EqualTo(new[]{"static:7","static:3"}));n.OnCall=null;
            Assert.DoesNotThrow(()=>w.Root.GetBaseUnit());Assert.That(Batch(w.Output,op,new[]{9}),Has.Count.EqualTo(1));
        }

        [TestCase("batch"),TestCase("threads"),TestCase("batchTask")]
        public void BothStaticVectorsMustQualifyForEveryRecordIncludingAliasedAndInPlaceBuffers(string op)
        {
            var n=new Native{WriteReactions=false};using var w=Wrap(n);
            Assert.Throws<InvalidOperationException>(()=>Batch(w.Output,op,new[]{7}));
            n.WriteReactions=true;n.InPlace=true;
            var a=Batch(w.Output,op,new[]{7}).Single();Assert.That(a.Forces.Fx,Is.EqualTo(11));
            n.InPlace=false;n.Reactions=n.Loads;
            var b=Batch(w.Output,op,new[]{7}).Single();b.Forces.Fx=500;Assert.That(b.Reactions.Fx,Is.EqualTo(11));
            n.Loads=new double[]{1,2,3,4,5,double.NaN};Assert.Throws<InvalidOperationException>(()=>Batch(w.Output,op,new[]{7}));
            n.Loads=new float[]{1,2,3,4,5,6};Assert.Throws<InvalidOperationException>(()=>Batch(w.Output,op,new[]{7}));
        }

        [TestCaseSource(nameof(GoodStatuses))]
        public void StrictBooleanOrIntegralOnePlateSuccessMapsAllFiveSignedStressComponents(object flag)
        {
            var n=new Native{Status=flag};using var w=Wrap(n);var lc=new LoadCase(7,"Never read");var p=new Plate{Id=17};
            var result=w.Output.GetPlateCenterForces(lc,p);
            Assert.That(Components(result),Is.EqualTo(new[]{1d,-2d,3d,-4d,5d}));
            Assert.That(result.LoadCase,Is.SameAs(lc));Assert.That(result.Plate,Is.SameAs(p));
            Assert.That(n.Calls,Is.EqualTo(new[]{"plate:17:7"}));
        }

        [TestCaseSource(nameof(BadStatuses))]
        public void FailedOrMalformedStatusesNeverPublishWrittenPlateOrBatchData(object flag)
        {
            var n=new Native{Status=flag};using var w=Wrap(n);
            Assert.Throws<InvalidOperationException>(()=>w.Output.GetPlateCenterForces(new LoadCase(7,""),new Plate{Id=17}));
            Assert.Throws<InvalidOperationException>(()=>w.Output.GetStaticCheckResults(new[]{7}));
        }

        [TestCaseSource(nameof(BadPlateArrays))]
        public void PlateArrayMustBeWrittenRankOneExactlyFiveFiniteDoubleComponents(object value)
        {
            var n=new Native{PlateValues=value};using var w=Wrap(n);
            Assert.Throws<InvalidOperationException>(()=>w.Output.GetPlateCenterForces(new LoadCase(7,""),new Plate{Id=17}));
            Assert.That(n.Calls,Is.EqualTo(new[]{"plate:17:7"}));
        }

        [TestCase("plate"),TestCase("plateTask")]
        public void PlateSupportsInPlaceNonzeroBoundZeroAndExtremeFiniteOutputsWithIndependentStorage(string op)
        {
            var n=new Native{InPlace=true,PlateValues=new double[]{0,-0d,double.MaxValue,-double.MaxValue,double.Epsilon}};
            using var w=Wrap(n);var lc=new LoadCase(7,"");var p=new Plate{Id=17};
            var a=PlateRead(w.Output,op,lc,p);Assert.That(Components(a),Is.EqualTo(n.PlateValues));
            var values=Array.CreateInstance(typeof(double),new[]{5},new[]{-4});
            for(int i=0;i<5;i++)values.SetValue((double)(i+1),i-4);
            n.InPlace=false;n.PlateValues=values;
            var b=PlateRead(w.Output,op,lc,p);values.SetValue(999d,-4);
            Assert.That(Components(b),Is.EqualTo(new[]{1d,2d,3d,4d,5d}));Assert.That(a,Is.Not.SameAs(b));
            n.Write=false;Assert.Throws<InvalidOperationException>(()=>PlateRead(w.Output,op,lc,p));
        }

        [TestCase(0),TestCase(1),TestCase(2),TestCase(3),TestCase(4)]
        public void EveryPlateComponentMustBeFiniteInBothApiVariants(int index)
        {
            var n=new Native();using var w=Wrap(n);
            foreach(double bad in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})
            {
                var values=new double[5];values[index]=bad;n.PlateValues=values;
                foreach(string op in new[]{"plate","plateTask"})
                    Assert.Throws<InvalidOperationException>(()=>PlateRead(w.Output,op,new LoadCase(7,""),new Plate{Id=17}));
            }
        }

        [Test]
        public void PositiveMaximumIdsAndWrittenZeroStaticAndPlateVectorsRemainValid()
        {
            var n=new Native{PlateValues=new double[5],Loads=new double[6],Reactions=new double[6]};
            using var w=Wrap(n);
            var p=w.Output.GetPlateCenterForces(new LoadCase(int.MaxValue,""),new Plate{Id=int.MaxValue});
            Assert.That(Components(p),Is.All.Zero);
            var b=w.Output.GetStaticCheckResults(new[]{int.MaxValue}).Single();
            Assert.That(b.Forces.Fx,Is.Zero);Assert.That(b.Reactions.Mz,Is.Zero);
            Assert.That(n.Calls,Is.EqualTo(new[]{"plate:"+int.MaxValue+":"+int.MaxValue,"static:"+int.MaxValue}));
        }

        private sealed class CountingCase:ILoadCase
        {
            internal int Reads;internal Func<int> Read;
            public int Id{get{Reads++;return Read();}}
            public string Title=>throw new AssertionException("Title must not be read.");
            public LoadType Type { get => throw new AssertionException("Type must not be read."); set => throw new AssertionException("Type must not be written."); }
            public LoadCaseType CaseType { get => throw new AssertionException("CaseType must not be read."); set => throw new AssertionException("CaseType must not be written."); }
        }

        [TestCase("plate"),TestCase("plateTask")]
        public void PlateReadsIdsOnceOnlyAndRetainsOriginalMutableContextReferences(string op)
        {
            var n=new Native();using var w=Wrap(n);var p=new Plate{Id=17};
            var lc=new CountingCase{Read=()=>7};
            n.OnCall=_=>{p.Id=999;lc.Read=()=>300;};
            var result=PlateRead(w.Output,op,lc,p);
            Assert.That(lc.Reads,Is.EqualTo(1));Assert.That(n.Calls,Is.EqualTo(new[]{"plate:17:7"}));
            Assert.That(result.LoadCase,Is.SameAs(lc));Assert.That(result.Plate.Id,Is.EqualTo(999));Assert.That(result.Sqx,Is.EqualTo(1));
        }

        [TestCase("plate"),TestCase("plateTask")]
        public void PlateNullInvalidIdsAndThrowingMetadataFailBeforeNativeAccess(string op)
        {
            var n=new Native();using var w=Wrap(n);var lc=new LoadCase(7,"");var p=new Plate{Id=17};
            Assert.That(Assert.Throws<ArgumentNullException>(()=>PlateRead(w.Output,op,null,p)).ParamName,Is.EqualTo("loadCase"));
            Assert.That(Assert.Throws<ArgumentNullException>(()=>PlateRead(w.Output,op,lc,null)).ParamName,Is.EqualTo("plate"));
            foreach(int id in new[]{0,-1})
            {
                lc.Id=id;Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>PlateRead(w.Output,op,lc,p)).ParamName,Is.EqualTo("loadCase"));
                lc.Id=7;p.Id=id;Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>PlateRead(w.Output,op,lc,p)).ParamName,Is.EqualTo("plate"));p.Id=17;
            }
            var error=new ApplicationException("metadata getter");
            Assert.That(Assert.Throws<ApplicationException>(()=>PlateRead(w.Output,op,new CountingCase{Read=()=>throw error},p)),Is.SameAs(error));
            Assert.That(n.Calls,Is.Empty);Assert.DoesNotThrow(()=>w.Root.GetBaseUnit());
        }

        [TestCase("batchTask"),TestCase("plateTask")]
        public void TaskVariantsCompleteNativeWorkBeforeReturningSettledTasksAndPreserveFaults(string op)
        {
            var n=new Native();using var w=Wrap(n);
            Task task=op=="batchTask"?w.Output.GetStaticCheckResultsAsync(new[]{7}):w.Output.GetPlateCenterForcesAsync(new LoadCase(7,""),new Plate{Id=17});
            Assert.That(task.IsCompletedSuccessfully,Is.True);Assert.That(n.Calls,Has.Count.EqualTo(1));
            var error=new COMException("native failure",unchecked((int)0x80004005));n.Failure=error;
            Task failed=op=="batchTask"?w.Output.GetStaticCheckResultsAsync(new[]{7}):w.Output.GetPlateCenterForcesAsync(new LoadCase(7,""),new Plate{Id=17});
            Assert.That(failed.IsFaulted,Is.True);
            Assert.That(Assert.Throws<COMException>(()=>failed.GetAwaiter().GetResult()),Is.SameAs(error));
            Assert.That(n.Threads,Is.All.EqualTo(Environment.CurrentManagedThreadId));
        }

        [TestCaseSource(nameof(Operations))]
        public void GuardRejectsManualStaleDisposedAndOtherThreadCallsBeforeNativeAccess(string op)
        {
            var n=new Native();using var w=Wrap(n);var output=w.Output;
            Assert.Throws<InvalidOperationException>(()=>Invoke(new OSOutputAdapter(w),op));
            Exception error=null;var thread=new Thread(()=>{try{Invoke(output,op);}catch(Exception e){error=e;}});
            thread.SetApartmentState(ApartmentState.MTA);thread.Start();Assert.That(thread.Join(5000),Is.True);
            Assert.That(error,Is.TypeOf<InvalidOperationException>());
            w.RefreshComObjects();Assert.Throws<InvalidOperationException>(()=>Invoke(output,op));
            output=w.Output;w.Dispose();Assert.Throws<ObjectDisposedException>(()=>Invoke(output,op));Assert.That(n.Calls,Is.Empty);
        }

        [TestCaseSource(nameof(Operations))]
        public void OneLeaseRejectsReentrantDomainRefreshAndDisposalCallbacksAndThenReleases(string op)
        {
            var n=new Native();using var w=Wrap(n);
            n.OnCall=_=>{
                foreach(string nested in Operations)Assert.Throws<InvalidOperationException>(()=>Invoke(w.Output,nested));
                Assert.Throws<InvalidOperationException>(()=>w.Load.GetLoadCaseTitle(1));
                Assert.Throws<InvalidOperationException>(()=>w.Root.GetBaseUnit());
                Assert.Throws<InvalidOperationException>(()=>w.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(()=>w.RefreshUnits());
                Assert.Throws<InvalidOperationException>(()=>w.Dispose());
            };
            Invoke(w.Output,op);n.OnCall=null;Assert.DoesNotThrow(()=>w.Root.GetBaseUnit());
        }

        [TestCase("batch"),TestCase("threads"),TestCase("batchTask")]
        public void EnumerationRunsInsideLeaseAndMutableSourceIsCapturedBeforeDispatch(string op)
        {
            var n=new Native();using var w=Wrap(n);
            var ids=new Once<int>(new[]{7,3},()=>{Assert.Throws<InvalidOperationException>(()=>w.RefreshUnits());Assert.Throws<InvalidOperationException>(()=>w.Output.AreResultsAvailable());});
            Assert.That(Batch(w.Output,op,ids),Has.Count.EqualTo(2));
        }

        [TestCaseSource(nameof(Operations))]
        public void MissingMembersAndNativeFailuresAreNotRetriedCoercedOrRedirected(string op)
        {
            using var missing=Wrap(new object());Assert.Throws<RuntimeBinderException>(()=>Invoke(missing.Output,op));
            var error=new COMException("native fault",unchecked((int)0x80010108));var n=new Native{Failure=error};using var w=Wrap(n);
            Assert.That(Assert.Throws<COMException>(()=>Invoke(w.Output,op)),Is.SameAs(error));Assert.That(n.Calls,Has.Count.EqualTo(1));
            n.Failure=null;Assert.DoesNotThrow(()=>Invoke(w.Output,op));
        }

        [Test]
        public void PlateContextAndScalarModelsAreDetachedAndSourceCompatible()
        {
            var lc=new LoadCase(7,"Case");var p=new Plate();
            int events=0;p.PropertyChanged+=(_,e)=>{if(e.PropertyName=="Id")events++;};
            Assert.That(p.Id,Is.Zero);p.Id=17;Assert.That(events,Is.EqualTo(1));
            var manual=new PlateCenterForces(lc,p,1,-2,3,-4,5);
            Assert.That(manual,Is.InstanceOf<StaadPro.Interop.Core.Base.PlateStress>());
            Assert.That(manual,Is.InstanceOf<StaadPro.Interop.Core.Interfaces.IPlateStress>());
            Assert.That(Components(manual),Is.EqualTo(new[]{1d,-2d,3d,-4d,5d}));
            manual.Sqx=double.NaN;Assert.That(manual.Sqx,Is.NaN); // source manual setters deliberately unvalidated
            var zero=new PlateCenterForces(null,null);Assert.That(Components(zero),Is.All.Zero);
            Assert.That(zero.LoadCase,Is.Null);Assert.That(zero.Plate,Is.Null);
            Assert.That(typeof(Plate).GetProperties().Any(x=>x.Name=="A"),Is.False); // identity subset, no geometry hydration
        }

        [TestCaseSource(nameof(Operations))]
        public void ExactPublicContractsAndExpandedXmlMatch(string op)
        {
            string name=op=="batchTask"?"GetStaticCheckResultsAsync":op=="plateTask"?"GetPlateCenterForcesAsync":op=="plate"?"GetPlateCenterForces":"GetStaticCheckResults";
            Type[] parameters=op=="threads"?[typeof(IEnumerable<int>),typeof(int)]:op.StartsWith("plate",StringComparison.Ordinal)?[typeof(ILoadCase),typeof(Plate)]:[typeof(IEnumerable<int>)];
            string[] names=op=="threads"?["loadCaseIds","nThreads"]:op.StartsWith("plate",StringComparison.Ordinal)?["loadCase","plate"]:["loadCaseIds"];
            Type result=op.StartsWith("plate",StringComparison.Ordinal)?typeof(PlateCenterForces):typeof(HashSet<StaticCheckResult>);
            if(op.EndsWith("Task",StringComparison.Ordinal))result=typeof(Task<>).MakeGenericType(result);
            var docs=XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location,".xml"));XElement prior=null;
            foreach(Type type in new[]{typeof(IOSOutput),typeof(OSOutputAdapter)})
            {
                var method=type.GetMethod(name,parameters);Assert.That(method,Is.Not.Null);Assert.That(method.ReturnType,Is.EqualTo(result));
                Assert.That(method.GetParameters().Select(p=>p.Name),Is.EqualTo(names));
                Assert.That(method.GetParameters().Any(p=>p.IsOptional||p.IsOut||p.ParameterType.IsByRef),Is.False);
                string suffix=op.StartsWith("plate",StringComparison.Ordinal)?"(StaadPro.Interop.Core.Entities.ILoadCase,StaadPro.Interop.Core.Entities.Plate)":op=="threads"?"(System.Collections.Generic.IEnumerable{System.Int32},System.Int32)":"(System.Collections.Generic.IEnumerable{System.Int32})";
                var doc=docs.Descendants("member").Single(e=>(string)e.Attribute("name")=="M:"+type.FullName+"."+name+suffix);
                foreach(string tag in new[]{"summary","param","returns","remarks","exception","example"})Assert.That(doc.Element(tag),Is.Not.Null);
                Assert.That(doc.Elements("param").Select(e=>(string)e.Attribute("name")),Is.EqualTo(names));
                Assert.That(doc.Descendants().Any(e=>e.Name=="include"||e.Name=="inheritdoc"),Is.False);
                if(prior!=null)Assert.That(doc.Elements().Select(e=>e.ToString()),Is.EqualTo(prior.Elements().Select(e=>e.ToString())));
                prior=doc;
            }
        }

        private static IEnumerable<int> Broken(){yield return 7;throw new ApplicationException("enumerator failed");}
        private sealed class Once<T>:IEnumerable<T>
        {
            private readonly IEnumerable<T> values;private readonly Action before;private bool used;
            internal Once(IEnumerable<T> values,Action before=null){this.values=values;this.before=before;}
            public IEnumerator<T> GetEnumerator(){if(used)throw new ApplicationException("enumerated twice");used=true;before?.Invoke();return values.GetEnumerator();}
            IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        }
    }
}
