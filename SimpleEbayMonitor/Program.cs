using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace SimpleEbayMonitor
{
    class Program
    {
        private static readonly string ItemsFile = "Items.txt";
        private static readonly Uri SearchUri = new Uri("https://www.ebay.com/sch/i.html?_sop=10&rt=nc&LH_BIN=1&_nkw=");
        private static readonly string Pattern = "https://www.ebay.com/itm/[^\"]+";
        private static string PricePattern = "item__price\">[^,]+";
        private const int PricePatternSkip = 13;

        // Parameters
        private const double CurrencyMultiplier = 1d / 67;
        private const int TooManyItemsCount = 10;
        private const int DelaySeconds = 5;

        static void Main()
        {
            Console.WriteLine("Enter search query (ex. 'macbook pro'):");
            var query = Console.ReadLine();

            Console.WriteLine("Enter max price (ex. 1000):");
            var maxPrice = int.Parse(Console.ReadLine());

            var items = new List<string>();
            var client = new WebClient();

            if (File.Exists(ItemsFile))
            {
                items.AddRange(File.ReadAllLines(ItemsFile));
                Console.WriteLine($"Initial items loaded: {items.Count}");
            }

            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(DelaySeconds));

                string page;
                try
                {
                    page = client.DownloadString(SearchUri + query);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                var tempNewItems = Regex.Matches(page, Pattern)
                    .Select(e => e.Value).Distinct().ToArray();

                var tempNewPrices = Regex.Matches(page, PricePattern)
                    .Select(e => e.Value).ToArray();

                var newPrices = tempNewItems.Zip(tempNewPrices, (k, v) => new { k, v })
                    .ToDictionary(x => x.k, x => x.v);

                var newItems = tempNewItems.Except(items).ToArray();
                items.AddRange(newItems);
                File.AppendAllLines(ItemsFile, newItems);
                Console.WriteLine($"New items loaded: {newItems.Length}");

                if (newItems.Length > TooManyItemsCount)
                {
                    Console.WriteLine("Too many new items. Skipping.");
                    continue;
                }

                foreach (var newItem in newItems)
                {
                    var price = double.Parse(newPrices[newItem]
                                    .Substring(PricePatternSkip)
                                    .Replace(" ", "")) * CurrencyMultiplier;
                    Console.WriteLine($"Price: {(int)price}, {newItem}");

                    if (price <= maxPrice)
                    {
                        Console.Beep();
                        OpenUrl(newItem);
                    }
                }
            }
        }


        private static void OpenUrl(string url)
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}
