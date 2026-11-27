using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public sealed class GeometryNodeSelectionTests
    {
        private static readonly string[] SelectionCalls = ["GetNoOfSelectedNodes", "GetSelectedNodes", "GetNoOfSelectedNodes"];

        [TestCase(3)]
        [TestCase(0)]
        [TestCase(-1)]
        public void SuppliedNodeLookupReturnsFirstReferenceWithoutDispatch(int id)
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var first = new Node(id, 9, -1, 0);
            IEnumerable<Node> Once()
            {
                yield return new Node(id + 1, 3, 4, 5);
                yield return first;
                throw new AssertionException("Lookup must stop at the first match.");
            }

            Assert.That(wrapper.Geometry.GetNode(id, Once()), Is.SameAs(first));
            Assert.That(wrapper.Geometry.GetNode(id, Array.Empty<Node>()), Is.Null);
            Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void SelectionArrayPreservesNativeOrderAndOwnsStorage()
        {
            var ids = Array.CreateInstance(typeof(int), [3], [-2]);
            ids.SetValue(29, -2);
            ids.SetValue(3, -1);
            ids.SetValue(17, 0);
            var native = new GeometryInventoryNative { Ids = ids, ReplaceBuffer = true };
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));

            var task = wrapper.Geometry.GetSelectedNodesListAsync();
            Assert.That(task.Status, Is.EqualTo(TaskStatus.RanToCompletion));
            Assert.That(native.Calls, Is.EqualTo(SelectionCalls));
            var set = wrapper.Geometry.GetSelectedNodesIds();
            ids.SetValue(11, -2);
            Assert.That(task.Result, Is.EqualTo(new[] { 29, 3, 17 }));
            Assert.That(set, Is.EquivalentTo(new[] { 29, 3, 17 }));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
        }

        [Test]
        public void SelectedCoordinatesAreReadOncePerIdAndReturnedInDetachedNodes()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var nodes = wrapper.Geometry.GetSelectedNodesExt();

            Assert.That(nodes.Select(node => node.Id), Is.EquivalentTo(new[] { 29, 3, 17 }));
            Assert.That(nodes.All(node => node.X == 0 && node.Y == -2.25 && node.Z == 7), Is.True);
            Assert.That(native.Calls, Is.EqualTo(SelectionCalls.Concat(new[]
                { "GetNodeCoordinates:29", "GetNodeCoordinates:3", "GetNodeCoordinates:17" })));
            native.Coordinates[1] = 25d;
            Assert.That(nodes.Select(node => node.Y), Is.All.EqualTo(-2.25d));
        }

        [Test]
        public void SelectedProjectionUsesFirstSuppliedMatchAndOneMissingMarker()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var match = new Node(3, 9, -1, 0);
            int enumerations = 0;
            IEnumerable<Node> Supplied()
            {
                Assert.That(++enumerations, Is.EqualTo(1));
                yield return new Node(1, 0, 1, 0);
                yield return match;
                yield return new Node(3, 3, 4, 5);
            }

            var result = wrapper.Geometry.GetSelectedNodesExt(Supplied());
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Single(node => node != null), Is.SameAs(match));
            Assert.That(result.Contains(null), Is.True);
            Assert.That(native.Calls, Is.EqualTo(SelectionCalls));
        }

        [Test]
        public void SelectedProjectionStopsAfterAllIdsMatch()
        {
            var native = new GeometryInventoryNative { Ids = new[] { 17, 3 } };
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var a = new Node(3, 1, 2, 3);
            var b = new Node(17, 2, 3, 4);
            IEnumerable<Node> Supplied()
            {
                yield return a;
                yield return b;
                throw new AssertionException("Projection must stop when the selection is satisfied.");
            }

            var result = wrapper.Geometry.GetSelectedNodesExt(Supplied());
            Assert.That(result.SetEquals(new[] { a, b }), Is.True);
            Assert.That(native.Calls, Is.EqualTo(SelectionCalls));
        }

        [Test]
        public void EmptyNativeSelectionSkipsListCoordinateAndInputEnumeration()
        {
            var native = new GeometryInventoryNative { Ids = Array.Empty<int>() };
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var geometry = wrapper.Geometry;
            IEnumerable<Node> Unused()
            {
                foreach (int _ in Enumerable.Range(0, 1))
                    throw new AssertionException("An empty selection must not enumerate supplied nodes.");
                yield break;
            }

            Assert.That(geometry.GetSelectedNodesIds(), Is.Empty);
            Assert.That(geometry.GetSelectedNodesExt(), Is.Empty);
            Assert.That(geometry.GetSelectedNodesListAsync().Result, Is.Empty);
            Assert.That(geometry.GetSelectedNodesExt(Unused()), Is.Empty);
            Assert.That(native.Calls, Is.EqualTo(Enumerable.Repeat("GetNoOfSelectedNodes", 4)));
        }

        [Test]
        public void NullSequencesFailBeforeNativeReadsAndNullElementsFailDuringLookup()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var geometry = wrapper.Geometry;

            Assert.That(Assert.Throws<ArgumentNullException>(() => geometry.GetNode(3, null)).ParamName, Is.EqualTo("fromModelNodes"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => geometry.GetSelectedNodesExt(null)).ParamName, Is.EqualTo("fromModelNodes"));
            Assert.That(Assert.Throws<ArgumentException>(() => geometry.GetNode(3, new Node[] { null })).ParamName, Is.EqualTo("fromModelNodes"));
            Assert.That(native.Calls, Is.Empty);
            Assert.Throws<ArgumentException>(() => geometry.GetSelectedNodesExt(new Node[] { null }));
            Assert.That(native.Calls, Is.EqualTo(SelectionCalls));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InvalidArrayOrChangedCountFaultsTaskBeforeReturningData(bool changedCount)
        {
            var native = new GeometryInventoryNative { Ids = changedCount ? new[] { 3, 5 } : new[] { 3, 3 } };
            if (changedCount)
            {
                native.Counts.Enqueue(2);
                native.Counts.Enqueue(3);
            }
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var task = wrapper.Geometry.GetSelectedNodesListAsync();

            Assert.That(task.IsFaulted, Is.True);
            Assert.That(Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().GetResult()).Message,
                Does.Contain(changedCount ? "changed during" : "duplicated"));
        }

        [Test]
        public void CoordinateFailureAbortsSelectionWithOriginalNativeError()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var failure = new COMException("Coordinate read failed.", unchecked((int)0x800706BA));
            native.OnCall = call => { if (call == "GetNodeCoordinates:3") throw failure; };

            Assert.That(Assert.Throws<COMException>(() => wrapper.Geometry.GetSelectedNodesExt()), Is.SameAs(failure));
            Assert.That(native.Calls.Last(), Is.EqualTo("GetNodeCoordinates:3"));
            Assert.That(native.Calls, Does.Not.Contain("GetNodeCoordinates:17"));
        }

        [Test]
        public void SelectedListTaskFaultsWithOriginalTransportError()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var failure = new COMException("Selection read failed.", unchecked((int)0x800706BA));
            native.OnCall = call => { if (call == "GetSelectedNodes") throw failure; };

            var task = wrapper.Geometry.GetSelectedNodesListAsync();
            Assert.That(task.IsFaulted, Is.True);
            Assert.That(Assert.Throws<COMException>(() => task.GetAwaiter().GetResult()), Is.SameAs(failure));
        }

        [Test]
        public void SelectedListTaskRejectsOtherThreadsAndReplacedBindings()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var geometry = wrapper.Geometry;

            var otherThread = Task.Run(() => geometry.GetSelectedNodesListAsync().Exception?.InnerException).GetAwaiter().GetResult();
            Assert.That(otherThread, Is.TypeOf<InvalidOperationException>());
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => geometry.GetSelectedNodesListAsync().GetAwaiter().GetResult());
            Assert.That(native.Calls, Is.Empty);
        }
    }
}
