using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Services.Output
{
    internal static class GoverningMemberReader
    {
        private sealed class Run
        {
            internal readonly Member Member;
            internal readonly List<Beam> Beams;
            internal Run(Member member,List<Beam> beams){Member=member;Beams=beams;}
        }
        internal static Member Read(object output,PhysicalMemberIdentificationResult pmiResult,bool ratiosPopulated)
        {
            if(pmiResult==null)throw new ArgumentNullException(nameof(pmiResult));
            var members=pmiResult.Members;if(members==null)return null;
            var runs=new List<Run>();var selected=new HashSet<Beam>(ReferenceEqualityComparer.Instance);
            var ids=new Dictionary<Beam,int>(ReferenceEqualityComparer.Instance);var nativeIds=new HashSet<int>();int memberships=0,memberCount=0;
            foreach(var member in members)
            {
                if(++memberCount>100000)throw new ArgumentException("At most 100,000 member entries are allowed.",nameof(pmiResult));
                if(member==null)continue;var set=member.Beams;if(set==null||set.Count==0)continue;
                var beams=new List<Beam>();
                foreach(var beam in set)
                {
                    if(++memberships>100000)throw new ArgumentException("At most 100,000 beam memberships are allowed.",nameof(pmiResult));
                    if(ReferenceEquals(beam,null))throw new ArgumentException("A selected member contains a null beam.",nameof(pmiResult));
                    beams.Add(beam);
                    if(selected.Add(beam)&&!ratiosPopulated)
                    {
                        int id=beam.Id;if(id<=0||!nativeIds.Add(id))throw new ArgumentException("Native retrieval needs unique positive IDs for distinct beam references.",nameof(pmiResult));
                        ids.Add(beam,id);
                    }
                }
                runs.Add(new Run(member,beams));
            }
            if(runs.Count==0)return null;
            var ratios=ratiosPopulated?new Dictionary<Beam,double>(ReferenceEqualityComparer.Instance):SteelDesignBatchReader.Read(output,selected);
            if(ratiosPopulated)foreach(var beam in selected)ratios.Add(beam,beam.UtilizationRatio);
            foreach(double value in ratios.Values)
                if(!double.IsFinite(value)||value<0)throw new InvalidOperationException("A governing member needs finite nonnegative utilization ratios; unavailable design sentinels cannot be ranked.");
            Member winner=null;double maximum=double.NegativeInfinity;
            foreach(var run in runs)
            {
                double runMax=double.NegativeInfinity;foreach(var beam in run.Beams)runMax=Math.Max(runMax,ratios[beam]);
                if(winner==null||runMax>maximum){winner=run.Member;maximum=runMax;}
            }
            if(!ratiosPopulated)
            {
                foreach(var beam in selected)
                {RequireUnchanged(beam,ids[beam]);beam.UtilizationRatio=ratios[beam];}
                foreach(var beam in selected)RequireUnchanged(beam,ids[beam]);
            }
            return winner;
        }
        private static void RequireUnchanged(Beam beam,int id)
        {if(beam.Id!=id)throw new InvalidOperationException("A selected beam ID changed during population.");}
    }
}
