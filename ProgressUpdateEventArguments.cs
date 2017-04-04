using System;

namespace BSU.Sync
{
    public class ProgressUpdateEventArguments : EventArgs
    {
        public int ProgressValue { get; set; }
        public int MaximumValue { get; set; }
    }
}
