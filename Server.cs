using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Sync.FileTypes;
using System.IO;
using System.Net;
using System.Threading;
using NLog;

namespace BSU.Sync
{
    public struct ModFolderHash
    {
        internal readonly ModFolder ModName;
        internal readonly List<HashType> Hashes;
        internal ModFolderHash(ModFolder modName, List<HashType> hashes)
        {
            ModName = modName;
            Hashes = hashes;
        }
    }

    public class Server
    {

        public delegate void ProgressUpdateEventHandler(object sender, ProgressUpdateEventArguments e);
        public delegate void FetchProgressUpdateEventHandler(object sender, ProgressUpdateEventArguments e);
        public delegate void DownloadProgressEventHandler(object sender, DownloadProgressEventArgs e);
        public delegate void UpdateProgressEventHandler(object sender, DownloadProgressEventArgs e);

        public event ProgressUpdateEventHandler ProgressUpdateEvent;
        public event FetchProgressUpdateEventHandler FetchProgessUpdateEvent;
        public event DownloadProgressEventHandler DownloadProgressEvent;
        public event UpdateProgressEventHandler UpdateProgressEvent;

        private string ServerFileName;

        protected virtual void OnProgressUpdateEvent(ProgressUpdateEventArguments e)
        {
            ProgressUpdateEvent?.Invoke(this, e);
        }

        protected virtual void OnFetchProgressUpdateEvent(ProgressUpdateEventArguments e)
        {
            FetchProgessUpdateEvent?.Invoke(this, e);
        }

        protected virtual void OnDownloadProgressEvent(DownloadProgressEventArgs e)
        {
            DownloadProgressEvent?.Invoke(this, e);
        }

