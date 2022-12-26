using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace SupportBackend.Controllers
{
    /*[Route("/ws")]
    [ApiController]*/
    public class WebSocketController : ControllerBase
    {
        [HttpGet("/ws")]
        public async Task Get()
        {    
            // TODO : проверять авторизацию
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                // достаем параметры get запроса
                HttpRequest req = HttpContext.Request;
                string messengerType = req.Query["messengerType"];
                string messengerUserId = req.Query["messengerUserId"];

                Logger.Info(
                    string.Format("Запрос на соединение {0}/{1}", req.Host.Value, req.QueryString), 
                    "WebSocketController.Get"
                );

                // выбираем обработчик для нужного мессенджера
                IMessenger messenger;
                try
                {
                    messenger = GetMessengerInstance(messengerType);
                }
                catch (UnknownMessengerTypeException)
                {
                    // TODO : подумать, что отдать в ответе
                    HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                // создаем экземляр клиента, добавляем его в контейнер
                Client client = new Client(HttpContext.WebSockets.AcceptWebSocketAsync().Result, messengerType, messengerUserId);
                ClientsProcessor clientsProcessor = ClientsProcessor.GetInstance();
                clientsProcessor.AddClient(client);

                // обрабатываем сообщения от клиента
                await HandleClientMessages(client, messenger);
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }

        // получение обработчика для нужного мессенджера
        private static IMessenger GetMessengerInstance(string messengerType)
        {
            if (messengerType == "Vk")
            {
                return VkMessenger.GetInstance();
            }

            throw new UnknownMessengerTypeException(string.Format("Мессенджер {0} не поддерживается", messengerType));
        }

        // ожидание и обработка сообщений клиента (оператора)
        private async Task HandleClientMessages(Client client, IMessenger messenger)
        {
            while (client.IsConnected())
            {
                try
                {
                    // ожидаем сообщение от оператора из веб-клиента
                    Message message = await client.WaitForNewMessageAsync();

                    // отправляем в мессенджер
                    messenger.Send(message);
                }
                catch (InvalidClientMessageException)
                {
                    // TODO : возможно, обравать соединение при большом количестве инвалидных сообщений
                }
                catch (Exception e)
                {
                    Logger.Error(
                        string.Format("Непредвиденная ошибка при получении сообщения от оператора:\n{0}", e.Message), 
                        "WebSocketController.HandleClientMessages"
                    );
                }
            }
        }
    }
}
