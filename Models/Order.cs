using System.Collections.Generic;
using Newtonsoft.Json;

public class Order
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
    public string OrderId { get; set; }
    public string OrderKey { get; set; }
    public string MerchantOrderId { get; set; }
    public string PurchaseDate { get; set; }
    public string LastUpdateDate { get; set; }
    public string OrderStatus { get; set; }
    public string Rate { get; set; }
    public string FulfillmentChannel { get; set; }
    public string SaleChannel { get; set; }
    public Listing Listing { get; set; }
    public Dictionary<string, Purchased> PurchaseDictionary { get; set; }
    public string OrderChannel { get; set; }
    public string Url { get; set; }
    public string ShipServiceLevel { get; set; }
    public string ProductName { get; set; }
    public string Sku { get; set; }
    public string Asin { get; set; }
    public string Tracking { get; set; }
    public string ItemStatus { get; set; }
    public string Quantity { get; set; }
    public string PrintError { get; set; }
    public string Currency { get; set; }
    public string ItemPrice { get; set; }
    public string ItemTax { get; set; }
    public string ShippingPrice { get; set; }
    public string ShippingTax { get; set; }
    public string Country { get; set; }
    public string BuyerEmail { get; set; }
    public string Weight { get; set; }
    public string Height { get; set; }
    public string Width { get; set; }
    public string Length { get; set; }
}