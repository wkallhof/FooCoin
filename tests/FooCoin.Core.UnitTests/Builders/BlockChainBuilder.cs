using System.Collections.Generic;
using System.Linq;
using FooCoin.Core.Models;
using FooCoin.Core.Extensions;

namespace FooCoin.Core.UnitTests.Builders
{
    public class BlockchainBuilder : IBuilder<Blockchain>
    {
        public List<BlockBuilder> Blocks { get; private set; }

        public BlockchainBuilder(){
            Blocks = new List<BlockBuilder>();
        }

        public Blockchain Build()
        {
            if(Blocks == null)
                return new Blockchain();

            var blockchain = new Blockchain();
            blockchain.AddRange(Blocks.Select(x => x.Build()));
            return blockchain;
        }

        public BlockchainBuilder WithBlocks(List<BlockBuilder> blocks){
            Blocks = blocks;
            return this;
        }

        public BlockchainBuilder WithBlocks(params BlockBuilder[] blocks){
            Blocks = blocks.ToList();
            return this;
        }

        public BlockchainBuilder WithValidBlocks(int numberOfBlocks){
            Blocks = new List<BlockBuilder>();
            
            if(numberOfBlocks <= 0)
                return this;

            for (var i = 0; i < numberOfBlocks; i++){
                if(i == 0){
                    Blocks.Add(new BlockBuilder());
                    continue;
                }

                Blocks.Add(new BlockBuilder().WithPreviousBlockHash(Blocks.ElementAt(i - 1).Hash));
            }

            return this;
        }
    }
}