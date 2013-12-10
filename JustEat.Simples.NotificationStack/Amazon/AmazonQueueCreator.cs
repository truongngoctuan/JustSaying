using Amazon;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Lookups;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;

namespace JustEat.Simples.NotificationStack.Stack.Amazon
{
    public class AmazonQueueCreator : IVerifyAmazonQueues
    {
        public SqsQueueByName VerifyOrCreateQueue(IMessagingConfig configuration, IMessageSerialisationRegister serialisationRegister, string queueName, string topic, int messageRetentionSeconds, int visibilityTimeoutSeconds = 30, int? instancePosition = null)
        {
            var sqsclient = AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.GetBySystemName(configuration.Region));
            var snsclient = AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(RegionEndpoint.GetBySystemName(configuration.Region));

            var queue = new SqsQueueByName(queueName, sqsclient);

            var eventTopic = new SnsTopicByName(new SnsPublishEndpointProvider(configuration).GetLocationName(topic), snsclient, serialisationRegister);

            if (!queue.Exists())
                queue.Create(messageRetentionSeconds, 0, visibilityTimeoutSeconds);

            if (!eventTopic.Exists())
                eventTopic.Create();

            if (!eventTopic.IsSubscribed(queue))
                eventTopic.Subscribe(queue);

            if (!queue.HasPermission(eventTopic))
                queue.AddPermission(eventTopic);

            return queue;
        }
    }
}