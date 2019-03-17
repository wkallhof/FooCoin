using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FooCoin.Core.Models
{
    public class State
    {
        public Blockchain Blockchain { get; set; } = new Blockchain();
        public ConcurrentDictionary<string, Transaction> OutstandingTransactions { get; set; } = new ConcurrentDictionary<string, Transaction>();
        public ConcurrentDictionary<string, Uri> Peers { get; set; } = new ConcurrentDictionary<string, Uri>();
        public int Difficulty = 5;
    }
}