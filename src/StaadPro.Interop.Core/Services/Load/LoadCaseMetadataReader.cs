using System;
using System.Collections.Generic;
using System.Linq;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Services.Interop;
namespace StaadPro.Interop.Core.Services.Load
{
    internal static class LoadCaseMetadataReader
    {
        internal const int MaximumCases=100000;
        internal static ILoadCase Read(object load,int lcId,bool primary)
        {
            if(lcId<=0)throw new ArgumentOutOfRangeException(nameof(lcId));
            object title=primary?((dynamic)load).GetLoadCaseTitle(lcId):((dynamic)load).GetReferenceLoadCaseTitle(lcId);
            string text=NativeValues.ReadText(title,"load-case title");
            object nativeType=primary?((dynamic)load).GetLoadType(lcId):((dynamic)load).GetReferenceLoadType(lcId);
            int code=NativeValues.ReadInt32(nativeType,"engineering load type");
            if(!Enum.IsDefined((LoadType)code))throw new InvalidOperationException("OpenSTAAD returned an undefined engineering load type.");
            return new LoadCase(lcId,text,primary?LoadCaseType.PrimaryLoad:LoadCaseType.ReferenceLoad,(LoadType)code);
        }
        internal static List<ILoadCase> ReadPrimary(object load,IEnumerable<int> loadCasesIds)
        {
            if(loadCasesIds==null)throw new ArgumentNullException(nameof(loadCasesIds));
            var ids=new List<int>();
            foreach(int id in loadCasesIds){if(id<=0)throw new ArgumentOutOfRangeException(nameof(loadCasesIds));if(ids.Count==MaximumCases)throw new ArgumentException("At most 100,000 cases may be requested.",nameof(loadCasesIds));ids.Add(id);}
            var result=new List<ILoadCase>(ids.Count);foreach(int id in ids)result.Add(Read(load,id,true));return result;
        }
        internal static HashSet<ILoadCase> ReadAll(object load,bool primary)
        {
            int count=Count(load,primary);var result=new HashSet<ILoadCase>();if(count==0)return result;
            object buffer=Enumerable.Repeat(int.MinValue,count).ToArray();
            object status=primary?((dynamic)load).GetPrimaryLoadCaseNumbers(ref buffer):((dynamic)load).GetReferenceLoadCaseNumbers(ref buffer);
            if(primary&&status is bool success){if(!success)throw new InvalidOperationException("Primary case-number retrieval failed.");}
            else if(NativeValues.ReadInt32(status,"load-case number count")!=count)throw new InvalidOperationException("Case-number count changed or is inconsistent.");
            if(buffer is not Array a||a.Rank!=1||a.Length!=count||a.GetType().GetElementType()!=typeof(int))throw new InvalidOperationException("Case IDs require a one-dimensional Int32 array matching the count.");
            var ids=new int[count];var seen=new HashSet<int>();int lower=a.GetLowerBound(0);
            for(int i=0;i<count;i++){int id=(int)a.GetValue(lower+i);if(id<=0||!seen.Add(id))throw new InvalidOperationException("Case IDs are unwritten, nonpositive or duplicated.");ids[i]=id;}
            if(primary&&status is bool&&Count(load,true)!=count)throw new InvalidOperationException("Primary inventory changed after Boolean number retrieval.");
            foreach(int id in ids)result.Add(Read(load,id,primary));return result;
        }
        private static int Count(object load,bool primary)
        {
            object value=primary?((dynamic)load).GetPrimaryLoadCaseCount():((dynamic)load).GetReferenceLoadCaseCount();
            int count=NativeValues.ReadInt32(value,"load-case count");if(count<0||count>MaximumCases)throw new InvalidOperationException("Case count is negative or exceeds 100,000.");return count;
        }
    }
}
