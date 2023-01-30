using System.Numerics;
using Neo;
using Neo.IO;
using Neo.Network.RPC;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;

namespace NeoUtil.Compare
{
    public static class GasComparer
    {
        const byte PrefixAccount = 20;

        public static void Run(string[] args)
        {
            if (UInt160.TryParse(args[0], out var account))
            {
                var height = uint.Parse(args[1]);
                Console.WriteLine($"Track account {account} from {height}!");
                TrackAccount(account, height);
                return;
            }
            else if (uint.TryParse(args[0], out var height))
            {
                Console.WriteLine($"Compare at {height}!");
                CompareHeightBetweenNodes(height);
                return;
            }
            Console.WriteLine("Invalid gas comparer args!");
        }

        static void TrackAccount(UInt160 account, uint maxHeight)
        {
            uint low = 0;
            uint high = maxHeight;
            var client1 = new StateAPI(new RpcClient(Comparer.TestN));
            var client2 = new StateAPI(new RpcClient(Comparer.ExpectN));
            var height = high;
            while (true)
            {
                var root1 = client1.GetStateRootAsync(height).Result?.RootHash;
                var root2 = client2.GetStateRootAsync(height).Result?.RootHash;
                if (root1 is null || root2 is null)
                    throw new InvalidOperationException($"can't get root from height, r1={root1}, r2={root2}");
                BigInteger expect, actual;
                try
                {
                    expect = GetBalanceFromHistory(client2, root2, account);
                }
                catch (AggregateException e)
                {
                    if (e.InnerExceptions[0] is Neo.Network.RPC.RpcException re && re.Message == "The given key was not present in the dictionary.")
                        expect = -1;
                    else
                        throw;
                }
                try
                {
                    actual = GetBalanceFromHistory(client1, root1, account);
                }
                catch (AggregateException e)
                {
                    if (e.InnerExceptions[0] is Neo.Network.RPC.RpcException re && re.Message == "The given key was not present in the dictionary.")
                        actual = -1;
                    else
                        throw;
                }
                Console.WriteLine($"{height} tested, equal={actual == expect} actual={actual}, expect={expect}");
                if (actual != expect)
                    high = height;
                else
                    low = height;
                height = (high + low) / 2;
                if (height == low)
                {
                    Console.WriteLine($"Found, {high}");
                    break;
                }
            }
        }

        static void CompareHeightBetweenNodes(uint height)
        {
            var client1 = new StateAPI(new RpcClient(Comparer.TestN));
            var client2 = new StateAPI(new RpcClient(Comparer.ExpectN));
            var root1 = client1.GetStateRootAsync(height).Result?.RootHash;
            var root2 = client2.GetStateRootAsync(height).Result?.RootHash;
            if (root1 is null || root2 is null)
                throw new InvalidOperationException($"can't get root from height, r1={root1}, r2={root2}");
            var fromKey = Array.Empty<byte>();
            Console.WriteLine("Running stage1...");
            while (true)
            {
                var states = client2.FindStatesAsync(root2, Constants.GasHash, new byte[] { 0x14 }, fromKey).Result;
                if (states is null) throw new InvalidOperationException("states not found");
                foreach (var (key, value) in states.Results)
                {
                    var account = AccountFromKey(key);
                    var expect = BalanceFromState(value);
                    var actual = GetBalanceFromHistory(client1, root1, account);
                    if (actual != expect)
                        throw new InvalidOperationException($"balance not match, account={account}, expect={expect}, actual={actual}");
                    else
                        Console.WriteLine($"Passed! account={account}, balance={actual}");
                }
                if (!states.Truncated) break;
                fromKey = states.Results.Last().key;
            }
            Console.WriteLine("Stage1 success!");
            Console.WriteLine("Running stage2...");
            fromKey = Array.Empty<byte>();
            while (true)
            {
                var states = client1.FindStatesAsync(root1, Constants.GasHash, new byte[] { 0x14 }, fromKey).Result;
                if (states is null) throw new InvalidOperationException("states not found");
                foreach (var (key, value) in states.Results)
                {
                    var account = AccountFromKey(key);
                    var expect = BalanceFromState(value);
                    var actual = GetBalanceFromHistory(client2, root2, account);
                    if (actual != expect)
                        throw new InvalidOperationException($"balance not match, account={account}, expect={expect}, actual={actual}");
                    else
                        Console.WriteLine($"Passed! account={account}, balance={actual}");
                }
                if (!states.Truncated) break;
                fromKey = states.Results.Last().key;
            }
            Console.WriteLine("Success!");
        }

        static BigInteger BalanceFromState(byte[] value)
        {
            AccountState gasState = new();
            gasState.FromStackItem(BinarySerializer.Deserialize(value, ExecutionEngineLimits.Default));
            return gasState.Balance;
        }

        static BigInteger GetBalanceFromHistory(StateAPI client, UInt256 root, UInt160 account)
        {
            byte[] state;
            try
            {
                state = client.GetStateAsync(root, Constants.GasHash, CreateAccountKey(account, PrefixAccount)).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine($"can't get state return zero {e.Message}");
                return BigInteger.Zero;
            }
            return BalanceFromState(state);
        }

        static byte[] CreateAccountKey(UInt160 account, byte prefix)
        {
            return Helper.CreateKey(account.ToArray(), prefix);
        }

        static UInt160 AccountFromKey(byte[] key)
        {
            return new UInt160(key.AsSpan()[1..]);
        }
    }
}
