using System;
using System.Web.Http;
using System.Linq;
using Newtonsoft.Json.Linq;
using Owin;
using Apache.Geode.Client;
using System.Threading;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;

using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;

using FizzWare.NBuilder;
using System.Text;
using System.Net.Http.Headers;
using System.Diagnostics;

using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using System.Data;
using System.Web;

namespace pccpad
{
    class Program
    {
        static void Main(string[] args)
        {
            string port = Environment.GetEnvironmentVariable("PORT");
            if (port == null)
            {
                port = "9000";
            }
            using (Microsoft.Owin.Hosting.WebApp.Start<Startup>("http://*:" + port))
            {
                Console.WriteLine("Starting Serving with http://*:" + port);
                Console.WriteLine("Press [enter] to quit...");
                Console.ReadLine();
            }
        }
    }
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
           
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            appBuilder.UseWebApi(config);

            var physicalFileSystem = new PhysicalFileSystem(@"./static");

            var options = new FileServerOptions
            {
                EnableDefaultFiles = true,
                FileSystem = physicalFileSystem
            };
            options.StaticFileOptions.FileSystem = physicalFileSystem;
            options.StaticFileOptions.ServeUnknownFileTypes = true;
            options.DefaultFilesOptions.DefaultFileNames = new[]
            {
                "index.html"
            };

