using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FooCoin.Core;
using FooCoin.Core.Validation;
using FooCoin.Core.Extensions;
using FooCoin.Core.Models;

namespace FooCoin.Node.Wallet
{
    [Route("wallet")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private ILogger<WalletController> _logger;
        private State _state;
        private PrivateState _privateState;
        private ITransactionValidator _transactionValidator;
        private IGossipService _gossipService;
        private ICrypto _crypto;

        public WalletController(ILogger<WalletController> logger, State state, PrivateState privateState, ITransactionValidator transactionValidator, IGossipService gossipService, ICrypto crypto){
            _logger = logger;
            _state = state;
            _privateState = privateState;
            _transactionValidator = transactionValidator;
            _gossipService = gossipService;
            _crypto = crypto;
        }

        [HttpGet("")]
        public IActionResult Wallet(){
            var pubKeyHash = _crypto.DoubleHash(_privateState.PublicKey);
            var balance = GetUnspentOutputs(pubKeyHash).Sum(x => x.Amount);

            return Ok(new
            {
                PubKeyHash = pubKeyHash,
                Balance = balance
            });
        }

        [HttpPost("send-money")]
        public async Task<ActionResult<Transaction>> SendMoney([FromBody] SendMoneyRequest request){
            if(!ModelState.IsValid)
                return BadRequest("Invalid request. Make sure To and Amount are provided");

            var pubKeyHash = _crypto.DoubleHash(_privateState.PublicKey);
            var unspentOutputs = GetUnspentOutputs(pubKeyHash);
            var unspentValue = unspentOutputs.Sum(x => x.Amount);
            if( unspentValue < request.Amount)
                return BadRequest($"You do not have {request.Amount} to spend. Current unspent balance: {unspentValue}");

            var outputsToSpend = new List<UnspentOutput>();
            while(outputsToSpend.Sum(x => x.Amount) < request.Amount){
                var lastItem = unspentOutputs.Last();
                outputsToSpend.Add(lastItem);
                unspentOutputs.Remove(lastItem);
            }

            // build inputs
            var inputs = outputsToSpend.Select(x => new Input()
            {
                TransactionId = x.TransactionId,
                OutputIndex = x.OutputIndex
            });

            // build outputs
            var outputs = new List<Output>();
            var valueBackToSelf = outputsToSpend.Sum(x => x.Amount) - request.Amount;
            
            if(valueBackToSelf > 0)
                outputs.Add(new Output() { PubKeyHash = pubKeyHash, Amount = valueBackToSelf });

            outputs.Add(new Output() { PubKeyHash = request.To, Amount = request.Amount });

            var transaction = new Transaction(inputs.ToList(), outputs);

            return await BuildTransaction(transaction);
        }

        private List<UnspentOutput> GetUnspentOutputs(string pubKeyHash){
            var unspentOutputs = new List<UnspentOutput>();
            var transactions = GetAllTransactionsWhereCoinReceived(pubKeyHash);
            foreach(var transaction in transactions){
                for (var i = 0; i < transaction.Outputs.Count; i++){
                    var output = transaction.Outputs.ElementAt(i);
                    if(output.PubKeyHash != pubKeyHash)
                        continue;

                    if(!TransactionOutputWasSpent(transaction.Id, i))
                        unspentOutputs.Add(new UnspentOutput() { 
                            OutputIndex = i, 
                            TransactionId = transaction.Id, 
                            Amount = output.Amount 
                        });
                }
            }

            return unspentOutputs;
        }

        private IEnumerable<Transaction> GetAllTransactionsWhereCoinReceived(string pubKeyHash){
            return _state.Blockchain.Blocks.Where(x => x.Transaction.Outputs.Any(y => y.PubKeyHash.Equals(pubKeyHash))).Select(x => x.Transaction);
        }

        private bool TransactionOutputWasSpent(string transactionId, int outputIndex){
            return _state.Blockchain.Blocks
                .SelectMany(x => x.Transaction.Inputs)
                .Any(x => x.TransactionId.Equals(transactionId) && x.OutputIndex.Equals(outputIndex));
        }

        private async Task<ActionResult<Transaction>> BuildTransaction(Transaction transaction){
            if(transaction?.Inputs == null 
                || transaction?.Outputs == null 
                || !transaction.Inputs.Any() 
                || !transaction.Outputs.Any())

                return BadRequest("Invalid transaction request (missing params)");

            decimal moneyIn = 0;
            decimal moneyOut = transaction.Outputs.Sum(x => x.Amount);
            foreach(var input in transaction.Inputs){
                // find the previous transaction referenced by new input
                var previousTransaction = _state.Blockchain.FindTransaction(input.TransactionId);
                if (previousTransaction == null)
                    return BadRequest($"Previous transaction {input.TransactionId} not found.");

                // find the output from the previous transaction referenced in input
                var output = previousTransaction.Outputs.ElementAt(input.OutputIndex);

                // verify the output address matches my public key
                if (output.PubKeyHash != _crypto.DoubleHash(_privateState.PublicKey))
                    return BadRequest("Public key mismatch from previous transaction output");
                
                // sign the proof to verify I own the private key associated with the public key
                input.Signature = _crypto.Sign(_crypto.Hash($"{input.TransactionId}{input.OutputIndex}{output.PubKeyHash}"), _privateState.PrivateKey);
                input.FullPubKey = _privateState.PublicKey;

                moneyIn += output.Amount;
            }

            // make sure inputs match outputs
            if(moneyIn != moneyOut)
                return BadRequest("Money in does not match money out.");

            // build the ID
            transaction.Id = _crypto.DoubleHash(transaction);

            if(!_transactionValidator.IsUnconfirmedTransactionValid(transaction, _state.Blockchain))
                return BadRequest("Transaction was invalid");

            _state.OutstandingTransactions.TryAdd(transaction.Id, transaction);

            await Task.WhenAll(_state.Peers.Select(x => _gossipService.ShareNewTransactionAsync(x.Value, transaction)));

            return transaction;
        }
    }
}