using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using piqee.Models;

namespace piqee
{
    public static class HandleTOrders
    {
        private static readonly CosmosClient _client;
        private static readonly Container _container;
        private static readonly Container _orderContainer;
        private static ILogger _log;
        static HandleTOrders()
        {
            var connStr = Environment.GetEnvironmentVariable("piqee_DOCUMENTDB");
            _client = new CosmosClientBuilder(connStr)
                .WithSerializerOptions(new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                })
                .Build(); _container = _client.GetContainer("piqee", "purchases");
            _orderContainer = _client.GetContainer("piqee", "orders");
        }
        [FunctionName("HandleTOrders")]
        public static async Task RunAsync([CosmosDBTrigger(
            databaseName: "piqee",
            collectionName: "orders",
            ConnectionStringSetting = "piqee_DOCUMENTDB",
            LeaseCollectionName = "lease", LeaseCollectionPrefix = "HandleTOrders")]IReadOnlyList<Document> orders, ILogger log)
        {
            _log = log;
            if (orders != null && orders.Count > 0)
            {
                foreach (var item in orders)
                {
                    try
                    {
                        var order = JsonConvert.DeserializeObject<TOrder>(item.ToString());
                        ;
                        if (order.Partition == null)
                        {
                            continue;
                        }

                        if (order.Partition == "TCurrent" || order.Partition.Contains("report") || order.Partition.Contains("buylabel") || order.Partition.Contains("printlabel") || order.Partition.Contains("pick")) continue;
                        if (order.Sku.Contains("test"))
                            await UpdateTOrder(order);
                    }
                    catch (Exception e)
                    {

                    }
                }
                log.LogInformation("Documents modified " + orders.Count);
                log.LogInformation("First document Id " + orders[0].Id);
            }
        }
        static async Task<string> GetStreamContent(Stream stream)
        {
            using (stream)
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
        }
        static async Task UpdateTOrder(TOrder order)
        {
            if (order.OrderStatus == "Cancelled" || order.ItemStatus == "Shipped" || order.OrderStatus == "Closed")
            {
                try
                {
                    await _orderContainer.DeleteItemAsync<TOrder>(order.id, new Microsoft.Azure.Cosmos.PartitionKey("TCurrent"));
                }
                catch (Exception e)
                {
                    _log.LogError($"{order.ProductName} does not exist on the current partition");
                }
            }
            else
            {
                try
                {
                    order.Partition = "TCurrent";
                    try
                    {
                        await _orderContainer.UpsertItemAsync<TOrder>(order, new Microsoft.Azure.Cosmos.PartitionKey("TCurrent"));
                    }
                    catch (Exception e)
                    {
                        // TOrder response = await _orderContainer.ReadItemAsync<TOrder>(order.id, new Microsoft.Azure.Cosmos.PartitionKey(order.Partition));
                        // response.PurchaseDictionary = order.PurchaseDictionary;
                        // response.OrderStatus = order.OrderStatus;
                        // response.ItemStatus = order.ItemStatus;
                        // await _orderContainer.UpsertItemAsync<TOrder>(response, new Microsoft.Azure.Cosmos.PartitionKey("TCurrent"));
                    }
                }
                catch (Exception e)
                {

                }
            }
        }
    }
}
