using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using BSU.Sync.FileTypes;
using NLog;

namespace BSU.Sync
{
    public static class Hash
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static byte[] GetFileHash(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("File not found", filename);
            }
            using (FileStream fileStream = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                // As per T16, if the file is a PBO we just extract the pre-computed hash from the file
                if ((Path.GetExtension(filename) == ".pbo" || Path.GetExtension(filename) == ".ebo") && fileStream.Length > 20) // Adding ebo JUST in case there is ever a situation where they are shared
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
                        Logger.Trace("Hashing {0}", filename);
                        return cryptoProvider.ComputeHash(bufferedStream);
                    }
                }
            }
        }

        public static long GetFileSize(string filename)
        {
            var file = new FileInfo(filename);
            if (!file.Exists) throw new FileNotFoundException("File not found", filename);
            return file.Length;
        }
        /// <summary>
        /// Computes a PBO's hash (by removing the last 21 bytes). May be used for verification
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static byte[] ComputePboHash(string filename)
        {
            using (FileStream fileStream = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                using (SHA1Cng cryptoProvider = new SHA1Cng())
                {
                    byte[] array = new byte[fileStream.Length - 21];
                    fileStream.Read(array, 0, (int)fileStream.Length - 21);
                    return cryptoProvider.ComputeHash(array);
                }
            }
        }
        public static List<HashType> HashFolder(string dir)
        {
			if (Environment.OSVersion.Platform == PlatformID.Unix) 
			{
				dir = dir.Replace (@"\", "/");
			}
            LogManager.GetCurrentClassLogger().Info("HashFolder {0}", dir);
            var hashes = new List<HashType>();
            if (!Directory.Exists(dir)) return hashes;
            hashes.AddRange(from file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".zsync")) where !file.EndsWith("hash.json") && !file.EndsWith("server.json") let hash = GetFileHash(file) select new HashType(file.Replace(dir, string.Empty), hash, GetFileSize(file)));
            return hashes;
        }
    }
}
