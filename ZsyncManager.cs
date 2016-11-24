using System;
using com.salesforce.zsync;
using System.Runtime.CompilerServices;
using System.IO;
using System.Net;
using NLog;

[assembly: InternalsVisibleTo("BSOU.CommandLinePrototype")]
namespace BSU.Sync
{
    internal static class ZsyncManager
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        internal static void Make(string Filepath)
        {
            ZsyncMake zm = new ZsyncMake();
            java.nio.file.Path p;
            p = java.nio.file.Paths.get(Filepath);
            ZsyncMake.Options options = new ZsyncMake.Options();
            // The bisign for TFAR seems to crash this if you don't do this, yet the outputted URL looks normal in the file?? Who knows.. 
            options.setUrl(System.Net.WebUtility.UrlEncode(p.getFileName().ToString()));
            logger.Info("Writing zsync file {0}", Filepath);
            zm.writeToFile(java.nio.file.Paths.get(Filepath), options);
        }
        internal static void ZsyncDownload(Uri ControlFileUri, string SaveFolder, string FileName)
        {
            // Work around for BSOU-13
            if (java.util.Locale.getDefault() != java.util.Locale.UK)
            {
                java.util.Locale.setDefault(java.util.Locale.UK);
                logger.Trace("Java locale set to UK");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(SaveFolder + @"\" + FileName));
            Zsync zsync = new Zsync();
            
            Zsync.Options options = new Zsync.Options();
            string ControlFileString = ControlFileUri.ToString().Replace(Path.GetFileName(ControlFileUri.LocalPath), Uri.EscapeDataString(Path.GetFileName(ControlFileUri.LocalPath)));

            java.net.URI javaURI = new java.net.URI(ControlFileString);
            options.setOutputFile(java.nio.file.Paths.get(SaveFolder + @"\" + FileName));
            try
            {
                logger.Trace("Fetching {0}", javaURI.toString());
                zsync.zsync(javaURI, options);
            }
            catch (Exception ex)
            {
                if (ex.Message == "Invalid Length header value '0'")
                {
                    logger.Warn(ex, "Invalid Length Header");
                    // Is a 0 byte, just write it 
                    using (WebClient client = new WebClient())
                    {
                        logger.Info("Saving {0} to {1}", ControlFileString.Replace(".zsync", string.Empty), SaveFolder + @"\" + FileName);
                        client.DownloadFile(ControlFileString.Replace(".zsync", string.Empty), SaveFolder + @"\" + FileName);
                        logger.Info("Downloaded file {0} directly");
                    }
                }
                else
                {
                    throw ex;
                    //Console.WriteLine("Something failed \n {0} ", ex.Message);
                    //Console.WriteLine("\t {0}", ControlFileString);
                    //logger.Warn(ex, "Error");
                }

            }

        }
    }
}
