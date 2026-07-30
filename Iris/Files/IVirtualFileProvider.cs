namespace Iris.Files
{
    internal interface IVirtualFileProvider
    {
        public bool Exists(string key);
        public byte[] ReadAllBytes(string key);
    }
}
