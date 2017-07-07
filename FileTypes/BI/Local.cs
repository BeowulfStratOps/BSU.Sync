using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BSU.Sync.FileTypes.BI
{
    class Local
    {
        [JsonProperty(propertyName: "autodetectionDirectories")]
        public List<string> AutodetectionDirectories { get; set; }

        [JsonProperty(propertyName: "dateCreated")]
        public DateTime DateCreated { get; set; }

        [JsonProperty(propertyName: "knownLocalMods")]
        public List<string> KnownLocalMods { get; set; }

        [JsonProperty(propertyName: "userDirectories")]
        public List<string> UserDirectories { get; set; }
    }
}
