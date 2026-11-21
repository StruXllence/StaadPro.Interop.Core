using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Output;
public sealed class SteelRecordNative
{
 public object Status=true,DesignText="PASS";
 public int Calls;
 public object GetMemberSteelDesignResults(int id,ref object code,ref object status,ref object ratio,ref object allowable,ref object load,ref object location,ref object clause,ref object section,ref object forces,ref object slender)
 {if(ratio is not double r||!double.IsNaN(r)||load is not int i||i!=int.MinValue||forces is not double[] a||a.Length!=3||!a.All(double.IsNaN))throw new Exception("Steel record sentinels changed.");Calls++;code=" Code ";status=DesignText;ratio=1.2d;allowable=1d;load=1;location=2d;clause=" Clause ";section=" Section ";forces=new[]{-4d,8d,-12d};slender=50d;return Status;}
 public object GetMemberSteelDesignRatio(int id,ref object value){Calls++;value=id==7?.8d:1.3d;return Status;}
}
public sealed class SteelRecordLoad
{
 public int GetPrimaryLoadCaseCount()=>1;
 public int GetLoadCombinationCaseCount()=>1;
 public object GetPrimaryLoadCaseNumbers(ref object ids){ids=new[]{1};return true;}
 public object GetLoadCombinationCaseNumbers(ref object ids){ids=new[]{201};return true;}
 public string GetLoadCaseTitle(int id)=>"Case "+id;
 public int GetLoadType(int id)=>0;
}
public sealed class SteelRecordRoot:NativeRoot
{
 public SteelRecordNative Output {get;}=new();
 public new SteelRecordLoad Load {get;}=new();
}
internal static class SteelRecordFixture
{
 internal static void Verify(XDocument docs)
 {
  var root=new SteelRecordRoot();using var wrapper=new OpenStaadWrapper(root);var beam=new Beam{Id=7};var cases=new[]{new LoadCase(1," supplied ")};
  var one=wrapper.Output.GetSteelDesignResultsForBeam(beam,cases);
  if(one.DesignStatus!=DesignStatus.Passed||one.DesignForces.Fx!=-4||one.DesignForces.My!=8||one.DesignForces.Mz!=-12||!ReferenceEquals(one.Member,beam)||one.CriticalLoadCase.Title!=" supplied ")throw new Exception("Steel record qualification/mapping changed.");
  var discovered=wrapper.Output.GetSteelDesignResultsForBeam(wrapper.Load,beam);if(discovered.CriticalLoadCase.Title!="Case 1")throw new Exception("Current Load composition failed.");
  var many=wrapper.Output.GetSteelDesignResultsForBeams(new[]{beam,beam},cases).ToList();if(many.Count!=2||ReferenceEquals(many[0].CriticalLoadCase,many[1].CriticalLoadCase)||ReferenceEquals(many[0].DesignForces,many[1].DesignForces))throw new Exception("Repeated beam record ownership changed.");
  if(wrapper.Output.GetSteelDesignResultsForBeams(wrapper.Load,new[]{beam}).Count()!=1)throw new Exception("Load batch composition failed.");
  root.Output.DesignText=1d;bool rejected=false;try{wrapper.Output.GetSteelDesignResultsForBeam(beam,cases);}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Ambiguous numeric status was guessed.");root.Output.DesignText="FAIL";
  if(wrapper.Output.GetSteelDesignResultsForBeam(beam,cases).DesignStatus!=DesignStatus.Failed)throw new Exception("Explicit FAIL mapping changed.");
  root.Output.Status=false;rejected=false;try{wrapper.Output.GetSteelDesignResultsForBeam(beam,cases);}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Failed retrieval returned a record.");root.Output.Status=true;
  var low=new Member{Beams=new(){new Beam{Id=7,UtilizationRatio=.7}}};var high=new Member{Beams=new(){new Beam{Id=8,UtilizationRatio=1.4}}};var groups=new PhysicalMemberIdentificationResult{Members=new(){low,high}};int before=root.Output.Calls;
  if(!ReferenceEquals(wrapper.Output.GetGoverningMember(groups,true),high)||root.Output.Calls!=before)throw new Exception("Cached governing selection performed native work.");
  if(!ReferenceEquals(wrapper.Output.GetGoverningMember(groups),high)||low.Beams.Single().UtilizationRatio!=.8||high.Beams.Single().UtilizationRatio!=1.3)throw new Exception("Queried governing selection/population failed.");
  high.Beams.Single().UtilizationRatio=-999;rejected=false;try{wrapper.Output.GetGoverningMember(groups,true);}catch(InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Unavailable ratio was ranked.");
  var signatures=new[]{
   new{Name="GetSteelDesignResultsForBeam",Types=new[]{typeof(Beam),typeof(IEnumerable<ILoadCase>)},Names=new[]{"beam","allLoadCases"},Result=typeof(MemberSteelDesignResults)},
   new{Name="GetSteelDesignResultsForBeam",Types=new[]{typeof(IOSLoad),typeof(Beam)},Names=new[]{"load","beam"},Result=typeof(MemberSteelDesignResults)},
   new{Name="GetSteelDesignResultsForBeams",Types=new[]{typeof(IEnumerable<Beam>),typeof(IEnumerable<ILoadCase>)},Names=new[]{"beams","allLoadCases"},Result=typeof(IEnumerable<MemberSteelDesignResults>)},
   new{Name="GetSteelDesignResultsForBeams",Types=new[]{typeof(IOSLoad),typeof(IEnumerable<Beam>)},Names=new[]{"load","beams"},Result=typeof(IEnumerable<MemberSteelDesignResults>)},
   new{Name="GetGoverningMember",Types=new[]{typeof(PhysicalMemberIdentificationResult),typeof(bool)},Names=new[]{"pmiResult","ratiosPopulated"},Result=typeof(Member)}};
  foreach(var signature in signatures){var m=typeof(IOSOutput).GetMethod(signature.Name,signature.Types);if(m==null||m.ReturnType!=signature.Result||!m.GetParameters().Select(p=>p.Name).SequenceEqual(signature.Names)||m.GetParameters().Any(p=>p.ParameterType.IsByRef)||m.GetParameters()[1].IsOptional!=(signature.Name=="GetGoverningMember")||(signature.Name=="GetGoverningMember"&&!Equals(m.GetParameters()[1].DefaultValue,false)))throw new Exception("Steel record/governing signature changed.");}
  foreach(Type type in new[]{typeof(MemberSteelDesignResults),typeof(Member),typeof(PhysicalMemberIdentificationResult),typeof(SteelDesignForces),typeof(DesignStatus)})
  {if(!docs.Descendants("member").Any(x=>(string)x.Attribute("name")=="T:"+type.FullName&&x.Element("summary")!=null))throw new Exception("Steel support type IntelliSense missing.");foreach(var p in type.GetProperties(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly))if(!docs.Descendants("member").Any(x=>(string)x.Attribute("name")=="P:"+type.FullName+"."+p.Name&&x.Element("summary")!=null&&x.Element("value")!=null))throw new Exception("Steel support property IntelliSense missing.");}
 }
}
