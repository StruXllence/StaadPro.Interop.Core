using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Adapters.Models;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public sealed class GeometryInventoryTests
    {
        [TestCase("GetMemberCount", "GetBeamList")]
        [TestCase("GetNodeCount", "GetNodeList")]
        [TestCase("GetPlateCount", "GetPlateList")]
        [TestCase("GetSolidCount", "GetSolidList")]
        public void InventoryUsesItsOwnCountAndListAndReturnsAnEagerOwnedSequence(string countCall, string listCall)
        {
            var native = new GeometryInventoryNative();
            native.Counts.Enqueue(3L);
            native.Counts.Enqueue((ushort)3);
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var read = Inventory(wrapper.Geometry, listCall);

            var ids = read();
            Assert.That(native.Calls, Is.EqualTo(new[] { countCall, listCall, countCall }));
            ((int[])native.Ids)[0] = 99;
            Assert.That(ids.ToArray(), Is.EqualTo(new[] { 29, 3, 17 }));
            Assert.That(native.Calls, Has.Count.EqualTo(3));

            native.Ids = Array.Empty<int>();
            Assert.That(read(), Is.Empty);
            Assert.That(native.Calls, Is.EqualTo(new[] { countCall, listCall, countCall, countCall }));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
        }

        [Test]
        public void ReplacementSafeArrayWithNonzeroLowerBoundIsCopied()
        {
            var returned = Array.CreateInstance(typeof(int), [2], [-2]);
            returned.SetValue(45, -2);
            returned.SetValue(10, -1);
            var native = new GeometryInventoryNative { Ids = returned, ReplaceBuffer = true };
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var ids = wrapper.Geometry.GetAllNodesList();

            returned.SetValue(20, -2);
            Assert.That(ids, Is.EqualTo(new[] { 45, 10 }));
        }

        [TestCase(false)]
        [TestCase(-1)]
        [TestCase(1_000_001)]
        [TestCase(long.MaxValue)]
        [TestCase(3.0)]
        public void MalformedOrOverLimitCountFailsBeforeListDispatch(object count)
        {
            var native = new GeometryInventoryNative();
            native.Counts.Enqueue(count);
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));

            Assert.Throws<InvalidOperationException>(() => wrapper.Geometry.GetAllNodesList());
            Assert.That(native.Calls, Is.EqualTo(new[] { "GetNodeCount" }));
        }

        private static readonly object[] BadLists = [
            new object[] { null }, new object[] { new int[1, 2] }, new object[] { new long[] { 4, 5 } },
            new object[] { new[] { 4 } }, new object[] { new[] { 0, 5 } }, new object[] { new[] { -4, 5 } },
            new object[] { new[] { 4, 4 } }];

        [TestCaseSource(nameof(BadLists))]
        public void WrongShapeUnwrittenOrDuplicateIdsFailWithoutPublishingPartialData(object ids)
        {
            var native = new GeometryInventoryNative { Ids = ids, ReplaceBuffer = true };
            native.Counts.Enqueue(2);
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));

            Assert.Throws<InvalidOperationException>(() => wrapper.Geometry.GetAllSolidsList());
        }

        [Test]
        public void InventoryCountChangeAndTransportFailureAreVisible()
        {
            var native = new GeometryInventoryNative();
            native.Counts.Enqueue(3);
            native.Counts.Enqueue(4);
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            Assert.Throws<InvalidOperationException>(() => wrapper.Geometry.GetAllBeamsList());

            var failure = new COMException("Native plate query failed", unchecked((int)0x800706BA));
            native.OnCall = name => { if (name == "GetPlateList") throw failure; };
            Assert.That(Assert.Throws<COMException>(() => wrapper.Geometry.GetAllPlatesList()), Is.SameAs(failure));
            Assert.That(native.Calls.Last(), Is.EqualTo("GetPlateList"));
        }

        [Test]
        public void NodeSentinelAndInputValidationPrecedeDispatchAndCoordinatesAreDetached()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var geometry = wrapper.Geometry;
            Assert.That(geometry.GetNode(0), Is.Null);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => geometry.GetNode(-1)).ParamName, Is.EqualTo("nId"));
            Assert.That(native.Calls, Is.Empty);

            var node = geometry.GetNode(12);
            Assert.That(node.Id, Is.EqualTo(12));
            Assert.That((node.X, node.Y, node.Z), Is.EqualTo((0d, -2.25d, 7d)));
            native.Coordinates[1] = 55d;
            Assert.That(node.Y, Is.EqualTo(-2.25d));
            Assert.That(native.Calls, Is.EqualTo(new[] { "GetNodeCoordinates:12" }));
        }

        private static readonly object[] BadCoordinates = [
            new object[] { new object[] { 1d, 2d } },
            new object[] { new object[] { 1d, 2d, double.PositiveInfinity } },
            new object[] { new object[] { 1d, "2", 3d } },
            new object[] { new object[] { 1d, 2, 3d } },
            new object[] { new object[] { 1d, 2d, double.NaN } }];

        [TestCaseSource(nameof(BadCoordinates))]
        public void NodeRequiresEveryOutputToBeAFiniteDouble(object[] coordinates)
        {
            var native = new GeometryInventoryNative { Coordinates = coordinates };
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));

            Assert.Throws<InvalidOperationException>(() => wrapper.Geometry.GetNode(12));
            Assert.That(native.Calls, Is.EqualTo(new[] { "GetNodeCoordinates:12" }));
        }

        [Test]
        public void GeometryUsesTheExistingWrapperThreadAndBindingGuards()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var old = wrapper.Geometry;
            var offThreadError = Task.Run(() => Capture(() => old.GetAllNodesList())).GetAwaiter().GetResult();
            Assert.That(offThreadError, Is.TypeOf<InvalidOperationException>());
            Assert.Throws<InvalidOperationException>(() => new OSGeometryAdapter(wrapper).GetAllBeamsList());
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => old.GetNode(0));
            Assert.That(native.Calls, Is.Empty);

            var current = wrapper.Geometry;
            native.OnCall = _ =>
            {
                Assert.Throws<InvalidOperationException>(() => current.GetNode(1));
                Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
            };
            Assert.That(current.GetAllPlatesList(), Is.EqualTo(new[] { 29, 3, 17 }));
            Assert.That(native.Calls, Is.EqualTo(new[] { "GetPlateCount", "GetPlateList", "GetPlateCount" }));
            wrapper.Dispose();
            Assert.Throws<ObjectDisposedException>(() => current.GetNode(0));
        }

        private static Exception Capture(Action action)
        {
            try { action(); return null; }
            catch (Exception error) { return error; }
        }

        private static Func<IEnumerable<int>> Inventory(IOSGeometry geometry, string listCall) => listCall switch
        {
            "GetBeamList" => geometry.GetAllBeamsList,
            "GetNodeList" => geometry.GetAllNodesList,
            "GetPlateList" => geometry.GetAllPlatesList,
            "GetSolidList" => geometry.GetAllSolidsList,
            _ => throw new ArgumentOutOfRangeException(nameof(listCall))
        };
    }
}
