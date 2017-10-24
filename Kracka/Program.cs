using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Kracka
{
    class Program
    {
        private static readonly byte[] sha1Head = { 0x01, 0x00 };
        private static readonly byte[] sha512Head = { 0x02, 0x00 };
        private static readonly string tsql = @"SELECT @@SERVERNAME AS [server]
                                                ,[name]
                                                ,SUBSTRING([password_hash], 0, 3) AS [head]
                                                ,SUBSTRING([password_hash], 3, 4) AS [salt]
                                                ,SUBSTRING([password_hash], 7, DATALENGTH([password_hash]) -6) [hash]
                                                FROM sys.sql_logins WHERE[is_disabled] = 0";
        static void Main(string[] args)
        {
            /* Load Arguments */
            Arguments a = new Arguments();

            try
            {
                a.Parse(ref args);
            }
            catch
            {
                a.Help();
                return;
            }

            DataTable dt = new DataTable();
            /* Load SQL hashes into datatable */
            foreach (string s in File.ReadAllLines(a.S))
            {
                SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder();
                sb.DataSource = s;
                sb.InitialCatalog = "master";
                sb.IntegratedSecurity = true;
                sb.Encrypt = true;
                sb.TrustServerCertificate = true;

                using (SqlConnection con = new SqlConnection(sb.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(tsql, con))
                {
                    try
                    {
                        con.Open();
                        dt.Load(cmd.ExecuteReader());
                    }
                    catch
                    {
                        return;
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }

            Console.WriteLine("{0,-20} {1,-30} {2,-30}\n", "[Server]", "[Login]", "[Password]");
            /* Read password dictionary and generate hashes to compare */
            foreach (DataRow dr in dt.Rows)
            {
                string sqlServer = (string)dr["server"];
                string sqlLogin = (string)dr["name"];
                byte[] sqlHead = (byte[])dr["head"];
                byte[] sqlSalt = (byte[])dr["salt"];
                byte[] sqlHash = (byte[])dr["hash"];
                string password = sqlLogin;
                SHA1 sha1 = new SHA1Managed();
                SHA512 sha2 = new SHA512Managed();

                byte[] loginHash = Encoding.Unicode.GetBytes(password);
                byte[] loginHashSalt = new byte[loginHash.Length + sqlSalt.Length];

                Array.Copy(loginHash, 0, loginHashSalt, 0, loginHash.Length);
                Array.Copy(sqlSalt, 0, loginHashSalt, loginHash.Length, sqlSalt.Length);

                if (ByteArrayCompare(sqlHead, sha512Head))
                {
                    byte[] sha2Hash = sha2.ComputeHash(loginHashSalt);

                    if (ByteArrayCompare(sha2Hash, sqlHash))
                    {
                        Console.WriteLine("{0,-20} {1,-30} {2,-30}", sqlServer, sqlLogin, password);
                        //Console.WriteLine("Server: {0}\tLogin: {1}\tPassword: {2}", sqlServer, sqlLogin, password);
                    }
                }
                else if (ByteArrayCompare(sqlHead, sha1Head))
                {
                    byte[] sha1Hash = sha1.ComputeHash(loginHashSalt);

                    if (ByteArrayCompare(sha1Hash, sqlHash))
                    {
                        Console.WriteLine("{0,-20} {1,-30} {2,-30}", sqlServer, sqlLogin, password);
                        //Console.WriteLine("Server: {0}\tLogin: {1}\tPassword: {2}", sqlServer, sqlLogin, password);
                    }
                }

                using (FileStream fs = new FileStream(a.I, FileMode.Open, FileAccess.Read))
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true, 128))
                {
                    while ((password = sr.ReadLine()) != null)
                    {
                        if (password == sqlLogin)
                        {
                            continue;
                        }

                        byte[] passHash = Encoding.Unicode.GetBytes(password);
                        byte[] passHashSalt = new byte[passHash.Length + sqlSalt.Length];

                        Array.Copy(passHash, 0, passHashSalt, 0, passHash.Length);
                        Array.Copy(sqlSalt, 0, passHashSalt, passHash.Length, sqlSalt.Length);

                        if (ByteArrayCompare(sqlHead, sha512Head))
                        {
                            byte[] sha2Hash = sha2.ComputeHash(passHashSalt);

                            if (ByteArrayCompare(sha2Hash, sqlHash))
                            {
                                Console.WriteLine("{0,-20} {1,-30} {2,-30}", sqlServer, sqlLogin, password);
                                //Console.WriteLine("Server: {0}\tLogin: {1}\tPassword: {2}", sqlServer, sqlLogin, password);
                            }
                        }
                        else if (ByteArrayCompare(sqlHead, sha1Head))
                        {
                            byte[] sha1Hash = sha1.ComputeHash(passHashSalt);

                            if (ByteArrayCompare(sha1Hash, sqlHash))
                            {
                                Console.WriteLine("{0,-20} {1,-30} {2,-30}", sqlServer, sqlLogin, password);
                                //Console.WriteLine("Server: {0}\tLogin: {1}\tPassword: {2}", sqlServer, sqlLogin, password);
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Total Password(s) Recovered = {0}", dt.Rows.Count);
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        public static bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            if (b1 == b2)
            {
                return true;
            }
            if ((b1 != null) && (b2 != null))
            {
                if (b1.Length != b2.Length)
                {
                    return false;
                }
                for (int i = 0; i <b1.Length; i++)
                {
                    if (b1[i] != b2[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
