using System;
using System.Collections.Generic;
using System.Linq;

namespace BSU.Sync
{
    class UpdateTracker
    {
        private readonly int _updates;
        private readonly long _updatesBytes;
        private readonly int _downloads;
        private readonly long _downloadsBytes;
        private readonly Action<DownloadProgressEventArgs> _downloadHandler;
        private readonly Action<DownloadProgressEventArgs> _updateHandler;
        private readonly List<TaskState> _states = new List<TaskState>();
        private readonly object _stateslock = new object();

        public UpdateTracker(int updates, long updatesBytes, int downloads, long downloadsBytes, Action<DownloadProgressEventArgs> downloadHandler, Action<DownloadProgressEventArgs> updateHandler)
        {
            _updates = updates;
            _updatesBytes = updatesBytes;
            _downloads = downloads;
            _downloadsBytes = downloadsBytes;
            _downloadHandler = downloadHandler;
            _updateHandler = updateHandler;
        }

        public TaskState NewTask(ChangeReason type)
        {
            var state = new TaskState
            {
                BytesDownloaded = 0,
                Complete = false,
                Type = type
            };
            lock (_stateslock)
            {
                _states.Add(state);
            }
            return state;
        }

        public void Update(TaskState state)
        {
            long bytes;
            int items;
            lock (_stateslock)
            {
                bytes = _states.Where(s => s.Type == state.Type).Sum(s => s.BytesDownloaded);
                items = _states.Count(s => s.Type == state.Type && s.Complete);
            }

            if (state.Type == ChangeReason.New)
            {
                _downloadHandler(new DownloadProgressEventArgs
                {
                    BytesDonwloaded = bytes,
                    BytesTotal = _downloadsBytes,
                    Files = items,
                    FilesTotal = _downloads
                });
            }
            else
            {
                _updateHandler(new DownloadProgressEventArgs
                {
                    BytesDonwloaded = bytes,
                    BytesTotal = _updatesBytes,
                    Files = items,
                    FilesTotal = _updates
                });
            }
        }

        public class TaskState
        {
            public long BytesDownloaded;
            public bool Complete;
            public ChangeReason Type;
        }
    }
}
