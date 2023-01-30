using System.Numerics;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;

namespace NeoUtil.Compare
{
    public class CandidateState : IInteroperable
    {
        public bool Registered;
        public BigInteger Votes;

        public void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Registered = @struct[0].GetBoolean();
            Votes = @struct[1].GetInteger();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { Registered, Votes };
        }
    }
}
