using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using BSO.Sync.FileTypes;

namespace BSO.Sync
{
    public static class Hash
    {
        public static byte[] GetFileHash(string Filename)
        {
            if (!File.Exists(Filename))
            {
                throw new FileNotFoundException("File not found", Filename);
            }
            using (FileStream fileStream = File.Open(Filename, FileMode.Open, FileAccess.Read))
            {
                using (SHA1Cng cryptoProvider = new SHA1Cng())
                {
                    using (BufferedStream bufferedStream = new BufferedStream(fileStream))
                    {
                        return cryptoProvider.ComputeHash(bufferedStream);
                    }
                }
            }
        }
        public static List<HashType> HashFolder(string Dir)
        {
            List<HashType> hashes = new List<HashType>();
            foreach (string file in Directory.EnumerateFiles(Dir,"*",SearchOption.AllDirectories).Where(f => !f.EndsWith(".zsync")))
            {
                byte[] hash = GetFileHash(file);
                hashes.Add(new HashType(file.Replace(Dir,string.Empty), hash));
            }
            return hashes;
        }
    }
}
