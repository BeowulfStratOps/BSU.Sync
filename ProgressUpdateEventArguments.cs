using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSO.Sync
{
    public class ProgressUpdateEventArguments : EventArgs
    {
        public int ProgressValue { get; set; }
        public int MaximumValue { get; set; }
    }
}
