using System;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Root.Common
{
    internal static class RootJobInformation
    {
        internal static ShortJobInfo GetShortJobInfo(object root)
        {
            string sentinel = CreateSentinel();
            object name = sentinel, client = sentinel, originator = sentinel;
            ((dynamic)root).GetShortJobInfo(ref name, ref client, ref originator);
            return ShortJobInfo.From(ReadText(name, "name", sentinel), ReadText(client, "client", sentinel),
                ReadText(originator, "originator", sentinel));
        }

        internal static FullJobInfo GetFullJobInfo(object root)
        {
            string sentinel = CreateSentinel();
            object name = sentinel, client = sentinel, originator = sentinel, originated = sentinel;
            object number = sentinel, revision = sentinel, part = sentinel, reference = sentinel;
            object checker = sentinel, dateChecked = sentinel, approver = sentinel, approved = sentinel, comments = sentinel;
            ((dynamic)root).GetFullJobInfo(ref name, ref client, ref originator, ref originated,
                ref number, ref revision, ref part, ref reference, ref checker, ref dateChecked, ref approver, ref approved, ref comments);
            // Validate every output before parsing any dates or constructing a result.
            string[] fields = [ReadText(name, "name", sentinel), ReadText(client, "client", sentinel),
                ReadText(originator, "originator", sentinel), ReadText(originated, "originated", sentinel),
                ReadText(number, "number", sentinel), ReadText(revision, "revision", sentinel),
                ReadText(part, "part", sentinel), ReadText(reference, "reference", sentinel),
                ReadText(checker, "checker", sentinel), ReadText(dateChecked, "dateChecked", sentinel),
                ReadText(approver, "approver", sentinel), ReadText(approved, "approved", sentinel), ReadText(comments, "comments", sentinel)];
            return FullJobInfo.From(fields[0], fields[1], fields[2], fields[3], fields[8], fields[9],
                fields[10], fields[11], fields[4], fields[5], fields[6], fields[7], fields[12]);
        }

        private static string CreateSentinel() => "__OpenSTAAD_UnwrittenJob_" + Guid.NewGuid().ToString("N");
        private static string ReadText(object value, string field, string sentinel)
        {
            string text = NativeValues.ReadText(value, "job " + field);
            if (text == sentinel) throw new InvalidOperationException("OpenSTAAD left job " + field + " unwritten.");
            return text;
        }
    }
}
