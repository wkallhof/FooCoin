using System.Linq;
using FooCoin.Core.Models;

namespace FooCoin.Core.Validation
{
    public interface ITransactionValidator
    {
        ValidationResult IsBlockTransactionValid(Transaction transaction, BlockChain blockChain);
        ValidationResult IsUnconfirmedTransactionValid(Transaction transaction, BlockChain blockChain);
    }

    public class TransactionValidationMessage{
        public static string TransactionAlreadyInBlockChain = "Transaction is already in blockchain";
        public static string IdDoesNotMatchHash = "Id does match transaction hash";
        public static string MatchingTransactionNotFoundForInput = "No matching transaction found for input";
        public static string PublicKeyMismatch = "Output PubKeyHash does not match hash of Input's full public key";
        public static string InputsOutputSpent = "Input's referenced output was found in another transaction. It has been spent.";
        public static string InputSignatureInvalid = "Input signature validation failed";
        public static string InputValueDoesNotMatchOutputValue = "The sum of the inputs do not match the sum of the outputs.";
        public static string TransactionMissingInputs = "The transaction is missing inputs";
        public static string TransactionMissingOutputs = "The transaction is missing outputs";
        public static string InputsOutputIndexExceedsRange = "The input references an output index that exceeds the referenced transactions output range.";
    }

    public class DefaultTransactionValidator : ITransactionValidator
    {
        private ICrypto _crypto;

        public DefaultTransactionValidator(ICrypto crypto){
            _crypto = crypto;
        }

        public ValidationResult IsUnconfirmedTransactionValid(Transaction transaction, BlockChain blockChain)
        {
            // validate transaction not already in blockchain
            if(blockChain.Blocks.Any(x => x.Transaction.Id.Equals(transaction.Id)))
                return Invalid(TransactionValidationMessage.TransactionAlreadyInBlockChain);

            return IsBlockTransactionValid(transaction, blockChain);
        }

        public ValidationResult IsBlockTransactionValid(Transaction transaction, BlockChain blockChain){

            // validate Id
            if(_crypto.DoubleHash(transaction) != transaction.Id)
                return Invalid(TransactionValidationMessage.IdDoesNotMatchHash);

            if(transaction.Inputs == null || !transaction.Inputs.Any())
                return Invalid(TransactionValidationMessage.TransactionMissingInputs);

            if(transaction.Outputs == null || !transaction.Outputs.Any())
                return Invalid(TransactionValidationMessage.TransactionMissingOutputs);

            decimal moneyIn = 0;
            decimal moneyOut = transaction.Outputs.Sum(x => x.Amount);
            foreach(var input in transaction.Inputs){
                // validate we have a matching previous transaction for input
                var previousTransaction = blockChain.FindTransaction(input.TransactionId);
                if (previousTransaction == null)
                    return Invalid(TransactionValidationMessage.MatchingTransactionNotFoundForInput);

                if(input.OutputIndex < 0 || input.OutputIndex > previousTransaction.Outputs.Count -1)
                    return Invalid(TransactionValidationMessage.InputsOutputIndexExceedsRange);
                    
                var output = previousTransaction.Outputs.ElementAt(input.OutputIndex);

                // validate output pubkeymatch is pubkey
                if (output.PubKeyHash != _crypto.DoubleHash(input.FullPubKey))
                    return Invalid(TransactionValidationMessage.PublicKeyMismatch);

                // validate owner is owner
                if(!_crypto.ValidateSignature(GetInputSignatureHash(input, output), input.Signature, input.FullPubKey))
                    return Invalid(TransactionValidationMessage.InputSignatureInvalid);

                // validate matching output is unspent
                if(blockChain.Blocks.Any(x => 
                    !x.Transaction.Id.Equals(transaction.Id) 
                    && x.Transaction.Inputs.Any(y => y.TransactionId.Equals(input.TransactionId) && y.OutputIndex.Equals(input.OutputIndex))))
                    return Invalid(TransactionValidationMessage.InputsOutputSpent);

                moneyIn += output.Amount;
            }

            if(moneyIn != moneyOut)
                return Invalid(TransactionValidationMessage.InputValueDoesNotMatchOutputValue);

            return ValidationResult.Valid();
        }

        private string GetInputSignatureHash(Input input, Output matchingOutput){
            return _crypto.Hash($"{input.TransactionId}{input.OutputIndex}{matchingOutput.PubKeyHash}");
        }

        private ValidationResult Invalid(string error) => ValidationResult.Invalid(error);
    }
}