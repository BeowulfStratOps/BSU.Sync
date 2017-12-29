namespace BSU.Sync.FileTypes
{
    public class HashType
    {
        public string FileName { get; set; }
        public byte[] Hash { get; set; }
        public long FileSize { get; set; }

        public HashType(string fileName, byte[] hash, long filesize)
        {
            FileName = fileName;
            Hash = hash;
            FileSize = filesize;
        }

        public override string ToString()
        {
            return "Hash: " + FileName;
        }
    }
}
