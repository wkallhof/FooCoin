using System;
using System.Collections.Generic;

namespace WadeCoin.Node
{
    public class PrivateState
    {
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public List<Uri> PeersToIgnore { get; set; } = new List<Uri>();
    }
}