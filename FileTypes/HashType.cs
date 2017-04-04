namespace BSU.Sync.FileTypes
{
    public class HashType
    {
        public string FileName { get; set; }
        public byte[] Hash { get; set; }
        public HashType(string fileName, byte[] hash)
        {
            FileName = fileName;
            Hash = hash;
        }
    }
}
