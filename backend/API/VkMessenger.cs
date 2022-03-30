using Newtonsoft.Json;
using System.Net;

namespace SupportBackend
{
    using Attachments = List<Attachment>;

    public class VkMessenger : IMessenger
    {
        // TODO : убрать в конфиг
        private const string TOKEN = "1d18a6907f2f1b2fcd9f4d2e517351e6faffaac36e5081dfeeef7bacd8942b3b27c29354da582512f7017";
        private const string VERSION = "5.131";
        private const string GROUP_ID = "210608348";
        private const string SEND_MSG_URL = "https://api.vk.com/method/messages.send";
        private const string GET_LONG_POLL_URL = "https://api.vk.com/method/groups.getLongPollServer";
        private const string HANDLE_EVENTS_URL_TEMPLATE = "{0}?act=a_check&key={1}&ts={2}&wait=25";

        private VkMessenger()
        {
            clientsProcessor = ClientsProcessor.GetInstance();
            httpClient = new HttpClient();
            StartHandlingMessages();
        }

        private static VkMessenger instance;

        public static VkMessenger GetInstance()
        {
            if (instance == null)
            {
                instance = new VkMessenger();
            }

            return instance;
        }

        private ClientsProcessor clientsProcessor;
        private HttpClient httpClient;
        private bool enabled;

        // начать получать сообщения из ВК
        public void StartHandlingMessages()
        {
            if (!enabled)
            {
                HandleMessages();
                Logger.Info("Начато чтение сообщениий из ВК", "VkMessenger.StartHandlingMessages");
                enabled = true;
            }
        }

        // остановить получение сообщений из ВК
        public void StopHandlingMessages()
        {
            enabled = false;
        }

        // отправить сообщение пользователю ВК
        public void Send(Message msg)
        {
            var values = new Dictionary<string, string>
            {
                { "access_token", TOKEN },
                { "user_id", msg.UserId },
                { "random_id", "0" }, // TODO : поменять на рандомное число?
                { "message", msg.Text },
                { "v", VERSION }
            };
            var content = new FormUrlEncodedContent(values);

            // отправка запроса
            Logger.Info(string.Format("Отправка сообщения пользователю ВК {0} :\n{1}", msg.GetKey(), msg.Text), "VkMessenger.Send");
            var ans = httpClient.PostAsync(SEND_MSG_URL, content).Result;

            // разбор ответа
            var ansBody = ans.Content.ReadAsStringAsync().Result;
            dynamic ansJson = JsonConvert.DeserializeObject(ansBody);
            
            if (ans.StatusCode == HttpStatusCode.OK && ansJson["response"] != null)
            {
                Logger.Info(
                    string.Format("Cообщение доставлено пользователю ВК {0}, ответ мессенджера:\n{1}", msg.GetKey(), ansBody), 
                    "VkMessenger.Send"
                );
            } 
            else
            {
                Logger.Error(
                    string.Format("Cообщение не доставлено пользователю ВК {0}, ответ мессенджера:\n{1}", msg.GetKey(), ansBody),
                    "VkMessenger.Send"
                );
            }
        }

        // получать новые сообщения из ВК
        private async void HandleMessages()
        {
            await Task.Run(() =>
            {
                var values = new Dictionary<string, string>
                {
                    { "group_id", GROUP_ID },
                    { "access_token", TOKEN },
                    { "v", VERSION }
                };
                var content = new FormUrlEncodedContent(values);

                var ans = httpClient.PostAsync(GET_LONG_POLL_URL, content).Result;
                var ansBody = ans.Content.ReadAsStringAsync().Result;

                // TODO : валидировать ответ вк
                dynamic ansJson = JsonConvert.DeserializeObject(ansBody);

                string key = ansJson.response.key;
                string server = ansJson.response.server;
                string ts = ansJson.response.ts;

                while (enabled)
                {
                    string url = string.Format(
                        HANDLE_EVENTS_URL_TEMPLATE,
                        server,
                        key,
                        ts
                    );

                    try
                    {
                        ans = httpClient.GetAsync(url).Result;
                        ansBody = ans.Content.ReadAsStringAsync().Result;

                        Logger.Debug("События ВК\n" + ansBody, "VkMessenger.HandleMessages");

                        // TODO : валидировать ответ вк
                        ansJson = JsonConvert.DeserializeObject(ansBody);
                        ts = ansJson.ts;

                        foreach (var update in ansJson.updates)
                        {
                            if (update.type != "message_new" && update.type != "message_edit")
                            {
                                Logger.Warn("Недопустимый тип события, нужно отключить в настройках сообщества", "VkMessenger.HandleMessages");
                                continue;
                            }

                            var updateObj = update["object"];

                            Attachments attachments = new Attachments();
                            if (updateObj.message.attachments != null)
                            {
                                foreach (var attachment in updateObj.message.attachments)
                                {

                                    if (attachment.type == "photo")
                                    {
                                        // TODO : доставать имя файла, выбрать максимальный размер фотки
                                        int lastPhotoIdx = attachment.photo.sizes.Count - 1;
                                        attachments.Add(new Attachment("photo.jpg", (string)attachment.photo.sizes[lastPhotoIdx].url));
                                    }
                                }
                            }
                            

                            Message msg = new Message(
                                (string)updateObj.message.text,
                                update.type == "message_new" ? MessageType.New : MessageType.Edit,
                                MessageDirection.Operator,
                                "Vk",
                                (string)updateObj.message.from_id,
                                (string)updateObj.message.id,
                                attachments
                            );
                            
                            // отправим сообщение оператору
                            clientsProcessor.HandleMessage(msg);
                        }
                    }
                    catch (Exception)
                    {}
                }

                Logger.Info("Остановлено чтений сообщениий из ВК", "VkMessenger.HandleMessages");
            });
        }
    }
}
