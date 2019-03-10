using System;

namespace WadeCoin.Core.Models
{
    public class Block : IHashable
    {

        //Header : PrevHash + Unix + Diff + Nonce + Hash(Transaction)

        public string PreviousBlockHash { get; set; }
        public long UnixTimeStamp { get; set; }
        public int Difficulty { get; set; }
        public string Nonce { get; set; }
        public Transaction Transaction { get; set; }

        public string Hash { get; set; }
        public string Miner { get; set; }
        
        public Block(string previousBlockHash, Transaction transaction){
            if(transaction == null)
                throw new ArgumentException("Transaction needed when creating new Block");

            Transaction = transaction;
            PreviousBlockHash = previousBlockHash ?? string.Empty;
        }

        public string GetHashMessage()
        {
            return $"{PreviousBlockHash}{UnixTimeStamp}{Difficulty}{Nonce}{Crypto.Hash(Transaction)}";
        }
    }
}