using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Root;
public sealed class MemberBatchNative
{
    public readonly List<int> Ids=new();
    public object RatioStatus=true, ForceStatus=true;
    public Action<int> Callback;
    public int Thread;
    public object GetMemberSteelDesignRatio(int id,ref object value)
    {if(value is not double d||!double.IsNaN(d))throw new Exception("Missing ratio sentinel.");Thread=Environment.CurrentManagedThreadId;Ids.Add(id);Callback?.Invoke(id);value=id==7?-999d:1.2d;return RatioStatus;}
    public object GetMemberEndForces(int beam,int end,int load,ref object values,int axes)
    {if(axes!=0||values is not double[] a||a.Length!=6||!a.All(double.IsNaN))throw new Exception("Force arguments/sentinels changed.");Thread=Environment.CurrentManagedThreadId;Ids.Add(beam);values=new[]{1d,2d,3d,4d,5d,6d};return ForceStatus;}
}
public sealed class MemberBatchRoot:NativeRoot
{
    public MemberBatchNative Output {get;}=new();
}
internal static class MemberBatchFixture
{
    internal static void Verify(XDocument docs)
    {
        var native=new MemberBatchRoot();using var wrapper=new OpenStaadWrapper(native);
        var a=new Beam{Id=7,UtilizationRatio=42};var b=new Beam{Id=8,UtilizationRatio=42};var beams=new HashSet<Beam>{a,b};
        var result=wrapper.Output.GetMemberSteelUtilizationRatios(beams);
        if(!native.Output.Ids.SequenceEqual(new[]{7,8})||result[a]!=-999||result[b]!=1.2||a.UtilizationRatio!=42)throw new Exception("Batch qualification/population contract changed.");
        a.Id=8;if(!result.ContainsKey(a)||!result.ContainsKey(b)||result.ContainsKey(new Beam{Id=8}))throw new Exception("Dictionary lost reference key identity.");
        bool rejected=false;try{wrapper.Output.GetMemberSteelUtilizationRatios(beams);}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Duplicate current IDs accepted.");a.Id=7;
        var one=wrapper.Output.GetMemberSteelUtilizationRatioAsync(a);if(!one.IsCompletedSuccessfully||one.Result!=-999||native.Output.Thread!=Environment.CurrentManagedThreadId)throw new Exception("Beam task moved COM threads.");
        var many=wrapper.Output.GetMemberSteelUtilizationRatiosAsync(beams);if(!many.IsCompletedSuccessfully||many.Result.Count!=2||ReferenceEquals(many.Result,result))throw new Exception("Batch task/result ownership changed.");
        native.Output.RatioStatus=false;rejected=false;try{wrapper.Output.PopulateMemberSteelUtilizationRatios(beams);}catch(InvalidOperationException){rejected=true;}if(!rejected||a.UtilizationRatio!=42||b.UtilizationRatio!=42)throw new Exception("Failed read partially populated members.");native.Output.RatioStatus=true;
        int notified=0;a.PropertyChanged+=(_,e)=>{if(e.PropertyName=="UtilizationRatio")notified++;};wrapper.Output.PopulateMemberSteelUtilizationRatios(beams);if(a.UtilizationRatio!=-999||b.UtilizationRatio!=1.2||notified!=1)throw new Exception("Qualified population did not update detached data.");
        var q=wrapper.Output.GetMemberEndForcesAsync("literal",7,9,MemberEnd.End);if(!q.IsCompletedSuccessfully||!q.Result.IsSuccessful||q.Result.Forces.Mz!=6||q.Result.CoordinateSystem!=LocalOrGlobal.Local)throw new Exception("Async force result changed.");native.Output.ForceStatus=false;q=wrapper.Output.GetMemberEndForcesAsync("literal",7,9,MemberEnd.End);if(!q.IsCompletedSuccessfully||q.Result.IsSuccessful||q.Result.Forces!=null)throw new Exception("Async force lost native false.");
        var bad=wrapper.Output.GetMemberSteelUtilizationRatioAsync((Beam)null);if(!bad.IsFaulted)throw new Exception("Invalid Beam task was not faulted.");
        var captured=new Beam(9,new Node(1,0,0,0),new Node(2,1,0,0));if(captured.Id!=9||captured.StartNode.Id!=1||!captured.Equals(new Beam{Id=9}))throw new Exception("Beam metadata identity changed.");
        var signatures=new[]{
            new{ Name="GetMemberEndForcesAsync",Types=new[]{typeof(string),typeof(int),typeof(int),typeof(MemberEnd)},Names=new[]{"lcTitle","beamId","loadCaseId","memberEnd"},Result=typeof(Task<MemberForceQueryResult>)},
            new{ Name="GetMemberSteelUtilizationRatioAsync",Types=new[]{typeof(Beam)},Names=new[]{"b"},Result=typeof(Task<double>)},
            new{ Name="GetMemberSteelUtilizationRatios",Types=new[]{typeof(HashSet<Beam>)},Names=new[]{"beams"},Result=typeof(Dictionary<Beam,double>)},
            new{ Name="PopulateMemberSteelUtilizationRatios",Types=new[]{typeof(HashSet<Beam>)},Names=new[]{"forBeams"},Result=typeof(void)},
            new{ Name="GetMemberSteelUtilizationRatiosAsync",Types=new[]{typeof(HashSet<Beam>)},Names=new[]{"beams"},Result=typeof(Task<Dictionary<Beam,double>>)} };
        foreach(var contract in signatures)
        {var method=typeof(IOSOutput).GetMethod(contract.Name,contract.Types);if(method==null||method.ReturnType!=contract.Result||!method.GetParameters().Select(p=>p.Name).SequenceEqual(contract.Names)||method.GetParameters().Any(p=>p.IsOptional||p.ParameterType.IsByRef))throw new Exception("Batch/async public signature changed.");}
        if(!docs.Descendants("member").Any(x=>(string)x.Attribute("name")=="T:"+typeof(Beam).FullName&&x.Element("summary")!=null))throw new Exception("Beam type IntelliSense missing.");
        foreach(var property in typeof(Beam).GetProperties(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly))
            if(!docs.Descendants("member").Any(x=>(string)x.Attribute("name")=="P:"+typeof(Beam).FullName+"."+property.Name&&x.Element("summary")!=null&&x.Element("value")!=null))throw new Exception("Beam property IntelliSense missing.");
    }
}
