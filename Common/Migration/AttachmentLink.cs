using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Migration
{
    public class AttachmentLink
    {
        public AttachmentReference AttachmentReference { get; set; }
        public string FileName { get; set; }
        public string Comment { get; set; }
        public long ResourceSize { get; set; }

        public AttachmentLink(string filename, AttachmentReference aRef, long resourceSize, string comment = null)
        {
            this.FileName = filename;
            this.AttachmentReference = aRef;
            this.ResourceSize = resourceSize;
            this.Comment = comment;
        }
    }
}
