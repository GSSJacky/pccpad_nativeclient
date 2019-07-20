using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Data;
using MySql.Data.MySqlClient;
using Apache.Geode.Client;


/// <summary>
/// Evaluates the running environment.
/// </summary>
namespace pccpad
{
    public class MySQLConnections
    {
        private static readonly string PORT_ENV_VARIABLE_NAME = "PORT";
        private static readonly string INSTANCE_GUID_ENV_VARIABLE_NAME = "INSTANCE_GUID";
        private static readonly string INSTANCE_INDEX_ENV_VARIABLE_NAME = "INSTANCE_INDEX";
        private static readonly string BOUND_SERVICES_ENV_VARIABLE_NAME = "VCAP_SERVICES";
        private static readonly string NOT_ON_CLOUD_FOUNDRY_MESSAGE = "Not running in cloud foundry.";

        /// <summary>
        /// static constructor to determine the state of the environment and set defaults 
        /// </summary>
        static MySQLConnections()
        {
            // Not on cloud foundry filling in the blanks.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(INSTANCE_GUID_ENV_VARIABLE_NAME)))
            {
                Environment.SetEnvironmentVariable(BOUND_SERVICES_ENV_VARIABLE_NAME, "{}");
                Environment.SetEnvironmentVariable(PORT_ENV_VARIABLE_NAME, NOT_ON_CLOUD_FOUNDRY_MESSAGE);
                Environment.SetEnvironmentVariable(INSTANCE_GUID_ENV_VARIABLE_NAME, NOT_ON_CLOUD_FOUNDRY_MESSAGE);
                Environment.SetEnvironmentVariable(INSTANCE_INDEX_ENV_VARIABLE_NAME, NOT_ON_CLOUD_FOUNDRY_MESSAGE);
            }

            if (BoundServices.GetValue("p.mysql") != null)
            {
                DbEngine = DatabaseEngine.MySql; ;
                MySqlConnectionStringBuilder csbuilder = new MySqlConnectionStringBuilder();
                csbuilder.Add("server", BoundServices["p.mysql"][0]["credentials"]["hostname"].ToString());
                csbuilder.Add("port", BoundServices["p.mysql"][0]["credentials"]["port"].ToString());
                csbuilder.Add("uid", BoundServices["p.mysql"][0]["credentials"]["username"].ToString());
                csbuilder.Add("pwd", BoundServices["p.mysql"][0]["credentials"]["password"].ToString());
                csbuilder.Add("database", BoundServices["p.mysql"][0]["credentials"]["name"].ToString());
                _connectionString = csbuilder.ToString();
                
            }
            else
                DbEngine = DatabaseEngine.None;

        }


        /// <summary>
        /// The local port the container is listening on
        /// </summary>
        public static string Port
        {
            get { return Environment.GetEnvironmentVariable(PORT_ENV_VARIABLE_NAME); }
        }

        /// <summary>
        /// The instance GUID Cloud Foundry assigned to this app instance.
        /// </summary>
        public static string InstanceID
        {
            get { return Environment.GetEnvironmentVariable(INSTANCE_GUID_ENV_VARIABLE_NAME); }
        }

        /// <summary>
        /// The zero based index assigned to this instance of the app.
        /// </summary>
        public static string InstanceIndex
        {
            get { return Environment.GetEnvironmentVariable(INSTANCE_INDEX_ENV_VARIABLE_NAME); }
        }

        public static JObject BoundServices
        {
            get { return JObject.Parse(Environment.GetEnvironmentVariable(BOUND_SERVICES_ENV_VARIABLE_NAME)); }
        }

        private static string _connectionString = string.Empty;
        /// <summary>
        /// Detect a bound service for database, no database found will return an empty string.  Currently only supports SQL Server
        /// </summary>
        public static string DbConnectionString
        {
            get { return _connectionString; }
        }

        /// <summary>
        /// Looks to see if the connection string could be found in a bound service.
        /// </summary>
        public static bool hasDbConnection
        {
            get { return DbEngine != DatabaseEngine.None ? true : false; }
        }

        /// <summary>
        /// Tells which DB engine was detected during app startup.
        /// </summary>
        public static DatabaseEngine DbEngine
        {
            get;
            set;
        }

        /// <summary>
        /// Checks the database to see if it has the proper tables.  If not, add the table and an attendee.
        /// </summary>
        public static void InitializeMYSQLDB()
        {
            Console.WriteLine("InitializeMYSQLDB.--------------");

            //Console.WriteLine("initialize: dbconnectionstring:.--------------" + MySQLConnections.DbConnectionString);
            //MySqlConnection conn = new MySqlConnection(MySQLConnections.DbConnectionString);

            string strTableCreate = @"CREATE TABLE IF NOT EXISTS `customer` (
                      `Id` bigint(20) NOT NULL AUTO_INCREMENT,
                      `Name` varchar(255) DEFAULT NULL,
                      `Address` varchar(255) DEFAULT NULL,
                      `Email` varchar(255) DEFAULT NULL,
                      `TelephoneNumber` varchar(255) DEFAULT NULL,
                      PRIMARY KEY (`Id`)
                    ) AUTO_INCREMENT=8 DEFAULT CHARSET=utf8;";

