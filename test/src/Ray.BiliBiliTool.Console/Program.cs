﻿using System;
using System.Text.Json;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Agent.ServerChanAgent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Infrastructure;
using Serilog;
using System.Linq;
using System.Collections;

namespace Ray.BiliBiliTool.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            PreWorks(args);

            StartRun();

            //如果配置了“1”就立即关闭，否则保持窗口以便查看日志信息
            if (RayConfiguration.Root["CloseConsoleWhenEnd"] == "1") return;
            System.Console.ReadLine();
        }

        /// <summary>
        /// 初始化系统
        /// </summary>
        /// <param name="args"></param>
        public static void PreWorks(string[] args)
        {
            RayConfiguration.Root = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                //.AddJsonFile("appsettings.local.json", true,true)
                .AddEnvironmentVariables("Ray_")
                //.AddExcludeEmptyEnvironmentVariables("Ray_")
                .AddCommandLine(args, Constants.CommandLineMapper)
                .Build();


            Serilog.Events.LogEventLevel logEvent = GetConsoleLogLevel();

            bool b = logEvent == Serilog.Events.LogEventLevel.Information;
            System.Console.WriteLine(b);

            //日志:
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(RayConfiguration.Root)
                /*
                .WriteTo.TextWriter(PushService.PushStringWriter,
                                    logEvent,
                                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}\r\n")//用来做微信推送
                */
                .CreateLogger();

            var dictionary = Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Where(it => it.Key.ToString().StartsWith("Ray_", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(it => it.Key.ToString(), it => it.Value.ToString());

            System.Console.WriteLine("env:" + JsonSerializer.Serialize(dictionary, JsonSerializerOptionsBuilder.Builder(x => x.WriteIndented = true)));

            var nc = RayConfiguration.Root["DailyTaskConfig:NumberOfCoins"];

            Log.Logger.Information($"空:{nc == ""}");
            Log.Logger.Information($"null:{nc == null}");
            Log.Logger.Information($"空格:{nc == " "}");
            Log.Logger.Information($"5:{nc == "5"}");
            Log.Logger.Information($"10:{nc == "10"}");
            Log.Logger.Information($"20:{nc == "20"}");

            int.TryParse(nc, out int result);
            Log.Logger.Information($"value+1:{result + 1}");

            System.Console.WriteLine("test");

            //Host:
            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IConfiguration>(RayConfiguration.Root);
                    services.AddBiliBiliConfigs(RayConfiguration.Root);
                    services.AddBiliBiliClientApi();
                    services.AddDomainServices();
                    services.AddAppServices();
                })
                .UseSerilog()
                .UseConsoleLifetime();

            RayContainer.Root = hostBuilder.Build().Services;
        }

        /// <summary>
        /// 开始运行
        /// </summary>
        public static void StartRun()
        {
            using (var serviceScope = RayContainer.Root.CreateScope())
            {
                var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            }
        }

        /// <summary>
        /// 获取配置的Console的日志等级，作为推送日志的等级
        /// </summary>
        /// <returns></returns>
        private static Serilog.Events.LogEventLevel GetConsoleLogLevel()
        {
            var consoleLevelStr = RayConfiguration.Root["Serilog:WriteTo:0:Args:restrictedToMinimumLevel"];
            if (string.IsNullOrWhiteSpace(consoleLevelStr)) consoleLevelStr = "Information";

            System.Console.WriteLine(consoleLevelStr);

            Serilog.Events.LogEventLevel levelEnum = (Serilog.Events.LogEventLevel)
                Enum.Parse(typeof(Serilog.Events.LogEventLevel), consoleLevelStr);

            System.Console.WriteLine(levelEnum);

            return levelEnum;
        }
    }
}
