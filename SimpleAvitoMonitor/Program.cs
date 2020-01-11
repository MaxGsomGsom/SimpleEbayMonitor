using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace SimpleAvitoMonitor
{
    public class Program
    {
        private static string Query = "MacBook+Pro";
        private static int MaxItems = 5;
        private static int MinPrice = 10000;
        private static int MaxPrice = 60000;
        private static TimeSpan Delay = TimeSpan.FromSeconds(5);
        private static int Iteration;

        private const string QueryArgName = "query";
        private const string MinPriceArgName = "min_price";
        private const string MaxPriceArgName = "max_price";
        private const string DelayArgName = "delay";
        private const string MaxItemsArgName = "max_items";
        private const string ItemUriPrefix = "https://www.avito.ru";

        private static readonly string ItemsFile = "items.txt";

        private static readonly string ItemUriPattern = "/sankt-peterburg/noutbuki/[^\"]+";

        private static readonly string SearchUriPattern = "https://www.avito.ru/sankt-peterburg/noutbuki" +
                                                          $"?pmax={MaxPriceArgName}" + 
                                                          $"&pmin={MinPriceArgName}" +
                                                          "&s=104" +
                                                          $"&q={QueryArgName}";

        private static string HelpText => "==================================================\n" +
                                                  "Arguments and current values:\n\n" +
                                                  $"-{QueryArgName}={Query} - search query\n" +
                                                  $"-{MinPriceArgName}={MinPrice} - price in currency of your Ebay account\n" +
                                                  $"-{MaxPriceArgName}={MaxPrice} - price in currency of your Ebay account\n" +
                                                  $"-{DelayArgName}={Delay.Seconds} - delay between requests in seconds\n" +
                                                  $"-{MaxItemsArgName}={MaxItems} - max number of items to show after new request\n" +
                                                  "==================================================\n";

        static void Main(string[] args)
        {
            if (!ParseArguments(args)) return;

            var uri = SearchUriPattern
                .Replace(QueryArgName, Query)
                .Replace(MinPriceArgName, MinPrice.ToString())
                .Replace(MaxPriceArgName, MaxPrice.ToString());

            var items = new List<string>();

            if (File.Exists(ItemsFile))
            {
                items.AddRange(File.ReadAllLines(ItemsFile));
                Console.WriteLine($"Initial items loaded from file: {items.Count}");
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

                var newItems = Regex.Matches(page, ItemUriPattern)
                    .Select(e => ItemUriPrefix + e.Value)
                    .Distinct().Except(items).ToArray();

                items.AddRange(newItems);
                File.AppendAllLines(ItemsFile, newItems);
                Console.Write($"Iteration {++Iteration}. New items loaded: {newItems.Length}.{(newItems.Any() ? '\n' : '\r')}");

                if (newItems.Length > MaxItems)
                {
                    newItems = newItems.Take(MaxItems).ToArray();
                    Console.WriteLine($"Too many new items. First {MaxItems} will be shown.");
                }

                foreach (var newItem in newItems)
                {
                    Console.Beep();
                    Console.WriteLine(newItem);
                    OpenUrl(newItem);
                }
            }
        }

        private static bool ParseArguments(string[] args)
        {
            try
            {
                var tempQuery = args.FirstOrDefault(e => e.Contains(QueryArgName))?.Split('=').LastOrDefault();
                Query = string.IsNullOrWhiteSpace(tempQuery) ? Query : tempQuery;
                if (string.IsNullOrWhiteSpace(Query)) throw new ArgumentOutOfRangeException(QueryArgName);

                var tempMinPrice = args.FirstOrDefault(e => e.Contains(MinPriceArgName))?.Split('=').LastOrDefault();
                MinPrice = string.IsNullOrWhiteSpace(tempMinPrice) ? MinPrice : int.Parse(tempMinPrice);
                if (MinPrice < 1) throw new ArgumentOutOfRangeException(MinPriceArgName);

                var tempMaxPrice = args.FirstOrDefault(e => e.Contains(MaxPriceArgName))?.Split('=').LastOrDefault();
                MaxPrice = string.IsNullOrWhiteSpace(tempMaxPrice) ? MaxPrice : int.Parse(tempMaxPrice);
                if (MaxPrice <= MinPrice) throw new ArgumentOutOfRangeException(MaxPriceArgName);

                var tempDelay = args.FirstOrDefault(e => e.Contains(DelayArgName))?.Split('=').LastOrDefault();
                Delay = string.IsNullOrWhiteSpace(tempDelay) ? Delay : TimeSpan.FromSeconds(int.Parse(tempDelay));
                if (Delay.Seconds < 1) throw new ArgumentOutOfRangeException(DelayArgName);

                var tempMaxItems = args.FirstOrDefault(e => e.Contains(MaxItemsArgName))?.Split('=').LastOrDefault();
                MaxItems = string.IsNullOrWhiteSpace(tempMaxItems) ? MaxItems : int.Parse(tempMaxItems);
                if (MaxItems < 1) throw new ArgumentOutOfRangeException(MaxItemsArgName);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Incorrect input arguments: {e.Message}");
                return false;
            }
            finally
            {
                Console.WriteLine(HelpText);
            }

            return true;
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
