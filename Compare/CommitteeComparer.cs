using System.Numerics;
using Neo;
using Neo.Network.RPC;
using Neo.SmartContract;
using Neo.VM;

namespace NeoUtil.Compare
{
    public class CommitteeComparer
    {
        const byte PrefixCandidate = 33;
        const byte PrefixCommittee = 14;

        public static void Run(string[] args)
        {
            if (args.Length == 2)
            {
                var h = uint.Parse(args[1]);
                CompareCommittee(h);
                return;
            }
            else if (args.Length == 3)
            {
                var pubKey = args[1].HexToBytes();
                var height = uint.Parse(args[2]);
                TrackVotes(pubKey, height);
                return;
            }
            Console.WriteLine("Invalid committee comparer args");
        }

        static void CompareCommittee(uint maxHeight)
        {
            uint low = 0;
            uint high = maxHeight;
            var client1 = new StateAPI(new RpcClient(Comparer.TestN));
            var client2 = new StateAPI(new RpcClient(Comparer.ExpectN));
            var height = high;
            while (true)
            {
                Console.WriteLine($"Test {height}");
                var root1 = client1.GetStateRootAsync(height).Result?.RootHash;
                var root2 = client2.GetStateRootAsync(height).Result?.RootHash;
                if (root1 is null || root2 is null)
                    throw new InvalidOperationException($"can't get root from height, r1={root1}, r2={root2}");
                CachedCommittee expect, actual;
                expect = GetCommitteeFromHistory(client2, root2);
                actual = GetCommitteeFromHistory(client1, root1);
                bool equal = true;
                foreach (var (pk, votes) in expect)
                {
                    bool found = false;
                    foreach (var (p, v) in actual)
                    {
                        if (p == pk)
                        {
                            found = true;
                            if (v != votes)
                            {
                                Console.WriteLine($"votes not equal, pk={p}, expect={votes}, actual={v}");
                                equal = false;
                            }
                            break;
                        }
                    }
                    if (!found)
                    {
                        equal = false;
                        Console.WriteLine($"not found! pk={pk}");
                    }
                }
                Console.WriteLine($"{height} {(equal ? "Passed" : "Failed")}!");
                if (!equal)
                {
                    high = height;
                    PrintCachedCommittee(expect);
                    PrintCachedCommittee(actual);
                }
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

        static void TrackVotes(byte[] publicKey, uint maxHeight)
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
                    expect = GetVotesFromHistory(client2, root2, publicKey);
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
                    actual = GetVotesFromHistory(client1, root1, publicKey);
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

        static BigInteger VoteFromState(byte[] value)
        {
            CandidateState state = new();
            state.FromStackItem(BinarySerializer.Deserialize(value, ExecutionEngineLimits.Default));
            return state.Votes;
        }

        static BigInteger GetVotesFromHistory(StateAPI client, UInt256 root, byte[] publicKey)
        {
            var state = client.GetStateAsync(root, Constants.NeoHash, Helper.CreateKey(publicKey, PrefixCandidate)).Result;
            if (state is null) throw new InvalidOperationException("state not found");
            return VoteFromState(state);
        }

        static CachedCommittee GetCommitteeFromHistory(StateAPI client, UInt256 root)
        {
            var state = client.GetStateAsync(root, Constants.NeoHash, new[] { PrefixCommittee }).Result;
            if (state is null) throw new InvalidOperationException("state not found");
            var committee = new CachedCommittee();
            committee.FromStackItem(BinarySerializer.Deserialize(state, ExecutionEngineLimits.Default));
            return committee;
        }

        static void PrintCachedCommittee(CachedCommittee cc)
        {
            Console.WriteLine("[");
            foreach (var (p, v) in cc)
                Console.WriteLine($"  {p} {v}");
            Console.WriteLine("]");
        }
    }
}
