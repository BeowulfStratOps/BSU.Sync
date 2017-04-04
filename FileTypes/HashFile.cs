using System.Collections.Generic;
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
        public HashFile(string folderName, List<HashType> hashes)
        {
            Hashes = hashes;
            FolderName = folderName;
        }
    }
}
