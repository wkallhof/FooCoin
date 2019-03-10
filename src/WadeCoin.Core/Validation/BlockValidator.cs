using System.Linq;
using WadeCoin.Core.Models;

namespace WadeCoin.Core.Validation
{
    public interface IBlockValidator
    {
        bool HasValidHeader(Block block);
        bool IsValidBlock(Block block, BlockChain blockChain);
    }
    public class DefaultBlockValidator : IBlockValidator
    {
        private ITransactionValidator _transactionValidator;

        public DefaultBlockValidator(ITransactionValidator transactionValidator){
            _transactionValidator = transactionValidator;
        }

        public bool IsValidBlock(Block block, BlockChain blockChain)
        {
            if(!HasValidHeader(block))
                return false;
            
            return _transactionValidator.IsBlockTransactionValid(block.Transaction, blockChain);

        }

        public bool HasValidHeader(Block block)
        {
            var startString = string.Join("", Enumerable.Range(0, block.Difficulty).Select(x => "0"));
            return Crypto.Hash(block).StartsWith(startString);
        }
    }
}