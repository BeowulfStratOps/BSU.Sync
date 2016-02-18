using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSO.Sync.FileTypes;
using System.Net;

namespace BSO.Sync
{
    public static class Remote
    {
        public static List<HashFile> GetHashFiles(Uri ServerFileUri)
        {
            WebRequest request = WebRequest.CreateHttp(ServerFileUri);
            ServerFile sf = FileReader.ReadServerFileFromStream(request.GetResponse().GetResponseStream());
            List<HashFile> hashFiles = new List<HashFile>();
            foreach (ModFolder m in sf.ModFolders)
            {
                string x = string.Format("{0}/hash.json", m.ModName);
                Uri RequestUri = new Uri(sf.SyncUris[0], x);
                Console.WriteLine(RequestUri);
                WebRequest request2 = WebRequest.CreateHttp(RequestUri);
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
        public static List<ModFolderHash> GetModFolderHashes(Uri ServerFileUri)
        {
            List<ModFolderHash> mfh = new List<ModFolderHash>();
            List<HashFile> hashFiles = GetHashFiles(ServerFileUri);

            foreach (HashFile hf in hashFiles)
            {
                mfh.Add(new ModFolderHash(new ModFolder(hf.FolderName), hf.Hashes));
            }

            return mfh;
        }
    }
}
