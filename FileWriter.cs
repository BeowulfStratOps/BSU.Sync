using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using BSU.Sync.FileTypes;
using NLog;


namespace BSU.Sync
{
    internal static class FileWriter
    {
        internal static void WriteServerConfig(FileTypes.ServerFile serverFile, FileInfo outputFile)
        {
 
            using (TextWriter writer = new StreamWriter(new FileStream(outputFile.FullName, FileMode.Create)))
            {
                var js = new JsonSerializer {Formatting = Formatting.Indented};
                LogManager.GetCurrentClassLogger().Info("Writing server file with mods {0} to {1}", string.Join(", ", serverFile.ModFolders.Select(mf => mf.ModName)), outputFile.FullName);
                js.Serialize(writer, serverFile);
                writer.Flush();
            }
        }
        internal static void WriteModHashes(List<ModFolderHash> modHashes, DirectoryInfo baseDirectory)
        {
            foreach (ModFolderHash mfh in modHashes)
            {
                var hf = new HashFile(mfh.ModName.ModName, mfh.Hashes);
                using (TextWriter writer = new StreamWriter(new FileStream(Path.Combine(baseDirectory.FullName,mfh.ModName.ModName,"hash.json"), FileMode.Create)))
                {
                    var js = new JsonSerializer {Formatting = Formatting.Indented};
                    js.Serialize(writer, hf);
                    writer.Flush();
                }
            }
        }
    }
}
