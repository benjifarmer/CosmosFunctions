
using System;
using Newtonsoft.Json;

public class Listing
{
	[JsonProperty(PropertyName = "partition")]
	public string Partition
	{
		get; set;
	}
	public string Shelf { get; set; }
	public int Row { get; set; }
	public Double Position { get; set; }
	public Double Stack { get; set; }
	public string Ranking { get; set; }
	public string LowNewPrice { get; set; }
	public string LowUsedPrice { get; set; }
	public string Weight { get; set; }
	public string Condition { get; set; }
	public string Note { get; set; }
	public string ASIN { get; set; }
	public string SKU { get; set; }
	public bool Listed { get; set; }
	public double Quanity { get; set; }
	public string Title { get; set; }
	[JsonProperty(PropertyName = "id")]
	public string Id { get; set; }
	public double OldPosition { get; set; }
	public string Upc { get; set; }
}