using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WadeCoin.Core;
using WadeCoin.Core.Validation;
using WadeCoin.Core.Extensions;
using WadeCoin.Core.Models;

namespace WadeCoin.Node.Controllers
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

        public WalletController(ILogger<WalletController> logger, State state, PrivateState privateState, ITransactionValidator transactionValidator, IGossipService gossipService){
            _logger = logger;
            _state = state;
            _privateState = privateState;
            _transactionValidator = transactionValidator;
            _gossipService = gossipService;
        }

        [HttpGet("")]
        public IActionResult Wallet(){
            var pubKeyHash = Crypto.DoubleHash(_privateState.PublicKey);
            decimal balance = 0;
            var transactionsToMe = _state.BlockChain.Blocks.Where(x => x.Transaction.Outputs.Any(y => y.PubKeyHash.Equals(pubKeyHash))).Select(x => x.Transaction);
            foreach(var transaction in transactionsToMe){
                for (var i = 0; i < transaction.Outputs.Count; i++){
                    var output = transaction.Outputs.ElementAt(i);
                    if(output.PubKeyHash != pubKeyHash)
                        continue;

                    balance += output.Amount;
                    var moneySpent = _state.BlockChain.Blocks.FirstOrDefault(x => x.Transaction.Inputs.Any(y => y.TransactionId.Equals(transaction.Id) && y.OutputIndex.Equals(i))) != null;
                    if(moneySpent)
                        balance = balance - output.Amount;
                }
            }

            return Ok(new
            {
                PubKeyHash = pubKeyHash,
                Balance = balance
            });
        }

        [HttpPost("send-money")]
        public async Task<ActionResult<Transaction>> Transaction([FromBody] Transaction transaction){
            if(transaction?.Inputs == null 
                || transaction?.Outputs == null 
                || !transaction.Inputs.Any() 
                || !transaction.Outputs.Any())

                return BadRequest("Invalid transaction request (missing params)");

            decimal moneyIn = 0;
            decimal moneyOut = transaction.Outputs.Sum(x => x.Amount);
            foreach(var input in transaction.Inputs){
                // find the previous transaction referenced by new input
                var previousTransaction = _state.BlockChain.FindTransaction(input.TransactionId);
                if (previousTransaction == null)
                    return BadRequest($"Previous transaction {input.TransactionId} not found.");

                // find the output from the previous transaction referenced in input
                var output = previousTransaction.Outputs.ElementAt(input.OutputIndex);

                // verify the output address matches my public key
                if (output.PubKeyHash != Crypto.DoubleHash(_privateState.PublicKey))
                    return BadRequest("Public key mismatch from previous transaction output");
                
                // sign the proof to verify I own the private key associated with the public key
                input.Signature = Crypto.Sign(Crypto.Hash($"{input.TransactionId}{input.OutputIndex}{output.PubKeyHash}"), _privateState.PrivateKey);
                input.FullPubKey = _privateState.PublicKey;

                moneyIn += output.Amount;
            }

            // make sure inputs match outputs
            if(moneyIn != moneyOut)
                return BadRequest("Money in does not match money out.");

            // build the ID
            transaction.Id = Crypto.DoubleHash(transaction);

            if(!_transactionValidator.IsUncomfirmedTransactionValid(transaction, _state.BlockChain))
                return BadRequest("Transaction was invalid");

            _state.OutstandingTransactions.Add(transaction);

            await Task.WhenAll(_state.Peers.Select(x => _gossipService.ShareNewTransactionAsync(x, transaction)));

            return transaction;
        }
    }
}