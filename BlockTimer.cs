using Neo.Network.RPC;
using Org.BouncyCastle.Asn1.Cms;

namespace NeoUtil
{
    public static class BlockTimer
    {
        public static void Run(string[] args)
        {
            SyncTime();
        }

        public static void SyncTime()
        {
            var client = new RpcClient(new Uri("http://localhost:20332"));
            uint count = 0;
            while (true)
            {
                var c = client.GetBlockCountAsync().Result;
                Console.WriteLine($"block count: {c} {DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}");
                if (c == count)
                {
                    Console.WriteLine($"Synced. Time: {DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}");
                    break;
                }
                count = c;
                Thread.Sleep(15000);
            }
        }
    }
}
