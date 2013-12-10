using System;
using System.Collections.Generic;
using System.Threading;
using Amazon;
using Amazon.SQS.Model;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using NSubstitute;
using NUnit.Framework;
using Tests.MessageStubs;

namespace AwsTools.IntegrationTests
{
    // OK, I know it ain't pretty, but we needed this asap & it does the job. Deal with it. :)

    [TestFixture]
    public class BasicHandlingThrottlingTest
    {
        [TestCase(10000), Explicit]
        // Use this to manually test the performance / throttling of getting messages out of the queue.
        public void HandlingManyMessages(int throttleMessageCount)
        {
            var locker = new object();
            var awsQueueClient = AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1);
 
            var q = new SqsQueueByName("throttle_test", awsQueueClient);
            if (!q.Exists())
            {
                q.Create(60 * 2);
                Thread.Sleep(TimeSpan.FromMinutes(1));  // wait 60 secs for queue creation to be guaranteed completed by aws. :(
            }

            Assert.True(q.Exists());

            Console.WriteLine("{0} - Adding {1} messages to the queue.", DateTime.Now, throttleMessageCount);

            var entriesAdded = 0;
            // Add some messages
            do
            {
                var entries = new List<SendMessageBatchRequestEntry>();
                for (var j = 0; j < 10; j++)
                {
                    var batchEntry = new SendMessageBatchRequestEntry
                                         {
                                             MessageBody = "{\"Subject\":\"GenericMessage\", \"Message\": \"" + entriesAdded.ToString() + "\"}",
                                             Id = Guid.NewGuid().ToString()
                                         };
                    entries.Add(batchEntry);
                    entriesAdded++;
                }
                awsQueueClient.SendMessageBatch(new SendMessageBatchRequest { QueueUrl = q.Url, Entries = entries });
            }
            while (entriesAdded < throttleMessageCount);

            Console.WriteLine("{0} - Done adding messages.", DateTime.Now);
            
            var handleCount = 0;
            var serialisations = Substitute.For<IMessageSerialisationRegister>();
            var monitor = Substitute.For<IMessageMonitor>();
            var handler = Substitute.For<IHandler<GenericMessage>>();
            handler.Handle(null).ReturnsForAnyArgs(true).AndDoes(x => {lock (locker) { Thread.Sleep(10);handleCount++; } });
            //handler.Handle(null).ReturnsForAnyArgs(true).AndDoes(x => { throw new Exception(); });
            var serialiser = Substitute.For<IMessageSerialiser<GenericMessage>>();
            serialiser.Deserialise(string.Empty).ReturnsForAnyArgs(new GenericMessage());
            serialisations.GetSerialiser(string.Empty).ReturnsForAnyArgs(serialiser);
            var listener = new SqsNotificationListener(q, serialisations, new NullMessageFootprintStore(), monitor);

            listener.AddMessageHandler(handler);

            listener.Listen();

            var waitCount = 0;
            do
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Console.WriteLine("{0} - Handled {1} messages. Waiting for completion.", DateTime.Now, handleCount);
                waitCount++;
            }
            while (handleCount < throttleMessageCount && waitCount < 100);

            listener.StopListening();

            Console.WriteLine("{0} - Handled {1} messages.", DateTime.Now, handleCount);
            Assert.AreEqual(throttleMessageCount, handleCount);
        }
        
    }
}