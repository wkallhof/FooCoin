using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FooCoin.Core;
using FooCoin.Core.Models;

namespace FooCoin.Node
{
    public interface IGossipService
    {
        Task ShareNewTransactionAsync(Uri peer, Transaction transaction);
        Task SharePeersAsync(Uri peer, List<Uri> peers);
        Task ShareBlockChainAsync(Uri peer, BlockChain blockChain);
    }

    public class HttpGossipService : IGossipService
    {
        private readonly HttpClient _client;
        private readonly ILogger<HttpGossipService> _logger;
        private State _state;

        public HttpGossipService(HttpClient httpClient, ILogger<HttpGossipService> logger, State state){
            _logger = logger;
            _client = httpClient;
            _state = state;
        }

        public async Task ShareBlockChainAsync(Uri peer, BlockChain blockChain)
            => await Post("node/blockchain", peer, blockChain);

        public async Task ShareNewTransactionAsync(Uri peer, Transaction transaction)
            => await Post("node/transactions", peer, transaction);

        public async Task SharePeersAsync(Uri peer, List<Uri> peers)
            => await Post("node/peers", peer, peers.Select(x => x.ToString()).ToList());

        public async Task Post<T>(string path, Uri peer, T data)
        {
            var endpoint = new UriBuilder(peer);
            endpoint.Path += path;

            try
            {
                var response = await _client.PostAsJsonAsync(endpoint.Uri, data);

                if (!response.IsSuccessStatusCode)
                    _logger.LogInformation($"Unsuccessful Response from {endpoint.Uri}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error communicating with {endpoint.Uri}. Removing peer");
                _state.Peers.Remove(peer);
            }
        }

        public async Task<T> Get<T>(string path, Uri peer) where T : class
        {
            var endpoint = new UriBuilder(peer);
            endpoint.Path += path;

            try
            {
                var response = await _client.GetAsync(endpoint.Uri);

                if (!response.IsSuccessStatusCode){
                    _logger.LogInformation($"Unsuccessful Response from {endpoint.Uri}");
                    return null;
                }

                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error communicating with {endpoint.Uri}. Removing peer");
                _state.Peers.Remove(peer);
                return null;
            }
        }
    }
}