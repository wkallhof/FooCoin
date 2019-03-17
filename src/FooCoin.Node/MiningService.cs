using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FooCoin.Core;
using FooCoin.Core.Models;
using FooCoin.Core.Validation;
using Newtonsoft.Json;

namespace FooCoin.Node
{
    internal class MiningService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private NoOverlapTimer _timer;
        private State _state;
        private PrivateState _privateState;
        private IBlockValidator _blockValidator;
        private IBlockchainValidator _blockchainValidator;
        private ITransactionValidator _transactionValidator;
        private IGossipService _gossipService;
        private ICrypto _crypto;

        public MiningService(ILogger<MiningService> logger, State state, PrivateState privateState, IBlockValidator blockValidator, ITransactionValidator transactionValidator, IGossipService gossipService, IBlockchainValidator blockchainValidator, ICrypto crypto)
        {
            _logger = logger;
            _state = state;
            _privateState = privateState;
            _blockValidator = blockValidator;
            _transactionValidator = transactionValidator;
            _gossipService = gossipService;
            _blockchainValidator = blockchainValidator;
            _crypto = crypto;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Mining Service is starting.");
            _timer = new NoOverlapTimer(async() => await Mine(), TimeSpan.FromSeconds(6));
            _timer.Start();
            return Task.CompletedTask;
        }

        private async Task Mine()
        {
            if(!_state.OutstandingTransactions.Any())
                return;

            var lastBlock = _state.Blockchain.Blocks.LastOrDefault();
            
            if(lastBlock == null)
                return;
                
            var transaction = _state.OutstandingTransactions.First();

            if(!TransactionValid(transaction.Value))
                return;

            var block = new Block(lastBlock.Hash, transaction.Value);
            block.Difficulty = _state.Difficulty;
            block.Miner = "FooCoinMinder";
            block.UnixTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            block.Nonce = Guid.NewGuid().ToString();
            block.Hash = _crypto.Hash(block);

            while(!_blockValidator.HasValidHeader(block)){
                block.Nonce = Guid.NewGuid().ToString();
                block.UnixTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                block.Hash = _crypto.Hash(block);
            }

            if(!TransactionValid(transaction.Value))
                return;

            _logger.LogInformation("Block mined!");
            _state.OutstandingTransactions.TryRemove(transaction.Key, out var removedTransaction);
            _state.Blockchain.Blocks.Add(block);

            // if we've really goobered the blockchain, just reset it
            if(!_blockchainValidator.IsValid(_state.Blockchain)){
                _state.Blockchain = Blockchain.Initialize(_crypto, _privateState.PublicKey);
            }

            await Task.WhenAll(_state.Peers.Select(x => _gossipService.ShareBlockchainAsync(x.Value, _state.Blockchain)));
        }

        private bool TransactionValid(Transaction transaction){
            // validate this transaction is still valid
            if(!_transactionValidator.IsUnconfirmedTransactionValid(transaction, _state.Blockchain)){
                _state.OutstandingTransactions.TryRemove(transaction.Id, out var removedTransaction);
                return false;
            }

            return true;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");

            _timer?.Stop();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}