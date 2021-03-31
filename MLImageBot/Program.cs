using BaseBotLib.Interfaces.Logger;
using BaseBotLib.Interfaces.Storage;
using BaseBotLib.Services.Bot;
using BaseBotLib.Services.Logger;
using BaseBotLib.Services.Storage;
using Microsoft.Extensions.Configuration;
using MLImageBot.Logic;
using MLImageLib;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MLImageBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logger = GetLogger();

            try
            {
                var configs = GetConfiguration();

                var botId = configs["AppSettings:bot.id"];
                var botToken = configs["AppSettings:bot.token"];
                var storage = GetStorage(configs, logger);                

                var bot = new Bot(botId, botToken, storage, logger);

                var inceptionPath = configs["AppSettings:ml.inceptionPath"];
                var setsPath = configs["AppSettings:ml.setsPath"];
                var modelPath = configs["AppSettings:ml.modelPath"];
                var tempImageDir = configs["AppSettings:ml.tempImageDir"];

                var model = new Model(inceptionPath, setsPath, modelPath);
                model.FitModel();

                var ownerBot = new OwnerBot(bot, logger, model, setsPath, tempImageDir);

                while (true)
                {
                    try
                    {
                        await ownerBot.CheckRequests();
                    }
                    catch (Exception exc)
                    {
                        logger.Error($"Ошибка при обработке сообщения : {exc}");
                    }
                    await Task.Delay(100);
                }

                logger.Info("Работа программы завершена. Нажмите любую клавишу для выхода.");
                Console.ReadKey();
            }
            catch (Exception exc)
            {
                logger.Error("Не удалось запустить бота. Завершаем работу.");
                logger.Error($"Exception : {exc}");
            }
        }

        private static ILogger GetLogger()
        {
            return new ConsoleLogger();
        }

        private static IStorage GetStorage(IConfiguration configs, ILogger logger)
        {
            return new FileStorage(configs["AppSettings:storage.fileName"], logger);
        }

        private static IConfiguration GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            return builder.Build();
        }
    }
}