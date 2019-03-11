using System.Linq;
using WadeCoin.Core.Models;

namespace WadeCoin.Core.Validation
{
    public interface IBlockValidator
    {
        ValidationResult HasValidHeader(Block block);
        ValidationResult IsValidBlock(Block block, BlockChain blockChain);
    }
    public class DefaultBlockValidator : IBlockValidator
    {
        private ICrypto _crypto;
        private ITransactionValidator _transactionValidator;

        public DefaultBlockValidator(ICrypto crypto, ITransactionValidator transactionValidator){
            _crypto = crypto;
            _transactionValidator = transactionValidator;
        }

        public ValidationResult IsValidBlock(Block block, BlockChain blockChain)
        {
            if(!HasValidHeader(block))
                return false;
            
            return _transactionValidator.IsBlockTransactionValid(block.Transaction, blockChain);

        }

        public ValidationResult HasValidHeader(Block block)
        {
            var startString = string.Join("", Enumerable.Range(0, block.Difficulty).Select(x => "0"));
            return _crypto.Hash(block).StartsWith(startString);
        }
    }
}