            using (MySqlConnection localconn = new MySqlConnection(MySQLConnections.DbConnectionString))
            // if the table doesn't exist, create it
            using (MySqlCommand localcommand = new MySqlCommand()
            {
                CommandText = strTableCreate,
                Connection = localconn,
                CommandType = CommandType.Text
            })
            {
                localconn.Open();
                
                int rows = localcommand.ExecuteNonQuery();

                if (rows > 0)
                    Console.WriteLine("table customer didn't exist, creating it: " + rows + " rows affected.");
                
            };

        }


        /// <summary>
        /// count the table rows 
        /// </summary>
        public static int getTableCount(string strCountquery)
        {
            Console.WriteLine("getTableCount.--------------");
            int intCount = 0;

            using (MySqlConnection localconn = new MySqlConnection(MySQLConnections.DbConnectionString))
            using (MySqlCommand command = new MySqlCommand()
            {
                CommandText = strCountquery,
                Connection = localconn,
                CommandType = CommandType.Text
            })
            {
                localconn.Open();
                MySqlDataReader localrdr = command.ExecuteReader();
                if (localrdr.Read())
                {
                    intCount = localrdr.GetInt32(0);
                }

                if (localrdr != null)
                {
                    localrdr.Close();
                }

                if (localrdr != null)
                {
                    localrdr.Close();
                }
            }

            return intCount;

        }

        /// <summary>
        /// count the table content 
        /// </summary>
        public static string getTableContent(string strQuery)
        {
            Console.WriteLine("show the table content by query.--------------");
            string strResult = "";
            string strColumeName = " Id, Name, Email, Address, TelephoneNumber  <br/>";
            

            using (MySqlConnection localconn = new MySqlConnection(MySQLConnections.DbConnectionString))
            using (MySqlCommand command = new MySqlCommand()
            {
                CommandText = strQuery,
                Connection = localconn,
                CommandType = CommandType.Text
            })
            {
                localconn.Open();
                
                MySqlDataReader localrdr = command.ExecuteReader();
                while (localrdr.Read())
                {
                    strResult = strResult + " " + localrdr.GetString(0) + ", " + localrdr.GetString(1) + ", " + localrdr.GetString(2) + ", " + localrdr.GetString(3) + ", " + localrdr.GetString(4) + "<br/>";                  
                }
            };
            
            return strColumeName + strResult;

        }

        /// <summary>
        /// put data 
        /// </summary>
        public static void bulkLoaddataIntoCustomer(dynamic customerobj)
        {
            Console.WriteLine("bulkLoaddataIntoDB.--------------");
            
            string strInsertQuery = "INSERT INTO customer (Id, Name, Address, Email, TelephoneNumber ) VALUES (@Id, @Name, @Address, @Email, @TelephoneNumber) ON DUPLICATE KEY UPDATE Name=@Name, Address=@Address, Email=@Email, TelephoneNumber=@TelephoneNumber;";
            

            using (MySqlConnection localconn = new MySqlConnection(MySQLConnections.DbConnectionString))
            {
                localconn.Open();

                foreach (Customer curCustomer in customerobj)
                {
                    using (MySqlCommand myCmd = new MySqlCommand(strInsertQuery, localconn))
                    {
                        myCmd.CommandType = CommandType.Text;
                        myCmd.Parameters.AddWithValue("@Id", curCustomer.Id);
                        myCmd.Parameters.AddWithValue("@Name",curCustomer.Name);
                        myCmd.Parameters.AddWithValue("@Address", curCustomer.Address);
                        myCmd.Parameters.AddWithValue("@Email", curCustomer.Email);
                        myCmd.Parameters.AddWithValue("@TelephoneNumber", curCustomer.TelephoneNumber);

                        myCmd.ExecuteNonQuery();
                    }
                }
                
            }
        }

        public static void runDeleteDBTableQuery(string strDeleteQuery)
        {
            Console.WriteLine("runDeleteDBTableQuery.--------------");
            
            using (MySqlConnection localconn = new MySqlConnection(MySQLConnections.DbConnectionString))
            {
                localconn.Open();

                using (MySqlCommand myCmd = new MySqlCommand(strDeleteQuery, localconn))
                {
                    myCmd.CommandType = CommandType.Text;                 
                    myCmd.ExecuteNonQuery();
                }

            }
        }

        public enum DatabaseEngine
        {
            None = 0,
            SqlServer = 1,
            MySql = 2
        }
    }
}