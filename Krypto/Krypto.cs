using System;
using System.Security.Cryptography;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Krypto
{
    public class Krypto
    {
        private int _keySize = 4096;
        private RSACryptoServiceProvider _rsa = new RSACryptoServiceProvider();

        public int KeySize
        {
            get { return _keySize; }
            set { _keySize = value; }
        }

        public Krypto()
        {
            GenerateKey();
        }

        public Krypto(ref byte[] keyXml)
        {
            ImportKey(ref keyXml);
        }

        public void GenerateKey()
        {
            _rsa = new RSACryptoServiceProvider(KeySize);
        }

        public void ImportKey(ref byte[] keyXml)
        {
            _rsa.FromXmlString(Encoding.ASCII.GetString(keyXml));
        }

        public string ExportKey(bool includePrivate)
        {
            return _rsa.ToXmlString(includePrivate);
        }

        public void Clear()
        {
            _rsa.PersistKeyInCsp = false;
            _rsa.Clear();
        }

        public void Encrypt(string inFile, string outFile)
        {
            // If parameters are bad throw exception
            if (!File.Exists(inFile) || !Directory.Exists(Path.GetDirectoryName(outFile)))
            {
                throw new ArgumentException("Bad parameters supplied to decrypt method");
            }

            //Create a new instance of the RijndaelManaged class and RSACryptoServiceProvider class.
            using (RijndaelManaged rm = new RijndaelManaged())
            {
                // Initialise RijndaelManaged class.
                rm.KeySize = 256;
                rm.BlockSize = 256;
                rm.Mode = CipherMode.CBC;
                rm.Padding = PaddingMode.ISO10126;
                rm.GenerateKey();
                rm.GenerateIV();

                // Encrypt the symmetric key with the asymmetric key
                byte[] EncryptedKey = _rsa.Encrypt(rm.Key, false);
                byte[] EncryptedIv = _rsa.Encrypt(rm.IV, false);

                // Create file-out stream
                using (FileStream fsOut = new FileStream(outFile, FileMode.Create, FileAccess.Write))
                {
                    byte[] lenKey = new byte[4];
                    byte[] lenIv = new byte[4];
                    int lkey = EncryptedKey.Length;
                    int liv = EncryptedIv.Length;
                    lenKey = BitConverter.GetBytes(lkey);
                    lenIv = BitConverter.GetBytes(liv);

                    // Write encrypted key header.
                    fsOut.Write(lenKey, 0, 4);
                    fsOut.Write(lenIv, 0, 4);
                    fsOut.Write(EncryptedKey, 0, lkey);
                    fsOut.Write(EncryptedIv, 0, liv);

                    // Read input stream and write into deflate > crypto > file-out stream.
                    using (FileStream fsIn = new FileStream(inFile, FileMode.Open, FileAccess.Read))
                    using (CryptoStream cs = new CryptoStream(fsOut, rm.CreateEncryptor(), CryptoStreamMode.Write))
                    using (DeflateStream ds = new DeflateStream(cs, CompressionMode.Compress))
                    {
                        int read;
                        byte[] buffer = new byte[4096];

                        // read into buffer and write loop
                        while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ds.Write(buffer, 0, read);
                        }

                        // Flush and close streams
                        ds.Flush();
                        cs.Flush();
                        fsOut.Flush();
                        fsIn.Close();
                        ds.Close();
                        cs.Close();
                        fsOut.Close();
                    }
                }

                // Clear RijndaelManaged encryption provider
                rm.Clear();
            }
        }

        public void Decrypt(string inFile, string outFile)
        {
            // If parameters are bad clear memory and throw exception
            if (!File.Exists(inFile) || !Directory.Exists(Path.GetDirectoryName(outFile)))
            {
                throw new ArgumentException("Bad parameters supplied to decrypt method");
            }

            using (FileStream fsIn = new FileStream(inFile, FileMode.Open, FileAccess.Read))
            {
                byte[] lenKey = new byte[4];
                byte[] lenIv = new byte[4];

                // Read encrypted key header.
                fsIn.Seek(0, SeekOrigin.Begin);
                fsIn.Seek(0, SeekOrigin.Begin);
                fsIn.Read(lenKey, 0, 3);
                fsIn.Seek(4, SeekOrigin.Begin);
                fsIn.Read(lenIv, 0, 3);

                // convert key and iv lengths into integer
                int lkey = BitConverter.ToInt32(lenKey, 0);
                int liv = BitConverter.ToInt32(lenIv, 0);

                // set content offset
                int startc = 8 + lkey + liv;

                // Read encrypted key and iv into byte array
                byte[] keyEncrypted = new byte[lkey];
                byte[] ivEncrypted = new byte[liv];

                fsIn.Seek(8, SeekOrigin.Begin);
                fsIn.Read(keyEncrypted, 0, lkey);
                fsIn.Seek(8 + lkey, SeekOrigin.Begin);
                fsIn.Read(ivEncrypted, 0, liv);

                // Initialise RijndaelManaged class and decrypt key.
                using (RijndaelManaged rm = new RijndaelManaged())
                {
                    // Set RijndaelManaged properties
                    rm.KeySize = 256;
                    rm.BlockSize = 256;
                    rm.Mode = CipherMode.CBC;
                    rm.Padding = PaddingMode.ISO10126;
                    rm.Key = _rsa.Decrypt(keyEncrypted, false);
                    rm.IV = _rsa.Decrypt(ivEncrypted, false);

                    // Force seek to offset of content
                    fsIn.Seek(startc, SeekOrigin.Begin);

                    // Read input stream and write into deflate > crypto > file-out stream.
                    using (FileStream fsOut = new FileStream(outFile, FileMode.Create, FileAccess.ReadWrite))
                    using (CryptoStream cs = new CryptoStream(fsIn, rm.CreateDecryptor(), CryptoStreamMode.Read))
                    using (DeflateStream ds = new DeflateStream(cs, CompressionMode.Decompress))
                    {
                        int read;
                        byte[] buffer = new byte[4096];

                        // read into buffer and write loop
                        while ((read = ds.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fsOut.Write(buffer, 0, read);
                        }

                        // Flush and close streams
                        ds.Flush();
                        cs.Flush();
                        fsOut.Flush();
                        fsIn.Close();
                        ds.Close();
                        cs.Close();
                        fsOut.Close();
                    }

                    // Clear RijndaelManaged encryption provider
                    rm.Clear();
                }
            }
        }
    }
}
