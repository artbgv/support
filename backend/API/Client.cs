using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SupportBackend
{
    public class Client
    {
        public Client(WebSocket webSocket, string messengerType, string messengerUserId)
        {
            this.webSocket = webSocket;
            this.MessengerType = messengerType;
            this.MessengerUserId = messengerUserId;
        }

        public string MessengerType { get; }
        public string MessengerUserId { get; }
        private WebSocket webSocket;

        // отправка сообщения в веб-клент
        // TODO : проверять авторизацию при обмене сообщений с веб-клиентом
        public async void Send(Message message)
        {
            var jsonMsg = message.ToJson();
            byte[] buffer = Encoding.UTF8.GetBytes(jsonMsg);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // ожидание сообщения из веб-клиента
        public async Task<Message> WaitForNewMessageAsync()
        {
            // TODO : подумать над размером буфера (унести в конфиг?)
            byte[] buffer = new byte[1024 * 4];
            WebSocketReceiveResult recieved = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            string msgText = Encoding.UTF8.GetString(buffer, 0, recieved.Count);

            Logger.Info(
                string.Format("Новое сообщение от оператора для пользователя {0}:\n{1}", GetKey(), msgText),
                "Client.WaitForNewMessageAsync"
            );

            return Message.ParseJson(msgText);
        }

        // проверить активность соединения
        public bool IsConnected()
        {
            return webSocket.State == WebSocketState.Open;
        }

        public string GetKey()
        {
            return string.Format("{0}:{1}", MessengerType, MessengerUserId);
        }
    }
}
