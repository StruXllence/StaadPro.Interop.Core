using System;
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
using StaadPro.Interop.Core.Common;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Output;
using NativeRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;
namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class BucklingAndSteelQueryTests
    {
        private static readonly string[] Operations = ["factor", "node", "displacement", "ratio", "async"];
        public sealed class Native
        {
            public object Status = true, Factor = -2d, Displacements = new[]{1d,-2d,3d,-4d,5d,-6d}, Ratio = 1.25d;
            public bool Write = true;
            public Action Callback;
            public Exception Failure;
            public readonly List<string> Calls = [];
            public readonly List<int> Threads = [];
            private void Step(string call) { Calls.Add(call); Threads.Add(Environment.CurrentManagedThreadId); Callback?.Invoke(); if(Failure!=null)throw Failure; }
            public object GetBucklingFactor(int mode, ref object value)
            { Assert.That(value,Is.TypeOf<double>());Assert.That(double.IsNaN((double)value),Is.True);Step("factor:"+mode);if(Write)value=Factor;return Status; }
            public object GetBucklingModeDisplacementAtNode(int mode,int node,ref object value)
            { Assert.That(value,Is.TypeOf<double[]>());Assert.That(((double[])value).All(double.IsNaN),Is.True);Assert.That(((double[])value).Length,Is.EqualTo(6));Step("displacement:"+mode+":"+node);if(Write)value=Displacements;return Status; }
            public object GetMemberSteelDesignRatio(int member, ref object value)
            { Assert.That(value,Is.TypeOf<double>());Assert.That(double.IsNaN((double)value),Is.True);Step("ratio:"+member);if(Write)value=Ratio;return Status; }
        }
        private static OpenStaadWrapper Wrap(object native)=>new(new NativeRoot{Output=native,Load=native});
        private static object Invoke(IOSOutput output,string op)=>op switch
        { "factor"=>output.GetBucklingFactor(2),"node"=>output.GetBucklingModeDisplacement(2,new Node(4,0,0,0)),"displacement"=>output.GetBucklingModeDisplacementAtNode(2,4),"ratio"=>output.GetMemberSteelUtilizationRatio(7),"async"=>output.GetMemberSteelUtilizationRatioAsync(7).GetAwaiter().GetResult(),_=>throw new ArgumentOutOfRangeException(nameof(op)) };
        private static IEnumerable<TestCaseData> BadStatuses()
        {
            foreach(string op in Operations)foreach(object value in new object[]{null,false,0,-1,2,"1",1d,1m,DayOfWeek.Monday,ulong.MaxValue})yield return new TestCaseData(op,value);
        }
        private static IEnumerable<TestCaseData> BadScalars()
        {
            foreach(string op in new[]{"factor","ratio","async"})foreach(object value in new object[]{null,true,1,1L,"1",1f,1m,double.NaN,double.PositiveInfinity,double.NegativeInfinity})yield return new TestCaseData(op,value);
        }
        private static IEnumerable<TestCaseData> BadArrays()
        {
            foreach(string op in new[]{"node","displacement"})foreach(object value in new object[]{null,0d,Array.Empty<double>(),new double[5],new double[7],new double[2,3],new int[6],new object[]{1d,2d,3d,4d,5d,6d},new[]{1d,2d,3d,4d,5d,double.NaN},new[]{1d,2d,3d,4d,5d,double.PositiveInfinity}})yield return new TestCaseData(op,value);
        }
        [TestCaseSource(nameof(Operations))]
        public void SuccessFlagsIssueOneExactNativeCallOnTheOwningThread(string op)
        {
            foreach(object flag in new object[]{true,(sbyte)1,(byte)1,(short)1,(ushort)1,1,1u,1L,1UL})
            { var native=new Native{Status=flag};using var wrapper=Wrap(native);var result=Invoke(wrapper.Output,op);Assert.That(native.Calls,Has.Count.EqualTo(1));Assert.That(native.Threads,Is.All.EqualTo(Environment.CurrentManagedThreadId));if(op=="factor")Assert.That(result,Is.EqualTo(-2d));if(op is "ratio" or "async")Assert.That(result,Is.EqualTo(1.25d));if(op is "node" or "displacement")Assert.That(((BucklingModeDisplacementResult)result).Components,Is.EqualTo(new[]{1d,-2d,3d,-4d,5d,-6d})); }
        }
        [TestCaseSource(nameof(BadStatuses))]
        public void UnsuccessfulAndMalformedStatusesNeverReturnInitializedDefaults(string op,object flag)
        { var native=new Native{Status=flag};using var wrapper=Wrap(native);Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,op));Assert.That(native.Calls,Has.Count.EqualTo(1)); }
        [TestCaseSource(nameof(BadScalars))]
        public void ScalarOutputMustBeWrittenFiniteDouble(string op,object value)
        { var native=new Native{Factor=value,Ratio=value};using var wrapper=Wrap(native);Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,op)); }
        [TestCaseSource(nameof(BadArrays))]
        public void DisplacementOutputMustContainExactlySixWrittenFiniteDoubles(string op,object value)
        { var native=new Native{Displacements=value};using var wrapper=Wrap(native);Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,op)); }
        [TestCaseSource(nameof(Operations))]
        public void UnwrittenSuccessfulOutputsThrowAndReleaseTheLease(string op)
        { var native=new Native{Write=false};using var wrapper=Wrap(native);Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,op));native.Write=true;Assert.DoesNotThrow(()=>Invoke(wrapper.Output,op)); }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MinValue)]
        public void InvalidIdsAndNullNodeFailBeforeNativeDispatch(int id)
        {
            var native=new Native();using var wrapper=Wrap(native);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>wrapper.Output.GetBucklingFactor(id)).ParamName,Is.EqualTo("bucklingMode"));
            Assert.Throws<ArgumentOutOfRangeException>(()=>wrapper.Output.GetBucklingModeDisplacementAtNode(id,4));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>wrapper.Output.GetBucklingModeDisplacementAtNode(2,id)).ParamName,Is.EqualTo("nodeId"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>wrapper.Output.GetBucklingModeDisplacement(2,new Node(id,0,0,0))).ParamName,Is.EqualTo("atNode"));
            Assert.That(Assert.Throws<ArgumentNullException>(()=>wrapper.Output.GetBucklingModeDisplacement(2,null)).ParamName,Is.EqualTo("atNode"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>wrapper.Output.GetMemberSteelUtilizationRatio(id)).ParamName,Is.EqualTo("beamId"));
            var task=wrapper.Output.GetMemberSteelUtilizationRatioAsync(id);Assert.That(task.IsFaulted,Is.True);Assert.Throws<ArgumentOutOfRangeException>(()=>task.GetAwaiter().GetResult());Assert.That(native.Calls,Is.Empty);
        }
        [TestCase(-999d)] [TestCase(-1d)] [TestCase(0d)] [TestCase(.9d)] [TestCase(2d)] [TestCase(double.MaxValue)]
        public void RatiosPreserveFiniteDimensionlessValuesAndDocumentedAbsenceSentinels(double value)
        { var native=new Native{Ratio=value};using var wrapper=Wrap(native);Assert.That(wrapper.Output.GetMemberSteelUtilizationRatio(7),Is.EqualTo(value));Assert.That(wrapper.Output.GetMemberSteelUtilizationRatioAsync(7).Result,Is.EqualTo(value)); }
        [TestCase(-.1d)] [TestCase(-2d)] [TestCase(-1000d)]
        public void UnknownNegativeDesignRatiosThrow(double ratio)
        { var native=new Native{Ratio=ratio};using var wrapper=Wrap(native);Assert.Throws<InvalidOperationException>(()=>wrapper.Output.GetMemberSteelUtilizationRatio(7));Assert.Throws<InvalidOperationException>(()=>wrapper.Output.GetMemberSteelUtilizationRatioAsync(7).GetAwaiter().GetResult()); }
        [TestCase(0d)] [TestCase(-1d)] [TestCase(double.MaxValue)] [TestCase(double.Epsilon)]
        public void FactorPreservesFiniteZeroNegativeAndExtremeValuesWithoutCountQueries(double factor)
        { var native=new Native{Factor=factor};using var wrapper=Wrap(native);Assert.That(wrapper.Output.GetBucklingFactor(int.MaxValue),Is.EqualTo(factor));Assert.That(native.Calls,Is.EqualTo(new[]{"factor:"+int.MaxValue})); }
        [Test]
        public void NonzeroBoundVectorsAreCopiedWithCapturedQueryIdentityAndReadOnlyStorage()
        {
            var array=Array.CreateInstance(typeof(double),new[]{6},new[]{-3});for(int i=0;i<6;i++)array.SetValue((double)(i-2),i-3);
            var native=new Native{Displacements=array};using var wrapper=Wrap(native);var node=new Node(4,double.NaN,double.PositiveInfinity,0);
            native.Callback=()=>node.Id=99;var result=wrapper.Output.GetBucklingModeDisplacement(2,node);
            Assert.That(result.BucklingMode,Is.EqualTo(2));Assert.That(result.NodeId,Is.EqualTo(4));Assert.That(result.Components,Is.EqualTo(new[]{-2d,-1d,0d,1d,2d,3d}));
            array.SetValue(99d,-3);Assert.That(result.Components[0],Is.EqualTo(-2));Assert.Throws<NotSupportedException>(()=>((IList<double>)result.Components)[0]=42);
            Assert.That(result.Components.Count,Is.EqualTo(6));Assert.That(native.Calls,Is.EqualTo(new[]{"displacement:2:4"}));
            native.Callback=null;Assert.That(wrapper.Output.GetBucklingModeDisplacementAtNode(int.MaxValue,int.MaxValue).Components,Is.Not.SameAs(result.Components));
        }
        [Test]
        public void AsyncQueriesSettleOnTheCallingThreadAndRetainNativeExceptionIdentity()
        {
            var native=new Native();using var wrapper=Wrap(native);var task=wrapper.Output.GetMemberSteelUtilizationRatioAsync(7);
            Assert.That(task.IsCompletedSuccessfully,Is.True);Assert.That(native.Threads,Is.EqualTo(new[]{Environment.CurrentManagedThreadId}));
            var failure=new COMException("native failed",unchecked((int)0x80010108));native.Failure=failure;
            Task<double> fault=null;Assert.DoesNotThrow(()=>fault=wrapper.Output.GetMemberSteelUtilizationRatioAsync(7));Assert.That(fault.IsFaulted,Is.True);Assert.That(Assert.Throws<COMException>(()=>fault.GetAwaiter().GetResult()),Is.SameAs(failure));
        }
        [TestCaseSource(nameof(Operations))]
        public void BindingThreadLifetimeAndStaleGuardsPrecedeNativeCalls(string op)
        {
            var native=new Native();using var wrapper=Wrap(native);var output=wrapper.Output;
            Assert.Throws<InvalidOperationException>(()=>Invoke(new OSOutputAdapter(wrapper),op));
            Exception error=null;var thread=new Thread(()=>{try{Invoke(output,op);}catch(Exception e){error=e;}});thread.Start();Assert.That(thread.Join(5000),Is.True);Assert.That(error,Is.TypeOf<InvalidOperationException>());
            wrapper.RefreshComObjects();Assert.Throws<InvalidOperationException>(()=>Invoke(output,op));output=wrapper.Output;wrapper.Dispose();Assert.Throws<ObjectDisposedException>(()=>Invoke(output,op));Assert.That(native.Calls,Is.Empty);
        }
        [TestCaseSource(nameof(Operations))]
        public void SharedLeaseRejectsCallbacksAndPreservesNativeFailures(string op)
        {
            var native=new Native();using var wrapper=Wrap(native);
            native.Callback=()=>{foreach(string nested in Operations)Assert.Throws<InvalidOperationException>(()=>Invoke(wrapper.Output,nested));Assert.Throws<InvalidOperationException>(()=>wrapper.Root.GetBaseUnit());Assert.Throws<InvalidOperationException>(()=>wrapper.Load.GetEnvelopeIds());Assert.Throws<InvalidOperationException>(()=>wrapper.RefreshComObjects());Assert.Throws<InvalidOperationException>(()=>wrapper.Dispose());};
            Invoke(wrapper.Output,op);native.Callback=null;
            var failure=new COMException("native failed",unchecked((int)0x80010108));native.Failure=failure;Assert.That(Assert.Throws<COMException>(()=>Invoke(wrapper.Output,op)),Is.SameAs(failure));native.Failure=null;Assert.DoesNotThrow(()=>Invoke(wrapper.Output,op));
            using var missing=Wrap(new object());Assert.Throws<RuntimeBinderException>(()=>Invoke(missing.Output,op));
        }
        [Test]
        public void NodeRecordsAreDetachedEditableScalarMetadata()
        {
            Assert.That(typeof(Node).GetConstructor(new[]{typeof(int),typeof(double),typeof(double),typeof(double)}).GetParameters().Select(p=>p.Name),Is.EqualTo(new[]{"id","x","y","z"}));
            var node=new Node();Assert.That(node,Is.InstanceOf<IEntity>());Assert.That(node.Id,Is.Zero);Assert.That(new Node(1,2,3).Id,Is.Zero);
            var names=new List<string>();node.PropertyChanged+=(_,e)=>names.Add(e.PropertyName);node.Id=4;node.X=1;node.Y=2;node.Z=3;node.Z=3;
            Assert.That(names,Is.EqualTo(new[]{"Id","X","Y","Z"}));Assert.That(new Node(4,1,2,3),Is.Not.EqualTo(node));
        }
        [TestCaseSource(nameof(Operations))]
        public void PublicSignaturesAndExpandedInterfaceAdapterDocumentationMatch(string op)
        {
            string name=op switch{"factor"=>"GetBucklingFactor","node"=>"GetBucklingModeDisplacement","displacement"=>"GetBucklingModeDisplacementAtNode","ratio"=>"GetMemberSteelUtilizationRatio",_=>"GetMemberSteelUtilizationRatioAsync"};
            Type result=op is "node" or "displacement"?typeof(BucklingModeDisplacementResult):op=="async"?typeof(Task<double>):typeof(double);
            string[] names=op=="factor"?["bucklingMode"]:op=="node"?["bucklingMode","atNode"]:op=="displacement"?["bucklingMode","nodeId"]:["beamId"];
            Type[] types=op=="node"?[typeof(int),typeof(Node)]:names.Select(_=>typeof(int)).ToArray();var docs=XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location,".xml"));XElement previous=null;
            foreach(Type owner in new[]{typeof(IOSOutput),typeof(OSOutputAdapter)})
            { var method=owner.GetMethod(name);Assert.That(method.ReturnType,Is.EqualTo(result));Assert.That(method.GetParameters().Select(p=>p.Name),Is.EqualTo(names));Assert.That(method.GetParameters().Select(p=>p.ParameterType),Is.EqualTo(types));Assert.That(method.GetParameters().Any(p=>p.IsOptional||p.ParameterType.IsByRef),Is.False);
              var doc=docs.Descendants("member").Single(e=>((string)e.Attribute("name")).StartsWith("M:"+owner.FullName+"."+name+"("));foreach(string tag in new[]{"summary","returns","remarks","exception","example"})Assert.That(doc.Element(tag),Is.Not.Null);Assert.That(doc.Elements("param").Select(p=>(string)p.Attribute("name")),Is.EqualTo(names));Assert.That(doc.Descendants().Any(e=>e.Name=="include"||e.Name=="inheritdoc"),Is.False);if(previous!=null)Assert.That(doc.Elements().Select(e=>e.ToString()),Is.EqualTo(previous.Elements().Select(e=>e.ToString())));previous=doc;
            }
        }
    }
}
