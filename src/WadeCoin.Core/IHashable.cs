namespace WadeCoin.Core
{
    public interface IHashable
    {
        string GetHashMessage(ICrypto crypto);
    }
}