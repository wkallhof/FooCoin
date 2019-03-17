using System.Collections.Generic;
using System.Linq;

namespace FooCoin.Core.Models
{
    public class Blockchain
    {
        public List<Block> Blocks { get; set; } = new List<Block>();

        public Transaction FindTransaction(string id){
            var block = Blocks.FirstOrDefault(x => x.Transaction.Id.Equals(id));
            return block == null ? null : block.Transaction;
        }

        public static Blockchain Initialize(ICrypto crypto, string publicKey){
            var firstTransaction = new Transaction(new List<Input>(), new List<Output>(){
                new Output(){ Amount = 500000, PubKeyHash = crypto.DoubleHash(publicKey) }
            });
            firstTransaction.Id = crypto.Hash(firstTransaction);
            var block = new Block(null, firstTransaction);
            block.Hash = crypto.Hash(block);

            var blockchain = new Blockchain();
            blockchain.Blocks.Add(block);
            return blockchain;
        }
    }
}