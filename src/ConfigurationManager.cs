using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace asktonidata {
    public sealed class ConfigurationManager {

        private static readonly ConfigurationManager _config = new ConfigurationManager();
        private static IConfigurationRoot Configuration { get; set; }

        ConfigurationManager() {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
        }

        public static ConfigurationManager Config {
            get 
            {
                return _config;
            }
        }

        public string GetSetting(string key) {
            return Configuration[key];
        }

        public void UpdateSetting(string key, string value) {
            Configuration[key] = value;
        }

    }
}