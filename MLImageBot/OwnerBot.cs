using BaseBotLib.Interfaces.Bot;
using BaseBotLib.Interfaces.Logger;
using MLImageLib;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MLImageBot
{
    public class OwnerBot
    {
        private readonly IBot _bot;
        private readonly ILogger _logger;
        private readonly Model _model;

        public OwnerBot(IBot bot, ILogger logger, Model model)
        {
            _bot = bot;
            _logger = logger;
            _model = model;
        }

        public async Task CheckRequests()
        {
            var newMessages = await _bot.GetNewMessages();

            foreach (var newMessage in newMessages)
            {
                _logger.Info($"Новое сообщение для бота : \"{newMessage.Text}\" от пользователя \"{newMessage.UserName}\".");
                await ProcessMessage(newMessage);
            }
        }

        private async Task ProcessMessage(Message message)
        {
            var msg = message.Text?.ToLower();
            switch (msg)
            {
                case "привет":
                    await HiMessage(message);
                    return;
            }

            if (!string.IsNullOrWhiteSpace(message.FileId))
            {
                var fileBody = await _bot.GetFile(message.FileId);
                if (fileBody == null)
                {
                    return;
                }

                var tempPath = Path.GetTempPath();
                var filePath = tempPath + "/" + message.FileId;

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(fileBody, 0, fileBody.Length);
                }


                var classification = _model.ClassifySingleImage(filePath);
                var percent = classification.score.Max() * 100;

                var result = 
                    $"Данное изображение содержит : {classification.label}" +
                    $" (вероятность = {percent.ToString("0.##")} %)";
                await SendMessage(message, result);
            }
        }

        private Task HiMessage(Message message)
        {
            return _bot.SendMessage(message.ChatId.ToString(), "Привет привет");
        }

        private Task SendMessage(Message message, string text)
        {
            return _bot.SendMessage(message.ChatId.ToString(), text);
        }
    }
}