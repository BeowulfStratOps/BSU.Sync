using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSU.Sync.FileTypes
{
    public class HashType
    {
        public string FileName { get; set; }
        public byte[] Hash { get; set; }
        public HashType(string FileName, byte[] Hash)
        {
            this.FileName = FileName;
            this.Hash = Hash;
        }
    }
}
