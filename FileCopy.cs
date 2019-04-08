using System.IO;
using System.Linq;
using NLog;

namespace BSU.Sync
{
    internal static class FileCopy
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Copies filtered sub-folders from one folder to another
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="filter"></param>
        internal static void CopyFolders(DirectoryInfo source, DirectoryInfo target, string[] filter = null)
        {
            foreach (var directory in source.GetDirectories())
            {
                if (filter != null && !filter.Contains(directory.Name)) continue;
                var nextTarget = target.CreateSubdirectory(directory.Name);
                CopyAll(directory, nextTarget);
            }
        }

        /// <summary>
        /// Copies all files from one folder to another, overwriting if the source file is newer
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        internal static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);
            foreach (FileInfo file in source.GetFiles())
            {
                if (File.Exists(Path.Combine(target.FullName, file.Name)))
                {
                   if (file.LastWriteTime > new FileInfo(Path.Combine(target.FullName, file.Name)).LastWriteTime)
                    {
                        file.CopyTo(Path.Combine(target.FullName, file.Name), true);
                    }
                }
                else 
                {
                    file.CopyTo(Path.Combine(target.FullName, file.Name), true);
                }
            }
            foreach (DirectoryInfo directory in source.GetDirectories())
            {
                DirectoryInfo nextTarget = target.CreateSubdirectory(directory.Name);
                CopyAll(directory, nextTarget);
            }
        }

        /// <summary>
        /// Removes any files found in the Target directory that do not exist in the Base directory
        /// </summary>
        /// <param name="Base"></param>
        /// <param name="target"></param>
        /// <param name="targetBaseFolder"></param>
        internal static void CleanUpFolder(DirectoryInfo Base, DirectoryInfo target, DirectoryInfo targetBaseFolder)
        {
            foreach (FileInfo file in target.GetFiles())
            {
                // Do not delete the control files, they never exist in the input folder and are quite useful.. 
                if (Path.GetExtension(file.FullName) != ".zsync" && Path.GetExtension(file.FullName) != ".json")
                {
                    if (!File.Exists(Path.Combine(Base.FullName, file.FullName.Replace(targetBaseFolder.FullName, string.Empty).Substring(1))))
                    {
                        File.Delete(Path.Combine(target.FullName, file.Name));
                        // Well actually on second thoughts, deleting the control is a good idea in some situations..
                        if (File.Exists(Path.Combine(target.FullName, file.Name) +  ".zsync"))
                        {
                            File.Delete(Path.Combine(target.FullName, file.Name) + ".zsync");
                        }
                        Logger.Info("Deleting {0}", Path.Combine(target.FullName, file.Name));


                    }
                }
            }
            foreach (DirectoryInfo directory in target.GetDirectories())
            {
                var nextBaseDirectory = new DirectoryInfo(Path.Combine(Base.FullName, directory.Name));
                CleanUpFolder(nextBaseDirectory, new DirectoryInfo(Path.Combine(targetBaseFolder.FullName, directory.Name)), new DirectoryInfo(Path.Combine(targetBaseFolder.FullName, directory.Name)));
            }
        }
    }
}
