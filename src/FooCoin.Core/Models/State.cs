using System;
using System.Collections.Generic;

namespace FooCoin.Core.Models
{
    public class State
    {
        public BlockChain BlockChain { get; set; } = new BlockChain();
        public List<Transaction> OutstandingTransactions { get; set; } = new List<Transaction>();
        public List<Uri> Peers { get; set; } = new List<Uri>();
    }
}