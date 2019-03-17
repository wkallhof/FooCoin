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
        private const int DIFFICULTY = 4;
        private readonly ILogger _logger;
        private NoOverlapTimer _timer;
        private State _state;
        private PrivateState _privateState;
        private IBlockValidator _blockValidator;
        private IBlockChainValidator _blockChainValidator;
        private ITransactionValidator _transactionValidator;
        private IGossipService _gossipService;
        private ICrypto _crypto;

        public MiningService(ILogger<MiningService> logger, State state, PrivateState privateState, IBlockValidator blockValidator, ITransactionValidator transactionValidator, IGossipService gossipService, IBlockChainValidator blockChainValidator, ICrypto crypto)
        {
            _logger = logger;
            _state = state;
            _privateState = privateState;
            _blockValidator = blockValidator;
            _transactionValidator = transactionValidator;
            _gossipService = gossipService;
            _blockChainValidator = blockChainValidator;
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

            var lastBlock = _state.BlockChain.Blocks.LastOrDefault();
            
            if(lastBlock == null)
                return;
                
            var transaction = _state.OutstandingTransactions.First();

            if(!TransactionValid(transaction))
                return;

            var block = new Block(lastBlock.Hash, transaction);
            block.Difficulty = DIFFICULTY;
            block.Miner = "FooCoinMinder";
            block.UnixTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            block.Nonce = Guid.NewGuid().ToString();
            block.Hash = _crypto.Hash(block);

            while(!_blockValidator.HasValidHeader(block)){
                block.Nonce = Guid.NewGuid().ToString();
                block.UnixTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                block.Hash = _crypto.Hash(block);
            }

            if(!TransactionValid(transaction))
                return;

            _logger.LogInformation("Block mined!");
            _state.OutstandingTransactions.Remove(transaction);
            _state.BlockChain.Blocks.Add(block);

            // if we've really goobered the blockchain, just reset it
            if(!_blockChainValidator.IsValid(_state.BlockChain)){
                _state.BlockChain = BlockChain.Initialize(_crypto, _privateState.PublicKey);
            }

            await Task.WhenAll(_state.Peers.Select(x => _gossipService.ShareBlockChainAsync(x, _state.BlockChain)));
        }

        private bool TransactionValid(Transaction transaction){
            // validate this transaction is still valid
            if(!_transactionValidator.IsUnconfirmedTransactionValid(transaction, _state.BlockChain)){
                _state.OutstandingTransactions.Remove(transaction);
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