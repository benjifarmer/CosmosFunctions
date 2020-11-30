using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace piqee.Models
{
    public class TOrder
    {
        public string id
        {
            get
            {
                return this.OrderKey;
            }
        }
        [JsonProperty(PropertyName = "partition")]
        public string Partition
        {
            get; set;
        }
        public string MerchantId { get; set; }
        public string Asin { get; set; }
        public string ItemPrice { get; set; }
        public string Tracking { get; set; }
        public string Sku { get; set; }
        public string OrderId { get; set; }
        public string ProductName { get; set; }
        public string PriceOfOurPurchase { get; set; }
        public string Note { get; set; }
        public bool Damaged { get; set; }
        public DateTime DamagedDate { get; set; }
        public string Rate { get; set; }
        public string DamagedNote { get; set; }
        public Dictionary<string, Purchased> PurchaseDictionary { get; set; }
        public bool Watch { get; set; }
        public bool Cantfind { get; set; }
        public bool TooExpensive { get; set; }
        public string PurchaseId { get; set; }
        public string OrderStatus { get; set; }
        public string PurchasedStatus { get; set; }

        public string ItemStatus { get; set; }
        public string PurchaseDate { get; set; }
        public string OrderKey { get; set; }
        public string PotentialLink { get; set; }
        public string Country { get; set; }
        public string BuyerEmail { get; set; }
        public string Weight { get; set; }
        public string Height { get; set; }
        public string Width { get; set; }
        public string Length { get; set; }
        public string PrintError { get; set; }
    }
}