using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using BSU.Sync.FileTypes.BI;
using Newtonsoft.Json;
using NLog;
using Formatting = Newtonsoft.Json.Formatting;

namespace BSU.Sync
{
    public static class ArmALauncher
    {
        internal static Logger Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
        public static string LauncherDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArmA 3 Launcher");
        /// <summary>
        /// 
        /// </summary>
        public static string PresetDirectory => Path.Combine(LauncherDirectory, "Presets");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="modFolders"></param>
        /// <returns></returns>
        public static string GeneratePreset(string baseDirectory, List<ModFolder> modFolders, List<string> dlcs)
        {
            Local local = ReadLocal() ?? new Local
            {
                DateCreated = DateTime.Now,
                AutodetectionDirectories = new List<string>(),
                KnownLocalMods = new List<string>(),
                UserDirectories = new List<string>()
            };

            var preset = new Preset2
            {
                LastUpdated = DateTime.UtcNow,
                PublishedId = new List<string>(),
                DlcIds = dlcs
            };

            foreach (ModFolder mf in modFolders)
            {
                preset.PublishedId.Add($"local:{Path.Combine(baseDirectory, mf.ModName).ToUpper()}");
            }

            var xmlSerializer = new XmlSerializer(typeof(Preset2));

            using (var sw = new StringWriterWithEncoding(Encoding.UTF8))
            {
                xmlSerializer.Serialize(sw,preset);
                return sw.ToString();
            }
        }

        // Borrowed from https://stackoverflow.com/a/32259619
        private sealed class StringWriterWithEncoding : System.IO.StringWriter
        {
            public StringWriterWithEncoding(System.Text.StringBuilder sb) : base(sb)
            {
                Encoding = System.Text.Encoding.Unicode;
            }


            public StringWriterWithEncoding(System.Text.Encoding encoding)
            {
                Encoding = encoding;
            }

            public StringWriterWithEncoding(System.Text.StringBuilder sb, System.Text.Encoding encoding) : base(sb)
            {
                Encoding = encoding;
            }

            public override System.Text.Encoding Encoding { get; }
        }

        public static void WritePreset(string preset, string name)
        {
            Logger.Info($"Writing launcher preset to {Path.Combine(PresetDirectory, $"{name}.preset2")}");
            File.WriteAllText(Path.Combine(PresetDirectory,$"{name}.preset2"),preset);
        }

        public static FileTypes.BI.Local ReadLocal()
        {
            Logger.Info($"Reading local.json from {Path.Combine(LauncherDirectory, "Local.json")}");
            if (!File.Exists(Path.Combine(LauncherDirectory, "Local.json")))
            {
                Logger.Warn("Local.json does not exist");
                return null;
            }
            string json = File.ReadAllText(Path.Combine(LauncherDirectory, "Local.json"));

            return JsonConvert.DeserializeObject<Local>(json);
        }

        public static Local UpdateLocal(List<ModFolder> modFolders, Local local, string baseDirectory)
        {
            Logger.Info("Updating local.json");
            if (local == null)
            {
                local = new Local
                {
                    DateCreated = DateTime.Now,
                    AutodetectionDirectories = new List<string>(),
                    KnownLocalMods = new List<string>(),
                    UserDirectories = new List<string>()
                };
            }

          

            foreach (ModFolder mf in modFolders)
            {
                if (!local.KnownLocalMods.Exists(x => x.ToLower() == Path.Combine(baseDirectory, mf.ModName)))
                {
                    local.KnownLocalMods.Add(Path.Combine(baseDirectory, mf.ModName));
                }

                if (!local.UserDirectories.Exists(x => x.ToLower() == Path.Combine(baseDirectory, mf.ModName)))
                {
                    local.UserDirectories.Add(Path.Combine(baseDirectory, mf.ModName));
                }
            }

            local.KnownLocalMods = local.KnownLocalMods.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
            local.UserDirectories = local.UserDirectories.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();

            return local;
        }

        public static void WriteLocal(FileTypes.BI.Local local)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            };

            string json = JsonConvert.SerializeObject(local, Formatting.None, settings);
            File.WriteAllText(Path.Combine(LauncherDirectory,"Local.json"),json);
            Logger.Info($"Writing local.json to {Path.Combine(LauncherDirectory, "Local.json")}");

        }


    }
}
