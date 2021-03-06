﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Nimbus.Handlers;
using Nimbus.StressTests.ThreadStarvationTests.BadlyBehavedHandlersThatDoNotKnowAboutAsync.MessageContracts;
using Nimbus.Tests.Common.TestUtilities;

namespace Nimbus.StressTests.ThreadStarvationTests.BadlyBehavedHandlersThatDoNotKnowAboutAsync.Handlers
{
    public class CommandThatWillBlockTheThreadHandler : IHandleCommand<CommandThatWillBlockTheThread>
    {
        public static readonly TimeSpan SleepDuration = TimeSpan.FromSeconds(5);

        public async Task Handle(CommandThatWillBlockTheThread busCommand)
        {
            Thread.Sleep(SleepDuration); // deliberately block the handling thread
            MethodCallCounter.RecordCall<CommandThatWillBlockTheThreadHandler>(h => h.Handle(busCommand));
        }
    }
}