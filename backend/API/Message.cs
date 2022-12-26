using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SupportBackend
{
    using Attachments = List<Attachment>;

    public enum MessageType
    {
        New,
        Edit
    }

    public enum MessageDirection
    {
        Operator,
        Messenger
    }

    public class Attachment
    {
        [JsonProperty("FileName")]
        public string FileName { get; }
        [JsonProperty("Url")]
        public string Url { get; }
        // TODO : подумать над необходимостью гонять Base64
        //[JsonProperty("Base64")]
        //public string Base64 { get; }

        public Attachment(string fileName, string url)
        {
            FileName = fileName;
            Url = url;
        }
    }

    public class Message
    {
        [JsonProperty("Text")]
        public string Text { get; }
        [JsonProperty("UserId")]
        public string UserId { get; }
        [JsonProperty("MessengerType")]
        public string MessengerType { get; }
        [JsonProperty("MessageType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public MessageType MessageType { get; }
        [JsonProperty("MessageDirection")]
        [JsonConverter(typeof(StringEnumConverter))]
        public MessageDirection MessageDirection { get; }
        [JsonProperty("MessengerMsgId")]
        public string MessengerMsgId { get; }
        [JsonProperty("Attachments")]
        public Attachments Attachments { get; }

        private bool valid = true;
        
        public bool IsValid()
        {
            return valid;
        }

        public Message(string text, MessageType messageType, MessageDirection direction, string messengerType, string userId, string messengerMsgId, Attachments attachments) 
        {
            Text = text;
            MessageType = messageType;
            MessageDirection = direction;
            MessengerType = messengerType;
            UserId = userId;
            MessengerMsgId = messengerMsgId;

            Attachments = new Attachments();
            if (attachments != null)
            {
                Attachments = attachments;
            }
        }

        private Message()
        {

        }

        public static Message ParseJson(string json)
        {
            Message msg;
            try
            {
                msg = JsonConvert.DeserializeObject<Message>(json);
            }
            catch (Exception e)
            {
                var errMsg = string.Format("Произошла ошибка при разборе сообщения из JSON:\n{0}\n{1}", json, e.Message);
                Logger.Error(errMsg,"Message.ParseJson");

                throw new InvalidClientMessageException(errMsg);
            }

            return msg;
        }

        public string GetKey()
        {
            // вынести формирование ключа в отдельный утилитарный метод и использовать его здесь и в классе Clients
            return string.Format("{0}:{1}", MessengerType, UserId);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
