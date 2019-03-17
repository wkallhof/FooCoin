using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FooCoin.Node.Wallet
{
    public class SendMoneyRequest
    {
        [BindRequired]
        public string To { get; set; }

        [BindRequired]
        public decimal Amount { get; set; }
    }
}