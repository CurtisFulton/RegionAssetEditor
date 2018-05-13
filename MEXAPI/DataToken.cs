using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace MEXModel
{
    public class DataToken : MEXEntities
    {
        public DataToken(Uri serviceRoot, string username = "admin", string password = "admin") : base(serviceRoot)
        {
            this.ReadingEntity += DataToken_ReadingEntity;

            this.BuildingRequest += (sender, e) => {
                string auth = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{username}:{password}")
                    );

                e.Headers.Add("Authorization", $"Basic {auth}");
            };
        }
        
        private void DataToken_ReadingEntity(object sender, System.Data.Services.Client.ReadingWritingEntityEventArgs e)
        {
            var entity = (e.Entity as INotifyPropertyChanged);
            entity.PropertyChanged += (s, e2) => this.UpdateObject(s);
        }

        /// <summary>
        /// Helper method for writing Dapper Queries. 
        /// </summary>
        /// <typeparam name="T">Type of object the query returns</typeparam>
        /// <param name="query">SQL Query</param>
        /// <returns>List of objects returned by the query</returns>
        public List<T> DynamicQuery<T>(string query)
        {
            DataServiceQuery<string> dapperQuery = this.CreateQuery<string>("DapperQuery").AddQueryOption("sql", ValidateDapperQuery(query));
            List<T> results = null;

            try {
                results = JsonConvert.DeserializeObject<List<T>>(dapperQuery.Execute().FirstOrDefault());
            } catch (Exception e) {
                Console.WriteLine(e);
            }

            return results;

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
