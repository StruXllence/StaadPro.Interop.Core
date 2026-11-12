using System;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models.Root;
using StaadPro.Interop.Core.Services.Interop;
namespace StaadPro.Interop.Core.Services.Output
{
    internal static class MemberForceReader
    {
        internal static bool AreResultsAvailable(object output) => NativeValues.ReadBoolean(((dynamic)output).AreResultsAvailable(), "result availability");
        internal static string GetLoadCaseTitle(object load, int lcId)
        {
            if(lcId<=0)throw new ArgumentOutOfRangeException(nameof(lcId));
            return NativeValues.ReadText(((dynamic)load).GetLoadCaseTitle(lcId), "load-case title");
        }
        internal static MemberForceQueryResult Read(object output,string lcTitle,int beamId,int loadCaseId,MemberEnd memberEnd,LocalOrGlobal coordinateSystem)
        {
            if(lcTitle==null)throw new ArgumentNullException(nameof(lcTitle));
            if(beamId<=0)throw new ArgumentOutOfRangeException(nameof(beamId));
            if(loadCaseId<=0)throw new ArgumentOutOfRangeException(nameof(loadCaseId));
            if(!Enum.IsDefined(memberEnd))throw new ArgumentOutOfRangeException(nameof(memberEnd));
            if(!Enum.IsDefined(coordinateSystem))throw new ArgumentOutOfRangeException(nameof(coordinateSystem));
            object buffer=new[]{double.NaN,double.NaN,double.NaN,double.NaN,double.NaN,double.NaN};
            object status=((dynamic)output).GetMemberEndForces(beamId,(int)memberEnd,loadCaseId,ref buffer,(int)coordinateSystem);
            bool success=NativeValues.ReadBoolean(status,"member-end force flag");
            MemberForces forces=null;
            if(success)
            {
                if(buffer is not Array array||array.Rank!=1||array.Length!=6||array.GetType().GetElementType()!=typeof(double))
                    throw new InvalidOperationException("Successful member-end forces require six Double values.");
                var values=new double[6];int lower=array.GetLowerBound(0);
                for(int i=0;i<6;i++){double value=(double)array.GetValue(lower+i);if(!double.IsFinite(value))throw new InvalidOperationException("Successful member-end forces are unwritten or nonfinite.");values[i]=value;}
                forces=new MemberForces{BeamId=beamId,LoadCaseId=loadCaseId,LoadCaseTitle=lcTitle,End=memberEnd,Fx=values[0],Fy=values[1],Fz=values[2],Mx=values[3],My=values[4],Mz=values[5]};
            }
            return new MemberForceQueryResult(beamId,loadCaseId,lcTitle,memberEnd,success,forces,coordinateSystem);
        }
    }
}
