using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.salesforce.zsync;
using System.Runtime.CompilerServices;
using System.IO;
using System.Net;

[assembly: InternalsVisibleTo("BSOU.CommandLinePrototype")]
namespace BSO.Sync
{
    internal static class ZsyncManager
    {
        internal static void Make(string Filepath)
        {
            ZsyncMake zm = new ZsyncMake();
            java.nio.file.Path p;
            p = java.nio.file.Paths.get(Filepath);
            ZsyncMake.Options options = new ZsyncMake.Options();
            // The bisign for TFAR seems to crash this if you don't do this, yet the outputted URL looks normal in the file?? Who knows.. 
            options.setUrl(System.Net.WebUtility.UrlEncode(p.getFileName().ToString()));
            zm.writeToFile(java.nio.file.Paths.get(Filepath), options);
        }
        internal static void ZsyncDownload(Uri ControlFileUri, string SaveFolder, string FileName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SaveFolder + @"\" + FileName));
            Zsync zsync = new Zsync();
            
            Zsync.Options options = new Zsync.Options();
            string ControlFileString = ControlFileUri.ToString().Replace(Path.GetFileName(ControlFileUri.LocalPath), Uri.EscapeDataString(Path.GetFileName(ControlFileUri.LocalPath)));

            java.net.URI javaURI = new java.net.URI(ControlFileString);
            options.setOutputFile(java.nio.file.Paths.get(SaveFolder + @"\" + FileName));
            try
            {
                zsync.zsync(javaURI, options);
            }
            catch (Exception ex)
            {
                if (ex.Message == "Invalid Length header value '0'")
                {
                    // Is a 0 byte, just write it 
                    using (WebClient client = new WebClient())
                    {
                        Console.WriteLine("Saving {0} to {1}", ControlFileString.Replace(".zsync", string.Empty), SaveFolder + @"\" + FileName);
                        client.DownloadFile(ControlFileString.Replace(".zsync", string.Empty), SaveFolder + @"\" + FileName);
                    }
                }
                Console.WriteLine("Something failed \n {0} ", ex.Message);
                Console.WriteLine("\t {0}", ControlFileString);

            }

        }
    }
}
