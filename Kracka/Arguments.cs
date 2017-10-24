using System;
using System.IO;

namespace Kracka
{
    class Arguments
    {
        private string s = String.Empty;
        private string i = String.Empty;

        public string S { get; set; }
        public string I { get; set; }

        public void Parse(ref string[] args)
        {
            int _s = -1;
            int _i = -1;

            _s = Array.IndexOf(args, "-s");
            _i = Array.IndexOf(args, "-i");

            // Check if user needs help.
            if (Array.IndexOf(args, "--help") >= 0 || Array.IndexOf(args, "/?") >= 0)
            {
                Help();
                throw new ArgumentException("User requested help");
            }

            // Update public strings
            if (_s >= 0) { this.S = args[_s + 1]; }
            if (_i >= 0) { this.I = args[_i + 1]; }

            // Check argument combinations 
            if (!String.IsNullOrEmpty(this.S) && File.Exists(this.I))
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
            Console.WriteLine("KRACKA -s [file] -i [file]");
            Console.WriteLine("-s \"MSSQL Server list\"");
            Console.WriteLine("-i \"Password dictionary\"");
            Console.WriteLine("");
            Console.WriteLine("EXAMPLES:");
            Console.WriteLine("KRACKA -s \"C:\\Temp\\server.txt\" -i \"C:\\Temp\\password.txt\"");
        }
    }
}
