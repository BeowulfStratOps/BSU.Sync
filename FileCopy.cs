using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BSU.Sync
{
    internal static class FileCopy
    {
        /// <summary>
        /// Copies all files from one folder to another, overwriting if the source file is newer
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Destination"></param>
        internal static void CopyAll(DirectoryInfo Source, DirectoryInfo Target)
        {
            Directory.CreateDirectory(Target.FullName);
            foreach (FileInfo file in Source.GetFiles())
            {
                if (File.Exists(Path.Combine(Target.FullName, file.Name)))
                {
                   if (file.LastWriteTime > new FileInfo(Path.Combine(Target.FullName, file.Name)).LastWriteTime)
                    {
                        file.CopyTo(Path.Combine(Target.FullName, file.Name), true);
                    }
                }
                else 
                {
                    file.CopyTo(Path.Combine(Target.FullName, file.Name), true);
                }
            }
            foreach (DirectoryInfo directory in Source.GetDirectories())
            {
                DirectoryInfo nextTarget = Target.CreateSubdirectory(directory.Name);
                CopyAll(directory, nextTarget);
            }
        }
        /// <summary>
        /// Removes any files found in the Target directory that do not exist in the Base directory
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Target"></param>
        internal static void CleanUpFolder(DirectoryInfo Base, DirectoryInfo Target, DirectoryInfo TargetBaseFolder)
        {
            foreach (FileInfo file in Target.GetFiles())
            {
                if (!File.Exists(Path.Combine(Base.FullName, file.FullName.Replace(TargetBaseFolder.FullName, string.Empty).Substring(1))))
                {
                    File.Delete(Path.Combine(Target.FullName, file.Name));
                    Console.WriteLine("Deleting {0}", Path.Combine(Target.FullName, file.Name));
                }
            }
            foreach (DirectoryInfo directory in Target.GetDirectories())
            {
                DirectoryInfo NextBaseDirectory = new DirectoryInfo(Path.Combine(Base.FullName, directory.Name));
                CleanUpFolder(NextBaseDirectory, new DirectoryInfo(Path.Combine(Base.FullName, directory.Name)), NextBaseDirectory);
            }
        }
    }
}
