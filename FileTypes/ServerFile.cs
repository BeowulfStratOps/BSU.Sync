using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BSU.Sync.FileTypes
{

    [JsonObject(MemberSerialization.OptIn)]
    public class ServerFile
    {
        [JsonProperty]
        public string ServerName { get; set; }
        [JsonProperty]
        public string ServerAddress { get; set; }
        [JsonProperty]
        public int ServerPort { get; set; }
        [JsonProperty]
        public string Password { get; set; }
        [JsonProperty]
        public DateTime CreationDate { get; set; }
        [JsonProperty]
        public DateTime LastUpdateDate { get; set; }
        [JsonProperty]
        public List<Uri> SyncUris { get; set; }
        [JsonProperty]
        public Guid ServerGUID { get; set; }
        [JsonProperty]
        public List<ModFolder> ModFolders { get; set; }
        internal ServerFile(string ServerName, string ServerAddress, int ServePort, string Password, List<ModFolder> ModFolders,DateTime LastUpdate, DateTime CreationDate, Guid ServerGUID, List<Uri> SyncUris)
        {
            this.ServerAddress = ServerAddress;
            this.ServerName = ServerName;
            this.Password = Password;
            this.ServerPort = ServerPort;
            this.ModFolders = ModFolders;
            this.CreationDate = CreationDate;
            this.ServerGUID = ServerGUID;
            this.SyncUris = SyncUris;
            LastUpdateDate = LastUpdate;
        }
        internal ServerFile()
        {

        }
    }
}
