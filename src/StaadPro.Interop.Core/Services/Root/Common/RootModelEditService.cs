using System;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Services.Root.Common
{
    // The adapter holds one wrapper lease across validation, snapshotting and native dispatch.
    internal static class RootModelEditService
    {
        internal static void SetInputUnitForLength(object root, LengthInputUnit lengthUnit)
        {
            ((dynamic)root).SetInputUnitForLength(ValidateLengthUnit(lengthUnit));
        }

        internal static void SetInputUnitForForce(object root, ForceInputUnit forceUnit)
        {
            ((dynamic)root).SetInputUnitForForce(ValidateForceUnit(forceUnit));
        }

        internal static int ValidateLengthUnit(LengthInputUnit lengthUnit)
        {
            int code = (int)lengthUnit;
            if (code < 0 || code > 7) throw new ArgumentOutOfRangeException(nameof(lengthUnit));
            return code;
        }

        internal static int ValidateForceUnit(ForceInputUnit forceUnit)
        {
            if (!Enum.IsDefined(forceUnit)) throw new ArgumentOutOfRangeException(nameof(forceUnit));
            return (int)forceUnit;
        }

        internal static void SetShortJobInfo(object root, string jobName, string jobClient, string engineerName)
        {
            if (jobName == null) throw new ArgumentNullException(nameof(jobName));
            if (jobClient == null) throw new ArgumentNullException(nameof(jobClient));
            if (engineerName == null) throw new ArgumentNullException(nameof(engineerName));
            ((dynamic)root).SetShortJobInfo(jobName, jobClient, engineerName);
        }

        internal static void SetShortJobInfo(object root, ShortJobInfo jobInfo)
        {
            if (jobInfo == null) throw new ArgumentNullException(nameof(jobInfo));
            string jobName = jobInfo.Name;
            string jobClient = jobInfo.Client;
            string engineerName = jobInfo.Originator;
            if (jobName == null || jobClient == null || engineerName == null)
                throw new ArgumentException("Job name, client and originating engineer must all be initialized.", nameof(jobInfo));
            SetShortJobInfo(root, jobName, jobClient, engineerName);
        }

        internal static void SetFullJobInfo(object root, string jobName, string jobClient, string engineerName, string engineerDate, string jobNumber, string revision, string part, string reference, string checkerName, string checkerDate, string approverName, string approverDate, string comments)
        {
            if (jobName == null) throw new ArgumentNullException(nameof(jobName));
            if (jobClient == null) throw new ArgumentNullException(nameof(jobClient));
            if (engineerName == null) throw new ArgumentNullException(nameof(engineerName));
            if (engineerDate == null) throw new ArgumentNullException(nameof(engineerDate));
            if (jobNumber == null) throw new ArgumentNullException(nameof(jobNumber));
            if (revision == null) throw new ArgumentNullException(nameof(revision));
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            if (checkerName == null) throw new ArgumentNullException(nameof(checkerName));
            if (checkerDate == null) throw new ArgumentNullException(nameof(checkerDate));
            if (approverName == null) throw new ArgumentNullException(nameof(approverName));
            if (approverDate == null) throw new ArgumentNullException(nameof(approverDate));
            if (comments == null) throw new ArgumentNullException(nameof(comments));
            ((dynamic)root).SetFullJobInfo(jobName, jobClient, engineerName, engineerDate, jobNumber, revision, part, reference, checkerName, checkerDate, approverName, approverDate, comments);
        }

        internal static void UpdateStructure(object root) => ((dynamic)root).UpdateStructure();
    }
}
