namespace FooCoin.Core
{
    public interface IHashable
    {
        string GetHashMessage(ICrypto crypto);
    }
}