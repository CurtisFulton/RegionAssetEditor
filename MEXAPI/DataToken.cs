using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Services.Client;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MEXModel
{
    public class DataToken : MEXEntities
    {
        public HttpClient HttpClient { get; private set; }
        public string BaseURL { get; private set; }

        public DataToken(Uri serviceRoot, string username = "admin", string password = "admin")
            : this (serviceRoot.OriginalString, username, password) { }

        public DataToken(string serviceRoot, string username = "admin", string password = "admin") : base(new Uri(GetBaseMEXUrl(serviceRoot) + "/OData.svc"))
        {
            BaseURL = GetBaseMEXUrl(serviceRoot);

            this.ReadingEntity += DataToken_ReadingEntity;
            string auth = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{username}:{password}")
            );
            this.BuildingRequest += (sender, e) => e.Headers.Add("Authorization", $"Basic {auth}");

            // Create a HTTP client
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {auth}");

            TestConnection();
        }

        private static string GetBaseMEXUrl(string path)
        {
            int firstForwardSlash = NthIndexOf(path, '/', 3);

            int secondForwardSlash = path.IndexOf('/', firstForwardSlash + 1);

            if (secondForwardSlash != -1)
                path = path.Substring(0, secondForwardSlash);

            return path;
        }

        private static int NthIndexOf(string word, char character, int n)
        {
            int count = 0;
            for (int i = 0; i < word.Length; i++) {
                if (word[i] == character) {
                    count++;
                    if (count == n) {
                        return i;
                    }
                }
            }
            return -1;
        }

        private void TestConnection()
        {
            try {
                // Check that we can query without any errors
                this.Assets.FirstOrDefault();
            } catch (DataServiceQueryException e) {
                CheckException(e);
            }
        }
        
        private void DataToken_ReadingEntity(object sender, System.Data.Services.Client.ReadingWritingEntityEventArgs e)
        {
            var entity = (e.Entity as INotifyPropertyChanged);
            entity.PropertyChanged += (s, e2) => this.UpdateObject(s);
        }

        /// <summary>
        /// Helper method for writing Dapper Queries. This will allow you to send custom queries and return custom data.
        /// </summary>
        /// <typeparam name="T">Type of object the query returns</typeparam>
        /// <param name="query">SQL Query</param>
        /// <returns>List of objects returned by the query</returns>
        public List<T> DynamicQuery<T>(string query)
        {
            // If the opening XML tag is not there, assume there is no XML tags at all and add them
            if (!query.Contains("<paramstring>"))
                query = "<paramstring>" + query + "</paramstring>";

            var httpContent = new StringContent(query, Encoding.UTF8, "application/xml");

            try {
                var response = HttpClient.PostAsync($"{BaseURL}/API/DataAPI/PerformAction?ActionType=OData&ActionName=DapperQuery", httpContent).Result;
                var result =  response.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<List<T>>(result);
            } catch (Exception e) {
                if (e is DataServiceQueryException)
                    CheckException((DataServiceQueryException)e);

                throw e;
            }
        }

        private bool CheckException(DataServiceQueryException e)
        {
            if (e.Response.StatusCode == 401)
                throw new DataServiceRequestException("The login details you used are incorrect");

            return true;
        }

        /// <summary>
        /// Validates the Query to make sure it has single quotation marks around it.
        /// </summary>
        /// <param name="query">Dapper Query</param>
        /// <returns>Validated Query</returns>
        private string ValidateDapperQuery(string query)
        {
            // Possibly add other validation here in the future
            if (query[0] != '\'')
                query = "'" + query;

            if (query.Last() != '\'')
                query += "'";

            return query;
        }
    }
}
