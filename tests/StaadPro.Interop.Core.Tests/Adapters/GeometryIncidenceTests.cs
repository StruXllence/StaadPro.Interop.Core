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
    public sealed class GeometryIncidenceTests
    {
        [Test]
        public void MemberHydratesEndpointsInNativeOrderExactlyOnceEach()
        {
            var native = new GeometryIncidenceNative { MemberStatus = (short)0, MemberIds = [12L, (ushort)11] };
            using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));

            var member = wrapper.Geometry.GetBeam(beamNumber: 42);

            Assert.That(member.Id, Is.EqualTo(42));
            Assert.That(member.StartNode.Id, Is.EqualTo(12));
            Assert.That(member.EndNode.Id, Is.EqualTo(11));
            Assert.That(member.EndNode.X, Is.EqualTo(11d));
            Assert.That(member.EndNode.Y, Is.EqualTo(-0.5d));
            Assert.That(member.EndNode.Z, Is.EqualTo(3d));
            Assert.That(native.Calls, Is.EqualTo(new[]
                { "GetMemberIncidence:42", "GetNodeCoordinates:12", "GetNodeCoordinates:11" }));
            Assert.That(native.Threads, Is.All.EqualTo(Environment.CurrentManagedThreadId));
        }

        [TestCase(0)]
        [TestCase(11)]
        [TestCase(14)]
        public void PlateHydratesUniqueIdsWithAnOptionalFourthSlot(int d)
        {
            var native = new GeometryIncidenceNative { PlateIds = [11, 12, 13, d] };
            using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));
            string[] calls = d == 14
                ? ["GetPlateIncidence:7", "GetNodeCoordinates:11", "GetNodeCoordinates:12", "GetNodeCoordinates:13", "GetNodeCoordinates:14"]
                : ["GetPlateIncidence:7", "GetNodeCoordinates:11", "GetNodeCoordinates:12", "GetNodeCoordinates:13"];

            var plate = wrapper.Geometry.GetPlate(pId: 7);

            Assert.That(plate.Id, Is.EqualTo(7));
            Assert.That(new[] { plate.A.Id, plate.B.Id, plate.C.Id }, Is.EqualTo(new[] { 11, 12, 13 }));
            if (d == 0) Assert.That(plate.D, Is.Null);
            else if (d == 11) Assert.That(plate.D, Is.SameAs(plate.A));
            else Assert.That(plate.D.Id, Is.EqualTo(14));
            Assert.That(native.Calls, Is.EqualTo(calls));

            native.Calls.Clear();
            var corners = wrapper.Geometry.GetPlateIncidence(pId: 7);
            Assert.That(native.Calls, Is.EqualTo(calls));
            int[] expected = d == 14 ? [11, 12, 13, 14] : [11, 12, 13];
            Assert.That(corners.Select(node => node.Id), Is.EqualTo(expected));
            Assert.That(corners.Select(node => node.Id), Is.EqualTo(expected));
            Assert.That(native.Calls, Is.EqualTo(calls));
        }

        [Test]
        public void SuppliedNodesReuseFirstMatchingReferencesAndRetainNullForMissingSlots()
        {
            var native = new GeometryIncidenceNative { PlateIds = [11, 12, 13, 11] };
            using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));
            var first = new Node(11, 70, 3, 4);
            var third = new Node(13, 90, 5, 6);
            int enumerations = 0;
            IEnumerable<Node> Nodes()
            {
                enumerations++;
                yield return third;
                yield return first;
                yield return new Node(11, 99, 5, 6);
            }

            var member = wrapper.Geometry.GetBeam(bId: 42, fromModelNodes: Nodes());
            var plate = wrapper.Geometry.GetPlate(pId: 7, fromModelNodes: Nodes());

            Assert.That(enumerations, Is.EqualTo(2));
            Assert.That(member.StartNode, Is.SameAs(first));
            Assert.That(member.EndNode, Is.Null);
            Assert.That(plate.A, Is.SameAs(first));
            Assert.That(plate.B, Is.Null);
            Assert.That(plate.C, Is.SameAs(third));
            Assert.That(plate.D, Is.SameAs(first));
            Assert.That(native.Calls, Is.EqualTo(new[] { "GetMemberIncidence:42", "GetPlateIncidence:7" }));
        }

        [Test]
        public void SuppliedPlateIgnoresDraftZeroIdAndStopsOnceAllIncidenceIdsMatch()
        {
            var native = new GeometryIncidenceNative();
            using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));
            var a = new Node(11, 1, 0, 0);
            var b = new Node(12, 2, 0, 0);
            var c = new Node(13, 3, 0, 0);
            IEnumerable<Node> Nodes()
            {
                yield return new Node(0, 90, 90, 90);
                yield return c;
                yield return b;
                yield return a;
                Assert.Fail("Enumeration should stop once the required incidence IDs are matched.");
            }

            var plate = wrapper.Geometry.GetPlate(7, Nodes());

            Assert.That(plate.A, Is.SameAs(a));
            Assert.That(plate.B, Is.SameAs(b));
            Assert.That(plate.C, Is.SameAs(c));
            Assert.That(plate.D, Is.Null);
            Assert.That(native.Calls, Is.EqualTo(new[] { "GetPlateIncidence:7" }));
        }

        [TestCase(false, 0)]
        [TestCase(false, -1)]
        [TestCase(false, -4001)]
        [TestCase(true, 0)]
        [TestCase(true, -1)]
        [TestCase(true, -4001)]
        public void NativeIncidenceRejectsNonzeroAndBooleanStatusBeforeCoordinateReads(bool member, int status)
        {
            object invalid = status == 0 ? false : status;
            var native = new GeometryIncidenceNative { MemberStatus = invalid, PlateStatus = invalid };
            using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));

            if (member) Assert.Throws<InvalidOperationException>(() => wrapper.Geometry.GetBeam(42));
            else Assert.Throws<InvalidOperationException>(() => wrapper.Geometry.GetPlate(7));
            Assert.That(native.Calls, Is.EqualTo(new[] { member ? "GetMemberIncidence:42" : "GetPlateIncidence:7" }));
        }

        [TestCase(true, false)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public void UnwrittenOrRepeatedRequiredIdsFailBeforeCoordinateReads(bool member, bool repeated)
        {
            var native = new GeometryIncidenceNative
            {
                MemberIds = repeated ? [11, 11] : [11],
                PlateIds = repeated ? [11, 12, 12, 0] : [11, 12, 13]
            };
            using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));

            if (member) Assert.Throws<InvalidOperationException>(() => wrapper.Geometry.GetBeam(42));
            else Assert.Throws<InvalidOperationException>(() => wrapper.Geometry.GetPlateIncidence(7));
            Assert.That(native.Calls, Is.EqualTo(new[] { member ? "GetMemberIncidence:42" : "GetPlateIncidence:7" }));
        }

        [Test]
        public void PositiveIdAndNonNullSuppliedSequenceValidationPrecedeNativeReads()
        {
            var native = new GeometryIncidenceNative();
            using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));
            var geometry = wrapper.Geometry;

            Assert.That(geometry.GetBeam(beamNumber: 0), Is.Null);
            Assert.That(geometry.GetBeam(bId: 0, fromModelNodes: Array.Empty<Node>()), Is.Null);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => geometry.GetBeam(-1)).ParamName, Is.EqualTo("beamNumber"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => geometry.GetBeam(-1, Array.Empty<Node>())).ParamName, Is.EqualTo("bId"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => geometry.GetPlate(0)).ParamName, Is.EqualTo("pId"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => geometry.GetPlateIncidence(-1)).ParamName, Is.EqualTo("pId"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => geometry.GetBeam(42, null)).ParamName, Is.EqualTo("fromModelNodes"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => geometry.GetPlate(7, null)).ParamName, Is.EqualTo("fromModelNodes"));
            Assert.That(native.Calls, Is.Empty);
        }

        [Test]
        public void MalformedCoordinatesAbortHydrationWithoutFallback()
        {
            var native = new GeometryIncidenceNative { IncompleteNodeId = 12 };
            using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));

            Assert.Throws<InvalidOperationException>(() => wrapper.Geometry.GetPlate(7));
            Assert.That(native.Calls, Is.EqualTo(new[]
                { "GetPlateIncidence:7", "GetNodeCoordinates:11", "GetNodeCoordinates:12" }));
        }

        [Test]
        public void SuppliedIncidencePreservesOriginalNativeErrorAndSkipsEnumeration()
        {
            var native = new GeometryIncidenceNative();
            using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));
            var error = new COMException("Incidence failed", unchecked((int)0x80004005));
            native.OnCall = _ => throw error;
            bool enumerated = false;
            IEnumerable<Node> Nodes()
            {
                enumerated = true;
                yield return new Node(11, 0, 0, 0);
            }

            var observed = Assert.Throws<COMException>(() => wrapper.Geometry.GetBeam(42, Nodes()));

            Assert.That(observed, Is.SameAs(error));
            Assert.That(enumerated, Is.False);
            Assert.That(native.Calls, Is.EqualTo(new[] { "GetMemberIncidence:42" }));
        }

        [Test]
        public void GeometryLeaseRejectsForeignThreadsReentrantReadsAndOldAdapters()
        {
            var native = new GeometryIncidenceNative();
            using var wrapper = new OpenStaadWrapper(new GeometryIncidenceRoot(native));
            var old = wrapper.Geometry;

            Assert.Throws<InvalidOperationException>(() => Task.Run(() => old.GetPlateIncidence(7)).GetAwaiter().GetResult());
            wrapper.RefreshComObjects();
            Assert.Throws<InvalidOperationException>(() => old.GetBeam(42));
            Assert.That(native.Calls, Is.Empty);

            var current = wrapper.Geometry;
            native.OnCall = operation =>
            {
                if (operation == "GetPlateIncidence:7")
                {
                    Assert.Throws<InvalidOperationException>(() => current.GetBeam(42));
                    Assert.Throws<InvalidOperationException>(() => wrapper.RefreshComObjects());
                }
            };
            Assert.That(current.GetPlate(7).C.Id, Is.EqualTo(13));
            Assert.That(native.Calls.Count, Is.EqualTo(4));
        }
    }
}
