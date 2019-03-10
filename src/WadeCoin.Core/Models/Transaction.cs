using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace WadeCoin.Core.Models
{
    public class Transaction : IHashable
    {
        public string Id { get; set; }

        public List<Input> Inputs;
        public List<Output> Outputs;

        public Transaction(List<Input> inputs, List<Output> outputs){
            Inputs = inputs;
            Outputs = outputs;
        }

        public string GetHashMessage()
        {
            var inputs = string.Join(",", Inputs.Select(x => $"{x.TransactionId}:{x.OutputIndex}:{x.FullPubKey}{x.Signature}"));
            var outputs = string.Join(",", Outputs.Select(x => $"{x.PubKeyHash}:{x.Amount}"));
            return $"{inputs}:{outputs}";
        }
    }
}