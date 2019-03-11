using System.Linq;
using WadeCoin.Core.Models;

namespace WadeCoin.Core.Validation
{
    public interface ITransactionValidator
    {
        ValidationResult IsBlockTransactionValid(Transaction transaction, BlockChain blockChain);
        ValidationResult IsUnconfirmedTransactionValid(Transaction transaction, BlockChain blockChain);
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
                return Invalid("Transaction is already in blockchain");

            return IsBlockTransactionValid(transaction, blockChain);
        }

        public ValidationResult IsBlockTransactionValid(Transaction transaction, BlockChain blockChain){

            // validate Id
            if(_crypto.DoubleHash(transaction) != transaction.Id)
                return Invalid("Id does match transaction hash");

            decimal moneyIn = 0;
            decimal moneyOut = transaction.Outputs.Sum(x => x.Amount);
            foreach(var input in transaction.Inputs){
                // validate we have a matching previous transaction for input
                var previousTransaction = blockChain.FindTransaction(input.TransactionId);
                if (previousTransaction == null)
                    return Invalid("No matching transaction found for input");

                var output = previousTransaction.Outputs.ElementAt(input.OutputIndex);

                // validate output pubkeymatch is pubkey
                if (output.PubKeyHash != _crypto.DoubleHash(input.FullPubKey))
                    return Invalid("Output PubKeyHash does not match hash of Input's full public key");

                // validate owner is owner
                if(!_crypto.ValidateSignature(GetInputSignatureHash(input, output), input.Signature, input.FullPubKey))
                    return Invalid("Input signature validation failed");

                // validate matching output is unspent
                if(blockChain.Blocks.Any(x => 
                    !x.Transaction.Id.Equals(transaction.Id) 
                    && x.Transaction.Inputs.Any(y => y.TransactionId.Equals(input.TransactionId) && y.OutputIndex.Equals(input.OutputIndex))))
                    return Invalid("Input's referenced output was found in another transaction. It has been spent.");

                moneyIn += output.Amount;
            }

            if(moneyIn != moneyOut)
                return Invalid("The sum of the inputs do not match the sum of the outputs.");

            return ValidationResult.Valid();
        }

        private string GetInputSignatureHash(Input input, Output matchingOutput){
            return _crypto.Hash($"{input.TransactionId}{input.OutputIndex}{matchingOutput.PubKeyHash}");
        }

        private ValidationResult Invalid(string error) => ValidationResult.Invalid(error);
    }
}