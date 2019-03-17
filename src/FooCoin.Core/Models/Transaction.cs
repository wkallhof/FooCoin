using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace FooCoin.Core.Models
{
    public class Transaction : IHashable
    {
        public string Id { get; set; }

        public List<Input> Inputs { get; }
        public List<Output> Outputs { get; }

        public Transaction(List<Input> inputs, List<Output> outputs){
            Inputs = inputs;
            Outputs = outputs;
        }

        public string GetHashMessage(ICrypto crypto)
        {
            var inputs = string.Join(",", Inputs.Select(x => $"{x.TransactionId}:{x.OutputIndex}:{x.FullPubKey}{x.Signature}"));
            var outputs = string.Join(",", Outputs.Select(x => $"{x.PubKeyHash}:{x.Amount}"));
            return $"{inputs}:{outputs}";
        }
    }
}