using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Tests.Adapters
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public sealed class GeometryBeamPlateSelectionTests
    {
        private static Task<int[]> ArrayTask(IOSGeometry geometry, bool beams) =>
            beams ? geometry.GetSelectedBeamsListAsync() : geometry.GetSelectedPlatesListAsync();

        private static HashSet<int> Set(IOSGeometry geometry, bool beams) =>
            beams ? geometry.GetSelectedBeamsIds() : geometry.GetSelectedPlatesIds();

        [TestCase(false)]
        [TestCase(true)]
        public void ArraysAndSetsUseTheRightNativeSelectionAndCopyTheIds(bool beams)
        {
            var ids = Array.CreateInstance(typeof(int), [3], [-1]);
            ids.SetValue(29, -1);
            ids.SetValue(3, 0);
            ids.SetValue(17, 1);
            var native = new GeometryInventoryNative { Ids = ids, ReplaceBuffer = true };
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));

            var task = ArrayTask(wrapper.Geometry, beams);
            Assert.That(task.Status, Is.EqualTo(TaskStatus.RanToCompletion));
            var set = Set(wrapper.Geometry, beams);
            ids.SetValue(40, -1);
            Assert.That(task.Result, Is.EqualTo(new[] { 29, 3, 17 }));
            Assert.That(set, Is.EquivalentTo(task.Result));
            var count = beams ? "GetNoOfSelectedBeams" : "GetNoOfSelectedPlates";
            var list = beams ? "GetSelectedBeams" : "GetSelectedPlates";
            Assert.That(native.Calls, Is.EqualTo(new[] { count, list, count, count, list, count }));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmptySelectionSkipsTheNativeList(bool beams)
        {
            var native = new GeometryInventoryNative { Ids = Array.Empty<int>() };
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));

            Assert.That(Set(wrapper.Geometry, beams), Is.Empty);
            Assert.That(ArrayTask(wrapper.Geometry, beams).Result, Is.Empty);
            Assert.That(native.Calls, Is.EqualTo(Enumerable.Repeat(
                beams ? "GetNoOfSelectedBeams" : "GetNoOfSelectedPlates", 2)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidArraysAndChangedCountsAreRejected(bool beams)
        {
            var native = new GeometryInventoryNative { Ids = new[] { 7, 7 } };
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));

            Assert.Throws<InvalidOperationException>(() => Set(wrapper.Geometry, beams));
            native.Ids = new[] { 7, 8 };
            native.Counts.Enqueue(2);
            native.Counts.Enqueue(3);
            var task = ArrayTask(wrapper.Geometry, beams);
            Assert.That(task.IsFaulted, Is.True);
            Assert.That(Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().GetResult()).Message,
                Does.Contain("changed during"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ArrayTasksPreserveTheOriginalTransportFailure(bool beams)
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var failure = new COMException("Selection read failed.", unchecked((int)0x800706BA));
            string list = beams ? "GetSelectedBeams" : "GetSelectedPlates";
            native.OnCall = call => { if (call == list) throw failure; };

            var task = ArrayTask(wrapper.Geometry, beams);
            Assert.That(task.IsFaulted, Is.True);
            Assert.That(Assert.Throws<COMException>(() => task.GetAwaiter().GetResult()), Is.SameAs(failure));
        }

        [Test]
        public void ProjectionKeepsFirstBeamAndOneNullForMissingIds()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var first = new Beam { Id = 3, BetaAngle = 20 };
            int enumerations = 0;
            IEnumerable<Beam> Beams()
            {
                Assert.That(++enumerations, Is.EqualTo(1));
                yield return new Beam { Id = 2 };
                yield return first;
                yield return new Beam { Id = 3 };
            }

            var result = wrapper.Geometry.GetSelectedBeamsExt(Beams());
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Single(beam => beam is not null), Is.SameAs(first));
            Assert.That(result.Contains(null), Is.True);
            Assert.That(native.Calls, Is.EqualTo(new[] { "GetNoOfSelectedBeams", "GetSelectedBeams", "GetNoOfSelectedBeams" }));
        }

        [Test]
        public void ProjectionSkipsEnumerationForEmptySelectionAndStopsWhenMatched()
        {
            var native = new GeometryInventoryNative { Ids = Array.Empty<int>() };
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var first = new Beam { Id = 3 };
            int enumerations = 0;
            IEnumerable<Beam> Beams()
            {
                enumerations++;
                yield return first;
                throw new AssertionException("Enumeration must stop once all selected IDs match.");
            }

            Assert.That(wrapper.Geometry.GetSelectedBeamsExt(Beams()), Is.Empty);
            Assert.That(enumerations, Is.Zero);
            native.Ids = new[] { 3 };
            Assert.That(wrapper.Geometry.GetSelectedBeamsExt(Beams()).Single(), Is.SameAs(first));
            Assert.That(enumerations, Is.EqualTo(1));
        }

        [Test]
        public void ProjectionReportsInvalidSuppliedSequenceAndParameterName()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            Assert.That(Assert.Throws<ArgumentNullException>(() => wrapper.Geometry.GetSelectedBeamsExt(null)).ParamName,
                Is.EqualTo("fromModelBeams"));
            Assert.That(native.Calls, Is.Empty);
            Assert.That(Assert.Throws<ArgumentException>(() => wrapper.Geometry.GetSelectedBeamsExt(new Beam[] { null })).ParamName,
                Is.EqualTo("fromModelBeams"));
        }

        [Test]
        public void ArrayTasksRejectOtherThreadsAndReplacedBindingsBeforeDispatch()
        {
            var native = new GeometryInventoryNative();
            using var wrapper = new OpenStaadWrapper(new GeometryInventoryRoot(native));
            var current = wrapper.Geometry;
            var offThread = Task.Run(() => current.GetSelectedBeamsListAsync().Exception?.InnerException).GetAwaiter().GetResult();
            Assert.That(offThread, Is.TypeOf<InvalidOperationException>());
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => current.GetSelectedPlatesListAsync().GetAwaiter().GetResult());
            Assert.That(native.Calls, Is.Empty);
        }
    }
}
