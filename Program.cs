using NeoUtil.Compare;

namespace NeoUtil
{
    internal class Program
    {


        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please input command");
                return;
            }
            var command = args[0];
            args = args[1..];
            switch (command)
            {
                case "comparer":
                    Comparer.Run(args);
                    break;
                case "timer":
                    BlockTimer.Run(args);
                    break;
                default:
                    Console.WriteLine($"Unsupport command: {command}");
                    break;
            }
        }

    }
}
