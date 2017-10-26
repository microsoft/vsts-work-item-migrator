using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Common;

namespace Common
{
    public class RevAndPhaseStatus
    {
        public int Rev { get; set; }

        public ISet<string> PhaseStatus { get; set; }

        public RevAndPhaseStatus()
        {
            this.PhaseStatus = new HashSet<string>();
        }

        public RevAndPhaseStatus(string revAndPhaseStatusComment)
        {
            SetRevAndPhaseStatus(revAndPhaseStatusComment);
        }

        public void SetRevAndPhaseStatus(string revAndPhaseStatusComment)
        {
            string[] parts = revAndPhaseStatusComment.Split(';');
            this.Rev = int.Parse(parts[0]);
            string[] phaseStatusStringParts = parts.SubArray(1, parts.Length - 1);
            this.PhaseStatus = phaseStatusStringParts.ToHashSet();
        }

        public string GetCommentRepresentation()
        {
            return $"{this.Rev};{string.Join(";", this.PhaseStatus)}";
        }
    }
}
