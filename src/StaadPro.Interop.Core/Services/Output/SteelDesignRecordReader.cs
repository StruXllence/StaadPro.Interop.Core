using System;
using System.Collections.Generic;
using System.Linq;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Output;
using StaadPro.Interop.Core.Services.Interop;
using StaadPro.Interop.Core.Services.Load;
namespace StaadPro.Interop.Core.Services.Output
{
    internal static class SteelDesignRecordReader
    {
        private const int Limit=100000;
        private sealed class Selection
        {
            internal readonly Beam Beam; internal readonly int Id;
            internal Selection(Beam beam,int id){Beam=beam;Id=id;}
        }
        internal static MemberSteelDesignResults One(object output,Beam beam,IEnumerable<ILoadCase> allLoadCases)
        {
            if(ReferenceEquals(beam,null))throw new ArgumentNullException(nameof(beam));
            int id=beam.Id;if(id<=0)throw new ArgumentOutOfRangeException(nameof(beam),"The member ID must be positive.");
            var cases=CaptureCases(allLoadCases);RequireUnchanged(new Selection(beam,id));var result=Query(output,beam,id,cases);
            RequireUnchanged(new Selection(beam,id));return result;
        }
        internal static IEnumerable<MemberSteelDesignResults> Many(object output,IEnumerable<Beam> beams,IEnumerable<ILoadCase> allLoadCases)
        {
            var selected=CaptureBeams(beams);var cases=CaptureCases(allLoadCases);return ReadSelected(output,selected,cases);
        }
        internal static MemberSteelDesignResults OneWithLoad(OpenStaadWrapper wrapper,IOSLoad load,Beam beam)
        {
            ValidateLoad(wrapper,load);
            if(ReferenceEquals(beam,null))throw new ArgumentNullException(nameof(beam));int id=beam.Id;
            if(id<=0)throw new ArgumentOutOfRangeException(nameof(beam),"The member ID must be positive.");
            var cases=Discover(wrapper.RawLoad);RequireUnchanged(new Selection(beam,id));
            var result=Query(wrapper.RawOutput,beam,id,cases);RequireUnchanged(new Selection(beam,id));return result;
        }
        internal static IEnumerable<MemberSteelDesignResults> ManyWithLoad(OpenStaadWrapper wrapper,IOSLoad load,IEnumerable<Beam> beams)
        {
            ValidateLoad(wrapper,load);var selected=CaptureBeams(beams);
            if(selected.Count==0)return new List<MemberSteelDesignResults>();
            var cases=Discover(wrapper.RawLoad);return ReadSelected(wrapper.RawOutput,selected,cases);
        }
        private static void ValidateLoad(OpenStaadWrapper wrapper,IOSLoad load)
        {
            if(load==null)throw new ArgumentNullException(nameof(load));
            if(!ReferenceEquals(wrapper.Load,load))throw new ArgumentException("Use this wrapper's current Load adapter.",nameof(load));
        }
        private static Dictionary<int,LoadCase> Discover(object load)=>CaptureCases(
            LoadCaseMetadataReader.ReadAll(load,true).Concat(LoadCaseMetadataReader.ReadAllCombinations(load)));
        private static List<Selection> CaptureBeams(IEnumerable<Beam> beams)
        {
            if(beams==null)throw new ArgumentNullException(nameof(beams));var selected=new List<Selection>();
            foreach(var beam in beams)
            {
                if(selected.Count==Limit)throw new ArgumentException("At most 100,000 beam occurrences are allowed.",nameof(beams));
                if(ReferenceEquals(beam,null))throw new ArgumentException("A selected beam is null.",nameof(beams));
                int id=beam.Id;if(id<=0)throw new ArgumentException("Selected beam IDs must be positive.",nameof(beams));
                selected.Add(new Selection(beam,id));
            }
            return selected;
        }
        private static Dictionary<int,LoadCase> CaptureCases(IEnumerable<ILoadCase> allLoadCases)
        {
            if(allLoadCases==null)throw new ArgumentNullException(nameof(allLoadCases));var result=new Dictionary<int,LoadCase>();
            foreach(var loadCase in allLoadCases)
            {
                if(result.Count==Limit)throw new ArgumentException("At most 100,000 case records are allowed.",nameof(allLoadCases));
                if(loadCase==null)throw new ArgumentException("Case metadata contains null.",nameof(allLoadCases));
                int id=loadCase.Id;string title=loadCase.Title;var family=loadCase.CaseType;var type=loadCase.Type;
                if(id<=0||title==null||!Enum.IsDefined(family)||!Enum.IsDefined(type)||result.ContainsKey(id))
                    throw new ArgumentException("Case metadata needs unique positive IDs, non-null titles and defined classification codes.",nameof(allLoadCases));
                result.Add(id,new LoadCase(id,title,family,type));
            }
            return result;
        }
        private static List<MemberSteelDesignResults> ReadSelected(object output,List<Selection> selected,Dictionary<int,LoadCase> cases)
        {
            foreach(var entry in selected)RequireUnchanged(entry);
            var result=new List<MemberSteelDesignResults>(selected.Count);
            foreach(var entry in selected){RequireUnchanged(entry);result.Add(Query(output,entry.Beam,entry.Id,cases));}
            foreach(var entry in selected)RequireUnchanged(entry);
            return result;
        }
        private static void RequireUnchanged(Selection entry)
        {if(entry.Beam.Id!=entry.Id)throw new InvalidOperationException("A selected beam ID changed during the observation.");}
        private static MemberSteelDesignResults Query(object output,Beam beam,int id,Dictionary<int,LoadCase> cases)
        {
            string marker="\u001fsteel-"+Guid.NewGuid().ToString("N");
            object code=marker,status=marker,ratio=double.NaN,allowable=double.NaN,loadId=int.MinValue,location=double.NaN,clause=marker,section=marker,forces=new[]{double.NaN,double.NaN,double.NaN},slenderness=double.NaN;
            object success=((dynamic)output).GetMemberSteelDesignResults(id,ref code,ref status,ref ratio,ref allowable,ref loadId,ref location,ref clause,ref section,ref forces,ref slenderness);
            if(!NativeValues.ReadBoolean(success,"steel-design result success"))throw new InvalidOperationException("OpenSTAAD could not retrieve steel-design results.");
            string codeText=Text(code,marker,"design code"),statusText=Text(status,marker,"design status");
            DesignStatus designStatus;
            if(string.Equals(statusText.Trim(),"PASS",StringComparison.OrdinalIgnoreCase))designStatus=DesignStatus.Passed;
            else if(string.Equals(statusText.Trim(),"FAIL",StringComparison.OrdinalIgnoreCase))designStatus=DesignStatus.Failed;
            else throw new InvalidOperationException("Steel design status must be native PASS or FAIL; numeric/unknown statuses cannot be inferred.");
            double utilization=Nonnegative(ratio,"utilization ratio"),allowableValue=Nonnegative(allowable,"allowable ratio"),position=Nonnegative(location,"critical section"),slender=Nonnegative(slenderness,"slenderness ratio");
            int caseId=NativeValues.ReadInt32(loadId,"critical load-case ID");
            if(caseId<=0||!cases.TryGetValue(caseId,out var loadCase))throw new InvalidOperationException("The critical load-case ID cannot be uniquely resolved from supplied case metadata.");
            string clauseText=Text(clause,marker,"critical clause"),sectionText=Text(section,marker,"design section");
            if(forces is not Array array||array.Rank!=1||array.Length!=3||array.GetType().GetElementType()!=typeof(double))throw new InvalidOperationException("Steel critical-section forces require exactly three Double components.");
            int lower=array.GetLowerBound(0);double fx=Finite(array.GetValue(lower),"Fx"),my=Finite(array.GetValue(lower+1),"My"),mz=Finite(array.GetValue(lower+2),"Mz");
            return new MemberSteelDesignResults(beam,codeText,designStatus,utilization,allowableValue,
                new LoadCase(loadCase.Id,loadCase.Title,loadCase.CaseType,loadCase.Type),position,clauseText,sectionText,new SteelDesignForces(fx,my,mz),slender);
        }
        private static string Text(object value,string marker,string name)
        {string text=NativeValues.ReadText(value,name);if(text==marker)throw new InvalidOperationException("OpenSTAAD left "+name+" unwritten.");return text;}
        private static double Finite(object value,string name)
        {if(value is not double number||!double.IsFinite(number))throw new InvalidOperationException("OpenSTAAD returned an unwritten/nonfinite/non-Double "+name+".");return number;}
        private static double Nonnegative(object value,string name)
        {double number=Finite(value,name);if(number<0)throw new InvalidOperationException("OpenSTAAD returned negative "+name+".");return number;}
    }
}
