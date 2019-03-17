using System.Linq;
using Bogus;
using FooCoin.Core.Models;

namespace FooCoin.Core.UnitTests.Builders
{
    public class BlockBuilder : IBuilder<Block>
    {
        public string PreviousBlockHash { get; private set; }
        public long UnixTimeStamp { get; private set; }
        public int Difficulty { get; private set; }
        public string Nonce { get; private set; }
        public TransactionBuilder Transaction { get; private set; }

        public string Hash { get; private set; }
        public string Miner { get; private set; }

        private Faker _faker;
        
        public BlockBuilder(){
            _faker = new Faker();

            PreviousBlockHash = _faker.Random.Hash();
            UnixTimeStamp = _faker.Random.Long();
            Difficulty = _faker.Random.Int(min: 0, max: 10);
            Nonce = _faker.Random.String(10);
            Transaction = new TransactionBuilder();
            Hash = string.Join("", Enumerable.Range(0, Difficulty).Select(x => "0")) + _faker.Random.Hash();
            Miner = _faker.Internet.UserName();
        }

        public Block Build()
        {
            var block = new Block(PreviousBlockHash, Transaction != null ? Transaction.Build() : null);
            block.UnixTimeStamp = UnixTimeStamp;
            block.Difficulty = Difficulty;
            block.Nonce = Nonce;
            block.Hash = Hash;
            block.Miner = Miner;
            return block;
        }

        public BlockBuilder WithPreviousBlockHash(string previousBlockHash)
        {
            PreviousBlockHash = previousBlockHash;
            return this;
        }

        public BlockBuilder WithHash(string hash){
            Hash = hash;
            return this;
        }

        public BlockBuilder WithTransaction(TransactionBuilder transaction){
            Transaction = transaction;
            return this;
        }
    }
}