using System;
using System.Collections;
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
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Output;
using NativeRoot=StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;
namespace StaadPro.Interop.Core.Tests.Adapters
{
 [TestFixture,NonParallelizable,Apartment(ApartmentState.STA)]
 public sealed class SteelDesignRecordsTests
 {
  private static readonly string[] Operations=["one","oneLoad","many","manyLoad","governing"];
  private static readonly string[] Fields=["code","status","ratio","allowable","case","location","clause","section","forces","slender"];
  public sealed class Output
  {
   public readonly Dictionary<string,object> Values=new(){["code"]=" Code ",["status"]="PASS",["ratio"]=1.2d,["allowable"]=1d,["case"]=1,["location"]=2d,["clause"]=" Clause ",["section"]=" Section ",["forces"]=new[]{-10d,20d,-30d},["slender"]=70d};
   public readonly HashSet<string> Omit=[];
   public readonly List<int> Calls=[],Threads=[];
   public readonly Dictionary<int,double> Ratios=new(){[7]=.8,[8]=1.3};
   public object Success=true;
   public Exception Failure;
   public Action<int> Callback;
   private void Step(int id){Calls.Add(id);Threads.Add(Environment.CurrentManagedThreadId);Callback?.Invoke(id);if(Failure!=null)throw Failure;}
   private void Write(string field,ref object value){if(!Omit.Contains(field))value=Values[field];}
   public object GetMemberSteelDesignResults(int id,ref object code,ref object status,ref object ratio,ref object allowable,ref object load,ref object location,ref object clause,ref object section,ref object forces,ref object slender)
   {
    Assert.That(code,Is.TypeOf<string>());Assert.That(status,Is.TypeOf<string>());Assert.That(code,Is.Not.Empty);Assert.That(ratio,Is.TypeOf<double>());Assert.That(double.IsNaN((double)ratio),Is.True);Assert.That(load,Is.EqualTo(int.MinValue));Assert.That(forces,Is.TypeOf<double[]>());Assert.That(((double[])forces).Length,Is.EqualTo(3));Assert.That(((double[])forces).All(double.IsNaN),Is.True);
    Step(id);Write("code",ref code);Write("status",ref status);Write("ratio",ref ratio);Write("allowable",ref allowable);Write("case",ref load);Write("location",ref location);Write("clause",ref clause);Write("section",ref section);Write("forces",ref forces);Write("slender",ref slender);return Success;
   }
   public object GetMemberSteelDesignRatio(int id,ref object value)
   {Assert.That(value,Is.TypeOf<double>());Assert.That(double.IsNaN((double)value),Is.True);Step(id);value=Ratios[id];return Success;}
  }
  public sealed class Load
  {
   public readonly List<string> Calls=[];
   public Exception Failure;public Action Callback;
   private void Step(string name){Calls.Add(name);Callback?.Invoke();if(Failure!=null)throw Failure;}
   public int GetPrimaryLoadCaseCount(){Step("primaryCount");return 1;}
   public int GetLoadCombinationCaseCount(){Step("combinationCount");return 1;}
   public object GetPrimaryLoadCaseNumbers(ref object ids){Step("primaryIds");ids=new[]{1};return true;}
   public object GetLoadCombinationCaseNumbers(ref object ids){Step("combinationIds");ids=new[]{201};return true;}
   public string GetLoadCaseTitle(int id){Step("title:"+id);return "Case "+id;}
   public int GetLoadType(int id){Step("type:"+id);return 0;}
  }
  private static OpenStaadWrapper Wrap(Output output,Load load=null)=>new(new NativeRoot{Output=output,Load=load??new Load()});
  private static Beam B(int id=7)=>new(){Id=id,UtilizationRatio=42};
  private static ILoadCase[] Cases()=>[new LoadCase(1," Dead ")];
  private static PhysicalMemberIdentificationResult Groups(params Beam[] beams){var m=new Member();foreach(var b in beams)m.Beams.Add(b);return new(){Members=new(){m}};}
  private static object Invoke(IOSOutput output,IOSLoad load,string op)=>op switch
  {"one"=>output.GetSteelDesignResultsForBeam(B(),Cases()),"oneLoad"=>output.GetSteelDesignResultsForBeam(load,B()),"many"=>output.GetSteelDesignResultsForBeams(new[]{B(),B(8)},Cases()),"manyLoad"=>output.GetSteelDesignResultsForBeams(load,new[]{B(),B(8)}),"governing"=>output.GetGoverningMember(Groups(B(),B(8))),_=>throw new ArgumentOutOfRangeException(nameof(op))};
  private static IEnumerable<TestCaseData> BadFields()
  {
   foreach(string field in new[]{"ratio","allowable","location","slender"})foreach(object value in new object[]{null,1,1f,1m,"1",double.NaN,double.PositiveInfinity,-1d})yield return new(field,value);
   foreach(string field in new[]{"code","status","clause","section"})foreach(object value in new object[]{null,1,1d,true})yield return new(field,value);
   foreach(object value in new object[]{null,0,-1,1d,"1",true,2147483648L,999})yield return new("case",value);
   foreach(object value in new object[]{null,1d,new double[2],new double[4],new int[3],new double[1,3],new[]{1d,double.NaN,3d},new[]{double.PositiveInfinity,2d,3d}})yield return new("forces",value);
  }
  [TestCaseSource(nameof(Operations))]
  public void OwningThreadCallsAndLoadLeaseComposeWithoutPublicReentry(string op)
  {
   var native=new Output();var load=new Load();using var wrapper=Wrap(native,load);var result=Invoke(wrapper.Output,wrapper.Load,op);Assert.That(native.Threads,Is.All.EqualTo(Environment.CurrentManagedThreadId));Assert.That(native.Calls.Count,Is.EqualTo(op is "one" or "oneLoad"?1:2));
   if(op is "oneLoad" or "manyLoad"){Assert.That(load.Calls.Count(x=>x=="primaryIds"),Is.EqualTo(1));Assert.That(load.Calls.Count(x=>x=="combinationIds"),Is.EqualTo(1));}else Assert.That(load.Calls,Is.Empty);
   if(op!="governing"){var item=result is MemberSteelDesignResults one?one:((IEnumerable<MemberSteelDesignResults>)result).First();Assert.That(item.DesignForces.Fx,Is.EqualTo(-10));Assert.That(item.DesignForces.My,Is.EqualTo(20));Assert.That(item.DesignForces.Mz,Is.EqualTo(-30));Assert.That(item.Code,Is.EqualTo(" Code "));}
  }
  [TestCase("PASS",DesignStatus.Passed)] [TestCase("pass",DesignStatus.Passed)] [TestCase(" FAIL ",DesignStatus.Failed)]
  public void NativeTextStatusIsQualifiedWithoutInferringItFromRatio(string status,DesignStatus expected)
  {var native=new Output();native.Values["status"]=status;native.Values["ratio"]=0d;using var w=Wrap(native);Assert.That(w.Output.GetSteelDesignResultsForBeam(B(),Cases()).DesignStatus,Is.EqualTo(expected));}
  [TestCase("1.0")] [TestCase("0")] [TestCase(1d)] [TestCase("UNKNOWN")] [TestCase("")]
  public void AmbiguousStatusesAreRejectedRatherThanMappedToFail(object status)
  {var n=new Output();n.Values["status"]=status;using var w=Wrap(n);Assert.Throws<InvalidOperationException>(()=>w.Output.GetSteelDesignResultsForBeam(B(),Cases()));}
  [TestCaseSource(nameof(BadFields))]
  public void SuccessfulRecordsRejectMalformedOrUnresolvedFields(string field,object value)
  {var n=new Output();n.Values[field]=value;using var w=Wrap(n);Assert.Throws<InvalidOperationException>(()=>w.Output.GetSteelDesignResultsForBeam(B(),Cases()));}
  [TestCaseSource(nameof(Fields))]
  public void EverySuccessfulOutputMustBeWritten(string field)
  {var n=new Output();n.Omit.Add(field);using var w=Wrap(n);Assert.Throws<InvalidOperationException>(()=>w.Output.GetSteelDesignResultsForBeam(B(),Cases()));}
  [TestCase(false)] [TestCase(0)] [TestCase(-1)] [TestCase(2)] [TestCase("1")] [TestCase(1d)]
  public void BadNativeSuccessNeverReturnsInitializedRecords(object flag)
  {var n=new Output{Success=flag};using var w=Wrap(n);Assert.Throws<InvalidOperationException>(()=>w.Output.GetSteelDesignResultsForBeam(B(),Cases()));}
  [Test]
  public void NativeForcesSupportNonzeroBoundsAndCopiedOwnership()
  {var a=Array.CreateInstance(typeof(double),new[]{3},new[]{-2});a.SetValue(-4d,-2);a.SetValue(8d,-1);a.SetValue(-12d,0);var n=new Output();n.Values["forces"]=a;using var w=Wrap(n);var r=w.Output.GetSteelDesignResultsForBeam(B(),Cases());a.SetValue(99d,-2);Assert.That(r.DesignForces.Fx,Is.EqualTo(-4));Assert.That(r.DesignForces.My,Is.EqualTo(8));Assert.That(typeof(SteelDesignForces).GetProperties().Select(p=>p.Name),Is.EqualTo(new[]{"Fx","My","Mz"}));}
  [Test]
  public void CaseMetadataAndDuplicateBeamRecordsAreCapturedIndependently()
  {var n=new Output();using var w=Wrap(n);var b=B();var lc=new LoadCase(1," literal ");n.Callback=_=>lc.Title="changed";var r=w.Output.GetSteelDesignResultsForBeams(new[]{b,b},new[]{lc}).ToList();Assert.That(n.Calls,Is.EqualTo(new[]{7,7}));Assert.That(r.Count,Is.EqualTo(2));Assert.That(r[0].Member,Is.SameAs(b));Assert.That(r[0].CriticalLoadCase.Title,Is.EqualTo(" literal "));Assert.That(r[0].CriticalLoadCase,Is.Not.SameAs(r[1].CriticalLoadCase));r[0].CriticalLoadCase.Type=LoadType.Live;Assert.That(r[1].CriticalLoadCase.Type,Is.EqualTo(LoadType.None));Assert.That(r[0].DesignForces,Is.Not.SameAs(r[1].DesignForces));Assert.That(b.UtilizationRatio,Is.EqualTo(42));}
  private sealed class Once<T>:IEnumerable<T>
  {private readonly IEnumerable<T> source;public int Count;public Once(IEnumerable<T> source){this.source=source;}public IEnumerator<T> GetEnumerator(){if(++Count>1)throw new Exception("repeated enumeration");return source.GetEnumerator();}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();}
  [Test]
  public void InputsAreEnumeratedOnceAndResultsAreEager()
  {var n=new Output();using var w=Wrap(n);var beams=new Once<Beam>(new[]{B()});var cases=new Once<ILoadCase>(Cases());var result=w.Output.GetSteelDesignResultsForBeams(beams,cases);Assert.That(n.Calls.Count,Is.EqualTo(1));Assert.That(result.Count(),Is.EqualTo(1));Assert.That(result.Count(),Is.EqualTo(1));Assert.That(beams.Count,Is.EqualTo(1));Assert.That(cases.Count,Is.EqualTo(1));}
  [Test]
  public void FullInputQualificationAndChangedIdsPrecedeNativeCalls()
  {var n=new Output();using var w=Wrap(n);Assert.Throws<ArgumentException>(()=>w.Output.GetSteelDesignResultsForBeams(new[]{B(),null},Cases()));Assert.Throws<ArgumentException>(()=>w.Output.GetSteelDesignResultsForBeam(B(),new ILoadCase[]{Cases()[0],null}));Assert.Throws<ArgumentException>(()=>w.Output.GetSteelDesignResultsForBeam(B(),new[]{new LoadCase(1,"a"),new LoadCase(1,"b",LoadCaseType.LoadCombination)}));Assert.That(n.Calls,Is.Empty);var b=B();IEnumerable<ILoadCase> Edit(){b.Id=9;yield return new LoadCase(1,"a");}Assert.Throws<InvalidOperationException>(()=>w.Output.GetSteelDesignResultsForBeam(b,Edit()));Assert.That(n.Calls,Is.Empty);}
  [Test]
  public void NullInvalidAndOversizedInputsHaveExplicitErrors()
  {var n=new Output();using var w=Wrap(n);Assert.Throws<ArgumentNullException>(()=>w.Output.GetSteelDesignResultsForBeam((Beam)null,Cases()));Assert.Throws<ArgumentOutOfRangeException>(()=>w.Output.GetSteelDesignResultsForBeam(B(0),Cases()));Assert.Throws<ArgumentNullException>(()=>w.Output.GetSteelDesignResultsForBeam(B(),null));Assert.Throws<ArgumentNullException>(()=>w.Output.GetSteelDesignResultsForBeams((IEnumerable<Beam>)null,Cases()));Assert.Throws<ArgumentException>(()=>w.Output.GetSteelDesignResultsForBeams(Enumerable.Repeat(B(),100001),Cases()));Assert.Throws<ArgumentException>(()=>w.Output.GetSteelDesignResultsForBeam(B(),Enumerable.Range(1,100001).Select(i=>(ILoadCase)new LoadCase(i,"case"))));Assert.That(n.Calls,Is.Empty);}
  [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
  public void MalformedSuppliedCaseMetadataFailsBeforeNativeReads(int kind)
  {var n=new Output();using var w=Wrap(n);var lc=new LoadCase(1,"case");if(kind==0)lc.Id=0;else if(kind==1)lc.Title=null;else if(kind==2)lc.CaseType=(LoadCaseType)999;else lc.Type=(LoadType)999;Assert.Throws<ArgumentException>(()=>w.Output.GetSteelDesignResultsForBeam(B(),new[]{lc}));Assert.That(n.Calls,Is.Empty);}
  [Test]
  public void CurrentOwnedLoadIsRequiredEvenForEmptySelections()
  {var n=new Output();var load=new Load();using var w=Wrap(n,load);using var other=Wrap(new Output());Assert.Throws<ArgumentNullException>(()=>w.Output.GetSteelDesignResultsForBeams((IOSLoad)null,Array.Empty<Beam>()));Assert.Throws<ArgumentException>(()=>w.Output.GetSteelDesignResultsForBeam(other.Load,B()));var old=w.Load;w.RefreshComObjects();Assert.Throws<ArgumentException>(()=>w.Output.GetSteelDesignResultsForBeam(old,B()));Assert.Throws<ArgumentException>(()=>w.Output.GetSteelDesignResultsForBeam(new OSLoadAdapter(w),B()));Assert.That(w.Output.GetSteelDesignResultsForBeams(w.Load,Array.Empty<Beam>()),Is.Empty);Assert.That(load.Calls,Is.Empty);Assert.That(n.Calls,Is.Empty);}
  [Test]
  public void NativeLoadInventoryFailuresAndIdentityChangesPropagate()
  {var n=new Output();var load=new Load();using var w=Wrap(n,load);var failure=new COMException("inventory failure");load.Failure=failure;Assert.That(Assert.Throws<COMException>(()=>w.Output.GetSteelDesignResultsForBeam(w.Load,B())),Is.SameAs(failure));Assert.That(n.Calls,Is.Empty);load.Failure=null;var b=B();load.Callback=()=>b.Id=99;Assert.Throws<InvalidOperationException>(()=>w.Output.GetSteelDesignResultsForBeam(w.Load,b));Assert.That(n.Calls,Is.Empty);}
  [Test]
  public void CriticalCombinationIdentityUsesTheDiscoveredFamily()
  {var n=new Output();n.Values["case"]=201;using var w=Wrap(n);var r=w.Output.GetSteelDesignResultsForBeam(w.Load,B());Assert.That(r.CriticalLoadCase.CaseType,Is.EqualTo(LoadCaseType.LoadCombination));Assert.That(r.CriticalLoadCase.Title,Is.EqualTo("Case 201"));}
  [Test]
  public void ChangedBeamIdsAndLaterFailuresReturnNoPartialBatch()
  {var n=new Output();using var w=Wrap(n);var a=B();var b=B(8);n.Callback=id=>{if(id==8)a.Id=99;};Assert.Throws<InvalidOperationException>(()=>w.Output.GetSteelDesignResultsForBeams(new[]{a,b},Cases()));Assert.That(a.UtilizationRatio,Is.EqualTo(42));n.Callback=id=>{if(id==8)throw new COMException("later failure");};a.Id=7;Assert.Throws<COMException>(()=>w.Output.GetSteelDesignResultsForBeams(new[]{a,b},Cases()));}
  [Test]
  public void EmptyAndNullPhysicalMemberGroupsHaveNoNativeWork()
  {var n=new Output();using var w=Wrap(n);Assert.Throws<ArgumentNullException>(()=>w.Output.GetGoverningMember(null));Assert.That(w.Output.GetGoverningMember(new(){Members=null}),Is.Null);Assert.That(w.Output.GetGoverningMember(new(){Members=new(){null,new Member(),new Member{Beams=null}}}),Is.Null);Assert.That(n.Calls,Is.Empty);}
  [Test]
  public void CachedGoverningSelectionUsesGreatestConstituentAndStableFirstTie()
  {var n=new Output();using var w=Wrap(n);var low=new Member{Beams=new(){new Beam{Id=0,UtilizationRatio=.8}}};var high=new Member{Beams=new(){new Beam{Id=-1,UtilizationRatio=.9},new Beam{Id=-2,UtilizationRatio=1.4}}};var tie=new Member{Beams=new(){new Beam{Id=-3,UtilizationRatio=1.4}}};var groups=new PhysicalMemberIdentificationResult{Members=new(){low,high,tie}};Assert.That(w.Output.GetGoverningMember(groups,true),Is.SameAs(high));groups.Members.Reverse();Assert.That(w.Output.GetGoverningMember(groups,true),Is.SameAs(tie));Assert.That(n.Calls,Is.Empty);}
  [TestCase(-1d)] [TestCase(-999d)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
  public void UnavailableOrInvalidRatiosCannotChooseAGoverningMember(double value)
  {var n=new Output();using var w=Wrap(n);var b=B();b.UtilizationRatio=value;Assert.Throws<InvalidOperationException>(()=>w.Output.GetGoverningMember(Groups(b),true));b.UtilizationRatio=42;n.Ratios[7]=value;Assert.Throws<InvalidOperationException>(()=>w.Output.GetGoverningMember(Groups(b)));Assert.That(b.UtilizationRatio,Is.EqualTo(42));}
  [Test]
  public void SharedBeamReferencesAreReadAndPopulatedOnceAcrossRuns()
  {var n=new Output();using var w=Wrap(n);var a=B();var b=B(8);var first=new Member{Beams=new(){a}};var second=new Member{Beams=new(){a,b}};int notifications=0;a.PropertyChanged+=(_,_)=>notifications++;var g=new PhysicalMemberIdentificationResult{Members=new(){first,second}};Assert.That(w.Output.GetGoverningMember(g),Is.SameAs(second));Assert.That(n.Calls,Is.EqualTo(new[]{7,8}));Assert.That(notifications,Is.EqualTo(1));Assert.That(a.UtilizationRatio,Is.EqualTo(.8));Assert.That(b.UtilizationRatio,Is.EqualTo(1.3));}
  [Test]
  public void GoverningReadFailuresDoNotPopulateButNotificationFailuresRetainPriorWrites()
  {var n=new Output();using var w=Wrap(n);var a=B();var b=B(8);var g=Groups(a,b);n.Callback=id=>{if(id==8)throw new COMException("read failed");};Assert.Throws<COMException>(()=>w.Output.GetGoverningMember(g));Assert.That(a.UtilizationRatio,Is.EqualTo(42));n.Callback=null;var failure=new ApplicationException("handler failed");a.PropertyChanged+=(_,_)=>throw failure;Assert.That(Assert.Throws<ApplicationException>(()=>w.Output.GetGoverningMember(g)),Is.SameAs(failure));Assert.That(a.UtilizationRatio,Is.EqualTo(.8));Assert.That(b.UtilizationRatio,Is.EqualTo(42));}
  [Test]
  public void GoverningRejectsMalformedMembershipAndNativeDuplicateIdentity()
  {var n=new Output();using var w=Wrap(n);var g=Groups(B());g.Members[0].Beams.Add(null);Assert.Throws<ArgumentException>(()=>w.Output.GetGoverningMember(g));var a=B();var b=B();g=new(){Members=new(){new(){Beams=new(ReferenceEqualityComparer.Instance){a,b}}}};Assert.Throws<ArgumentException>(()=>w.Output.GetGoverningMember(g));Assert.That(n.Calls,Is.Empty);}
  [TestCase(false)] [TestCase(true)]
  public void GoverningEntryAndTotalMembershipBudgetsFailBeforeNativeReads(bool members)
  {var n=new Output();using var w=Wrap(n);var g=members?new PhysicalMemberIdentificationResult{Members=Enumerable.Repeat<Member>(null,100001).ToList()}:Groups(Enumerable.Range(1,100001).Select(B).ToArray());Assert.Throws<ArgumentException>(()=>w.Output.GetGoverningMember(g));Assert.That(n.Calls,Is.Empty);}
  [TestCaseSource(nameof(Operations))]
  public void ThreadCurrentBindingLifetimeAndNativeFailureIdentityArePreserved(string op)
  {var n=new Output();using var w=Wrap(n);var output=w.Output;Assert.Throws<InvalidOperationException>(()=>Invoke(new OSOutputAdapter(w),w.Load,op));Exception failure=null;var thread=new Thread(()=>{try{Invoke(output,w.Load,op);}catch(Exception e){failure=e;}});thread.Start();Assert.That(thread.Join(5000),Is.True);Assert.That(failure,Is.TypeOf<InvalidOperationException>());w.RefreshComObjects();Assert.Throws<InvalidOperationException>(()=>Invoke(output,w.Load,op));output=w.Output;var original=new COMException("native failure",unchecked((int)0x80010108));n.Failure=original;Assert.That(Assert.Throws<COMException>(()=>Invoke(output,w.Load,op)),Is.SameAs(original));n.Failure=null;Assert.DoesNotThrow(()=>Invoke(output,w.Load,op));w.Dispose();Assert.Throws<ObjectDisposedException>(()=>Invoke(output,w.Load,op));}
  [TestCaseSource(nameof(Operations))]
  public void SharedLeaseRejectsCallbacksAcrossDomainsAndLifecycle(string op)
  {var n=new Output();using var w=Wrap(n);n.Callback=_=>{foreach(string nested in Operations)Assert.Throws<InvalidOperationException>(()=>Invoke(w.Output,w.Load,nested));Assert.Throws<InvalidOperationException>(()=>w.Load.GetAllPrimaryLoadCases());Assert.Throws<InvalidOperationException>(()=>w.Root.GetBaseUnit());Assert.Throws<InvalidOperationException>(()=>w.RefreshComObjects());Assert.Throws<InvalidOperationException>(()=>w.Dispose());};Invoke(w.Output,w.Load,op);n.Callback=null;Assert.DoesNotThrow(()=>Invoke(w.Output,w.Load,op));using var missing=WrapMissing();Assert.Throws<RuntimeBinderException>(()=>Invoke(missing.Output,missing.Load,op));}
  private static OpenStaadWrapper WrapMissing()=>new(new NativeRoot{Output=new object(),Load=new Load()});
  [Test]
  public void SupportingModelsHaveIndependentContainersAndNamedReportedComponents()
  {var a=new PhysicalMemberIdentificationResult();var b=new PhysicalMemberIdentificationResult();Assert.That(a.Members,Is.Not.SameAs(b.Members));Assert.That(a.DiscardedBeams,Is.Not.SameAs(b.DiscardedBeams));Assert.That(new Member().Beams,Is.Not.SameAs(new Member().Beams));var x=new Node(1,0,0,0);var m=new Member(9,x,null);Assert.That(m.Id,Is.EqualTo(9));Assert.That(m.StartNode,Is.SameAs(x));Assert.That(m.Nodes,Does.Contain(x));Assert.That((int)DesignStatus.Passed,Is.Zero);Assert.That((int)DesignStatus.Failed,Is.EqualTo(1));var r=new MemberSteelDesignResults(null,null,DesignStatus.Failed,-1,-1,null,-1,null,null,null,-1);Assert.That(r.UtilizationRatio,Is.EqualTo(-1));r.Code="edit";Assert.That(r.Code,Is.EqualTo("edit"));}
  [TestCaseSource(nameof(Operations))]
  public void ExactPublicOverloadsDefaultsAndMatchingCompiledXmlAreShipped(string op)
  {
   string name=op=="governing"?"GetGoverningMember":op is "one" or "oneLoad"?"GetSteelDesignResultsForBeam":"GetSteelDesignResultsForBeams";
   Type[] types=op=="one"?[typeof(Beam),typeof(IEnumerable<ILoadCase>)]:op=="oneLoad"?[typeof(IOSLoad),typeof(Beam)]:op=="many"?[typeof(IEnumerable<Beam>),typeof(IEnumerable<ILoadCase>)]:op=="manyLoad"?[typeof(IOSLoad),typeof(IEnumerable<Beam>)]:[typeof(PhysicalMemberIdentificationResult),typeof(bool)];
   string[] names=op=="one"?["beam","allLoadCases"]:op=="oneLoad"?["load","beam"]:op=="many"?["beams","allLoadCases"]:op=="manyLoad"?["load","beams"]:["pmiResult","ratiosPopulated"];
   Type result=op=="governing"?typeof(Member):op is "one" or "oneLoad"?typeof(MemberSteelDesignResults):typeof(IEnumerable<MemberSteelDesignResults>);
   var docs=XDocument.Load(Path.ChangeExtension(typeof(IOSOutput).Assembly.Location,".xml"));XElement previous=null;
   foreach(Type owner in new[]{typeof(IOSOutput),typeof(OSOutputAdapter)})
   {var method=owner.GetMethod(name,types);Assert.That(method.ReturnType,Is.EqualTo(result));Assert.That(method.GetParameters().Select(p=>p.Name),Is.EqualTo(names));Assert.That(method.GetParameters().Any(p=>p.ParameterType.IsByRef),Is.False);Assert.That(method.GetParameters()[1].IsOptional,Is.EqualTo(op=="governing"));if(op=="governing")Assert.That(method.GetParameters()[1].DefaultValue,Is.EqualTo(false));
    string Format(Type t)=>t.IsGenericType?t.GetGenericTypeDefinition().FullName.Split('`')[0]+"{"+string.Join(",",t.GetGenericArguments().Select(Format))+"}":t.FullName;
    string id="M:"+owner.FullName+"."+name+"("+string.Join(",",types.Select(Format))+")";var doc=docs.Descendants("member").Single(e=>(string)e.Attribute("name")==id);foreach(string tag in new[]{"summary","returns","remarks","exception","example"})Assert.That(doc.Element(tag),Is.Not.Null);Assert.That(doc.Elements("param").Select(p=>(string)p.Attribute("name")),Is.EqualTo(names));Assert.That(doc.Descendants().Any(e=>e.Name=="include"||e.Name=="inheritdoc"),Is.False);if(previous!=null)Assert.That(doc.Elements().Select(e=>e.ToString()),Is.EqualTo(previous.Elements().Select(e=>e.ToString())));previous=doc;}
  }
 }
}
