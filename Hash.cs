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
                // As per T16, if the file is a PBO we just extract the pre-computed hash from the file
                if (Path.GetExtension(Filename) == ".pbo" || Path.GetExtension(Filename) == ".ebo") // Adding ebo JUST in case there is ever a situation where they are shared
                {
                    byte[] array = new byte[20];
                    fileStream.Seek(-20L, SeekOrigin.End);
                    fileStream.Read(array, 0, 20);
                    return array;
                }
                using (SHA1Cng cryptoProvider = new SHA1Cng())
                {
                    using (BufferedStream bufferedStream = new BufferedStream(fileStream, 1000000))
                    {
                        logger.Trace("Hashing {0}", Filename);
                        return cryptoProvider.ComputeHash(bufferedStream);
                    }
                }
            }
        }
        /// <summary>
        /// Computes a PBO's hash (by removing the last 21 bytes). May be used for verification
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns></returns>
        public static byte[] ComputePBOHash(string Filename)
        {
            using (FileStream fileStream = File.Open(Filename, FileMode.Open, FileAccess.Read))
            {
                using (SHA1Cng cryptoProvider = new SHA1Cng())
                {
                    byte[] array = new byte[fileStream.Length - 21];
                    fileStream.Read(array, 0, (int)fileStream.Length - 21);
                    return cryptoProvider.ComputeHash(array);
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
