using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.salesforce.zsync;
using System.Runtime.CompilerServices;

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
            Zsync zsync = new Zsync();
            Zsync.Options options = new Zsync.Options();
            java.net.URI javaURI = new java.net.URI(ControlFileUri.ToString());
            options.setOutputFile(java.nio.file.Paths.get(SaveFolder + @"\" + FileName));
            zsync.zsync(javaURI, options);

        }
    }
}
