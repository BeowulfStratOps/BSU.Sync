using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Sync.FileTypes;
using System.Net;

namespace BSU.Sync
{
    public static class Remote
    {
        public static List<HashFile> GetHashFiles(Uri serverFileUri)
        {
            WebRequest request = WebRequest.CreateHttp(serverFileUri);
            ServerFile sf = FileReader.ReadServerFileFromStream(request.GetResponse().GetResponseStream());
            var hashFiles = new List<HashFile>();
            foreach (ModFolder m in sf.ModFolders)
            {
                string x = $"{m.ModName}/hash.json";
                var requestUri = new Uri(sf.SyncUris[0], x);
                Console.WriteLine(requestUri);
                WebRequest request2 = WebRequest.CreateHttp(requestUri);
                HashFile newHashFile = FileReader.ReadHashFileFromStream(request2.GetResponse().GetResponseStream());
                foreach (HashType h in newHashFile.Hashes)
                {
                    if (h.FileName.StartsWith("/")) // File was probally made on linux..
                    {
                        h.FileName = h.FileName.Replace('/', System.IO.Path.DirectorySeparatorChar);
                    }
                }
                hashFiles.Add(newHashFile);
            }

            return hashFiles;
        }
        public static List<ModFolderHash> GetModFolderHashes(Uri serverFileUri)
        {
            List<HashFile> hashFiles = GetHashFiles(serverFileUri);

            return hashFiles.Select(hf => new ModFolderHash(new ModFolder(hf.FolderName), hf.Hashes)).ToList();
        }
    }
}
