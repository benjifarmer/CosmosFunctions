using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using piqee.Models;
using System.Linq;
using Microsoft.Azure.Cosmos.Fluent;

namespace piqee
{
    public static class HandlePurchases
    {
        private static readonly CosmosClient _client;
        private static readonly Container _container;
        private static readonly Container _orderContainer;
        private static ILogger _log;
        private static string _merchantId = "A2JTWJSZTZHYJL";
        static HandlePurchases()
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

        [FunctionName("HandlePurchases")]
        public static async Task RunAsync([CosmosDBTrigger(
            databaseName: "piqee",
            collectionName: "purchases",
            ConnectionStringSetting = "piqee_DOCUMENTDB",
            LeaseCollectionName = "lease", LeaseCollectionPrefix = "HandlePurchases")]IReadOnlyList<Document> purchases, ILogger log)
        {
            _log = log;
            if (purchases != null && purchases.Count > 0)
            {
                foreach (var item in purchases)
                {
                    if (item.Id.Contains("linked")) continue;
                    var purchase = JsonConvert.DeserializeObject<Purchased>(item.ToString());
                    if (purchase.ASIN == "ebay") continue;
                    await UpdatePurchases(purchase);
                }
            }
        }
        private static async Task UpdatePurchases(Purchased purchase)
        {
            try
            {
                var item = await TryGetLinkedPurchase(purchase);
                await TryAddPurchaseToOrder(purchase, item.AssociatedOrderId);
            }
            catch (Exception e)
            {
                _log.LogInformation(e.ToString());
            }
        }
        private static async Task TryAddPurchaseToOrder(Purchased item, string associatedId)
        {
            if (associatedId != null)
            {
                TOrder order = await _orderContainer.ReadItemAsync<TOrder>(associatedId, new Microsoft.Azure.Cosmos.PartitionKey(_merchantId + item.ASIN));
                if (order.PurchaseDictionary == null)
                    order.PurchaseDictionary = new Dictionary<string, Purchased>();
                if (order.PurchaseDictionary.ContainsKey(order.id))
                    order.PurchaseDictionary.Remove(order.id);
                if (order.PurchaseDictionary.ContainsKey(item.id))
                    order.PurchaseDictionary[item.id] = item;
                else
                    order.PurchaseDictionary.Add(item.id, item);
                await _orderContainer.UpsertItemAsync(order);
            }
            _log.LogWarning("Updated purchase");
        }
        private static async Task<Purchased> TryGetLinkedPurchase(Purchased purchase)
        {
            Purchased item = new Purchased();
            try
            {
                item = await _container.ReadItemAsync<Purchased>(purchase.id + "linked", new Microsoft.Azure.Cosmos.PartitionKey(purchase.ASIN));
            }
            catch (CosmosException e)
            {
                if (e.Message.Contains("404"))
                {
                    await CreateLinkedPurchase(purchase);
                }
            }
            return item;
        }
        private static async Task CreateLinkedPurchase(Purchased purchase)
        {
            string response = await FindAssociatedOrder(purchase);
            if (response != "not-found")
            {
                purchase.AssociatedOrderId = response;
                purchase.id = purchase.id + "linked";
                await _container.CreateItemAsync(purchase);
            }
        }
        private static async Task<List<TOrder>> FindItemsByTitle(string title)
        {
            var itemsByTitle = new List<TOrder>();
            if (!String.IsNullOrEmpty(title))
            {
                title = new string(title.Where(c => !char.IsPunctuation(c)).ToArray());
                IEnumerable<string> words = title.Split(' ');
                while (words.Any())
                {
                    itemsByTitle = await GetOrdersFromTitle(words.Take(2));
                    if (itemsByTitle.Count() < 5) return itemsByTitle;
                    words = words.Skip(2);
                }
            }
            return itemsByTitle;
        }
        private static async Task<string> FindAssociatedOrder(Purchased purchase)
        {
            var items = await GetOrdersFromAsin(purchase.ASIN);
            items = items.OrderByDescending(x => x.PurchaseDate).ToList();
            _log.LogWarning(items.Count + "");
            foreach (var item in items)
            {
                if (item.OrderStatus == "Cancelled") continue;
                //check if all are cancelled and the item is not damaged
                if (item.PurchaseDictionary != null && item.Damaged == false && !AllCancelled(item.PurchaseDictionary))
                {
                    _log.LogWarning($"Possible duplicate {item.ProductName}");
                    continue;
                }
                if (DateTime.Parse(item.PurchaseDate).AddHours(-DateTime.Parse(item.PurchaseDate).Hour - 1).Ticks
 <= purchase.OrderDate.Value.Ticks)
                {
                    if (item.Damaged)
                    {
                        item.Damaged = false;
                        item.DamagedNote = "";

                    }
                    if (item.PurchaseDictionary == null) item.PurchaseDictionary = new Dictionary<string, Purchased>();
                    else if (item.PurchaseDictionary.ContainsKey(purchase.id)) continue;
                    else if (item.PurchaseDictionary.ContainsKey(item.id)) item.PurchaseDictionary.Remove(item.id);
                    item.PurchaseDictionary.Add(purchase.id, purchase);
                    await _orderContainer.UpsertItemAsync(item);
                    return item.id;
                }
            }

            _log.LogWarning("not found");
            return "not-found";
        }
        public static bool AllCancelled(Dictionary<string, Purchased> purchases)
        {
            foreach (var p in purchases.Values)
            {
                if (p.OrderStatus != "Cancelled") return false;
            }
            return true;
        }

        private async static Task<List<TOrder>> GetOrdersFromAsin(string asin)
        {
            var sql = $"SELECT * FROM c WHERE c.partition = '{_merchantId + asin}'";
            List<TOrder> items = new List<TOrder>();
            var iterator = _orderContainer.GetItemQueryIterator<TOrder>(sql);

            var pageCount = 0;
            while (iterator.HasMoreResults)
            {
                pageCount++;
                var documents = await iterator.ReadNextAsync();
                foreach (var customer in documents)
                {
                    if (!customer.Sku.Contains("test")) continue;
                    items.Add(customer);
                }
            }
            return items;
        }
        private async static Task<List<TOrder>> GetOrdersFromTitle(IEnumerable<string> words)
        {
            var sql = $"SELECT * FROM c WHERE STARTSWITH(c.partition, \"{_merchantId}\")";
            foreach (var w in words)
            {
                sql += $"AND CONTAINS(c.ProductName, \"{w}\", true)";

            }

            List<TOrder> items = new List<TOrder>();
            var iterator = _orderContainer.GetItemQueryIterator<TOrder>(sql);

            var pageCount = 0;
            while (iterator.HasMoreResults)
            {
                pageCount++;
                var documents = await iterator.ReadNextAsync();
                foreach (var customer in documents)
                {
                    if (!customer.Sku.Contains("test")) continue;
                    items.Add(customer);
                }
            }
            return items;
        }

    }
}
