using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WadeCoin.Core;
using WadeCoin.Core.Models;
using WadeCoin.Core.Validation;

namespace WadeCoin.Node
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var crypto = new Crypto();
            (var privateKey, var publicKey) = crypto.GenerateKeys();
            Console.WriteLine($"Public Key:\n{publicKey}");
            Console.WriteLine();
            Console.WriteLine($"Public Key Hash:\n{crypto.DoubleHash(publicKey)}");

            services.AddSingleton<State>(new State(){
                BlockChain = BlockChain.Initialize(crypto, publicKey)
            });

            services.AddSingleton<PrivateState>(new PrivateState()
            {
                PublicKey = publicKey,
                PrivateKey = privateKey
            });

            services.AddTransient<ITransactionValidator, DefaultTransactionValidator>();
            services.AddTransient<IBlockValidator, DefaultBlockValidator>();
            services.AddTransient<IBlockChainValidator, DefaultBlockChainValidator>();
            services.AddTransient<ICrypto, Crypto>();

            services.AddHttpClient<IGossipService, HttpGossipService>();
            services.AddHostedService<MiningService>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseMvc();
        }
    }
}
