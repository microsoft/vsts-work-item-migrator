namespace Common.Migration
{
    public class WorkItemLink
    {
        //This is the target id of the link (not the target id of the work item we are migrating)
        public int Id {get; set;}
        public string ReferenceName {get; set;}
        public bool IsDirectional {get; set;}
        public bool IsForward {get; set;}
        //Index will be used only for updates 
        public int? Index {get; set;}
        public string Comment;
        public WorkItemLink(int id, string name, bool isDirectional, bool isForward, string comment, int index)
        {
            this.Id = id;
            this.ReferenceName = name;
            this.IsDirectional = isDirectional;
            this.IsForward = isForward;
            this.Comment = comment;
            this.Index = index;
        }
    }
}