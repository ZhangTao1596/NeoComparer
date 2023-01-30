using Neo;

namespace NeoUtil.Compare
{
    public static class Comparer
    {
        static readonly string DefaultTestNode = "http://localhost:21332";
        static readonly string DefaultExpectNode = "http://seed1t5.neo.org:20332";

        public static Uri TestN = new(DefaultTestNode);
        public static Uri ExpectN = new(DefaultExpectNode);
        
        public static void Run(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("invalid comparer command");
                return;
            }
            if (args.Length >= 3)
            {
                if (Uri.IsWellFormedUriString(args[0], UriKind.Absolute) && Uri.IsWellFormedUriString(args[1], UriKind.Absolute))
                {
                    TestN = new(args[0]);
                    ExpectN = new(args[1]);
                    args = args[2..];
                }
            }
            switch (args[0])
            {
                case "gas":
                    GasComparer.Run(args[1..]);
                    break;
                case "votes":
                    CommitteeComparer.Run(args[1..]);
                    break;
                default:
                    Console.WriteLine("Invalid comparer command!");
                    break;
            }
        }
    }
}
