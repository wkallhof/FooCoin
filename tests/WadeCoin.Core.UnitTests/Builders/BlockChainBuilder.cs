using System.Collections.Generic;
using System.Linq;
using WadeCoin.Core.Models;
using WadeCoin.Core.Extensions;

namespace WadeCoin.Core.UnitTests.Builders
{
    public class BlockChainBuilder : IBuilder<BlockChain>
    {
        public List<BlockBuilder> Blocks { get; private set; }

        public BlockChainBuilder(){
            Blocks = new List<BlockBuilder>();
        }

        public BlockChain Build()
        {
            return new BlockChain()
            {
                Blocks = Blocks != null ? Blocks.Select(x => x.Build()).ToList() : null
            };
        }

        public BlockChainBuilder WithBlocks(List<BlockBuilder> blocks){
            Blocks = blocks;
            return this;
        }

        public BlockChainBuilder WithBlocks(params BlockBuilder[] blocks){
            Blocks = blocks.ToList();
            return this;
        }

        public BlockChainBuilder WithValidBlocks(int numberOfBlocks){
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