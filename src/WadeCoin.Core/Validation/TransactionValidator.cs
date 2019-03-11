using System.Linq;
using WadeCoin.Core.Models;

namespace WadeCoin.Core.Validation
{
    public interface ITransactionValidator
    {
        bool IsBlockTransactionValid(Transaction transaction, BlockChain blockChain);
        bool IsUncomfirmedTransactionValid(Transaction transaction, BlockChain blockChain);
    }

    public class DefaultTransactionValidator : ITransactionValidator
    {
        private ICrypto _crypto;

        public DefaultTransactionValidator(ICrypto crypto){
            _crypto = crypto;
        }

        public bool IsBlockTransactionValid(Transaction transaction, BlockChain blockChain)
        {
            return ValidateTransactionBody(transaction, blockChain);
        }

        public bool IsUncomfirmedTransactionValid(Transaction transaction, BlockChain blockChain)
        {
            // validate transaction not already in blockchain
            if(blockChain.Blocks.Any(x => x.Transaction.Id.Equals(transaction.Id)))
                return false;

            return ValidateTransactionBody(transaction, blockChain);
        }

        private bool ValidateTransactionBody(Transaction transaction, BlockChain blockChain){

            // validate Id
            if(_crypto.DoubleHash(transaction) != transaction.Id)
                return false;

            decimal moneyIn = 0;
            decimal moneyOut = transaction.Outputs.Sum(x => x.Amount);
            foreach(var input in transaction.Inputs){
                // validate we have a matching previous transaction for input
                var previousTransaction = blockChain.FindTransaction(input.TransactionId);
                if (previousTransaction == null)
                    return false;

                var output = previousTransaction.Outputs.ElementAt(input.OutputIndex);

                // validate output pubkeymatch is pubkey
                if (output.PubKeyHash != _crypto.DoubleHash(input.FullPubKey))
                    return false;

                // validate owner is owner
                if(!_crypto.ValidateSignature(GetInputSignatureHash(input, output), input.Signature, input.FullPubKey))
                    return false;

                // validate matching output is unspent
                if(blockChain.Blocks.Any(x => 
                    !x.Transaction.Id.Equals(transaction.Id) 
                    && x.Transaction.Inputs.Any(y => y.TransactionId.Equals(input.TransactionId) && y.OutputIndex.Equals(input.OutputIndex))))
                    return false;

                moneyIn += output.Amount;
            }

            if(moneyIn != moneyOut)
                return false;

            return true;
        }

        private string GetInputSignatureHash(Input input, Output matchingOutput){
            return _crypto.Hash($"{input.TransactionId}{input.OutputIndex}{matchingOutput.PubKeyHash}");
        }
    }
}