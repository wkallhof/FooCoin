using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;

namespace FooCoin.Core.Extensions
{
    internal static class RSAKeyExtensions
    {

        public static string ToKeyString(this RSA rsa, bool includePrivateParameters)
        {
            RSAParameters parameters = rsa.ExportParameters(includePrivateParameters);

            var xmlStringResult = string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
                  parameters.Modulus != null ? Convert.ToBase64String(parameters.Modulus) : null,
                  parameters.Exponent != null ? Convert.ToBase64String(parameters.Exponent) : null,
                  parameters.P != null ? Convert.ToBase64String(parameters.P) : null,
                  parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null,
                  parameters.DP != null ? Convert.ToBase64String(parameters.DP) : null,
                  parameters.DQ != null ? Convert.ToBase64String(parameters.DQ) : null,
                  parameters.InverseQ != null ? Convert.ToBase64String(parameters.InverseQ) : null,
                  parameters.D != null ? Convert.ToBase64String(parameters.D) : null);

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(xmlStringResult));
        }

        public static void FromString(this RSA rsa, string encodedKeyString)  
        {
            var xmlString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedKeyString));
            RSAParameters parameters = new RSAParameters();  
            XmlDocument xmlDoc = new XmlDocument();  
            xmlDoc.LoadXml(xmlString);  
            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))  
            {  
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)  
                {  
                    switch (node.Name)  
                    {  
                        case "Modulus": parameters.Modulus = ConvertXmlToByteArray(node); break;  
                        case "Exponent": parameters.Exponent = ConvertXmlToByteArray(node); break;  
                        case "P": parameters.P = ConvertXmlToByteArray(node); break;  
                        case "Q": parameters.Q = ConvertXmlToByteArray(node); break;  
                        case "DP": parameters.DP = ConvertXmlToByteArray(node); break;  
                        case "DQ": parameters.DQ = ConvertXmlToByteArray(node); break;  
                        case "InverseQ": parameters.InverseQ = ConvertXmlToByteArray(node); break;  
                        case "D": parameters.D = ConvertXmlToByteArray(node); break;  
                    }  
                }  
            }  
            else  
            {  
                throw new Exception("Invalid XML RSA key.");  
            }  
  
            rsa.ImportParameters(parameters);  
        }

        private static byte[] ConvertXmlToByteArray(XmlNode node){
            if(string.IsNullOrEmpty(node.InnerText))
                return null;

            return Convert.FromBase64String(node.InnerText);
        }
    }
}
