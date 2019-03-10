using System.Collections.Generic;
using System.Linq;

namespace WadeCoin.Core.Models
{
    public class BlockChain
    {
        public List<Block> Blocks { get; set; } = new List<Block>();

        public Transaction FindTransaction(string id){
            var block = Blocks.FirstOrDefault(x => x.Transaction.Id.Equals(id));
            return block == null ? null : block.Transaction;
        }

        public static BlockChain Initialize(string publicKey){
            var firstTransaction = new Transaction(new List<Input>(), new List<Output>(){
                new Output(){ Amount = 500000, PubKeyHash = Crypto.DoubleHash(publicKey) }
            });
            firstTransaction.Id = Crypto.Hash(firstTransaction);

            var blockChain = new BlockChain();
            blockChain.Blocks.Add(new Block(null, firstTransaction));
            return blockChain;
        }
    }
}