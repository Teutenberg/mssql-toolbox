using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.DirectoryServices;
using System.Security.Principal;

namespace Knowa
{
    class Knowa
    {
        private DataTable _results = new DataTable();
        private List<string[]> _localAdmins = new List<string[]>();
        private List<string> _adGroups = new List<string>();
        private List<string[]> _persons = new List<string[]>();
        private string _server = Environment.MachineName;

        public DataTable Results { get { return _results; } set { _results = value; } }
        private List<string[]> LocalAdmins { get { return _localAdmins; } set { _localAdmins = value; } }
        private List<string> AdGroups { get { return _adGroups; } set { _adGroups = value; } }
        private List<string[]> Persons { get { return _persons; } set { _persons = value; } }
        private string Server { get { return _server; } set { _server = value; } }

        enum DC : int {
            SERVER,
            OBJECT_TYPE,
            OBJECT_SID,
            SAM_ACCOUNT,
            DISTINGUISHED_NAME,
            STATUS,
            SOURCE
        }

        private void Init()
        {
            _results.TableName = "dbo.knowa_results";
            _results.Columns.Add(new DataColumn("server", System.Type.GetType("System.String")));
            _results.Columns.Add(new DataColumn("object_type", System.Type.GetType("System.String")));
            _results.Columns.Add(new DataColumn("object_sid", System.Type.GetType("System.String")));
            _results.Columns.Add(new DataColumn("sam_account", System.Type.GetType("System.String")));
            _results.Columns.Add(new DataColumn("distinguished_name", System.Type.GetType("System.String")));
            _results.Columns.Add(new DataColumn("status", System.Type.GetType("System.String")));
            _results.Columns.Add(new DataColumn("source", System.Type.GetType("System.String")));
        }

        public Knowa()
        {
            Init();
            Load();
        }

        public Knowa(string server)
        {
            Server = server;
            Init();
            Load();
        }

        public void Load()
        {
            GetLocalAdminsistrators(Server);

            foreach (string[] admin in LocalAdmins)
            {
                string account = admin[(int)DC.SAM_ACCOUNT];
                string type = admin[(int)DC.OBJECT_TYPE];

                if (type == "localuser" || type == "localgroup")
                {
                    Persons.Add(admin);
                }
                else if (type == "aduser")
                {
                    LoadAdUserDetails(account, Server);
                }
                else if (type == "adgroup")
                {
                    LoadAdGroupMemberDetails(account);
                }
            }

            for (int i = 0; i < AdGroups.Count; i++) /* loop to get all leaf person members of parent group */
            {
                LoadAdGroupMemberDetails(AdGroups[i]);
                AdGroups.RemoveAt(i);
            }

            Results.BeginLoadData();

            foreach (string[] person in Persons)
            {
                Results.LoadDataRow(person, true);
            }

            Results.EndLoadData();
        }

        public void Clear()
        {
            Results.Clear();
            LocalAdmins.Clear();
            Persons.Clear();
            Server = "";
        }

        private void GetLocalAdminsistrators(string server)
        {
            using (DirectoryEntry localAdminsistrators = new DirectoryEntry(String.Format("WinNT://{0}", server)).Children.Find("Administrators", "group"))
            {
                object localAdminMembers = localAdminsistrators.Invoke("members", null);

                foreach (object localAdminObject in (IEnumerable)localAdminMembers)
                {
                    string[] properties = new string[7];

                    using (DirectoryEntry localAdminMember = new DirectoryEntry(localAdminObject))
                    {
                        string[] adsPath = localAdminMember.InvokeGet("Adspath").ToString().Split('/');
                        string type = localAdminMember.InvokeGet("Class").ToString();
                        string account = adsPath[adsPath.Length - 1];

                        properties[(int)DC.SAM_ACCOUNT] = Server;
                        properties[(int)DC.OBJECT_SID] = new SecurityIdentifier((byte[])localAdminMember.Properties["objectSid"].Value, 0).ToString();
                        properties[(int)DC.SAM_ACCOUNT] = account;
                        properties[(int)DC.DISTINGUISHED_NAME] = "";
                        properties[(int)DC.STATUS] = "";
                        properties[(int)DC.SOURCE] = Server;

                        if (adsPath.Length > 4 && type.ToLower() == "user")
                        {
                            properties[(int)DC.OBJECT_TYPE] = "localuser";
                        }
                        else if (adsPath.Length > 4 && type.ToLower() == "group")
                        {
                            properties[(int)DC.OBJECT_TYPE] = "localgroup";
                        }
                        else if (type.ToLower() == "user")
                        {
                            properties[(int)DC.OBJECT_TYPE] = "aduser";
                        }
                        else if (type.ToLower() == "group")
                        {
                            properties[(int)DC.OBJECT_TYPE] = "adgroup";
                        }           
                    }

                    LocalAdmins.Add(properties);
                }
            }
        }

        private void LoadAdGroupMemberDetails(string group)
        {
            using (DirectorySearcher searcher = new DirectorySearcher())
            {
                searcher.PropertiesToLoad.AddRange(new string[] { "objectSid", "sAMAccountName", "distinguishedName" });
                searcher.Filter = string.Format("(&(ObjectClass=Group)(CN={0}))", group);
                object memberResults = searcher.FindOne().GetDirectoryEntry().Invoke("members");

                foreach (object member in (IEnumerable)memberResults)
                {
                    string[] properties = new string[7];

                    using (DirectoryEntry dEntry = new DirectoryEntry(member))
                    {
                        string type = dEntry.InvokeGet("class").ToString();
                        string account = dEntry.Properties["sAMAccountName"].Value.ToString();

                        if (type.ToLower() == "user")
                        {
                            LoadAdUserDetails(account, group);
                        }
                        else if (type.ToLower() == "group")
                        {
                            AdGroups.Add(account);
                        }
                    }
                }
            }
        }

        private void LoadAdUserDetails(string account, string parent)
        {
            string[] properties = new string[7];

            using (DirectorySearcher searcher = new DirectorySearcher())
            {
                searcher.PropertiesToLoad.AddRange(new string[] { "objectSid", "sAMAccountName", "distinguishedName" });
                searcher.Filter = string.Format("(&(ObjectClass=Person)(sAMAccountName={0}))", account);

                using (DirectoryEntry dEntry = searcher.FindOne().GetDirectoryEntry())
                {
                    string type = dEntry.InvokeGet("class").ToString();

                    if (type.ToLower() == "user")
                    {
                        properties[(int)DC.OBJECT_TYPE] = "aduser";
                    }
                    else
                    {
                        return;
                    }

                    int userFlags = (int)dEntry.Properties["userAccountControl"].Value;
                    bool disabled = Convert.ToBoolean(userFlags & 0x0002);

                    properties[(int)DC.SERVER] = Server;
                    properties[(int)DC.OBJECT_SID] = new SecurityIdentifier((byte[])dEntry.Properties["objectSid"].Value, 0).ToString();
                    properties[(int)DC.SAM_ACCOUNT] = dEntry.Properties["sAMAccountName"].Value.ToString();
                    properties[(int)DC.DISTINGUISHED_NAME] = dEntry.Properties["distinguishedName"].Value.ToString();
                    properties[(int)DC.STATUS] = disabled ? "disabled" : "enabled";
                    properties[(int)DC.SOURCE] = parent;
                }
            }

            Persons.Add(properties);
        }
    }
}
