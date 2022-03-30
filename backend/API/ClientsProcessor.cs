using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SupportBackend
{
    // контейнер в виде HashMap для хранения активных клиентов
    // составной ключ - Мессенджер:IdПользователя (например Vk:141576988)
    // значение - список из клиентов, которые общаются с пользователем мессенджера
    using ClientsContainer = ConcurrentDictionary<string, List<Client>>;

    public class ClientsProcessor
    {
        private static ClientsProcessor instance;

        private ClientsProcessor() 
        {
            clientsContainer = new ClientsContainer();
            DeleteDisconectedClientsAsync();
        }

        public static ClientsProcessor GetInstance()
        {
            if (instance == null)
            {
                instance = new ClientsProcessor();
            }

            return instance;
        }

        private ClientsContainer clientsContainer;

        // с определенной периодичностью удалять отвалившихся клиентов из хранилища
        private async void DeleteDisconectedClientsAsync()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    // TODO : возможно, вынести в конфиг время
                    Thread.Sleep(3000);

                    var updatedClients = new ClientsContainer();
                    foreach (var key in clientsContainer.Keys)
                    {
                        var clients = clientsContainer[key];
                        foreach (var client in clients)
                        {
                            if (client.IsConnected())
                            {
                                updatedClients.TryAdd(key, new List<Client>());
                                updatedClients[key].Add(client);
                            }
                        }
                    }

                    // TODO : возможно, нужно использовать блокировки на время обновления хранилища
                    clientsContainer = updatedClients;
                }
            }); 
        }

        // добавить клиента (как бы это ни было странно)
        public void AddClient(Client client)
        {
            var key = client.GetKey();
            clientsContainer.TryAdd(key, new List<Client>());
            clientsContainer[key].Add(client);
        }

        // обработать сообщение от пользователя мессенджера
        public void HandleMessage(Message msg)
        {
            var key = msg.GetKey();

            Logger.Info(
                string.Format("Новое сообщение от пользователя мессенджера {0} :\n{1}", key, msg.Text),
                "VkMessenger.Send"
            );

            if (clientsContainer.ContainsKey(key))
            {
                var clients = clientsContainer[key];
                foreach (var client in clients)
                {
                    client.Send(msg);
                }
            }
        }
    }
}
