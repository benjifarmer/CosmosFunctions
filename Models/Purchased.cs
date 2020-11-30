using System;
using Newtonsoft.Json;


public class Purchased
{
    public string id
    {
        get; set;

    }

    public DateTime? OrderDate { get; set; }
    public bool Damaged { get; set; }
    public DateTime DamagedDate { get; set; }
    public string DamagedNote { get; set; }
    public string Venue { get; set; }
    public string OrderId { get; set; }
    public string OrderQuantity { get; set; }
    public string OrderSubtotal { get; set; }
    public string OrderShippingHandling { get; set; }
    public string OrderPromotion { get; set; }
    public string OrderTax { get; set; }
    public string OrderNet { get; set; }
    public string OrderStatus { get; set; }
    public string AccountUser { get; set; }
    public string ShipmentDate { get; set; }
    public string ShipmentStatus { get; set; }
    public string CarrierTracking { get; set; }
    public string ShipmentQuantity { get; set; }
    [JsonProperty(PropertyName = "asin")]
    public string ASIN { get; set; }
    public string Title { get; set; }
    public string Condition { get; set; }
    public string ItemQuantity { get; set; }
    public string ItemSubtotal { get; set; }
    public string SellerName { get; set; }
    public string SellerCredentials { get; set; }
    public string AccountGroup { get; set; }
    public string PONumber { get; set; }
    public string Approver { get; set; }
    public string AccountUserEmail { get; set; }
    public string CarrierName { get; set; }
    public string AssociatedOrderId { get; set; }
    public string ItemNet { get; set; }
}