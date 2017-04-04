using System.IO;
using BSU.Sync.FileTypes;
using Newtonsoft.Json;
using NLog;

namespace BSU.Sync
{
    internal static class FileReader
    {
        internal static Logger Logger = LogManager.GetCurrentClassLogger();
        internal static ServerFile ReadServerFileFromStream(Stream s)
        {
            var js = new JsonSerializer();
            using (TextReader tr = new StreamReader(s))
            {
                try
                {
                    return (ServerFile)js.Deserialize(tr, typeof(ServerFile));
                }
                catch (JsonReaderException e)
                {
                    Logger.Error(e, "Failed to deserialize server file");
                    return null;
                }
            }
        }
        internal static HashFile ReadHashFileFromStream(Stream s)
        {
            JsonSerializer js = new JsonSerializer();
            using (TextReader tr = new StreamReader(s))
            {
                return (HashFile)js.Deserialize(tr, typeof(HashFile));
            }
        }
    }
}
