using System;

namespace BSU.Sync
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public long BytesDonwloaded { get; set; }
        public long BytesTotal { get; set; }
        public int Files { get; set; }
        public int FilesTotal { get; set; }
    }
}
