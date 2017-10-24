using System;
using System.IO;
using System.Text;

namespace Krypto
{
    class Program
    {
        static void Main(string[] args)
        {
            Arguments a = new Arguments();

            try // Process arguments
            {
                a.Parse(ref args);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            string encryptedFileExtention = ".krypto";
            Krypto k = new Krypto();

            if (a.T == "G")
            {
                File.WriteAllText(a.K, k.ExportKey(false));
                File.WriteAllText(a.P, k.ExportKey(true));
            }
            else if (a.T == "E")
            {
                byte[] rsaKey = Encoding.ASCII.GetBytes(File.ReadAllText(a.K));
                if (rsaKey.Length > 0) { k.ImportKey(ref rsaKey); }
                rsaKey = null;

                if (File.Exists(a.I)) // Encrypt single file
                {
                    k.Encrypt(a.I, Path.Combine(a.O, Path.GetFileName(a.I) + encryptedFileExtention));
                }
                else if (Directory.Exists(a.I)) // Encrypt multiple files in directory
                {
                    foreach (string i in Directory.GetFiles(a.I))
                    {
                        k.Encrypt(i, Path.Combine(a.O, Path.GetFileName(i) + encryptedFileExtention));
                    }
                }
            }
            else if (a.T == "D")
            {
                byte[] rsaKey = Encoding.ASCII.GetBytes(File.ReadAllText(a.P));
                if (rsaKey.Length > 0) { k.ImportKey(ref rsaKey); }
                rsaKey = null;

                if (File.Exists(a.I)) // Decrypt single file
                {
                    k.Decrypt(a.I, Path.Combine(a.O, Path.GetFileNameWithoutExtension(a.I)));
                }
                else if (Directory.Exists(a.I)) // Decrypt multiple files in directory
                {
                    foreach (string i in Directory.GetFiles(a.I, "*" + encryptedFileExtention))
                    {
                        k.Decrypt(i, Path.Combine(a.O, Path.GetFileNameWithoutExtension(i)));
                    }
                }
            }

            k.Clear();
        }
    }
}
