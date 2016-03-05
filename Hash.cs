using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using BSU.Sync.FileTypes;
using NLog;

namespace BSU.Sync
{
    public static class Hash
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
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
                        logger.Trace("Hashing {0}", Filename);
                        return cryptoProvider.ComputeHash(bufferedStream);
                    }
                }
            }
        }
        public static List<HashType> HashFolder(string Dir)
        {
			if (System.Environment.OSVersion.Platform == PlatformID.Unix) 
			{
				Dir = Dir.Replace (@"\", string.Empty);
			}
            List<HashType> hashes = new List<HashType>();
            if (Directory.Exists(Dir))
            {
                foreach (string file in Directory.EnumerateFiles(Dir, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".zsync")))
                {
                    if (!file.EndsWith("hash.json") && !file.EndsWith("server.json")) // Lets not hash the control files
                    {
                        byte[] hash = GetFileHash(file);
                        hashes.Add(new HashType(file.Replace(Dir, string.Empty), hash));
                    }
                }
            }
            return hashes;
        }
    }
}
