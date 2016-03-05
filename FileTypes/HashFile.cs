using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BSU.Sync.FileTypes
{
    [JsonObject(MemberSerialization.OptIn)]
    public class HashFile
    {
        [JsonProperty]
        public string FolderName { get; set; }
        [JsonProperty]
        public List<HashType> Hashes { get; set; }
        public HashFile()
        {
        }
        public HashFile(string FolderName, List<HashType> Hashes)
        {
            this.Hashes = Hashes;
            this.FolderName = FolderName;
        }
    }
}
