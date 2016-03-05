using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BSU.Sync.FileTypes;
using Newtonsoft.Json;

namespace BSU.Sync
{
    internal static class FileReader
    {
        internal static ServerFile ReadServerFileFromStream(Stream s)
        {
            JsonSerializer js = new JsonSerializer();
            using (TextReader tr = new StreamReader(s))
            {
                return (ServerFile)js.Deserialize(tr,typeof(ServerFile));
            }
        }
        internal static HashFile ReadHashFileFromStream(Stream s)
        {
            JsonSerializer js = new JsonSerializer();
            using (TextReader tr = new StreamReader(s))
            {
                return (HashFile)js.Deserialize(tr, typeof(HashFile));
            }
        }
    }
}
