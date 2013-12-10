using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using NLog;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public abstract class SnsTopicBase
    {
        private readonly IMessageSerialisationRegister _serialisationRegister;
        public string Arn { get; protected set; }
        public IAmazonSimpleNotificationService Client { get; protected set; }
        private static readonly Logger EventLog = LogManager.GetLogger("EventLog");
        private static readonly Logger Log = LogManager.GetLogger("JustEat.Simples.NotificationStack");

        public SnsTopicBase(IMessageSerialisationRegister serialisationRegister)
        {
            _serialisationRegister = serialisationRegister;
        }

        public abstract bool Exists();

        public bool IsSubscribed(SqsQueueBase queue)
        {
            var result = Client.ListSubscriptionsByTopic(new ListSubscriptionsByTopicRequest(Arn));
            
            return result.Subscriptions.Any(x => !string.IsNullOrEmpty(x.SubscriptionArn) && x.Endpoint == queue.Arn);
            
            return false;
        }

        public bool Subscribe(SqsQueueBase queue)
        {
            var response = Client.Subscribe(new SubscribeRequest(Arn, "sqs", queue.Arn));
            if (!string.IsNullOrEmpty(response.SubscriptionArn))
            {
                queue.AddPermission(this);
                Log.Info(string.Format("Subscribed Queue to Topic - Queue: {0}, Topic: {1}", queue.Arn, Arn));
                return true;
            }
            Log.Info(string.Format("Failed to subscribe Queue to Topic: {0}, Topic: {1}", queue.Arn, Arn));
            return false;
        }

        public void Publish(Message message)
        {
            var messageToSend = _serialisationRegister.GetSerialiser(message.GetType()).Serialise(message);
            var messageType = message.GetType().Name;

            Client.Publish(new PublishRequest
                               {
                                   Subject = messageType,
                                   Message = messageToSend,
                                   TopicArn = Arn
                               });

            EventLog.Info("Published message: '{0}' with content {1}", messageType, messageToSend);
        }
    }
}