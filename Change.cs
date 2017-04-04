namespace BSU.Sync
{
    public enum ChangeAction
    {
        Acquire,
        Delete
    }
    public class Change
    {
        public string FilePath { get; set; }
        public ChangeAction Action { get; set; }
        internal Change(string filePath, ChangeAction action)
        {
            this.FilePath = filePath;
            this.Action = action;
        }

    }
}
