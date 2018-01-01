using System;
using com.salesforce.zsync;
using System.Runtime.CompilerServices;
using System.IO;
using System.Net;
using com.salesforce.zsync.http;
using NLog;

[assembly: InternalsVisibleTo("BSOU.CommandLinePrototype")]
namespace BSU.Sync
{
    internal static class ZsyncManager
    {
        public const long UpdateIntervalBytes = 10 * 1024 * 1024;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        internal static void Make(string filepath)
        {
            var zm = new ZsyncMake();
            java.nio.file.Path p;
            p = java.nio.file.Paths.get(filepath);
            var options = new ZsyncMake.Options();
            // The bisign for TFAR seems to crash this if you don't do this, yet the outputted URL looks normal in the file?? Who knows.. 
            options.setUrl(WebUtility.UrlEncode(p.getFileName().ToString()));
            Logger.Info("Writing zsync file {0}", filepath);
            zm.writeToFile(java.nio.file.Paths.get(filepath), options);
        }
        internal static void ZsyncDownload(Uri controlFileUri, string saveFolder, string fileName, Action<long> updateEvent = null)
        {
            // Work around for BSOU-13
            if (java.util.Locale.getDefault() != java.util.Locale.UK)
            {
                java.util.Locale.setDefault(java.util.Locale.UK);
                Logger.Trace("Java locale set to UK");
            }
            // ReSharper disable once AssignNullToNotNullAttribute
            Directory.CreateDirectory(Path.GetDirectoryName(saveFolder + @"\" + fileName));
            var zsync = new Zsync();
            
            var options = new Zsync.Options();
            string controlFileString = controlFileUri.ToString().Replace(Path.GetFileName(controlFileUri.LocalPath), Uri.EscapeDataString(Path.GetFileName(controlFileUri.LocalPath)));

            var javaUri = new java.net.URI(controlFileString);
            options.setOutputFile(java.nio.file.Paths.get(saveFolder + @"\" + fileName));
            try
            {
                Logger.Trace("Fetching {0}", javaUri.toString());
                if (updateEvent != null)
                    zsync.zsync(javaUri, options, new MyZsyncObserver(updateEvent));
                else
                    zsync.zsync(javaUri, options);
            }
            catch (Exception ex)
            {
                if (ex.Message == "Invalid Length header value '0'")
                {
                    Logger.Warn(ex, "Invalid Length Header");
                    // Is a 0 byte, just write it 
                    using (WebClient client = new WebClient())
                    {
                        Logger.Info("Saving {0} to {1}", controlFileString.Replace(".zsync", string.Empty), saveFolder + @"\" + fileName);
                        client.DownloadFile(controlFileString.Replace(".zsync", string.Empty), saveFolder + @"\" + fileName);
                        Logger.Info("Downloaded file {0} directly");
                    }
                }
                else
                {
                    throw;
                    //Console.WriteLine("Something failed \n {0} ", ex.Message);
                    //Console.WriteLine("\t {0}", ControlFileString);
                    //logger.Warn(ex, "Error");
                }

            }

        }

        private class MyZsyncObserver : ZsyncObserver
        {
            private readonly Action<long> _updateEvent;
            private readonly long _interval;

            private long _bytestotal;
            private long _lastupdate;

            public MyZsyncObserver(Action<long> updateEvent, long? interval = null)
            {
                _updateEvent = updateEvent;
                _interval = interval ?? UpdateIntervalBytes;
                _lastupdate = 0;
                _bytestotal = 0;
            }

            public override void bytesDownloaded(long bytes)
            {
                _bytestotal += bytes;
                base.bytesDownloaded(bytes);
                if (_bytestotal <= _lastupdate + _interval) return;
                _lastupdate = _bytestotal;
                _updateEvent(_bytestotal);
            }
        }
    }
}
