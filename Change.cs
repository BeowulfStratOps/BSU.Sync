using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSO.Sync
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
        internal Change(string FilePath, ChangeAction Action)
        {
            this.FilePath = FilePath;
            this.Action = Action;
        }

    }
}
