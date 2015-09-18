﻿// Copyright (c) 2012-2015 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

namespace Dicom
{
    using System;

    using Dicom.IO.Buffer;

    using Xunit;

    public class DicomDatasetWalkerTest
    {
        #region Fields

        private readonly DicomDatasetWalker walker;

        private readonly DatasetWalkerImpl walkerImpl;

        #endregion

        #region Constructors

        public DicomDatasetWalkerTest()
        {
            var dataset = new DicomDataset(
                new DicomUniqueIdentifier(DicomTag.SOPClassUID, DicomUID.RTDoseStorage),
                new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"),
                new DicomDate(DicomTag.AcquisitionDate, DateTime.Today),
                new DicomPersonName(DicomTag.ConsultingPhysicianName, "Doe", "John"),
                new DicomDecimalString(DicomTag.GridFrameOffsetVector, 1.0m, 2.0m, 3.0m, 4.0m, 5.0m, 6.0m),
                new DicomSequence(
                    DicomTag.BeamSequence,
                    new DicomDataset(
                        new DicomIntegerString(DicomTag.BeamNumber, 1),
                        new DicomDecimalString(DicomTag.FinalCumulativeMetersetWeight, 1.0m),
                        new DicomLongString(DicomTag.BeamName, "Ant")),
                    new DicomDataset(
                        new DicomIntegerString(DicomTag.BeamNumber, 2),
                        new DicomDecimalString(DicomTag.FinalCumulativeMetersetWeight, 100.0m),
                        new DicomLongString(DicomTag.BeamName, "Post")),
                    new DicomDataset(
                        new DicomIntegerString(DicomTag.BeamNumber, 3),
                        new DicomDecimalString(DicomTag.FinalCumulativeMetersetWeight, 2.0m),
                        new DicomLongString(DicomTag.BeamName, "Left"))),
                new DicomIntegerString(DicomTag.NumberOfContourPoints, 120));

            this.walker = new DicomDatasetWalker(dataset);
            this.walkerImpl = new DatasetWalkerImpl();
        }

        #endregion

        #region Unit tests

        [Fact]
        public void Walk_CheckSequenceItems_ShouldBeThree()
        {
            this.walker.Walk(this.walkerImpl);
            Assert.Equal(3, this.walkerImpl.itemVisits);
        }

        [Fact]
        public void Walk_OnElementReturnedFalse_FallbackBehaviorContinueWalk()
        {
            this.walker.Walk(this.walkerImpl);
            Assert.Equal(120, this.walkerImpl.numberOfCountourPoints);
        }

        [Fact]
        public void Walk_OnBeginSequenceItemReturnedFalse_FallbackBehaviorContinueWalk()
        {
            this.walker.Walk(this.walkerImpl);
            Assert.Equal(100.0, this.walkerImpl.maxFinalCumulativeMetersetWeight);
        }

        #endregion

        #region Mock classes

        private class DatasetWalkerImpl : IDicomDatasetWalker
        {
            #region Fields

            private DicomDatasetWalkerCallback callback;

            internal int itemVisits = 0;

            internal int numberOfCountourPoints;

            internal double maxFinalCumulativeMetersetWeight;

            #endregion

            #region Methods

            public void OnBeginWalk(DicomDatasetWalker walker, DicomDatasetWalkerCallback callback)
            {
                this.callback = callback;
            }

            public bool OnElement(DicomElement element)
            {
                if (element.Tag.Equals(DicomTag.NumberOfContourPoints))
                {
                    this.numberOfCountourPoints = element.Get<int>();
                }
                if (element.Tag.Equals(DicomTag.FinalCumulativeMetersetWeight))
                {
                    this.maxFinalCumulativeMetersetWeight = Math.Max(
                        element.Get<double>(),
                        this.maxFinalCumulativeMetersetWeight);
                }

                var success = !element.Tag.Equals(DicomTag.AcquisitionDate);
                if (!success) this.callback();

                return success;
            }

            public bool OnBeginSequence(DicomSequence sequence)
            {
                return true;
            }

            public bool OnBeginSequenceItem(DicomDataset dataset)
            {
                ++this.itemVisits;

                var success = this.itemVisits != 2;
                if (!success) this.callback();

                return success;
            }

            public bool OnEndSequenceItem()
            {
                return true;
            }

            public bool OnEndSequence()
            {
                return true;
            }

            public bool OnBeginFragment(DicomFragmentSequence fragment)
            {
                return true;
            }

            public bool OnFragmentItem(IByteBuffer item)
            {
                return true;
            }

            public bool OnEndFragment()
            {
                return true;
            }

            public void OnEndWalk()
            {
            }

            #endregion
        }

        #endregion
    }
}