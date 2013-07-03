using System;

namespace SimplesNotificationStack.Messaging.Lookups
{
    public interface IPublishSubscribtionEndpointProvider
    {
        string GetLocationEndpoint(Component component, NotificationTopic topic);
    }

    /// <summary>
    /// Provides endpoint locations for SQS queues subscribed to topics
    /// </summary>
    public class SqsSubscribtionEndpointProvider : IPublishSubscribtionEndpointProvider
    {
        public string GetLocationEndpoint(Component component, NotificationTopic topic)
        {
            switch (component)
            {
                case Component.OrderEngine:
                    if (topic == NotificationTopic.CustomerCommunication)
                        return "https://sqs.eu-west-1.amazonaws.com/507204202721/uk-qa12-orderengine-customer-order-communication";
                    break;
            }

            throw new IndexOutOfRangeException(string.Format("Cannot map an endpoint for component '{0}' and topic '{1}'", component.ToString(), topic.ToString()));
        }
    }
}