using Moq;

namespace WadeCoin.Core.UnitTests
{
    public class FakeCrypto : Mock<ICrypto>
    {
        public FakeCrypto():base(){
            Setup(x => x.Hash(It.IsAny<string>())).Returns((string s)=> Hash(s));
            Setup(x => x.Hash(It.IsAny<IHashable>())).Returns((IHashable item) => Hash(item.GetHashMessage(Object)));
            Setup(x => x.DoubleHash(It.IsAny<string>())).Returns((string s)=> DoubleHash(s));
            Setup(x => x.DoubleHash(It.IsAny<IHashable>())).Returns((IHashable item) => DoubleHash(item.GetHashMessage(Object)));
            Setup(x => x.Sign(It.IsAny<string>(), It.IsAny<string>())).Returns((string m, string p) => Hash(m));
            Setup(x => x.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns((string m, string s, string p) => Hash(m).Equals(s));
        }

        private string Hash(string input)
            =>  input.GetHashCode().ToString();

        private string DoubleHash(string input) 
            => Hash(Hash(input));
    }
}