using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Konfig
{
    class Program
    {
        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;
            var connectionStrings = ConfigurationManager.ConnectionStrings;
            SqlKonfig konf = new SqlKonfig();

            foreach (ConnectionStringSettings setting in connectionStrings)
            {
                var csb = new SqlConnectionStringBuilder(setting.ConnectionString);
                csb.TrustServerCertificate = true;
                csb.Encrypt = true;

                foreach (string key in appSettings.AllKeys)
                {
                    konf.Load(appSettings[key], csb.DataSource);
                }

                using (SqlConnection connect = new SqlConnection(csb.ConnectionString))
                using (SqlCommand command = new SqlCommand())
                {
                    string create = "IF (SELECT OBJECT_ID(N'" + konf.Results.TableName + "')) IS NULL "
                        + "CREATE TABLE " + konf.Results.TableName + "([class] VARCHAR(256),[property] VARCHAR(256),[value] SQL_VARIANT)";
                    string delete = @"DELETE FROM " + konf.Results.TableName + ";";

                    connect.Open();

                    try
                    {
                        command.Connection = connect;
                        command.CommandText = create;
                        command.ExecuteNonQuery();
                        command.CommandText = delete;
                        command.ExecuteNonQuery();

                        using (SqlBulkCopy bulk = new SqlBulkCopy(connect))
                        {
                            bulk.DestinationTableName = konf.Results.TableName;
                            bulk.WriteToServer(konf.Results);
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
