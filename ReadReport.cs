using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class ReadReport
{
    private static string PATH = @"C:\Users\desol\Downloads";
    public static List<Purchased> ReadShipped()
    {
        string filePath = "";
        while (filePath == "")
            filePath = GetFileName();
        System.Console.WriteLine(filePath);
        var orders = Read(filePath);
        return orders;
    }
    public static string GetFileName()
    {
        DirectoryInfo d = new DirectoryInfo(PATH);//Assuming Test is your Folder
        FileInfo[] Files = d.GetFiles("*.csv"); //Getting Text files
        foreach (FileInfo file in Files)
        {
            if (file.FullName.Contains("orders_")) return file.FullName;
        }
        return "";
    }
    public static List<Purchased> Read(string filePath)
    {
        while (!File.Exists(filePath))
        {
            Console.WriteLine(filePath + " does not exist");
            Task.Delay(2000).Wait();
        }
        var fileAsString = WriteSafeReadAllLines(filePath);
        List<Purchased> orders = new List<Purchased>();
        foreach (string line in fileAsString)
        {

            Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            var fields = csvParser.Split(line);
            if (line.Contains("Order Date")) continue;
            var purchased = BuildOrderFromFields(fields);
            orders.Add(purchased);
        }
        System.Console.WriteLine(orders.Count);
        return orders;
    }
    public static string[] WriteSafeReadAllLines(String path)
    {
        using (var csv = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(csv))
        {
            List<string> file = new List<string>();
            while (!sr.EndOfStream)
            {
                file.Add(sr.ReadLine());
            }

            return file.ToArray();
        }
    }
    public static readonly Regex DisallowedCharsInTableKeys = new Regex(@"[\\\\#%+/?\u0000-\u001F\u007F-\u009F]");


    //InvoiceStatus	Total Amount	Invoice Due Amount	Invoice Issue Date	Invoice Due Date	Payment Reference ID	Payment Date	Payment Amount	Payment Instrument Type	Payment Identifier	Shipment Date	Shipment Status	Carrier Tracking #	Shipment Quantity	Shipping Address	Shipment Subtotal	Shipment Shipping & Handling	Shipment Promotion	Shipment Tax	Shipment Net Total	Carrier Name	Product Category	ASIN	Title	UNSPSC	Brand Code	Brand	Manufacturer	National Stock Number	Item model number	Part number	Product Condition	Company Compliance	Listed PPU	Purchase PPU	Item Quantity	Item Subtotal	Item Shipping & Handling	Item Promotion	Item Tax	Item Net Total	PO Line Item Id	Tax Exemption Applied	Tax Exemption Type	Tax Exemption Opt Out	Discount Program	Pricing Discount applied ($ off)	Pricing Discount applied (% off)	Order Type	GL Code	Department	Cost Center	Project Code	Location	Custom Field 1	Seller Name	Seller Credentials	Seller Address

    private static Purchased BuildOrderFromFields(string[] fields)
    {
        return new Purchased
        {
            OrderDate = DateTime.Parse(fields[0]),
            OrderId = fields[1],
            OrderStatus = fields[3],
            AccountUser = fields[4],
            ShipmentDate = fields[5],
            ShipmentStatus = fields[6],
            CarrierTracking = fields[7].Replace("\"", "").Replace("=", ""),
            ShipmentQuantity = fields[8],
            CarrierName = fields[9],
            ASIN = fields[10].Replace("\"", "").Replace("=", ""),
            Title = fields[11].Replace("\"", "").Replace("=", ""),
            Condition = fields[12],
            ItemQuantity = fields[13],
            ItemNet = fields[15].Replace("\"", "").Replace("=", ""),
            SellerName = fields[15],

        };
    }
    public static void Delete()
    {

        if (File.Exists(PATH))
            File.Delete(PATH);
    }
}