using System.Linq;
using WadeCoin.Core.Extensions;
using WadeCoin.Core.Models;

namespace WadeCoin.Core.Validation
{
    public interface IBlockChainValidator
    {
        bool IsValid(BlockChain blockChain);
    }

    public class DefaultBlockChainValidator : IBlockChainValidator
    {
        private IBlockValidator _blockValidator;

        public DefaultBlockChainValidator(IBlockValidator blockValidator){
            _blockValidator = blockValidator;
        }

        public bool IsValid(BlockChain blockChain)
        {
            var blocksWithoutGenesis = blockChain.Blocks.Skip(1);

            var allBlocksValid = blocksWithoutGenesis.All(x => _blockValidator.IsValidBlock(x, blockChain));
            var hashesValid = blocksWithoutGenesis.SelectTwo((a, b) => a.Hash == b.PreviousBlockHash).All(x => x == true);

            return allBlocksValid && hashesValid;
        }
    }
}