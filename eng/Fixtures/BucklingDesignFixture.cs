using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Output;

public sealed class BucklingDesignNative
{
    public object Status=true, Factor=-2d, Ratio=1.2d, Vector=new[]{1d,-2d,3d,-4d,5d,-6d};
    public int Calls, Thread;
    public Exception Failure;
    public object GetBucklingFactor(int mode,ref object value)
    {Scalar(value);Calls++;if(Failure!=null)throw Failure;value=Factor;return Status;}
    public object GetBucklingModeDisplacementAtNode(int mode,int node,ref object values)
    {if(values is not double[] buffer||buffer.Length!=6||!buffer.All(double.IsNaN))throw new Exception("Vector sentinels changed.");Calls++;values=Vector;return Status;}
    public object GetMemberSteelDesignRatio(int member,ref object value)
    {Scalar(value);Calls++;Thread=Environment.CurrentManagedThreadId;if(Failure!=null)throw Failure;value=Ratio;return Status;}
    private static void Scalar(object value){if(value is not double d||!double.IsNaN(d))throw new Exception("Scalar output sentinel changed.");}
}
public sealed class BucklingDesignRoot : NativeRoot
{
    public BucklingDesignNative Output {get;}=new();
}
internal static class BucklingDesignFixture
{
    internal static void Verify(XDocument docs)
    {
        var root=new BucklingDesignRoot();using var wrapper=new OpenStaadWrapper(root);
        if(wrapper.Output.GetBucklingFactor(1)!=-2d||root.Output.Calls!=1)throw new Exception("Buckling factor output was not written by reference.");
        var node=new Node(4,double.NaN,0,0);var first=wrapper.Output.GetBucklingModeDisplacement(1,node);node.Id=9;
        if(first.NodeId!=4||first.BucklingMode!=1||!first.Components.SequenceEqual(new[]{1d,-2d,3d,-4d,5d,-6d}))throw new Exception("Vector/node identity was lost.");
        bool rejected=false;try{((IList<double>)first.Components)[0]=8;}catch(NotSupportedException){rejected=true;}if(!rejected)throw new Exception("Vector storage is mutable.");
        ((double[])root.Output.Vector)[0]=99;if(first.Components[0]!=1)throw new Exception("Native array escaped ownership.");
        var shifted=Array.CreateInstance(typeof(double),new[]{6},new[]{-2});for(int i=0;i<6;i++)shifted.SetValue((double)i,i-2);root.Output.Vector=shifted;
        if(!wrapper.Output.GetBucklingModeDisplacementAtNode(2,4).Components.SequenceEqual(new[]{0d,1d,2d,3d,4d,5d}))throw new Exception("Nonzero lower bounds changed.");
        root.Output.Vector=new double[5];rejected=false;try{wrapper.Output.GetBucklingModeDisplacementAtNode(1,4);}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Malformed vector accepted.");
        foreach(double ratio in new[]{-999d,-1d,0d,1.2d})
        {root.Output.Ratio=ratio;if(wrapper.Output.GetMemberSteelUtilizationRatio(7)!=ratio)throw new Exception("Design ratio/sentinel changed.");var task=wrapper.Output.GetMemberSteelUtilizationRatioAsync(7);if(!task.IsCompletedSuccessfully||task.Result!=ratio||root.Output.Thread!=Environment.CurrentManagedThreadId)throw new Exception("Async COM dispatch moved threads.");}
        root.Output.Ratio=-2d;var fault=wrapper.Output.GetMemberSteelUtilizationRatioAsync(7);if(!fault.IsFaulted)throw new Exception("Invalid ratio did not fault task.");
        var failure=new System.Runtime.InteropServices.COMException("native failure");root.Output.Failure=failure;fault=wrapper.Output.GetMemberSteelUtilizationRatioAsync(7);rejected=false;try{fault.GetAwaiter().GetResult();}catch(System.Runtime.InteropServices.COMException e){rejected=ReferenceEquals(e,failure);}if(!rejected)throw new Exception("Async native error identity changed.");root.Output.Failure=null;
        root.Output.Status=false;rejected=false;try{wrapper.Output.GetBucklingFactor(1);}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Unsuccessful factor returned data.");
        string[] names={"GetBucklingFactor","GetBucklingModeDisplacement","GetBucklingModeDisplacementAtNode","GetMemberSteelUtilizationRatio","GetMemberSteelUtilizationRatioAsync"};
        foreach(string name in names)
        {
            var method=typeof(IOSOutput).GetMethod(name);var p=method.GetParameters();
            Type expected=name=="GetMemberSteelUtilizationRatioAsync"?typeof(Task<double>):name is "GetBucklingModeDisplacement" or "GetBucklingModeDisplacementAtNode"?typeof(BucklingModeDisplacementResult):typeof(double);
            string[] expectedNames=name=="GetBucklingFactor"?new[]{"bucklingMode"}:name=="GetBucklingModeDisplacement"?new[]{"bucklingMode","atNode"}:name=="GetBucklingModeDisplacementAtNode"?new[]{"bucklingMode","nodeId"}:new[]{"beamId"};
            if(method.ReturnType!=expected||!p.Select(x=>x.Name).SequenceEqual(expectedNames)||p.Any(x=>x.IsOptional||x.ParameterType.IsByRef)||p.Where(x=>x.Name!="atNode").Any(x=>x.ParameterType!=typeof(int))||p.Any(x=>x.Name=="atNode"&&x.ParameterType!=typeof(Node)))throw new Exception("Result query signature changed.");
        }
        if(!typeof(BucklingModeDisplacementResult).IsSealed||typeof(BucklingModeDisplacementResult).GetConstructors().Length!=0||typeof(BucklingModeDisplacementResult).GetProperties().Any(x=>x.GetSetMethod(true)!=null))throw new Exception("Result is not immutable.");
        foreach(Type type in new[]{typeof(Node),typeof(BucklingModeDisplacementResult)})
        {if(!docs.Descendants("member").Any(x=>(string)x.Attribute("name")=="T:"+type.FullName&&x.Element("summary")!=null))throw new Exception("Result/node type XML missing.");foreach(var property in type.GetProperties(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly))if(!docs.Descendants("member").Any(x=>(string)x.Attribute("name")=="P:"+type.FullName+"."+property.Name&&x.Element("summary")!=null&&x.Element("value")!=null))throw new Exception("Result/node property XML missing.");}
    }
}
