using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace Naga
{
    class Crypto
    {
        public static byte[] ECDHKeyExchange(Uri URL, byte[] PSK, string Endpoint = "")
        {
            byte[] key = default(byte[]);

            using (ECDiffieHellmanCng AsymAlgo = new ECDiffieHellmanCng())
            {
                byte[] encryptedPublicKey = Encrypt(PSK, Encoding.UTF8.GetBytes(AsymAlgo.PublicKey.ToXmlString()));
                byte[] r = Comms.HttpPost(URL, Endpoint, encryptedPublicKey);

                string decryptedPeerPublicKey = Encoding.UTF8.GetString(Decrypt(PSK, r));
                ECDiffieHellmanCngPublicKey peerPublicKey = ECDiffieHellmanCngPublicKey.FromXmlString(decryptedPeerPublicKey);
                key = AsymAlgo.DeriveKeyMaterial(peerPublicKey);
            }
            return key;
        }

        public static byte[] AesDecrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.KeySize = 256;
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream decryptedData = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(decryptedData, decryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                        cryptoStream.FlushFinalBlock();
                        return decryptedData.ToArray();
                    }
                }
            }
        }
        public static byte[] AesEncrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.KeySize = 256;
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream encryptedData = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(encryptedData, decryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data, 0, data.Length);
                        cryptoStream.FlushFinalBlock();
                        return encryptedData.ToArray();
                    }
                }
            }
        }
        public static byte[] Encrypt(byte[] key, byte[] data)
        {
            IEnumerable<byte> blob = default(byte[]);

            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] iv = new byte[16];
                rng.GetBytes(iv);

                byte[] encryptedData = AesEncrypt(data, key, iv);

                using (HMACSHA256 hmacsha256 = new HMACSHA256(key))
                {
                    byte[] ivEncData = iv.Concat(encryptedData).ToArray();
                    byte[] hmac = hmacsha256.ComputeHash(ivEncData);
                    blob = ivEncData.Concat(hmac);
                }
            }
            return blob.ToArray();
        }
        public static byte[] Decrypt(byte[] key, byte[] data)
        {
            byte[] decryptedData = default(byte[]);

            byte[] iv = new byte[16];
            byte[] ciphertext = new byte[(data.Length - 32) - 16];
            byte[] hmac = new byte[32];

            Array.Copy(data, iv, 16);
            Array.Copy(data, data.Length - 32, hmac, 0, 32);
            Array.Copy(data, 16, ciphertext, 0, (data.Length - 32) - 16);

            using (HMACSHA256 hmacsha256 = new HMACSHA256(key))
            {
                byte[] computedHash = hmacsha256.ComputeHash(iv.Concat(ciphertext).ToArray());
                for (int i = 0; i < hmac.Length; i++)
                {
                    if (computedHash[i] != hmac[i])
                    {
                        Console.WriteLine("Invalid HMAC: {0}", i);
                        return decryptedData;
                    }
                }
                decryptedData = AesDecrypt(ciphertext, key, iv);
            }
            return decryptedData;
        }
    }
}
