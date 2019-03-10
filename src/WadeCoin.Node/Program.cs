using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WadeCoin.Node
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            if(args.Length <= 0 || string.IsNullOrWhiteSpace(args[0]))
                throw new ArgumentNullException("Port argument required");

            return WebHost.CreateDefaultBuilder()
            .UseUrls(new string[]{$"http://localhost:{args[0]}"})
            .UseStartup<Startup>();
        }
    }
}
