namespace FooCoin.Node.Wallet
{
    public class UnspentOutput
    {
        public int OutputIndex { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
    }
}