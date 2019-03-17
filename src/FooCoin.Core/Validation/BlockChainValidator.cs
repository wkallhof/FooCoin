using System.Linq;
using FooCoin.Core.Extensions;
using FooCoin.Core.Models;

namespace FooCoin.Core.Validation
{
    public interface IBlockChainValidator
    {
        ValidationResult IsValid(BlockChain blockChain);
    }

    public class BlockChainValidationMessage{
        public static string BlockChainIsNullOrEmpty = "The blockchain is null or empty";
        public static string NotAllBlocksAreValid = "Not all blocks in blockchain are valid";
        public static string NotAllBlocksAreLinked = "Not all blocks are linked in blockchain";
    }

    public class DefaultBlockChainValidator : IBlockChainValidator
    {
        private IBlockValidator _blockValidator;

        public DefaultBlockChainValidator(IBlockValidator blockValidator){
            _blockValidator = blockValidator;
        }

        public ValidationResult IsValid(BlockChain blockChain)
        {
            // make sure that we have blocks
            if(blockChain?.Blocks == null || !blockChain.Blocks.Any())
                return ValidationResult.Invalid(BlockChainValidationMessage.BlockChainIsNullOrEmpty);

            // if we only have one, return valid
            if(blockChain.Blocks.Count == 1)
                return ValidationResult.Valid();

            // skip block validation for the genesis block
            var blocksWithoutGenesis = blockChain.Blocks.Skip(1);

            // ensure all blocks themselves are valid
            if(!blocksWithoutGenesis.All(x => _blockValidator.IsValidBlock(x, blockChain)))
                return ValidationResult.Invalid(BlockChainValidationMessage.NotAllBlocksAreValid);

            // ensure all blocks are properly linked in order
            if(!blockChain.Blocks.SelectTwo((a, b) => a.Hash == b.PreviousBlockHash).All(x => x == true))
                return ValidationResult.Invalid(BlockChainValidationMessage.NotAllBlocksAreLinked);

            return ValidationResult.Valid();
        }
    }
}