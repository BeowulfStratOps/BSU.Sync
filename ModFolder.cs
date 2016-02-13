using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSO.Sync
{
    public class ModFolder
    {
        public string ModName { get; set; }
        internal ModFolder(string ModName)
        {
            this.ModName = ModName;
        }
        internal ModFolder()
        {

        }
    }
}
