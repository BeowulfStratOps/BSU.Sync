using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using BSU.Sync.FileTypes;


namespace BSU.Sync
{
    internal static class FileWriter
    {
        internal static void WriteServerConfig(FileTypes.ServerFile ServerFile, FileInfo OutputFile)
        {
 
            using (TextWriter writer = new StreamWriter(new FileStream(OutputFile.FullName, FileMode.Create)))
            {
                JsonSerializer js = new JsonSerializer();
                js.Formatting = Formatting.Indented;
                js.Serialize(writer, ServerFile);
                writer.Flush();
            }
        }
        internal static void WriteModHashes(List<ModFolderHash> ModHashes, DirectoryInfo BaseDirectory)
        {
            foreach (ModFolderHash mfh in ModHashes)
            {
                HashFile hf = new HashFile(mfh.ModName.ModName, mfh.Hashes);
                using (TextWriter writer = new StreamWriter(new FileStream(Path.Combine(BaseDirectory.FullName,mfh.ModName.ModName,"hash.json"), FileMode.Create)))
                {
                    JsonSerializer js = new JsonSerializer();
                    js.Formatting = Formatting.Indented;
                    js.Serialize(writer, hf);
                    writer.Flush();
                }
            }
        }
    }
}
