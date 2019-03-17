using System.Linq;
using FooCoin.Core.Models;

namespace FooCoin.Core.Validation
{
    public interface IBlockValidator
    {
        ValidationResult HasValidHeader(Block block);
        ValidationResult IsValidBlock(Block block, Blockchain blockchain);
    }

    public class BlockValidationMessage{
        public static string BlockWasNull = "The block was null";
        public static string TransactionWasNull = "The block transaction was null";
        public static string BlockHashInvalid = "The block's hash is not a hash of the block";
        public static string BlockHashDoesNotMeetDifficulty = "The block's hash does not meet difficulty standards";
        public static string BlockDifficultyInvalid = "The block's set difficulty is invalid for this node.";
    }

    public class DefaultBlockValidator : IBlockValidator
    {
        private ICrypto _crypto;
        private ITransactionValidator _transactionValidator;
        private State _state;

        public DefaultBlockValidator(ICrypto crypto, ITransactionValidator transactionValidator, State state){
            _crypto = crypto;
            _transactionValidator = transactionValidator;
            _state = state;
        }

        public ValidationResult IsValidBlock(Block block, Blockchain blockchain)
        {
            if(block == null)
                return ValidationResult.Invalid(BlockValidationMessage.BlockWasNull);

            if(block.Transaction == null)
                return ValidationResult.Invalid(BlockValidationMessage.TransactionWasNull);

            var headerValidationResult = HasValidHeader(block);
            if(!headerValidationResult.IsValid)
                return headerValidationResult;

            return _transactionValidator.IsBlockTransactionValid(block.Transaction, blockchain);
        }

        public ValidationResult HasValidHeader(Block block)
        {
            if(block.Difficulty != _state.Difficulty)
                return ValidationResult.Invalid(BlockValidationMessage.BlockDifficultyInvalid);

            var startString = string.Join("", Enumerable.Range(0, block.Difficulty).Select(x => "0"));
            var hash = _crypto.Hash(block);

            if(block.Hash != hash)
                return ValidationResult.Invalid(BlockValidationMessage.BlockHashInvalid);

            if(!block.Hash.StartsWith(startString))
                return ValidationResult.Invalid(BlockValidationMessage.BlockHashDoesNotMeetDifficulty);

            return ValidationResult.Valid();
        }
    }
}