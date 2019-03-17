using System.Linq;
using FooCoin.Core.Extensions;
using FooCoin.Core.Models;

namespace FooCoin.Core.Validation
{
    public interface IBlockchainValidator
    {
        ValidationResult IsValid(Blockchain blockchain);
    }

    public class BlockchainValidationMessage{
        public static string BlockchainIsNullOrEmpty = "The blockchain is null or empty";
        public static string NotAllBlocksAreValid = "Not all blocks in blockchain are valid";
        public static string NotAllBlocksAreLinked = "Not all blocks are linked in blockchain";
    }

    public class DefaultBlockchainValidator : IBlockchainValidator
    {
        private IBlockValidator _blockValidator;

        public DefaultBlockchainValidator(IBlockValidator blockValidator){
            _blockValidator = blockValidator;
        }

        public ValidationResult IsValid(Blockchain blockchain)
        {
            // make sure that we have blocks
            if(blockchain?.Blocks == null || !blockchain.Blocks.Any())
                return ValidationResult.Invalid(BlockchainValidationMessage.BlockchainIsNullOrEmpty);

            // if we only have one, return valid
            if(blockchain.Blocks.Count == 1)
                return ValidationResult.Valid();

            // skip block validation for the genesis block
            var blocksWithoutGenesis = blockchain.Blocks.Skip(1);

            // ensure all blocks themselves are valid
            if(!blocksWithoutGenesis.All(x => _blockValidator.IsValidBlock(x, blockchain)))
                return ValidationResult.Invalid(BlockchainValidationMessage.NotAllBlocksAreValid);

            // ensure all blocks are properly linked in order
            if(!blockchain.Blocks.SelectTwo((a, b) => a.Hash == b.PreviousBlockHash).All(x => x == true))
                return ValidationResult.Invalid(BlockchainValidationMessage.NotAllBlocksAreLinked);

            return ValidationResult.Valid();
        }
    }
}