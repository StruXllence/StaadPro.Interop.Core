using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Interfaces.Capabilities;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Enums.Root;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Models.Root;
using StaadPro.Interop.Core.Services.Root.Capabilities;
using RootOptionalTestDoubles = StaadPro.Interop.Core.Tests.Adapter.RootOptionalTestDoubles;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class OSRootViewAndCapabilityTests
    {
        public sealed class NativeRoot : RootOptionalTestDoubles.NativeRoot
        {
            public string ViewName;
            public object ErrorSlot;
            public int[] ViewIds;
            public int EntityCode, Mode;
            public void RemoveNamedView(object name, object error)
            {
                Assert.That(name, Is.TypeOf<string>()); Assert.That(error, Is.TypeOf<int>().And.EqualTo(0));
                ViewName=(string)name;ErrorSlot=error;Calls.Add("remove");OnCall?.Invoke();if(Failure!=null)throw Failure;
            }
            public void ModifyNamedView(object name, object ids, object entity, object mode)
            {
                Assert.That(name, Is.TypeOf<string>());Assert.That(ids, Is.TypeOf<int[]>());
                Assert.That(entity, Is.TypeOf<int>());Assert.That(mode, Is.TypeOf<int>());
                ViewName=(string)name;ViewIds=(int[])ids;EntityCode=(int)entity;Mode=(int)mode;
                Calls.Add("modify");OnCall?.Invoke();if(Failure!=null)throw Failure;
            }
        }
        private sealed class RecordingInspector : IRootMemberInspector
        {
            internal readonly List<string> Calls=[];
            internal readonly Dictionary<string,RootMemberEvidence> Results=[];
            internal Exception Failure;
            internal Action OnInspect;
            public RootMemberEvidence Inspect(object root,RootNativeContract contract)
            {Calls.Add(contract.Name);OnInspect?.Invoke();if(Failure!=null)throw Failure;return Results.TryGetValue(contract.Name,out var evidence)?evidence:RootMemberEvidence.Compatible;}
        }
        private sealed class SingleUseIds : IEnumerable<int>
        {
            internal int Enumerations;
            internal Action OnEnumerate;
            public IEnumerator<int> GetEnumerator()
            {if(++Enumerations!=1)throw new Exception("Re-enumerated");OnEnumerate?.Invoke();yield return 3;yield return 1;yield return 3;}
            IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
        }
        private static readonly int[] Qualifiers=[0,1,2,3,4];
        public sealed class NoVersionRoot
        {
            public object Geometry { get; }=new object();
            public object Load { get; }=new object();
        }
        private static readonly int[] Flags=[int.MinValue,-1,0,1,int.MaxValue];
        private static readonly string[] GuardOperations=["remove","modify","export","errors","warnings","acquireExport","acquireErrors","acquireWarnings","gateway"];
        private static Action Capture(OpenStaadWrapper wrapper,string operation)
        {
            var root=wrapper.Root;var gateway=root.Capabilities;
            return operation switch
            {
                "remove"=>()=>root.RemoveNamedView(" View "),
                "modify"=>()=>root.ModifyNamedView(" View ",new[]{1},NamedViewEntityType.Beam,0),
                "export"=>Export(gateway.GetAdinaExport().Capability),
                "errors"=>Errors(gateway.GetAnalysisErrorMessages().Capability),
                "warnings"=>Warnings(gateway.GetAnalysisWarningMessages().Capability),
                "acquireExport"=>()=>gateway.GetAdinaExport(),
                "acquireErrors"=>()=>gateway.GetAnalysisErrorMessages(),
                "acquireWarnings"=>()=>gateway.GetAnalysisWarningMessages(),
                _=>()=>{_ = root.Capabilities;}
            };
        }
        private static Action Export(IOSRootAdinaExport api)=>()=>api.ExportModelToAdina(AdinaAxisSystem.Preserve,AdinaLoadCaseHandling.MultipleLoadCases);
        private static Action Errors(IOSRootAnalysisErrorMessages api)=>()=>api.GetAnalysisErrorMessages(@"C:\Models\Frame.std");
        private static Action Warnings(IOSRootAnalysisWarningMessages api)=>()=>api.GetAnalysisWarningMessages(@"C:\Models\Frame.std");

        [Test]
        public void RemovalPreservesExactTextAndPassesOnlyRequiredByValueZeroWithoutQueries()
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);native.Queries.Clear();
            wrapper.Root.RemoveNamedView(" \u03a9 View\r\n ");
            Assert.That(native.ViewName,Is.EqualTo(" \u03a9 View\r\n "));Assert.That(native.ErrorSlot,Is.EqualTo(0));
            Assert.That(native.Calls,Is.EqualTo(new[]{"remove"}));Assert.That(native.Queries,Is.Empty);
        }
        [TestCaseSource(nameof(Qualifiers))]
        public void EveryQualifierAndArbitraryFlagRetainsArrayOrderDuplicatesAndPrivateOwnership(int qualifier)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);
            foreach(int flag in Flags)
            {
                int[] ids=[3,1,3];wrapper.Root.ModifyNamedView(" V ",ids,(NamedViewEntityType)qualifier,flag);
                Assert.That(native.ViewIds,Is.EqualTo(ids).And.Not.SameAs(ids));Assert.That(native.EntityCode,Is.EqualTo(qualifier));
                Assert.That(native.Mode,Is.EqualTo(flag));Assert.That(native.ViewName,Is.EqualTo(" V "));
                native.ViewIds[0]=999;Assert.That(ids[0],Is.EqualTo(3));
            }
        }
        [Test]
        public void EmptyModificationIsAnExplicitNativeCallAndSingleUseSequenceIsEnumeratedOnceInsideLease()
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);
            wrapper.Root.ModifyNamedView("view",Array.Empty<int>(),NamedViewEntityType.Node,0);Assert.That(native.ViewIds,Is.Empty);
            var ids=new SingleUseIds {OnEnumerate=()=>
            {
                Assert.Throws<InvalidOperationException>(()=>wrapper.Root.RemoveNamedView("v"));
                Assert.Throws<InvalidOperationException>(()=>wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(()=>wrapper.Dispose());
            }};
            wrapper.Root.ModifyNamedView("view",ids,NamedViewEntityType.Beam,1);
            Assert.That(ids.Enumerations,Is.EqualTo(1));Assert.That(native.ViewIds,Is.EqualTo(new[]{3,1,3}));
        }
        [TestCase(null)] [TestCase("")] [TestCase(" \t")]
        public void InvalidNamesPreventRemovalAndModificationBeforeEnumeration(string name)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);var ids=new SingleUseIds();
            Assert.That(Assert.Catch<ArgumentException>(()=>wrapper.Root.RemoveNamedView(name)).ParamName,Is.EqualTo("viewName"));
            Assert.That(Assert.Catch<ArgumentException>(()=>wrapper.Root.ModifyNamedView(name,ids,NamedViewEntityType.Beam,0)).ParamName,Is.EqualTo("viewName"));
            Assert.That(ids.Enumerations,Is.Zero);Assert.That(native.Calls,Is.Empty);
        }
        [TestCase(-1)] [TestCase(5)] [TestCase(int.MaxValue)]
        public void InvalidQualifierRejectsBeforeEnumeration(int qualifier)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);var ids=new SingleUseIds();
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(()=>wrapper.Root.ModifyNamedView("v",ids,(NamedViewEntityType)qualifier,0)).ParamName,Is.EqualTo("entityType"));
            Assert.That(ids.Enumerations,Is.Zero);Assert.That(native.Calls,Is.Empty);
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MinValue)]
        public void NonpositiveIdentifiersRejectBeforeMutation(int id)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);
            Assert.That(Assert.Throws<ArgumentException>(()=>wrapper.Root.ModifyNamedView("v",new[]{1,id,3},NamedViewEntityType.Beam,0)).ParamName,Is.EqualTo("entityIds"));
            Assert.That(native.Calls,Is.Empty);
        }
        [Test]
        public void NullOrFailingEnumeratorsRetainParameterAndOriginalErrorBeforeMutation()
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);
            Assert.That(Assert.Throws<ArgumentNullException>(()=>wrapper.Root.ModifyNamedView("v",null,NamedViewEntityType.Node,0)).ParamName,Is.EqualTo("entityIds"));
            var failure=new InvalidOperationException("enumeration failed");
            var ids=new SingleUseIds{OnEnumerate=()=>throw failure};
            Assert.That(Assert.Throws<InvalidOperationException>(()=>wrapper.Root.ModifyNamedView("v",ids,NamedViewEntityType.Node,0)),Is.SameAs(failure));
            Assert.That(native.Calls,Is.Empty);Assert.DoesNotThrow(()=>wrapper.Root.GetBaseUnit());
        }
        [TestCase("24.0.0.1",RootCapabilityAvailability.DisabledByVersionPolicy)]
        [TestCase("27.0.0.340",RootCapabilityAvailability.DisabledByVersionPolicy)]
        [TestCase("26.0.1.491",RootCapabilityAvailability.ContractUnverified)]
        [TestCase("26.0.0.341",RootCapabilityAvailability.ContractUnverified)]
        public void UncoveredVersionsFailClosedWithoutProbingOrExecutingOptionalMembers(string version,RootCapabilityAvailability expected)
        {
            var native=new NativeRoot{VersionText=version};using var wrapper=new OpenStaadWrapper(native);
            var inspector=new RecordingInspector();((OSRootAdapter)wrapper.Root).CapabilityInspector=inspector;
            var gateway=wrapper.Root.Capabilities;
            Assert.That(gateway.GetAdinaExport().Availability,Is.EqualTo(expected));
            Assert.That(gateway.GetAnalysisErrorMessages().Availability,Is.EqualTo(expected));
            Assert.That(gateway.GetAnalysisWarningMessages().Availability,Is.EqualTo(expected));
            Assert.That(inspector.Calls,Is.Empty);Assert.That(native.Calls,Is.Empty);
        }
        [Test]
        public void Exact2025ProfileGrantsOnlyExportAndNoGatewayAccessExecutesOperations()
        {
            var native=new NativeRoot{VersionText="25.0.1.424"};using var wrapper=new OpenStaadWrapper(native);native.Queries.Clear();
            var gateway=wrapper.Root.Capabilities;
            Assert.That(gateway.GetAdinaExport().IsAvailable,Is.True);
            Assert.That(gateway.GetAnalysisErrorMessages().Availability,Is.EqualTo(RootCapabilityAvailability.DisabledByVersionPolicy));
            Assert.That(gateway.GetAnalysisWarningMessages().Capability,Is.Null);
            Assert.That(native.Calls,Is.Empty);Assert.That(native.Queries,Is.Empty);
        }
        [Test]
        public void MissingVersionAndMissingMembersDoNotGrantOptionalOperations()
        {
            using(var wrapper=new OpenStaadWrapper(new NoVersionRoot()))
            {
                Assert.That(wrapper.Root.Capabilities.GetAdinaExport().Availability,Is.EqualTo(RootCapabilityAvailability.UnknownVersion));
                Assert.That(wrapper.Root.Capabilities.GetAnalysisErrorMessages().Capability,Is.Null);
            }
            using(var wrapper=new OpenStaadWrapper(new Models.OpenStaadWrapperParityTests.NativeRoot()))
            {
                Assert.That(wrapper.Root.Capabilities.GetAdinaExport().Availability,Is.EqualTo(RootCapabilityAvailability.NativeMemberAbsent));
                Assert.That(wrapper.Root.Capabilities.GetAnalysisWarningMessages().Capability,Is.Null);
            }
        }
        [Test]
        public void ManualNoncurrentRootCannotExposeAnyCapabilityGateway()
        {
            using var wrapper=new OpenStaadWrapper(new NativeRoot());
            Assert.Throws<InvalidOperationException>(()=>{_ = new OSRootAdapter(wrapper).Capabilities;});
        }
        [TestCase((int)RootMemberEvidence.Absent,RootCapabilityAvailability.NativeMemberAbsent)]
        [TestCase((int)RootMemberEvidence.SignatureUnverified,RootCapabilityAvailability.ContractUnverified)]
        public void EachCapabilityAndTheStatusDependencyMustHaveCompatibleMetadata(int code,RootCapabilityAvailability outcome)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);var inspector=new RecordingInspector();
            ((OSRootAdapter)wrapper.Root).CapabilityInspector=inspector;var gateway=wrapper.Root.Capabilities;
            inspector.Results["GetAnalysisErrorMessages"]=(RootMemberEvidence)code;
            Assert.That(gateway.GetAnalysisErrorMessages().Availability,Is.EqualTo(outcome));
            Assert.That(gateway.GetAnalysisWarningMessages().IsAvailable,Is.True);
            inspector.Results["GetAnalysisStatus"]=(RootMemberEvidence)code;
            Assert.That(gateway.GetAnalysisWarningMessages().Availability,Is.EqualTo(outcome));
            Assert.That(gateway.GetAdinaExport().IsAvailable,Is.True);Assert.That(native.Calls,Is.Empty);
        }
        [TestCase(unchecked((int)0x80010001))] [TestCase(unchecked((int)0x80010108))] [TestCase(unchecked((int)0x8002000c))]
        public void MetadataFailuresRemainOriginalExceptionsAndNeverBecomeCachedAbsence(int hresult)
        {
            using var wrapper=new OpenStaadWrapper(new NativeRoot());var inspector=new RecordingInspector();
            ((OSRootAdapter)wrapper.Root).CapabilityInspector=inspector;var gateway=wrapper.Root.Capabilities;
            var failure=new COMException("inspection failure",hresult);inspector.Failure=failure;
            Assert.That(Assert.Throws<COMException>(()=>gateway.GetAdinaExport()),Is.SameAs(failure));
            inspector.Failure=null;Assert.That(gateway.GetAdinaExport().IsAvailable,Is.True);
            Assert.That(inspector.Calls,Has.Count.EqualTo(2));
        }
        [Test]
        public void AcquisitionRunsUnderTheSameNonreentrantRootLeaseAndReturnedVersionIsDefensive()
        {
            using var wrapper=new OpenStaadWrapper(new NativeRoot());var inspector=new RecordingInspector();
            ((OSRootAdapter)wrapper.Root).CapabilityInspector=inspector;
            var gateway=wrapper.Root.Capabilities;
            inspector.OnInspect=()=>
            {
                Assert.Throws<InvalidOperationException>(()=>gateway.GetAdinaExport());
                Assert.Throws<InvalidOperationException>(()=>wrapper.RefreshUnits());
                Assert.Throws<InvalidOperationException>(()=>wrapper.Dispose());
            };
            var grant=gateway.GetAdinaExport();grant.NativeVersion.Build=999;wrapper.StaadVersion.Build=123;
            Assert.That(grant.NativeVersion.Build,Is.EqualTo(340));Assert.That(grant.Reason,Is.Not.Empty);
            Assert.That(grant.IsAvailable,Is.True);
        }
        [TestCaseSource(nameof(GuardOperations))]
        public void CapturedGatewaysAndGrantsRejectStaleDisposedAndOtherThreadCalls(string operation)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);var action=Capture(wrapper,operation);
            Exception failure=null;var worker=new Thread(()=>{try{action();}catch(Exception e){failure=e;}});
            worker.Start();Assert.That(worker.Join(5000),Is.True);Assert.That(failure,Is.TypeOf<InvalidOperationException>());
            wrapper.RefreshComObjects();Assert.Throws<InvalidOperationException>(()=>action());
            action=Capture(wrapper,operation);wrapper.Dispose();Assert.Throws<ObjectDisposedException>(()=>action());
            Assert.That(native.Calls,Is.Empty);
        }
        [TestCase("remove")] [TestCase("modify")] [TestCase("export")] [TestCase("errors")] [TestCase("warnings")]
        public void OperationsRetainOneLeaseAndOriginalNativeFailuresWithoutFallback(string operation)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);var action=Capture(wrapper,operation);
            Action[] nested=GuardOperations.Select(s=>Capture(wrapper,s)).ToArray();
            native.OnCall=()=>
            {
                foreach(var request in nested)Assert.Throws<InvalidOperationException>(()=>request());
                Assert.Throws<InvalidOperationException>(()=>wrapper.RefreshUnits());
                Assert.Throws<InvalidOperationException>(()=>wrapper.RefreshComObjects());
                Assert.Throws<InvalidOperationException>(()=>wrapper.Dispose());
            };
            action();native.OnCall=null;native.Calls.Clear();
            var failure=new COMException("native error",unchecked((int)0x80004005));native.Failure=failure;
            Assert.That(Assert.Throws<COMException>(()=>action()),Is.SameAs(failure));Assert.That(native.Calls,Has.Count.EqualTo(1));
            native.Failure=null;Assert.DoesNotThrow(()=>wrapper.Root.GetBaseUnit());
        }
        [Test]
        public void CommonAdapterCannotBeCastToOptionalInterfacesAndModelsAreImmutable()
        {
            foreach(Type type in new[]{typeof(IOSRootAdinaExport),typeof(IOSRootAnalysisErrorMessages),typeof(IOSRootAnalysisWarningMessages)})
            {
                Assert.That(type.IsAssignableFrom(typeof(OSRootAdapter)),Is.False);
                var implementations=typeof(IOSRoot).Assembly.GetTypes().Where(t=>t.IsClass&&type.IsAssignableFrom(t)).ToArray();
                Assert.That(implementations,Has.Length.EqualTo(1));Assert.That(implementations[0].IsVisible,Is.False);
            }
            foreach(Type type in new[]{typeof(AdinaExportResult),typeof(AnalysisMessagesResult),typeof(RootCapabilityResult<IOSRootAdinaExport>)})
            {Assert.That(type.IsSealed,Is.True);Assert.That(type.GetConstructors(),Is.Empty);Assert.That(type.GetProperties().Any(p=>p.GetSetMethod(true)!=null),Is.False);}
        }
        [Test]
        public void CommonAndOptionalInterfaceDocumentationMatchesConcreteMembersAndContainsFullContracts()
        {
            var assembly=typeof(IOSRoot).Assembly;var docs=XDocument.Load(Path.ChangeExtension(assembly.Location,".xml"));
            var pairs=new[]{(typeof(IOSRootCapabilities),"OSRootCapabilitiesAdapter"),(typeof(IOSRootAdinaExport),"OSRootAdinaExportAdapter"),(typeof(IOSRootAnalysisErrorMessages),"OSRootAnalysisErrorMessagesAdapter"),(typeof(IOSRootAnalysisWarningMessages),"OSRootAnalysisWarningMessagesAdapter")};
            foreach(var pair in pairs)
            {
                Type concrete=assembly.GetType("StaadPro.Interop.Core.Adapters.Models.Capabilities."+pair.Item2);
                foreach(var method in pair.Item1.GetMethods())
                {
                    string suffix=method.Name+(method.GetParameters().Length==0?"":"("+string.Join(",",method.GetParameters().Select(p=>p.ParameterType.FullName))+")");
                    var a=docs.Descendants("member").Single(e=>(string)e.Attribute("name")=="M:"+pair.Item1.FullName+"."+suffix);
                    var b=docs.Descendants("member").Single(e=>(string)e.Attribute("name")=="M:"+concrete.FullName+"."+suffix);
                    Assert.That(a.Elements().Select(e=>e.ToString()),Is.EqualTo(b.Elements().Select(e=>e.ToString())),suffix);
                    foreach(string tag in new[]{"summary","returns","remarks","exception","example"})Assert.That(a.Element(tag)?.Value,Is.Not.Null.And.Not.Empty);
                    Assert.That(a.Elements("param").Select(e=>(string)e.Attribute("name")),Is.EqualTo(method.GetParameters().Select(p=>p.Name)));
                    Assert.That(method.GetParameters().Any(p=>p.IsOptional||p.IsOut||p.ParameterType.IsByRef),Is.False);
                }
            }
        }
    }
}
