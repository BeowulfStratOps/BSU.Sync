namespace BSU.Sync
{
    public enum ChangeAction
    {
        Acquire,
        Delete
    }
    public enum ChangeReason
    {
        New,
        Deleted,
        Update
    }
    public class Change
    {
        public string FilePath { get; set; }
        public ChangeAction Action { get; set; }
        public ChangeReason Reason { get; set; }
        public long Filesize { get; set; }
        internal Change(string filePath, ChangeAction action, ChangeReason reason, long filesize)
        {
            FilePath = filePath;
            Action = action;
            Reason = reason;
            Filesize = filesize;
        }

        public override string ToString()
        {
            return $"{Action}({Reason}): ({Filesize}) {FilePath}";
        }
    }
}
