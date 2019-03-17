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
        private IBlockChainValidator _blockChainValidator;
        private ITransactionValidator _transactionValidator;
        private IGossipService _gossipService;

        public NodeController(ILogger<NodeController> logger, State state, PrivateState privateState, IBlockChainValidator blockChainValidator, ITransactionValidator transactionValidator, IGossipService gossipService){
            _logger = logger;
            _state = state;
            _privateState = privateState;
            _blockChainValidator = blockChainValidator;
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
                .Where(x => !_privateState.PeersToIgnore.Contains(x) && !_state.Peers.Contains(x))
                .ToList();

            if(!peerUrisToAdd.Any())
                return Ok();

            _state.Peers.AddRange(peerUrisToAdd);
            _state.Peers = _state.Peers.DistinctBy(x => x.ToString()).ToList();
            peerUrisToAdd.ForEach(x => _logger.LogInformation($"Peer Added: {x}"));

            //TODO: consider fire and forget
            await Task.WhenAll(_state.Peers.Select(x => _gossipService.SharePeersAsync(x, _state.Peers)));
            
            return Ok();
        }

        [HttpPost("transactions")]
        public async Task<IActionResult> AddTransaction([FromBody] Transaction transaction){
            UpdatePeersToIgnoreList();

            if(_transactionValidator.IsUnconfirmedTransactionValid(transaction, _state.BlockChain)
                 && !_state.OutstandingTransactions.Any(x => x.Id.Equals(transaction.Id))){

                _state.OutstandingTransactions.Add(transaction);
                _logger.LogInformation($"Transaction Added : " + transaction.Id);

                await Task.WhenAll(_state.Peers.Select(x => _gossipService.ShareNewTransactionAsync(x, transaction)));
            }

            return Ok();
        }

        [HttpPost("blockchain")]
        public async Task<IActionResult> AddBlockChain([FromBody] BlockChain blockChain){
            UpdatePeersToIgnoreList();

            if(!_blockChainValidator.IsValid(blockChain))
                return BadRequest("Invalid Blockchain");

            if(blockChain.Blocks.Count > _state.BlockChain.Blocks.Count){
                _state.BlockChain = blockChain;
                _logger.LogInformation("BlockChain Copied From Peer");

                await Task.WhenAll(_state.Peers.Select(x => _gossipService.ShareBlockChainAsync(x, blockChain)));
            }
            
            return Ok();
        }

        /// <summary>
        /// Based on the current request, this will add the address of *this* application
        /// to the PeersToIgnore List so that we don't end up gossiping with ourselves.
        /// </summary>
        private void UpdatePeersToIgnoreList(){
            var currentRequest = $"{Request.Scheme}://{Request.Host}";
            _privateState.PeersToIgnore.Add(new Uri(currentRequest));
            _privateState.PeersToIgnore = _privateState.PeersToIgnore.Distinct().ToList();
        }
    }
}