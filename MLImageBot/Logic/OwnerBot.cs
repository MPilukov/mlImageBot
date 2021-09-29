using BaseBotLib.Interfaces.Bot;
using BaseBotLib.Interfaces.Logger;
using MLImageLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MLImageBot.Logic
{
    public class OwnerBot
    {
        private readonly Dictionary<int, UserData> _usersData;
        private readonly IBot _bot;
        private readonly ILogger _logger;
        private readonly Model _model;

        private readonly string _setsPath;
        private readonly string _tempImageDir;

        public OwnerBot(
            IBot bot, ILogger logger, Model model, string setsPath, string tempImageDir)
        {
            _bot = bot;
            _logger = logger;
            _model = model;

            _usersData = new Dictionary<int, UserData>();

            _setsPath = setsPath;
            _tempImageDir = tempImageDir;
        }

        public async Task CheckRequests()
        {
            var newMessages = await _bot.GetNewMessages();

            foreach (var newMessage in newMessages)
            {
                _logger.Info($"Новое сообщение для бота : \"{newMessage.Text}\"" +
                    $" от пользователя \"{newMessage.UserName}\".");
                await ProcessMessage(newMessage);
            }
        }

        private async Task ProcessMessage(Message message)
        {
            var msg = message.Text?.ToLower();
            var userData = GetUserData(message.ChatId);

            switch (msg)
            {
                case "привет":
                    await SendMessage(message.ChatId, "Привет привет");
                    return;
                case "изменить описание":
                    await ChangeImageLabelMenu(message.ChatId);
                    return;
            }

            if (!string.IsNullOrWhiteSpace(message.FileId))
            {
                await ProcessFile(message.ChatId, message.FileId, userData);
                return;
            }

            await PreActionHandle(message.ChatId, msg, userData);
        }

        private UserData GetUserData(int chatId)
        {
            if (_usersData.TryGetValue(chatId, out var userData))
            {
                return userData;
            }

            var newUserData = new UserData();
            _usersData.Add(chatId, newUserData);
            return newUserData;
        }

        private async Task ProcessFile(int chatId, string fileId, UserData userData)
        {
            var fileBody = await _bot.GetFile(fileId);
            if (fileBody == null)
            {
                return;
            }

            var fileDir = _tempImageDir + "/" + chatId;
            var filePath = fileDir + "/" + fileId;

            Directory.CreateDirectory(fileDir);

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(fileBody, 0, fileBody.Length);
            }

            var (score, label) = _model.ClassifySingleImage(filePath);
            var percent = score.Max() * 100;

            var result =
                $"Данное изображение содержит : {label}" +
                $" (вероятность = {percent.ToString("0.##")} %)";
            await SendMessage(chatId, result);

            await GetChangeLabelMenuMessage(chatId, userData, filePath);
        }

        private Task GetChangeLabelMenuMessage(int chatId, UserData userData, string filePath)
        {
            userData.Actions.Push(EAction.ChangeLabel);
            userData.PreData[EAction.ChangeLabel] = filePath;

            return _bot.CreateKeyboard(chatId.ToString(),
                "Если описание не верно, то вы можете его изменить",
                new[] { EAction.ChangeLabel.GetName() }, true, true);
        }

        private Task SendMessage(int chatId, string text)
        {
            return _bot.SendMessage(chatId.ToString(), text);
        }

        private string[] GetLabels()
        {
            var labels = Directory.GetDirectories(_setsPath);
            return labels.Select(x => x
                .Replace(_setsPath, "")
                .Replace("/", "")
                .Replace("\\", "")).ToArray();
        }

        private Task ChangeImageLabelMenu(int chatId)
        {
            var labels = GetLabels();

            return _bot.CreateKeyboard(chatId.ToString(),
                "Выберите описание для этой картинки из списка, либо введите своё описание",
                labels, true, true);
        }

        private async Task ChangeImageLabel(int chatId, string label, UserData userData)
        {
            if (!userData.PreData.TryGetValue(EAction.ChangeLabel, out var filePath))
            {
                return;
            }

            userData.PreData.Remove(EAction.ChangeLabel);

            var labelDir = _setsPath + "/" + label.ToLower();
            CopyFile(filePath, labelDir);

            _model.FitModel();
            await SendMessage(chatId, "Описание изображения изменено");
        }

        private static void CopyFile(string fileSource, string labelDir)
        {
            if (!Directory.Exists(labelDir))
            {
                Directory.CreateDirectory(labelDir);
            }

            var fileExtension = Path.GetExtension(fileSource);
            File.Copy(fileSource, labelDir + "/" + Guid.NewGuid() + fileExtension);
            File.Delete(fileSource);
        }

        private async Task PreActionHandle(int chatId, string text, UserData userData)
        {
            if (userData.Actions.Count == 0)
            {
                return;
            }

            var preAction = userData.Actions.Pop();

            switch (preAction)
            {
                case EAction.ChangeLabel:
                    await ChangeImageLabel(chatId, text, userData);
                    return;
            }
        }
    }
}