using Neo.Network.RPC;
using Org.BouncyCastle.Asn1.Cms;

namespace NeoUtil
{
    public static class BlockTimer
    {
        public static void Run(string[] args)
        {
            string host = "http://localhost:10332";
            uint best = uint.MaxValue;
            if (args.Length > 0)
                host = args[0];
            if (args.Length > 1)
                best = uint.Parse(args[1]);
            SyncTime(host, best);
        }

        public static void SyncTime(string host, uint best)
        {
            var client = new RpcClient(new Uri(host));
            uint count = 0;
            uint equaled = 0;
            while (true)
            {
                var c = client.GetBlockCountAsync().Result;
                Console.WriteLine($"block count: {c} {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}");
                if (c == count)
                {
                    equaled++;
                    if (equaled >= 3 && (best == uint.MaxValue || c == best))
                    {
                        Console.WriteLine($"Synced. Time: {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}");
                        break;
                    }

                }
                else if (equaled != 0)
                {
                    equaled = 0;
                }
                count = c;
                Thread.Sleep(15000);
            }
        }
    }
}
