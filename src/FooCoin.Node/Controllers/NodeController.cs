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

namespace FooCoin.Node.Controllers
{
    [Route("node")]
    [ApiController]
    public class NodeController : ControllerBase
    {
        private ILogger<NodeController> _logger;
        private State _state;
        private PrivateState _privateState;
        private IBlockchainValidator _blockchainValidator;
        private ITransactionValidator _transactionValidator;
        private IGossipService _gossipService;

        public NodeController(ILogger<NodeController> logger, State state, PrivateState privateState, IBlockchainValidator blockchainValidator, ITransactionValidator transactionValidator, IGossipService gossipService){
            _logger = logger;
            _state = state;
            _privateState = privateState;
            _blockchainValidator = blockchainValidator;
            _transactionValidator = transactionValidator;
            _gossipService = gossipService;
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok();

        [HttpGet("state")]
        public ActionResult<State> State() => _state;

        [HttpPost("peers")]
        public async Task<IActionResult> AddPeers([FromBody] List<string> peers){
            UpdatePeersToIgnoreList();

            if(!peers.Any(x => Uri.IsWellFormedUriString(x, UriKind.Absolute)))
                return BadRequest("Invalid peer URI format detected");

            var peerUrisToAdd = peers.Select(x => new Uri(x, UriKind.Absolute))
                .Where(x => !_privateState.PeersToIgnore.ContainsKey(x.ToString()) && !_state.Peers.ContainsKey(x.ToString()))
                .ToList();

            if(!peerUrisToAdd.Any())
                return Ok();

            peerUrisToAdd.ForEach(x => {
                if(_state.Peers.TryAdd(x.ToString(), x))
                    _logger.LogInformation($"Peer Added: {x}");
            });

            //TODO: consider fire and forget
            await Task.WhenAll(_state.Peers.Select(x => _gossipService.SharePeersAsync(x.Value, _state.Peers.ToList())));
            
            return Ok();
        }

        [HttpPost("transactions")]
        public async Task<IActionResult> AddTransaction([FromBody] Transaction transaction){
            UpdatePeersToIgnoreList();

            if(_transactionValidator.IsUnconfirmedTransactionValid(transaction, _state.Blockchain)
                 && !_state.OutstandingTransactions.ContainsKey(transaction.Id)){

                if(!_state.OutstandingTransactions.TryAdd(transaction.Id, transaction))
                    return BadRequest("Transaction Already added");

                _logger.LogInformation($"Incoming Transaction Added : " + transaction.Id);
                await Task.WhenAll(_state.Peers.Select(x => _gossipService.ShareNewTransactionAsync(x.Value, transaction)));
            }

            return Ok();
        }

        [HttpPost("blockchain")]
        public async Task<IActionResult> AddBlockchain([FromBody] Blockchain blockchain){
            UpdatePeersToIgnoreList();

            if(!_blockchainValidator.IsValid(blockchain))
                return BadRequest("Invalid Blockchain");

            if(blockchain.Blocks.Count > _state.Blockchain.Blocks.Count){
                _state.Blockchain = blockchain;
                _logger.LogInformation("Blockchain Copied From Peer");

                await Task.WhenAll(_state.Peers.Select(x => _gossipService.ShareBlockchainAsync(x.Value, blockchain)));
            }
            
            return Ok();
        }

        /// <summary>
        /// Based on the current request, this will add the address of *this* application
        /// to the PeersToIgnore List so that we don't end up gossiping with ourselves.
        /// </summary>
        private void UpdatePeersToIgnoreList(){
            var currentRequest = new Uri($"{Request.Scheme}://{Request.Host}");
            if(!_privateState.PeersToIgnore.ContainsKey(currentRequest.ToString()))
                _privateState.PeersToIgnore.TryAdd(currentRequest.ToString(), currentRequest);
        }
    }
}