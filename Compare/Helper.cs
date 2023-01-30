
namespace NeoUtil.Compare
{
    public static class Helper
    {
        public static byte[] CreateKey(byte[] data, byte prefix)
        {
            var key = new byte[data.Length + 1];
            key[0] = prefix;
            Array.Copy(data.ToArray(), 0, key, 1, data.Length);
            return key;
        }
    }
}
