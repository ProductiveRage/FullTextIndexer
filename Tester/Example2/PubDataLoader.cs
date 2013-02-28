using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using FullTextIndexer.Common.Lists;
using Tester.Common;
using Tester.Example2.SourceData;

namespace Tester.Example2
{
    /// <summary>
    /// This generates Product data from the Microsoft demo "Pub" database
    /// </summary>
    public class PubDataLoader
    {
        private string _connectionString;
        public PubDataLoader(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Null/empty connectionString specified");

            _connectionString = connectionString;
        }

        public NonNullImmutableList<Product> GetProducts()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql =
                    "SELECT title AS Name, pub_name AS Keywords, notes AS Description, pub_name AS Address1, City AS Address4, State AS Address5, Country AS Country FROM Titles t " +
                    "INNER JOIN Publishers p " +
                    "ON p.pub_id = t.pub_id " +
                    "WHERE LTrim(RTrim(IsNull(pub_name, ''))) <> ''";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        var english = new LanguageDetails(1, "en");
                        var products = new List<Product>();
                        while (rdr.Read())
                        {
                            var address1 = rdr.GetString("Address1");
                            AddressDetails address;
                            if (string.IsNullOrWhiteSpace(address1))
                                address = null;
                            else
                            {
                                address = new AddressDetails(
                                    english,
                                    address1,
                                    null,
                                    null,
                                    rdr.GetString("Address4"),
                                    rdr.GetString("Address5"),
                                    rdr.GetString("Country")
                                );
                            }
                            var keywords = rdr.GetString("Keywords");
                            var description = rdr.GetString("Description");
                            DescriptionDetails[] descriptions;
                            if (string.IsNullOrWhiteSpace(description))
                                descriptions = null;
                            else
                                descriptions = new[] { new DescriptionDetails(english, true, new int[0], description, description) };
                            
                            products.Add(new Product(
                                products.Count,
                                new[] { 1 },
                                new TranslatedString(rdr.GetString("Name"), new Dictionary<LanguageDetails, string>()),
                                keywords == null ? null : new TranslatedString(keywords, new Dictionary<LanguageDetails, string>()),
                                address,
                                descriptions
                            ));
                        }
                        return products.ToNonNullImmutableList();
                    }
                }
            }
        }
    }
}
