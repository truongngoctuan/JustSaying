﻿using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenPassingAHandledAndUnhandledMessage : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(null).ReturnsForAnyArgs(info => true).AndDoes(x => Thread.Sleep(1)); // Ensure at least one ms wait on processing
        }

        [Then]
        public async Task MessagesGetDeserialisedByCorrectHandler()
        {
            await Patiently.VerifyExpectationAsync(
                () => SerialisationRegister.Received().DeserializeMessage(
                    SqsMessageBody(_messageTypeString)));
        }

        [Then]
        public async Task ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            await Patiently.VerifyExpectationAsync(
                () =>Handler.Received().Handle(DeserialisedMessage));
        }

        [Then]
        public async Task MonitoringToldMessageHandlingTime()
        {
            await Patiently.VerifyExpectationAsync(
                () =>Monitor.Received().HandleTime(
                    Arg.Is<long>(x => x > 0)));
        }

        [Then]
        public async Task AllMessagesAreClearedFromQueue()
        {
            await Patiently.VerifyExpectationAsync(
                () => SerialisationRegister.Received(2).DeserializeMessage(
                    Arg.Any<string>()));

            await Patiently.VerifyExpectationAsync(
                () =>Sqs.Received().DeleteMessage(
                    Arg.Any<DeleteMessageRequest>()));
        }
    }

    /*
    Some more:
     * 1. Multiple handling of same message with different handlers
     * 2. etc
    */
}