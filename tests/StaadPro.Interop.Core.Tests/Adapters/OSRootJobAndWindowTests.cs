using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Common;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Root.Common;
using MetadataRoot = StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
    public sealed class OSRootJobAndWindowTests
    {
        private static readonly string[] Names = ["GetShortJobInfo", "GetFullJobInfo", "MinimizeStaadMainWindow", "MaximizeStaadMainWindow", "RestoreStaadMainWindow"];
        private static readonly string[] WindowNames = Names.Skip(2).ToArray();
        private static readonly object[] InvalidFields = [null, DBNull.Value, true, 1, 1.5, DateTime.MinValue];

        public sealed class NativeRoot : MetadataRoot
        {
            public object[] Fields = [" Name 雪 ", "Client", "Engineer", "2026-11-08", "N-17", "R2", "Part", "Ref", "Checker", "2026-11-09", "Approver", "2026-11-10", "first\r\nsecond"];
            public int Unwritten = -1;
            public object LiveHandle = new IntPtr(17), Pid = 1234u;
            public readonly List<string> Calls = [];
            public Exception Failure;
            public Action OnCall;
            private void Record(string name)
            {
                Calls.Add(name); OnCall?.Invoke();
                if (Failure != null) throw Failure;
            }
            public new object GetMainWindowHandle() { Record("GetMainWindowHandle"); return LiveHandle; }
            public object GetProcessId() { Record("GetProcessId"); return Pid; }
            private void Write(ref object target, int slot)
            {
                Assert.That(target, Is.TypeOf<string>(), "Native job outputs must start as BSTR-shaped variants.");
                if (slot != Unwritten) target = Fields[slot];
            }
            public void GetShortJobInfo(ref object name, ref object client, ref object originator)
            {
                Record("GetShortJobInfo"); Write(ref name,0); Write(ref client,1); Write(ref originator,2);
            }
            public void GetFullJobInfo(ref object name, ref object client, ref object originator, ref object originated,
                ref object number, ref object revision, ref object part, ref object reference,
                ref object checker, ref object dateChecked, ref object approver, ref object approved, ref object comments)
            {
                Record("GetFullJobInfo");
                Write(ref name,0); Write(ref client,1); Write(ref originator,2); Write(ref originated,3);
                Write(ref number,4); Write(ref revision,5); Write(ref part,6); Write(ref reference,7);
                Write(ref checker,8); Write(ref dateChecked,9); Write(ref approver,10); Write(ref approved,11); Write(ref comments,12);
            }
        }

        private sealed class Windows : IRootWindowApi
        {
            internal readonly List<string> Calls = [];
            internal readonly List<IntPtr> Handles = [];
            internal readonly List<int> Commands = [];
            internal bool Exists = true, Accepted = true;
            internal uint Owner = 1234u;
            internal Action OnPost;
            internal Exception Failure;
            public bool IsWindow(IntPtr window) { Calls.Add("IsWindow"); Handles.Add(window); return Exists; }
            public uint GetProcessId(IntPtr window) { Calls.Add("GetProcessId"); Handles.Add(window); return Owner; }
            public bool PostShowState(IntPtr window, int command)
            {
                Calls.Add("PostShowState"); Handles.Add(window); Commands.Add(command); OnPost?.Invoke();
                if (Failure != null) throw Failure;
                return Accepted;
            }
        }

        private static Windows Inject(OpenStaadWrapper wrapper)
        { var windows = new Windows(); ((OSRootAdapter)wrapper.Root).WindowApi = windows; return windows; }

        private static object Request(IOSRoot root, string name)
        {
            switch (name)
            {
                case "GetShortJobInfo": return root.GetShortJobInfo();
                case "GetFullJobInfo": return root.GetFullJobInfo();
                case "MinimizeStaadMainWindow": root.MinimizeStaadMainWindow(); break;
                case "MaximizeStaadMainWindow": root.MaximizeStaadMainWindow(); break;
                case "RestoreStaadMainWindow": root.RestoreStaadMainWindow(); break;
                default: throw new ArgumentOutOfRangeException(nameof(name));
            }
            return null;
        }

        [Test]
        public void ShortAndFullFieldsKeepExactNativeOrderAndProduceIndependentEditableSnapshots()
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.Calls.Clear(); native.Queries.Clear();
            var shortInfo = wrapper.Root.GetShortJobInfo(); var full = wrapper.Root.GetFullJobInfo();
            Assert.That(new[] { shortInfo.Name, shortInfo.Client, shortInfo.Originator }, Is.EqualTo(native.Fields.Take(3)));
            Assert.That(new[] { full.Name, full.Client, full.Originator, full.Number, full.Revision, full.Part, full.Reference, full.Checker, full.Approver, full.Comments },
                Is.EqualTo(new[] { native.Fields[0], native.Fields[1], native.Fields[2], native.Fields[4], native.Fields[5], native.Fields[6], native.Fields[7], native.Fields[8], native.Fields[10], native.Fields[12] }));
            Assert.That(full.Originated, Is.EqualTo(new DateTime(2026,11,8)));
            Assert.That(full.Checked, Is.EqualTo(new DateTime(2026,11,9)));
            Assert.That(full.Approved, Is.EqualTo(new DateTime(2026,11,10)));
            full.Name = "Edited"; full.Comments = "Changed";
            Assert.That(native.Fields[0], Is.EqualTo(" Name 雪 "));
            Assert.That(wrapper.Root.GetFullJobInfo().Name, Is.EqualTo(" Name 雪 "));
            Assert.That(shortInfo.Name, Is.EqualTo(" Name 雪 "));
            Assert.That(native.Calls, Is.EqualTo(new[] { "GetShortJobInfo", "GetFullJobInfo", "GetFullJobInfo" }));
            Assert.That(native.Queries, Is.Empty);
        }

        private static IEnumerable<object[]> MalformedJobFields
        {
            get
            {
                foreach (string operation in Names.Take(2))
                    for (int slot=0;slot<(operation=="GetFullJobInfo"?13:3);slot++)
                        foreach (object invalid in InvalidFields) yield return [operation,slot,invalid];
            }
        }
        [TestCaseSource(nameof(MalformedJobFields))]
        public void EveryJobOutputRejectsNonStringShapesWithoutFallback(string operation, int slot, object invalid)
        {
            var native = new NativeRoot(); using var wrapper = new OpenStaadWrapper(native);
            native.Calls.Clear(); native.Fields[slot] = invalid;
            Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root,operation));
            Assert.That(native.Calls, Is.EqualTo(new[] { operation }));
            native.Fields[slot] = "";
            Assert.DoesNotThrow(() => Request(wrapper.Root,operation));
        }

        private static IEnumerable<object[]> UnwrittenJobFields
        {
            get { foreach (string operation in Names.Take(2)) for (int i=0;i<(operation=="GetFullJobInfo"?13:3);i++) yield return [operation,i]; }
        }
        [TestCaseSource(nameof(UnwrittenJobFields))]
        public void EveryUnwrittenFieldIsDistinguishedFromAnExplicitEmptyString(string operation,int slot)
        {
            var native = new NativeRoot { Unwritten=slot }; using var wrapper = new OpenStaadWrapper(native);
            Assert.Throws<InvalidOperationException>(() => Request(wrapper.Root,operation));
            native.Unwritten=-1; native.Fields=Enumerable.Repeat<object>("",13).ToArray();
            var result=(ShortJobInfo)Request(wrapper.Root,operation); Assert.That(result.Name, Is.EqualTo(""));
            if (result is FullJobInfo full) Assert.That(new[] { full.Originated,full.Checked,full.Approved },Is.All.EqualTo(DateTime.MinValue));
        }

        [TestCase("en-US",5,6)]
        [TestCase("en-GB",6,5)]
        public void AmbiguousDatesUseOnlyTheCurrentCulture(string culture,int month,int day)
        {
            var saved=CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
                var native=new NativeRoot(); native.Fields[3]=native.Fields[9]=native.Fields[11]="05/06/2026";
                using var wrapper=new OpenStaadWrapper(native); var full=wrapper.Root.GetFullJobInfo();
                Assert.That(new[] { full.Originated,full.Checked,full.Approved },Is.All.EqualTo(new DateTime(2026,month,day)));
            } finally { CultureInfo.CurrentCulture=saved; }
        }

        [TestCase(3)] [TestCase(9)] [TestCase(11)]
        public void MalformedDatesThrowWhileBlankDatesRemainUnspecified(int slot)
        {
            var native=new NativeRoot(); using var wrapper=new OpenStaadWrapper(native);
            native.Fields[slot]="not a date"; Assert.Throws<FormatException>(()=>wrapper.Root.GetFullJobInfo());
            native.Fields[slot]=" \t "; var full=wrapper.Root.GetFullJobInfo();
            Assert.That(slot==3?full.Originated:slot==9?full.Checked:full.Approved,Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void AllTextShapesAreValidatedBeforeAnyDatesAreParsed()
        {
            var native=new NativeRoot(); using var wrapper=new OpenStaadWrapper(native);
            native.Fields[3]="invalid date";native.Fields[12]=false;
            Assert.Throws<InvalidOperationException>(()=>wrapper.Root.GetFullJobInfo());
        }

        [TestCaseSource(nameof(WindowNames))]
        public void WindowRequestsReadLiveHandleAndOwnerThenPostExactlyOneCorrectState(string name)
        {
            var native=new NativeRoot(); using var wrapper=new OpenStaadWrapper(native); var windows=Inject(wrapper);
            IntPtr cached=wrapper.StaadApplicationWindowHandle;native.LiveHandle=new IntPtr(99);native.Calls.Clear();native.Queries.Clear();
            Assert.That(Request(wrapper.Root,name),Is.Null);
            Assert.That(native.Calls,Is.EqualTo(new[] { "GetMainWindowHandle","GetProcessId" }));
            Assert.That(windows.Calls,Is.EqualTo(new[] { "IsWindow","GetProcessId","PostShowState" }));
            Assert.That(windows.Handles,Is.All.EqualTo(new IntPtr(99)));
            Assert.That(windows.Commands,Is.EqualTo(new[] { name.StartsWith("Min")?6:name.StartsWith("Max")?3:9 }));
            Assert.That(wrapper.StaadApplicationWindowHandle,Is.EqualTo(cached));Assert.That(native.Queries,Is.Empty);
        }

        private static IEnumerable<object> BadHandles => new object[] { null,DBNull.Value,true,"17",17.0,17m,0,-1,new IntPtr(-1),ulong.MaxValue };
        [TestCaseSource(nameof(BadHandles))]
        public void InvalidHandleShapesFailBeforePidOrWindowsQueries(object handle)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);var windows=Inject(wrapper);
            native.LiveHandle=handle;native.Calls.Clear();
            Assert.Throws<InvalidOperationException>(()=>wrapper.Root.MinimizeStaadMainWindow());
            Assert.That(native.Calls,Is.EqualTo(new[] { "GetMainWindowHandle" }));Assert.That(windows.Calls,Is.Empty);
        }

        private static IEnumerable<object> GoodHandles
        {
            get { yield return (byte)17;yield return (sbyte)17;yield return (short)17;yield return (ushort)17;yield return 17;yield return 17u;yield return 17L;yield return 17UL;yield return new IntPtr(17);if(IntPtr.Size==8) {yield return 0x100000011L;yield return new IntPtr(0x100000011L);} }
        }
        [TestCaseSource(nameof(GoodHandles))]
        public void ValidIntegralAndPointerHandlesKeepTheirCompleteBits(object handle)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);var windows=Inject(wrapper);native.LiveHandle=handle;
            wrapper.Root.RestoreStaadMainWindow();
            long expected=handle is IntPtr pointer?pointer.ToInt64():Convert.ToInt64(handle,CultureInfo.InvariantCulture);
            Assert.That(windows.Handles,Is.All.EqualTo(new IntPtr(expected)));
        }

        private static IEnumerable<object> BadPids => new object[] { null,DBNull.Value,true,"1234",1234.0,0,-1,0x100000000L,ulong.MaxValue };
        [TestCaseSource(nameof(BadPids))]
        public void InvalidProcessIdentifiersPreventEveryWindowsCall(object pid)
        {
            var native=new NativeRoot { Pid=pid };using var wrapper=new OpenStaadWrapper(native);var windows=Inject(wrapper);
            Assert.Throws<InvalidOperationException>(()=>wrapper.Root.MaximizeStaadMainWindow());Assert.That(windows.Calls,Is.Empty);
        }

        [TestCaseSource(nameof(WindowNames))]
        public void MissingOrForeignWindowsAreNeverPostedAndRejectedPostsAreNotRetried(string name)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);var windows=Inject(wrapper);
            windows.Exists=false;Assert.Throws<InvalidOperationException>(()=>Request(wrapper.Root,name));Assert.That(windows.Calls,Is.EqualTo(new[] { "IsWindow" }));
            windows.Calls.Clear();windows.Exists=true;windows.Owner=999;
            Assert.Throws<InvalidOperationException>(()=>Request(wrapper.Root,name));Assert.That(windows.Calls,Is.EqualTo(new[] { "IsWindow","GetProcessId" }));
            windows.Calls.Clear();windows.Owner=1234;windows.Accepted=false;
            Assert.Throws<InvalidOperationException>(()=>Request(wrapper.Root,name));Assert.That(windows.Commands.Count,Is.EqualTo(1));
            windows.Accepted=true;Assert.DoesNotThrow(()=>Request(wrapper.Root,name));Assert.That(windows.Commands.Count,Is.EqualTo(2));
        }

        [TestCaseSource(nameof(Names))]
        public void NativeExceptionsPropagateByIdentityWithoutRetryAndReleaseTheLease(string name)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);Inject(wrapper);native.Calls.Clear();
            var failure=new COMException("native failure",unchecked((int)0x80004005));native.Failure=failure;
            Assert.That(Assert.Throws<COMException>(()=>Request(wrapper.Root,name)),Is.SameAs(failure));Assert.That(native.Calls.Count,Is.EqualTo(1));
            native.Failure=null;Assert.DoesNotThrow(()=>Request(wrapper.Root,name));
        }

        [TestCaseSource(nameof(Names))]
        public void MissingNativeMembersAreNotSubstitutedFromAnotherSession(string name)
        {
            using var wrapper=new OpenStaadWrapper(new MetadataRoot());Inject(wrapper);
            Assert.Throws<RuntimeBinderException>(()=>Request(wrapper.Root,name));
        }

        [TestCaseSource(nameof(Names))]
        public void ReplacedDisposedWrongThreadAndNestedOperationsRejectBeforeDispatch(string name)
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);var windows=Inject(wrapper);var old=wrapper.Root;
            wrapper.RefreshComObjects();native.Calls.Clear();
            Assert.Throws<InvalidOperationException>(()=>Request(old,name));Assert.That(native.Calls,Is.Empty);Assert.That(windows.Calls,Is.Empty);
            windows=Inject(wrapper);Exception wrongThread=null;
            var worker=new Thread(()=>{try{Request(wrapper.Root,name);}catch(Exception e){wrongThread=e;}});worker.Start();Assert.That(worker.Join(5000),Is.True);
            Assert.That(wrongThread,Is.TypeOf<InvalidOperationException>());Assert.That(native.Calls,Is.Empty);
            native.OnCall=()=> { Assert.Throws<InvalidOperationException>(()=>wrapper.Root.GetShortJobInfo());Assert.Throws<InvalidOperationException>(()=>wrapper.Root.RestoreStaadMainWindow());Assert.Throws<InvalidOperationException>(wrapper.RefreshComObjects);Assert.Throws<InvalidOperationException>(wrapper.Dispose); };
            Request(wrapper.Root,name);native.OnCall=null;var current=wrapper.Root;native.Calls.Clear();windows.Calls.Clear();wrapper.Dispose();
            Assert.Throws<ObjectDisposedException>(()=>Request(current,name));Assert.That(native.Calls,Is.Empty);Assert.That(windows.Calls,Is.Empty);
        }

        [Test]
        public void AcceptedWindowRequestDoesNotWaitOrReadBackAndTransportExceptionsRetainIdentity()
        {
            var native=new NativeRoot();using var wrapper=new OpenStaadWrapper(native);var windows=Inject(wrapper);
            windows.OnPost=()=>{Assert.Throws<InvalidOperationException>(()=>wrapper.Root.GetBaseUnit());Assert.Throws<InvalidOperationException>(wrapper.RefreshUnits);};
            var failure=new IOException("transport failure");windows.Failure=failure;
            Assert.That(Assert.Throws<IOException>(wrapper.Root.RestoreStaadMainWindow),Is.SameAs(failure));Assert.That(windows.Commands.Count,Is.EqualTo(1));
        }

        [TestCaseSource(nameof(Names))]
        public void SignaturesAndInterfaceAdapterXmlAreCompleteAndMatching(string name)
        {
            var method=typeof(IOSRoot).GetMethod(name);Assert.That(method.GetParameters(),Is.Empty);
            Assert.That(method.ReturnType,Is.EqualTo(name=="GetShortJobInfo"?typeof(ShortJobInfo):name=="GetFullJobInfo"?typeof(FullJobInfo):typeof(void)));
            var docs=XDocument.Load(Path.ChangeExtension(typeof(IOSRoot).Assembly.Location,".xml"));
            var contract=docs.Descendants("member").Single(m=>(string)m.Attribute("name")=="M:"+typeof(IOSRoot).FullName+"."+name);
            var adapter=docs.Descendants("member").Single(m=>(string)m.Attribute("name")=="M:"+typeof(OSRootAdapter).FullName+"."+name);
            Assert.That(contract.Elements().Select(e=>e.ToString()),Is.EqualTo(adapter.Elements().Select(e=>e.ToString())));
            foreach(string tag in new[]{"summary","remarks","example","exception"})Assert.That(contract.Element(tag)?.Value,Is.Not.Null.And.Not.Empty);
            Assert.That(contract.Element("returns")!=null,Is.EqualTo(method.ReturnType!=typeof(void)));
        }

        private sealed class StoreProbe : ValidatablePropertyStore
        {
            internal T Read<T>(string name)=>Get<T>(name);
            internal bool Write<T>(T value,string name)=>Set(value,name);
        }
        [Test]
        public void ManagedModelsKeepSourceFactoriesNotificationsDefaultsAndIndependentStorage()
        {
            var shortInfo=new ShortJobInfo();Assert.That(shortInfo.Name,Is.Null);
            var events=new List<string>();shortInfo.PropertyChanged+=(sender,args)=>events.Add(args.PropertyName);
            shortInfo.Name=null;shortInfo.Name=null;shortInfo.Name="A";shortInfo.Name="A";shortInfo.Client="C";
            Assert.That(events,Is.EqualTo(new[]{"Name","Name","Client"}));
            var other=ShortJobInfo.From(null,"client","originator");Assert.That(other.Name,Is.Null);other.Name="B";Assert.That(shortInfo.Name,Is.EqualTo("A"));
            var full=FullJobInfo.From("n","c","o",null,"checker"," ","approver","","1","2","3","4","line\nline");
            Assert.That(new[]{full.Originated,full.Checked,full.Approved},Is.All.EqualTo(DateTime.MinValue));
            var store=new StoreProbe();Assert.That(store.Read<int>("absent"),Is.Zero);
            Assert.Throws<ArgumentNullException>(()=>store.Read<int>(null));Assert.Throws<ArgumentNullException>(()=>store.Write(1,""));
            store.Write("text","key");Assert.Throws<InvalidCastException>(()=>store.Read<int>("key"));
            var error=new InvalidOperationException("subscriber");shortInfo.PropertyChanged+=(s,e)=>throw error;
            Assert.That(Assert.Throws<InvalidOperationException>(()=>shortInfo.Name="stored"),Is.SameAs(error));Assert.That(shortInfo.Name,Is.EqualTo("stored"));
        }
    }
}
