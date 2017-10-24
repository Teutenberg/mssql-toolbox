using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Knowa
{
    class Program
    {
        static void Main(string[] args)
        {
            Knowa admins = new Knowa();

            BulkCopyDataTable(admins.Results);

            Console.ReadKey();
        }

        static void BulkCopyDataTable(DataTable dt)
        {
            var appSettings = ConfigurationManager.AppSettings;
            var connectionStrings = ConfigurationManager.ConnectionStrings;

            foreach (ConnectionStringSettings setting in connectionStrings)
            {
                var csb = new SqlConnectionStringBuilder(setting.ConnectionString);
                csb.TrustServerCertificate = true;
                csb.Encrypt = true;

                using (SqlConnection connect = new SqlConnection(csb.ConnectionString))
                using (SqlCommand command = new SqlCommand())
                {
                    string create = "IF (SELECT OBJECT_ID(N'" + dt.TableName + "')) IS NULL "
                        + "CREATE TABLE " + dt.TableName + "([server] VARCHAR(128),[object_type] VARCHAR(128),[object_sid] VARCHAR(128),[account_name] VARCHAR(256),[distinguished_name] VARCHAR(MAX),[status] VARCHAR(10), [source] VARCHAR(256))";
                    string delete = @"DELETE FROM " + dt.TableName + ";";

                    try
                    {
                        connect.Open();
                        command.Connection = connect;
                        command.CommandText = create;
                        command.ExecuteNonQuery();
                        command.CommandText = delete;
                        command.ExecuteNonQuery();

                        using (SqlBulkCopy bulk = new SqlBulkCopy(connect))
                        {
                            bulk.DestinationTableName = dt.TableName;
                            bulk.WriteToServer(dt);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        connect.Close();
                    }
                }
            }
        }
    }
}
