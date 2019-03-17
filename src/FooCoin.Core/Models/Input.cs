namespace FooCoin.Core.Models
{
    public class Input
    {
        public string TransactionId { get; set; }
        public int OutputIndex { get; set; }

        public string FullPubKey {get;set;}
        public string Signature { get; set; }

        // when double hash of fullpubkey == output.address && Crypto.Verify(signature, transactionmessage, fullPubKey)
    }
}