        protected virtual void OnUpdateProgressEvent(DownloadProgressEventArgs e)
        {
            UpdateProgressEvent?.Invoke(this, e);
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        string _localPath;
        public string ServerName { get; private set; }
        public string ServerAddress { get; private set; }
        public int ServerPort { get; private set; }
        public string Password { get; private set; }
        public List<ModFolder> Mods { get; private set; }
        public DateTime CreationDate { get; private set; }
        public DateTime LastUpdate { get; private set; }
        public List<ModFolderHash> ModHashes { get; private set; }
        public Guid ServerGuid { get; private set; }
        public List<Uri> SyncUris { get; private set; }
        public List<ModFolderHash> OldHashes { get; private set; }
        
        private string JsonFileName { get; set; }

        public bool LoadFromWeb(Uri remoteServerFile, DirectoryInfo localPath)
        {
            _logger.Info("Loading server from {0}, local path {1}", remoteServerFile, localPath);
            _localPath = localPath.ToString();

            ServerFileName = Path.GetFileName(remoteServerFile.LocalPath);

            OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 0 });

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(remoteServerFile);
                //var response = (HttpWebResponse)request.GetResponse();
                ServerFile sf = FileReader.ReadServerFileFromStream(request.GetResponse().GetResponseStream());
                if (sf == null)
                {
                    return false;
                }
                OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 5 });
                LoadServer(sf, _localPath);
                OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 10 });
                ModHashes = HashAllMods;
            }
            catch (WebException we)
            {
                _logger.Error(we, "Failed to load server json file");
                return false;
            }

            return true;
        }

        public bool LoadFromFile(FileInfo configFilePath, DirectoryInfo localPath)
        {
            _logger.Info("Loading server from {0}, local path {1}", configFilePath, localPath);
            _localPath = localPath.ToString();
            JsonFileName = configFilePath.Name;

            OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 0 });

            try
            {
                StreamReader sr = new StreamReader(configFilePath.FullName);
                ServerFile sf = FileReader.ReadServerFileFromStream(sr.BaseStream);
                if (sf == null)
                {
                    return false;
                }
                OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 5 });
                LoadServer(sf, _localPath);
                OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 10 });
                ModHashes = HashAllMods;
            }
            catch (Exception ex)
            {
                _logger.Error((Exception)ex, "Failed to load server json file");
                return false;
            }

            return true;
        }
        public void CreateNewServer(string serverName, string serverAddress, string password, int serverPort, string lPath, string outputPath, List<Uri> syncUris, string fileName, string[] filter)
        {
            _logger.Info("Creating new server: ServerName {0}, ServerAddress {1}, Password {2}, ServerPort {3}, LPath {4}, OutputPath {5}, SyncUri[0] {6}, FileName: {7}", serverName, serverAddress, password, serverPort, lPath, outputPath, syncUris[0], fileName);
            ServerAddress = serverAddress;
            ServerName = serverName;
            ServerPort = serverPort;
            Password = password;
            SyncUris = syncUris;
            CreationDate = DateTime.Now;
            LastUpdate = DateTime.Now;
            ServerGuid = Guid.NewGuid();
            _localPath = outputPath;
            SyncUris = syncUris;
            JsonFileName = fileName;
            UpdateServer(new DirectoryInfo(lPath), filter);

        }
        // ReSharper disable once UnusedMember.Local
        public List<ModFolder> GetFolders()
        {
            return GetFolders(new DirectoryInfo(_localPath));
        }
        public List<ModFolder> GetFolders(DirectoryInfo filePath, string[] filter = null)
        {
            _logger.Info("Finding folders in {0}. Using filter: {1}", filePath.FullName, filter != null);
            if (filter != null)
            {
                _logger.Info("Filter: {0}", string.Join(", ", filter));
            }
            var returnList = new List<ModFolder>();
            foreach (string d in Directory.GetDirectories(filePath.FullName))
            {
                var folder = d.Replace(filePath.FullName, string.Empty).Replace(@"\", string.Empty).Replace("/", string.Empty);
                if (filter != null && !filter.Contains(folder))
                {
                    _logger.Info("Skipping folder {0}", folder);
                    continue;
                }
                _logger.Info("Found folder {0}", folder);
                returnList.Add(new ModFolder(folder));

            }
            return returnList;
        }
        public List<ModFolderHash> HashAllMods
        {
            get
            {
                _logger.Info("Hashing all mods");
                //var taskList = new List<Task>();
                var returnHashes = new List<ModFolderHash>(Mods.Count);

                int currentModNumber = 1;
                int perc = 90;
                if (Mods.Count > 0)
                {
                    perc = (int)((90d / Mods.Count) * currentModNumber);
                }
                foreach (ModFolder mod in Mods)
                {
                    _logger.Info("Hashing {0}", mod.ModName);
                    List<HashType> hashes = Hash.HashFolder(_localPath + @"\" + mod.ModName);
                    returnHashes.Add(new ModFolderHash(mod, hashes));
                    _logger.Info("Hashed {0}", mod.ModName);
                    OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 10 + perc });
                    currentModNumber++;
                    perc = (int)((90d / Mods.Count) * currentModNumber);
                }
                OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 100 });
                return returnHashes;
            }
        }

        public ServerFile GetServerFile()
        {
            return new ServerFile(ServerName, ServerAddress, ServerPort, Password, Mods, LastUpdate, CreationDate, ServerGuid, SyncUris);
        }
        public void LoadServer(ServerFile sf, string localPath)
        {
            _localPath = localPath;
            ServerName = sf.ServerName;
            ServerAddress = sf.ServerAddress;
            ServerPort = sf.ServerPort;
            Password = sf.Password;
            Mods = sf.ModFolders;
            LastUpdate = sf.LastUpdateDate;
            CreationDate = sf.CreationDate;
            ServerGuid = sf.ServerGuid;
            SyncUris = sf.SyncUris;
        }
        public void UpdateServer(DirectoryInfo inputDirectory, string[] filter = null)
        {
            //if(System.Diagnostics.Debugger.IsAttached)
            //    System.Diagnostics.Debugger.Break();
            LastUpdate = DateTime.Now;
            Mods = GetFolders(inputDirectory, filter);
            OldHashes = HashAllMods;
            FileWriter.WriteServerConfig(GetServerFile(), new FileInfo(Path.Combine(inputDirectory.FullName, JsonFileName)));
            FileCopy.CopyFolders(inputDirectory, new DirectoryInfo(_localPath), filter);
            FileCopy.CleanUpFolder(inputDirectory, new DirectoryInfo(_localPath), new DirectoryInfo(_localPath));
            FileWriter.WriteServerConfig(GetServerFile(), new FileInfo(Path.Combine(_localPath,JsonFileName)));
            // TODO: Maybe remove all zsync files?
            ModHashes = HashAllMods;

            List<String> changedFiles = GetChangedFiles(inputDirectory, filter);

            foreach (string f in changedFiles)
            {
                ZsyncManager.Make(f);
            }
            FileWriter.WriteModHashes(ModHashes, new DirectoryInfo(_localPath));

        }
        /// <summary>
        /// Compares files in the input directory with the hashes of the old files, creating a list of those that have changed
        /// </summary>
        /// <param name="inputDirectory">Mod input directory</param>
        /// <returns>List of changed files</returns>
        private List<string> GetChangedFiles(DirectoryInfo inputDirectory, string[] filter)
        {
            var changedFiles = new List<string>();

            ServerFileName = JsonFileName;

            var modFolders = Directory.EnumerateDirectories(_localPath);
            if (filter != null)
                modFolders = modFolders.Where(path => filter.Contains(new DirectoryInfo(path).Name));

            foreach (var modFolder in modFolders)
            {
                var modFiles = Directory.EnumerateFiles(modFolder, "*", SearchOption.AllDirectories);
                int countNew = 0, countDeleted = 0, countChanged = 0;

                foreach (string f in modFiles.Where(name => !name.EndsWith(".zsync") && !name.EndsWith("hash.json") &&
                                                            !name.EndsWith(ServerFileName)))
                {
                    // Find the source file in the hashes and compare
                    string path = f.Replace(_localPath, string.Empty).TrimStart(Path.DirectorySeparatorChar);


                    string[] splitPath = path.Split(new[] {Path.DirectorySeparatorChar}, 2);

                    string mod = splitPath[0];
                    string relativePath = splitPath[1];

                    List<HashType> oldModHash = OldHashes.FirstOrDefault(x => x.ModName.ModName == mod).Hashes;

                    HashType hash1 =
                        oldModHash?.FirstOrDefault(x =>
                            x.FileName.TrimStart(Path.DirectorySeparatorChar) == relativePath);

                    List<HashType> newModHash = ModHashes.FirstOrDefault(x => x.ModName.ModName == mod).Hashes;

                    HashType hash2 =
                        newModHash?.FirstOrDefault(x =>
                            x.FileName.TrimStart(Path.DirectorySeparatorChar) == relativePath);


                    if (hash1 == null && hash2 == null)
                    {
                        _logger.Info("Couldn't find hash with relative path {0}.", relativePath);
                    }

                    if (hash1 == null || hash2 == null)
                    {
                        // File is new 
                        changedFiles.Add(f);
                        if (hash1 == null) countNew++;
                        if (hash2 == null) countDeleted++;
                        continue;
                    }

                    if (!hash1.Hash.SequenceEqual(hash2.Hash))
                    {
                        changedFiles.Add(f);
                        countChanged++;
                    }
                }

                _logger.Info("New files in {0}: {1}", modFolder, countNew);
                _logger.Info("Deleted files in {0}: {1}", modFolder, countDeleted);
                _logger.Info("Changed files in {0}: {1}", modFolder, countChanged);
            }

            return changedFiles;
        }
        /// <summary>
        /// Returns a list of all the mods this server is aware of
        /// </summary>
        /// <returns></returns>
        public List<ModFolder> GetLoadedMods()
        {
            return Mods;
        }
        /// <summary>
        /// Fetches changes
        /// </summary>
        /// <param name="baseDirectory">Directory to download mods to</param>
        /// <param name="newHashes">NewHashes to process</param>
        /// <returns>Number of changes that failed, 0 if none</returns>
        /// <remarks>If return > 0 then the process should be re-run</remarks>
        public int FetchChanges(DirectoryInfo baseDirectory, List<ModFolderHash> newHashes)
        {
            OnProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = 10 });

            var failedChanges = new List<Change>();
            
            List<Change> changes = GenerateChangeList(newHashes);

            var tasks = new List<Task>();
            
            var downloads = changes.Count(c => c.Reason == ChangeReason.New);
            var downloadBytes = changes.Where(c => c.Reason == ChangeReason.New).Select(c => c.Filesize).Sum();
            var updates = changes.Count(c => c.Reason == ChangeReason.Update);
            var updateBytes = changes.Where(c => c.Reason == ChangeReason.Update).Select(c => c.Filesize).Sum();

            // Allocated 80% for this task (10%-90%)
            var completedTasks = 0;

            var updateTracker = new UpdateTracker(updates, updateBytes, downloads, downloadBytes, OnDownloadProgressEvent, OnUpdateProgressEvent);

            var success = true;
            OnDownloadProgressEvent(new DownloadProgressEventArgs
            {
                BytesDonwloaded = 0,
                BytesTotal = downloadBytes,
                Files = 0,
                FilesTotal = downloads
            });
            OnUpdateProgressEvent(new DownloadProgressEventArgs()
            {
                BytesDonwloaded = 0,
                BytesTotal = updateBytes,
                Files = 0,
                FilesTotal = updates
            });
            OnFetchProgressUpdateEvent(new ProgressUpdateEventArguments() { ProgressValue = completedTasks, MaximumValue = changes.Count });
            foreach (Change c in changes)
            {
                switch (c.Action)
                {
                    case ChangeAction.Acquire:
                        //Changes.Remove(c);
                        if (c.FilePath != ServerFileName)
                        {
                            //Console.WriteLine("Getting {0}",c.FilePath);
                            var reqUri = new Uri(SyncUris[0], c.FilePath + ".zsync");
                            try
                            {
                                success = true;
                                //ZsyncManager.ZsyncDownload(reqUri, BaseDirectory.ToString(), c.FilePath);
                                /*
                            if (tasks.Count > 5)
                            { 
                                Task.WaitAll(tasks.ToArray());
                            }
                            */
                                while (tasks.Count > 5)
                                {
                                    Thread.Sleep(100);
                                }
                                var state = updateTracker.NewTask(c.Reason);
                                Task t = Task.Factory.StartNew(() => {
                                    //Console.WriteLine("Starting");
                                    try
                                    {
                                        Action<long> update = null;
                                        if (c.Filesize > ZsyncManager.UpdateIntervalBytes)
                                        {
                                            update = l =>
                                            {
                                                state.BytesDownloaded = l;
                                                updateTracker.Update(state);
                                            };
                                        }
                                        ZsyncManager.ZsyncDownload(reqUri, baseDirectory.ToString(), c.FilePath, update);
                                        if (!VerifyFile(newHashes, baseDirectory, c.FilePath))
                                        {
                                            success = false;
                                            failedChanges.Add(c);
                                            _logger.Error("Verification failed on file {0}", c.FilePath);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        success = false;
                                        failedChanges.Add(c);
                                        _logger.Error(ex, "Failed to acquire file {0}", c.FilePath);
                                    }
                                });
                                t.ContinueWith((prevTask) => {
                                    //Console.WriteLine("Ending");
                                    tasks.Remove(t);
                                    if (!success) return;
                                    state.BytesDownloaded = c.Filesize;
                                    state.Complete = true;
                                    updateTracker.Update(state);
                                    completedTasks++;
                                    OnFetchProgressUpdateEvent(new ProgressUpdateEventArguments()
                                    {
                                        ProgressValue = completedTasks,
                                        MaximumValue = changes.Count
                                    });
                                });
                                tasks.Add(t);

                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex);
                            }
                        }
                        break;
                    case ChangeAction.Delete:
                        _logger.Info("Deleting {0}", Path.Combine(baseDirectory.ToString(), c.FilePath));
                        if (File.Exists(Path.Combine(baseDirectory.ToString(), c.FilePath)))
                        {
                            File.Delete(Path.Combine(baseDirectory.ToString(), c.FilePath));
                            Console.WriteLine(Path.Combine(baseDirectory.ToString(), c.FilePath));
                        }
                        //Changes.Remove(c);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            Task.WaitAll(tasks.ToArray());
            if (failedChanges.Count() != 0)
            {
                // Failed to acquire at least one file
                _logger.Error("Failed to complete {0}/{1} changes", failedChanges.Count(), changes.Count());
                
            }
            return failedChanges.Count();


        }

        /// <summary>
        /// Verifies a local file against the remote hash to see if its been correctly downloaded
        /// </summary>
        /// <param name="newHashes">New hashes</param>
        /// <param name="baseDirectory">Local Directory</param>
        /// <param name="filePath">Path of the file to verify</param>
        /// <returns></returns>
        private bool VerifyFile(List<ModFolderHash> newHashes, DirectoryInfo baseDirectory, string filePath)
        {
            string[] split = filePath.Split(new[] {Path.DirectorySeparatorChar},2);

            string modName = split[0];
            string path = split[1];

            ModFolderHash mfh = newHashes.FirstOrDefault(x => x.ModName.ModName == modName);

            HashType correctHash = mfh.Hashes.FirstOrDefault(x => x.FileName == path || x.FileName == Path.DirectorySeparatorChar + path);

            byte[] actualHash = Hash.GetFileHash(Path.Combine(baseDirectory.FullName, filePath));

            if (correctHash == null)
            {
                // Something has gone a little wrong
                _logger.Error("correctHash == null");
                return false;
            }

            return correctHash.Hash.SequenceEqual(actualHash);
        }

        public List<Change> GenerateChangeList(List<ModFolderHash> newHashes)
        {
            var changeList = new List<Change>();
            foreach (ModFolderHash mfh in newHashes)
            {
                if (!ModHashes.Exists(x => x.ModName.ModName == mfh.ModName.ModName))
                {
                    // If the entire mod doesn't exist, add it all
                    foreach (HashType h in mfh.Hashes)
                    {
                        changeList.Add(new Change(mfh.ModName.ModName + h.FileName, ChangeAction.Acquire, ChangeReason.New, h.FileSize));
                    }
                }
                else
                {
                    
                    int indexInLocalHash = ModHashes.FindIndex(x => x.ModName.ModName == mfh.ModName.ModName);
                    int indexInNewHash = newHashes.FindIndex(x => x.ModName.ModName == mfh.ModName.ModName);
                    // Determine all deletions first
                    foreach (HashType ht in ModHashes[indexInLocalHash].Hashes)
                    {
                        int index = newHashes[indexInNewHash].Hashes.FindIndex(x => x.FileName == ht.FileName);
                        if (index == -1)
                        {
                            // need to add a delete change
                            changeList.Add(new Change(mfh.ModName.ModName + ht.FileName, ChangeAction.Delete, ChangeReason.Deleted, 0));

                        }
                    }
                    foreach (HashType h in mfh.Hashes)
                    {
                        
                        if (ModHashes[indexInLocalHash].Hashes.Exists(x => x.FileName == h.FileName))
                        {
                            // File exists both in the local hash and the remote hash
                            if (!ModHashes[indexInLocalHash]
                                .Hashes.Exists(x => x.FileName == h.FileName &&
                                                    !x.Hash.SequenceEqual(h.Hash))) continue;
                            {
                                // A file exists but has a different hash, it must be (re)acquired 
                                //HashType hash = _modHashes[indexInLocalHash].Hashes.Find(x => x.FileName == h.FileName);
                                changeList.Add(new Change(mfh.ModName.ModName + h.FileName, ChangeAction.Acquire, ChangeReason.Update, h.FileSize));
                            }
                        }
                        else if (!ModHashes[indexInLocalHash].Hashes.Exists(x => x.FileName == h.FileName) && newHashes[indexInNewHash].Hashes.Exists(x => x.FileName == h.FileName ))
                        {
                            // Does not exist locally, but does exist remotely. Acquire it
                            changeList.Add(new Change(mfh.ModName.ModName + h.FileName, ChangeAction.Acquire, ChangeReason.New, h.FileSize));
                        }
                        else if (ModHashes[indexInLocalHash].Hashes.Exists(x => x.FileName == h.FileName) && !newHashes[indexInNewHash].Hashes.Exists(x => x.FileName == h.FileName))
                        {
                            // Exists locally, but does not exist remotely. Delete it
                            changeList.Add(new Change(mfh.ModName.ModName +  h.FileName, ChangeAction.Delete, ChangeReason.Deleted, 0));
                        }
                    }
                }
            }
            return changeList;
        }
        public bool Validate(List<ModFolderHash> remoteHashes)
        {
            foreach (ModFolderHash mfh in remoteHashes)
            {
                int indexInLocal = ModHashes.FindIndex(x => x.ModName.ModName == mfh.ModName.ModName);
                foreach (HashType h in mfh.Hashes)
                {
                    if (ModHashes[indexInLocal].Hashes.Exists(x => x.FileName == h.FileName))
                    {
                        HashType remoteHash = ModHashes[indexInLocal].Hashes.Find(x => x.FileName == h.FileName);
                        if (remoteHash.Hash == h.Hash)
                        {
                            break;
                        }
                        _logger.Info("Validation of mods failed");
                        return false;
                    }
                    _logger.Info("Validation of mods failed");
                    return false;
                }
            }
            _logger.Info("Validation of mods passed");
            return true;

        }
        public Uri GetServerFileUri()
        {
            // TODO: Some sort of selection?
            return new Uri(SyncUris[0], ServerFileName);
        }
        public List<ModFolderHash> GetLocalHashes()
        {
            return ModHashes;
        }
        public DirectoryInfo GetLocalPath()
        {
            return new DirectoryInfo(_localPath);
        }
        public void UpdateHashes()
        {
            ModHashes = HashAllMods;
        }

    }
}