            appBuilder.UseFileServer(options);
            
        }
    }
    static class Constants
    {
        public const string jsonPathLocators = "$.p-cloudcache[0].credentials.locators";
        public const string jsonPathPassword = "$.p-cloudcache[0].credentials.users[?(@.roles[*] == 'developer')].password";
        public const string jsonPathUsername = "$.p-cloudcache[0].credentials.users[?(@.roles[*] == 'developer')].username";
        

    }
    public class CustomerController : ApiController
    {
        
        private static IRegion<int, Customer> region = null;
        private static Pool globalpool = null;
        private static Object monitor = new Object();

        Stopwatch mystopwatch = new System.Diagnostics.Stopwatch();

        public CustomerController()
        {
            if (region == null)
            {
                Monitor.Enter(monitor);
                try
                {
                    if (region == null)
                    {
                        ConnectToCloudCache();
                    }
                }
                finally
                {
                    Monitor.Exit(monitor);
                }
            }

            if (MySQLConnections.hasDbConnection)
            {

                MySQLConnections.InitializeMYSQLDB();

            }
            

        }
        private void ConnectToCloudCache()
        {
            JObject vcapJson = JObject.Parse(Environment.GetEnvironmentVariable("VCAP_SERVICES"));

            /**
            JObject vcapJson = new JObject
            {
                { "locators", "192.168.12.52[55221]" },
                { "username", "cluster_operator_57Z2ueingjHQrgwIAB389w" },
                { "password", "CTl9gZJlIoS0m2BUdWpQ" }
            };
            **/

            Cache cache = new CacheFactory()
                .Set("log-level","debug")
                .Set("log-file","pccpad.log")
                .SetAuthInitialize(
                new UsernamePassword(
                    (string)vcapJson.SelectToken(Constants.jsonPathUsername),
                    (string)vcapJson.SelectToken(Constants.jsonPathPassword)))
                .Create();

            cache.TypeRegistry.PdxSerializer = new ReflectionBasedAutoSerializer();

            PoolFactory pool = cache.GetPoolFactory();
            foreach (string locator in vcapJson.SelectToken(Constants.jsonPathLocators).Select(s => (string)s).ToArray())
            {
                string[] hostPort = locator.Split('[', ']');
                pool.AddLocator(hostPort[0], Int32.Parse(hostPort[1]));
            }
            globalpool=pool.Create("pool");
            
            region = cache.CreateRegionFactory(RegionShortcut.PROXY)
                .SetPoolName("pool")
                .Create<int, Customer>("customer");
            
        }

        [HttpGet]
        [ActionName("GetAllKeys")]
        public int[] GetAll()
        {
            Console.WriteLine("-----getall");
            int[] keys =  region.Keys.ToArray();
            //Console.WriteLine("-----keys:" + keys.ToString());
            return keys;
        }

        [HttpGet]
        [ActionName("showcache")]
        public HttpResponseMessage GetCustomers(int count)
        {
            Console.WriteLine("-----Get all Customers");

            int intMAXNumber = count == 0 ? 1000 : count;
            int counter = 0;
            HttpResponseMessage httpresponse = new HttpResponseMessage();


            string response= " ID, Name, Email, TelephoneNumber, Address <br/>";

            mystopwatch.Start();

            //Customer[] customers = region.Values.ToArray();
            int[] keys = region.Keys.ToArray();
            
            foreach (int key in keys)
            {
                //Console.WriteLine("-----in collecting customer cache");
                Customer curCustomer = region[key];
                response = response + " " + curCustomer.Id + ", " + curCustomer.Name + ", " + curCustomer.Email + ", " + curCustomer.TelephoneNumber + ", " + curCustomer.Address + "<br/>";

                if (counter == intMAXNumber) { break; }
                counter++;
            }

            mystopwatch.Stop();
            
            response = "PCC Region [customer], Elapsed time :" + mystopwatch.ElapsedMilliseconds + " Milliseconds<br/>==================================<br/>" + response;

            httpresponse.Content = new StringContent(response);
            httpresponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

            return httpresponse; 
        }


        [HttpGet]
        public Customer Get(int id)
        {
            Console.WriteLine("-----get id:" + id);
            return region[id];
        }

        [HttpGet]
        [ActionName("bulkload")]
        public Boolean Bulkload(int amount)
        {
            Console.WriteLine("-----get number:" + amount);
            var customers = Builder<Customer>.CreateListOfSize(amount)
                .All()
                    .With(c => c.Name = Faker.Name.FullName())
                    .With(c => c.Address = Faker.Address.City() + Faker.Address.StreetAddress())
                    .With(c => c.Email = Faker.Internet.Email())
                    .With(c => c.TelephoneNumber = Faker.Phone.Number())
                .Build();

            
            foreach (Customer curCustomer in customers)
            {
                region[curCustomer.Id] = curCustomer;
            }

            //load the same data into customer database
            MySQLConnections.bulkLoaddataIntoCustomer(customers);

            return true;
        }


        [HttpGet]
        [ActionName("clearcache")]
        public HttpResponseMessage clearcache()
        {  
            Console.WriteLine("-----clear cache");
            HttpResponseMessage httpresponse = new HttpResponseMessage();
            int count = 0;

            foreach (int key in region.Keys)
            {
                region.Remove(key);
                count++;
            }


            httpresponse.Content = new StringContent("cleared " + count + " entries from Region customer.");
            httpresponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

            return httpresponse;
        }


        [HttpGet]
        [ActionName("cleardb")]
        public HttpResponseMessage cleardb()
        {
            Console.WriteLine("-----clear db");
            HttpResponseMessage httpresponse = new HttpResponseMessage();
            
            MySQLConnections.runDeleteDBTableQuery("TRUNCATE TABLE customer");

            httpresponse.Content = new StringContent("cleared all entries from table customer.");
            httpresponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

            return httpresponse;
        }

        [HttpGet]
        [ActionName("countcache")]
        public int countcache()
        {
            Console.WriteLine("-----count cache");
            int count = region.Keys.ToArray().Length;
            
            return count;
        }

        [HttpGet]
        [ActionName("countdb")]
        public int countdb()
        {
            Console.WriteLine("-----count db table rows");
            int count = MySQLConnections.getTableCount("select count(*) from customer");
            return count;
        }

        [HttpGet]
        [ActionName("showdb")]
        public HttpResponseMessage GetCustomersTable(int count)
        {
            Console.WriteLine("-----Get all Customers from db------");

            int intMAXNumber = count == 0 ? 1000 : count;
            
            HttpResponseMessage httpresponse = new HttpResponseMessage();

            mystopwatch.Start();

            String response = MySQLConnections.getTableContent("select Id, Name, Email, Address,TelephoneNumber from customer");

            mystopwatch.Stop();
           
            response = "Database's Table [customer], Elapsed time :"+ mystopwatch.ElapsedMilliseconds + " Milliseconds <br/>==================================<br/>" + response;

            httpresponse.Content = new StringContent(response);
            httpresponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

            return httpresponse;
        }


        [HttpGet]
        [ActionName("lookasidesearch")]
        public HttpResponseMessage LookAsideSearch(string keyword)
        {
            Console.WriteLine("----LookAsideSearch------");
            String response = "";
            HttpResponseMessage httpresponse = new HttpResponseMessage();

            mystopwatch.Start();

            //response = funcQueryCache(keyword);
            response = funcGetCacheByName(keyword);
            if (response == "")
            {
                response = searchupdateContent("select Id, Name, Email, Address,TelephoneNumber from customer where Name like '%" + keyword + "%' COLLATE utf8_general_ci");
                mystopwatch.Stop();
                response = "Search Result with keyword=" + keyword + " from <b>Database</b>, Elapsed time :" + mystopwatch.ElapsedMilliseconds + " Milliseconds <br/>==================================<br/>" + response;
            }
            else {
                mystopwatch.Stop();
                response = "Search Result with keyword=" + keyword + " from <b>Cloud Cache</b>, Elapsed time :" + mystopwatch.ElapsedMilliseconds + " Milliseconds <br/>==================================<br/>" + response;
            }
            
            
            httpresponse.Content = new StringContent(response);
            httpresponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

            return httpresponse;
        }


        /// <summary>
        /// search from database and update the cache
        /// </summary>
        public string searchupdateContent(string strQuery)
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
                    strResult = strResult + " " + localrdr.GetInt32(0) + ", " + localrdr.GetString(1) + ", " + localrdr.GetString(2) + ", " + localrdr.GetString(3) + ", " + localrdr.GetString(4) + "<br/>";
                    //lookaside pattern: write the miss cache into cache
                    region[localrdr.GetInt32(0)] = new Customer(localrdr.GetInt32(0), localrdr.GetString(1), localrdr.GetString(2), localrdr.GetString(3), localrdr.GetString(4));

                }
            };

            return strColumeName + strResult;

        }

        public string funcQueryCache(string strkeyword)
        {
            Console.WriteLine("-----funcQueryCache----------");
            int count = 0;
            string response = "";

            var queryService = globalpool.GetQueryService();

            Console.WriteLine("Getting the orders from the region");

            //curCustomer.Id + ", " + curCustomer.Name + ", " + curCustomer.Email + ", " + curCustomer.TelephoneNumber + ", " + curCustomer.Address
            string strquery = "<TRACE>SELECT * FROM /customer c WHERE c.Name = '" + strkeyword + "' ";
            //string strquery = "<TRACE>SELECT * FROM /customer c ";

            var query = queryService.NewQuery<Customer>(strquery);
            var queryResults = query.Execute();

            Console.WriteLine("====================" + strquery);

            foreach (Customer curCustomer in queryResults)
            {
                Console.WriteLine("====================in the itertive place" );
                response = response + " " + curCustomer.Id + ", " + curCustomer.Name + ", " + curCustomer.Email + ", " + curCustomer.TelephoneNumber + ", " + curCustomer.Address + "<br/>";
                count++;
            }

            
            response = count >0 ? " ID, Name, Email, TelephoneNumber, Address <br/>" + response  : "";

            return response;
        }


        public string funcGetCacheByName(string strkeyword)
        {
            Console.WriteLine("-----funcGetCacheName----------");
            string result = "";
            int count = 0;

            int[] keys = region.Keys.ToArray();

            foreach (int key in keys)
            {
                //Console.WriteLine("-----in collecting customer cache");
                Customer curCustomer = region[key];
                if (curCustomer.Name.ToLower().Contains(strkeyword.ToLower()))
                {
                    result = result + " " + curCustomer.Id + ", " + curCustomer.Name + ", " + curCustomer.Email + ", " + curCustomer.TelephoneNumber + ", " + curCustomer.Address + "<br/>";
                    count++;
                }
            }


            result = count > 0 ? " ID, Name, Email, TelephoneNumber, Address <br/>" + result : "";

            return result;
        }


        [HttpGet]
        [ActionName("listapi")]
        public HttpResponseMessage listAPI()
        {
            Console.WriteLine("----listAPI------");
            String response = "";
            HttpResponseMessage httpresponse = new HttpResponseMessage();
            //string path = HttpContext.Current.Request.Url.AbsolutePath;

            //HttpContext.Current.Request.Url.Authority


            response = "Customer Search Service -- Available APIs: <br/>"
                    + "<br/>"
                    + "GET <a href='./api/customer/showcache?count=0'>/showcache?count={amount}</a>    	           - get all customer info in PCC<br/>"
                    + "GET <a href='./api/customer/clearcache'>/clearcache</a>                   - remove all customer info in PCC<br/>"
                    + "GET <a href='./api/customer/showdb?count=0'>/showdb?count={amount}</a>  	                   - get all customer info in MySQL<br/>"
                    + "GET <a href='./api/customer/cleardb'>/cleardb</a>                        - remove all customer info in MySQL<br/>"
                    + "GET <a href='./api/customer/bulkload?amount=100'>/loaddb?amount={amount}</a>         - load {amount} customer info into MySQL<br/>"
                    + "GET <a href='./api/customer/lookasidesearch?keyword=Jacky'>/lookasidesearch?keyword={Name}</a>   - get specific customer info by customer name and put entries into PCC<br/>"
                     + "GET <a href='./api/customer/countdb'>/countdb</a>                        - get count info from db<br/>"
                    + "GET <a href='./api/customer/countcache'>/countcache</a>                     - get count info from PCC<br/>";

            httpresponse.Content = new StringContent(response);
            httpresponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

            return httpresponse;
        }


        [HttpPost]
        public HttpResponseMessage Post([FromBody]Customer[] customers)
        {
            if (customers != null && customers.Length > 0)
            {
                Dictionary<int, Customer> bulk = new Dictionary<int, Customer>();

                foreach (Customer currCustomer in customers)
                {
                    region[currCustomer.Id] = currCustomer;
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [HttpPut]
        public HttpResponseMessage Put([FromBody]Customer value)
        {
            region[value.Id] = value;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [HttpDelete]
        public HttpResponseMessage Delete(int id)
        {
            region.Remove(id);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
        public HttpResponseMessage DeleteAll()
        {
            foreach(int key in region.Keys)
            {
                region.Remove(key);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        

    }
}
