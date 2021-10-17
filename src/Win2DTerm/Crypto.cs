using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace Win2DTerm
{
    public class Crypto
    {
        // Key with 256 and IV with 16 length
        private string AES_Key = "M+3xQDLPWalRKK3U/JuabsJNnuEO91zRiOH5gjgOqck=";
        private string AES_IV = "15CV1/MOnVI3rY4wk4INBg==";
        private IBuffer m_iv = null;
        private CryptographicKey m_key;

        public Crypto(string csAES_Key = "")
        {
            IBuffer key = Convert.FromBase64String(AES_Key).AsBuffer();
            if (csAES_Key.Length > 0)
            {
                key = Convert.FromBase64String(csAES_Key).AsBuffer();
            }
            m_iv = Convert.FromBase64String(AES_IV).AsBuffer();
            SymmetricKeyAlgorithmProvider provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
            m_key = provider.CreateSymmetricKey(key);
        }

        public byte[] Encrypt(byte[] input)
        {
            IBuffer bufferMsg = CryptographicBuffer.ConvertStringToBinary(Encoding.ASCII.GetString(input), BinaryStringEncoding.Utf8);
            IBuffer bufferEncrypt = CryptographicEngine.Encrypt(m_key, bufferMsg, m_iv);
            return bufferEncrypt.ToArray();
        }

        public byte[] Decrypt(byte[] input)
        {
            IBuffer bufferDecrypt = CryptographicEngine.Decrypt(m_key, input.AsBuffer(), m_iv);
            return bufferDecrypt.ToArray();
        }

        public byte[] EncryptStream(byte[] input)
        {
            IBuffer bufferMsg = CryptographicBuffer.CreateFromByteArray(input);
            IBuffer bufferEncrypt = CryptographicEngine.Encrypt(m_key, bufferMsg, m_iv);
            return bufferEncrypt.ToArray();
        }

        public byte[] DecryptStream(byte[] input)
        {
            IBuffer bufferDecrypt = CryptographicEngine.Decrypt(m_key, input.AsBuffer(), m_iv);
            return bufferDecrypt.ToArray();
        }
    }
}
