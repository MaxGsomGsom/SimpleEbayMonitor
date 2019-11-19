using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace SimpleEbayMonitor
{
    class Program
    {
        private static readonly string ItemsFile = "Items.txt";
        private static readonly string SearchUri = "https://www.ebay.com/sch/i.html?_sop=10&rt=nc&LH_BIN=1&_udhi=price&_nkw=query";
        private static readonly string Pattern = "https://www.ebay.com/itm/[^\"]+";

        // Parameters
        private const int MaxShowItemsNumber = 5;
        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(5);

        static void Main()
        {
            Console.WriteLine("Enter search query (ex. 'macbook pro 2018'):");
            var query = ReadLine(s => s);
            var uri = SearchUri.Replace("query", query);

            Console.WriteLine("Enter max price in RUB (ex. 60000):");
            var price = ReadLine(int.Parse);
            uri = uri.Replace("price", price.ToString());

            var items = new List<string>();

            if (File.Exists(ItemsFile))
            {
                items.AddRange(File.ReadAllLines(ItemsFile));
                Console.WriteLine($"Initial items loaded: {items.Count}");
            }

            while (true)
            {
                Thread.Sleep(Delay);

                string page;
                try
                {
                    page = new WebClient().DownloadString(uri);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                var newItems = Regex.Matches(page, Pattern)
                    .Select(e => e.Value)
                    .Distinct().Except(items).ToArray();

                items.AddRange(newItems);
                File.AppendAllLines(ItemsFile, newItems);
                Console.WriteLine($"New items loaded: {newItems.Length}");

                if (newItems.Length > MaxShowItemsNumber)
                {
                    newItems = newItems.Take(MaxShowItemsNumber).ToArray();
                    Console.WriteLine($"Too many new items. First {MaxShowItemsNumber} will be shown.");
                }

                foreach (var newItem in newItems)
                {
                    Console.Beep();
                    Console.WriteLine(newItem);
                    OpenUrl(newItem);
                }
            }
        }

        private static T ReadLine<T>(Func<string, T> parse)
        {
            while (true)
            {
                try
                {
                    return parse(Console.ReadLine());
                }
                catch { }
            }
        }

        private static void OpenUrl(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
    }
}
