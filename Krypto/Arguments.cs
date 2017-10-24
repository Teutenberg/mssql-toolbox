using System;
using System.IO;

namespace Krypto
{
    class Arguments
    {
        private string t = String.Empty;
        private string i = String.Empty;
        private string o = String.Empty;
        private string k = String.Empty;
        private string p = String.Empty;

        public string T { get; set; }
        public string I { get; set; }
        public string O { get; set; }
        public string K { get; set; }
        public string P { get; set; }

        public void Parse(ref string[] args)
        {
            int _t = -1;
            int _i = -1;
            int _o = -1;
            int _k = -1;
            int _p = -1;

            _t = Array.IndexOf(args, "-t");
            _i = Array.IndexOf(args, "-i");
            _o = Array.IndexOf(args, "-o");
            _k = Array.IndexOf(args, "-k");
            _p = Array.IndexOf(args, "-p");

            // Check if user needs help.
            if (Array.IndexOf(args, "--help") >= 0 || Array.IndexOf(args, "/?") >= 0)
            {
                Help();
                throw new ArgumentException("User requested help");
            }

            // Update public strings
            if (_t >= 0) { this.T = args[_t + 1]; }
            if (_i >= 0) { this.I = args[_i + 1]; }
            if (_o >= 0) { this.O = args[_o + 1]; }
            if (_k >= 0) { this.K = args[_k + 1]; }
            if (_p >= 0) { this.P = args[_p + 1]; }

            // Check argument combinations 
            if (this.T == "E" && File.Exists(this.I) && Directory.Exists(this.O) && File.Exists(this.K))
            {
                return;
            }
            else if (this.T == "D" && File.Exists(this.I) && Directory.Exists(this.O) && File.Exists(this.P))
            {
                return;
            }
            else if (this.T == "G" && !String.IsNullOrEmpty(this.K) && !String.IsNullOrEmpty(this.K))
            {
                return;
            }
            else
            {
                Help();
                throw new ArgumentException("Argument combination mismatch.");
            }
        }

        public void Help()
        {
            Console.WriteLine("KRYPTO -t [E|D] -i [file|directory] -o [directory] -k [file] -p [file]");
            Console.WriteLine("-t Flag");
            Console.WriteLine("\tG = Generate Keys");
            Console.WriteLine("\tE = Encrypt File(s)");
            Console.WriteLine("\tD = Decrypt File(s)");
            Console.WriteLine("-i \"Input file or directory\"");
            Console.WriteLine("-o \"Ouput directory\"");
            Console.WriteLine("-k \"RSA public key xml file\"");
            Console.WriteLine("\tIf existing file, use to encrypt the symmetric key.");
            Console.WriteLine("\tIf not existing file, generate new key and export public key.");
            Console.WriteLine("-p \"RSA private key xml file\"");
            Console.WriteLine("\tIf existing file, use to decrypt the symmetric key.");
            Console.WriteLine("\tIf not existing file, generate new key and export private key.");
            Console.WriteLine("\t *Note* The exported private key file should be kept secure.");
            Console.WriteLine("");
            Console.WriteLine("EXAMPLES:");
            Console.WriteLine("Generate Keys:");
            Console.WriteLine("KRYPTO -k \"C:\\Temp\\public.key\" -p \"C:\\Temp\\private.key\"");
            Console.WriteLine("Encrypt file:");
            Console.WriteLine("KRYPTO -t E -i \"C:\\Temp\\input.txt\" -o \"C:\\Temp\\\" -k \"C:\\Temp\\public.key\"");
            Console.WriteLine("Decrypt file:");
            Console.WriteLine("KRYPTO -t D -i \"C:\\Temp\\input.txt.krypto\" -o \"C:\\Temp\\\" -p \"C:\\Temp\\private.key\"");
        }
    }
}
