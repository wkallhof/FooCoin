using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using WadeCoin.Core.Extensions;

namespace WadeCoin.Core
{
    public interface ICrypto
    {
        string Hash<T>(T item) where T : IHashable;
        string DoubleHash<T>(T item) where T : IHashable;
        string Hash(string input);
        string DoubleHash(string input);
        (string privateKey, string publicKey) GenerateKeys();
        string Sign(string message, string privateKey);
        bool ValidateSignature(string message, string signedMessage, string publicKey);
        string Encrypt(string message, string publicKey);
        string Decrypt(string encryptedMessage, string privateKey);
    }

    public class Crypto : ICrypto
    {
        public string Hash<T>(T item) where T : IHashable
        {
            return Hash(item.GetHashMessage(this));
        }

        public string DoubleHash<T>(T item) where T: IHashable
        {
            return Hash(Hash(item.GetHashMessage(this)));
        }

        public string Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var outputBytes = sha.ComputeHash(inputBytes);
                return ByteArrayToHexViaLookup32(outputBytes);
            }
        }

        public string DoubleHash(string input){
            return Hash(Hash(input));
        }

        public (string privateKey, string publicKey) GenerateKeys(){
            using(var rsa = new RSACryptoServiceProvider(2048)){
                return (rsa.ToKeyString(true), rsa.ToKeyString(false));
            }
        }

        public string Sign(string message, string privateKey){
            var dataToSign = Encoding.UTF8.GetBytes(message); 
   
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))  
            {  
                rsa.FromString(privateKey);
                var result = rsa.SignData(dataToSign, CryptoConfig.MapNameToOID("SHA256"));
                return Convert.ToBase64String(result);
            }  
        }

        public bool ValidateSignature(string message, string signedMessage, string publicKey){
            var dataToVerify = Convert.FromBase64String(signedMessage);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))  
            {  
                rsa.FromString(publicKey);
                return rsa.VerifyData(messageBytes, CryptoConfig.MapNameToOID("SHA256"), dataToVerify);
            }  
        }

        public string Encrypt(string message, string publicKey){
            var dataToEncrypt = Encoding.UTF8.GetBytes(message);  
   
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))  
            {  
                rsa.FromString(publicKey);   
                return Convert.ToBase64String(rsa.Encrypt(dataToEncrypt, false));
            }  
        }

        public string Decrypt(string encryptedMessage, string privateKey){
            // read the encrypted bytes from the file   
            var dataToDecrypt = Convert.FromBase64String(encryptedMessage);  
  
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))  
            {  
                // Set the private key of the algorithm   
                rsa.FromString(privateKey);  
                return Encoding.UTF8.GetString(rsa.Decrypt(dataToDecrypt, false));   
            }  
        }

        private readonly uint[] _lookup32 = CreateLookup32();

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s=i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }

        private string ByteArrayToHexViaLookup32(byte[] bytes)
        {
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2*i] = (char)val;
                result[2*i + 1] = (char) (val >> 16);
            }
            return new string(result);
        }
    }
}
