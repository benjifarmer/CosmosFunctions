using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using piqee.Models;

namespace piqee
{
    public static class HandleOrderChanges
    {
        private static readonly CosmosClient _client;
        private static readonly Container _listingContainer;
        private static readonly Container _orderContainer;
        private static ILogger _log;
        static HandleOrderChanges()
        {
            var connStr = Environment.GetEnvironmentVariable("piqee_DOCUMENTDB");
            _client = new CosmosClientBuilder(connStr)
          .WithSerializerOptions(new CosmosSerializationOptions
          {
              PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
          })
          .Build(); ;
            _listingContainer = _client.GetContainer("piqee", "listings");
            _orderContainer = _client.GetContainer("piqee", "orders");
        }
        [FunctionName("HandleOrderChanges")]
        public static async Task RunAsync([CosmosDBTrigger(
            databaseName: "piqee",
            collectionName: "orders",
            ConnectionStringSetting = "piqee_DOCUMENTDB",
            LeaseCollectionName = "lease", LeaseCollectionPrefix = "HandleOrderChanges")]IReadOnlyList<Document> orders, ILogger log)
        {
            _log = log;
            if (orders != null && orders.Count > 0)
            {
                foreach (var item in orders)
                {
                    var order = JsonConvert.DeserializeObject<Order>(item.ToString());
                    if (order.Partition == "TCurrent" || order.Partition.Contains("report") || order.Partition.Contains("buylabel") || order.Partition.Contains("printlabel") || order.Partition.Contains("pick") || order.Sku.StartsWith("test")) continue;
                    await UpdateOrder(order);
                }
                log.LogInformation("Documents modified " + orders.Count);
                log.LogInformation("First document Id " + orders[0].Id);
            }
        }

        private static async Task UpdateOrder(Order order)
        {
            if (order.ItemStatus == "Unshipped")
            {
                if (order.Listing == null)
                {
                    try
                    {
                        if (order.Sku.Length < 19)
                        {
                            var items = await GetListingBySku(order.Sku, order.MerchantId);
                            if (items.Any()) order.Listing = items.ElementAt(0);
                            else
                                order.Listing = new Listing();
                        }
                        else
                        {
                            string id = order.Sku.Substring(order.Sku.Length - 19);
                            string shelf = order.Sku.Substring(0, order.Sku.Length - 19);
                            Listing listing = await _listingContainer.ReadItemAsync<Listing>(id, new Microsoft.Azure.Cosmos.PartitionKey(order.MerchantId + shelf));
                            order.Listing = listing;
                        }

                        await _orderContainer.UpsertItemAsync(order);
                        order.Partition = order.MerchantId + "pick";
                        await _orderContainer.CreateItemAsync(order);
                    }
                    catch (Exception e)
                    {

                    }
                }
                else
                {
                    order.Partition = order.MerchantId + "pick";
                    await _orderContainer.UpsertItemAsync(order);
                }
            }
            if (order.ItemStatus == "Shipped")
            {
                try
                {
                    order.Partition = order.MerchantId + "pick";
                    await _orderContainer.DeleteItemAsync<Order>(order.id, new Microsoft.Azure.Cosmos.PartitionKey(order.Partition));
                }
                catch (Exception e)
                {
                    _log.LogError($"Tried to erase {order.ProductName} but did not exist.");
                }
            }
            if (order.OrderStatus == "Cancelled")
            {
                try
                {
                    order.Partition = order.MerchantId + "pick";
                    await _orderContainer.DeleteItemAsync<Order>(order.id, new Microsoft.Azure.Cosmos.PartitionKey(order.Partition));
                }
                catch (Exception e)
                {
                    _log.LogError($"Tried to erase {order.ProductName} but did not exist.");
                }
            }

        }
        private async static Task<List<Listing>> GetListingBySku(string sku, string merchantId)
        {
            var sql = $"SELECT * FROM c WHERE STARTSWITH(c.partition, \"{merchantId}\")";
            sql += $"AND CONTAINS(c.sku, \"{sku}\", true)";

            List<Listing> items = new List<Listing>();
            var iterator = _listingContainer.GetItemQueryIterator<Listing>(sql);

            var pageCount = 0;
            while (iterator.HasMoreResults)
            {
                pageCount++;
                var documents = await iterator.ReadNextAsync();
                foreach (var customer in documents)
                {
                    items.Add(customer);
                }
            }
            return items;
        }
    }
}
