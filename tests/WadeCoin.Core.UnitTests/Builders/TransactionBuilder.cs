using System.Collections.Generic;
using Bogus;
using WadeCoin.Core.Models;

namespace WadeCoin.Core.UnitTests.Builders
{
    public class TransactionBuilder : IBuilder<Transaction>
    {
        public string Id { get; private set; }
        public List<Input> Inputs { get; private set; }
        public List<Output> Outputs { get; private set; }

        private Randomizer _random;

        public TransactionBuilder(){
            _random = new Randomizer();

            Id = _random.Hash();

            Inputs = new List<Input>();
            Outputs = new List<Output>();
        }

        public Transaction Build()
        {
            var transaction = new Transaction(Inputs, Outputs);
            transaction.Id = Id;
            return transaction;
        }
    }
}