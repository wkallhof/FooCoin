using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FooCoin.Node
{
    public class PrivateState
    {
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public ConcurrentDictionary<string, Uri> PeersToIgnore { get; set; } = new ConcurrentDictionary<string, Uri>();
    }
}