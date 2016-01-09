using System;
using System.Threading;
using System.Threading.Tasks;
using Nimbus.Configuration.Settings;
using Nimbus.Infrastructure;
using Nimbus.Infrastructure.MessageSendersAndReceivers;
using Nimbus.Infrastructure.Retries;
using StackExchange.Redis;

namespace Nimbus.Transports.Redis.MessageSendersAndReceivers
{
    internal class RedisMessageReceiver : ThrottlingMessageReceiver
    {
        private readonly string _redisKey;
        private readonly Func<ConnectionMultiplexer> _connectionMultiplexerFunc;
        private readonly Func<IDatabase> _databaseFunc;
        private readonly ISerializer _serializer;
        private readonly ILogger _logger;
        private readonly IRetry _retry;

        private ISubscriber _subscriber;
        readonly SemaphoreSlim _receiveSemaphore = new SemaphoreSlim(0, int.MaxValue);
        private bool _haveFetchedAllPreExistingMessages;

        public RedisMessageReceiver(string redisKey,
                                    Func<ConnectionMultiplexer> connectionMultiplexerFunc,
                                    Func<IDatabase> databaseFunc,
                                    ISerializer serializer,
                                    ConcurrentHandlerLimitSetting concurrentHandlerLimit,
                                    IGlobalHandlerThrottle globalHandlerThrottle,
                                    ILogger logger,
                                    IRetry retry) : base(concurrentHandlerLimit, globalHandlerThrottle, logger)
        {
            _redisKey = redisKey;
            _connectionMultiplexerFunc = connectionMultiplexerFunc;
            _databaseFunc = databaseFunc;
            _serializer = serializer;
            _logger = logger;
            _retry = retry;
        }

        protected override async Task WarmUp()
        {
            await _retry.DoAsync(async () =>
                                       {
                                           _subscriber = _connectionMultiplexerFunc().GetSubscriber();
                                           await _subscriber.SubscribeAsync(_redisKey, OnNotificationReceived);
                                       });
        }

        protected override void Dispose(bool disposing)
        {
            _subscriber?.UnsubscribeAll();
            base.Dispose(disposing);
        }

        private void OnNotificationReceived(RedisChannel redisChannel, RedisValue redisValue)
        {
            _receiveSemaphore.Release();
        }

        protected override async Task<NimbusMessage> Fetch(CancellationToken cancellationToken)
        {
            var successfullyAwaitedForSemaphore = true;
            if (_haveFetchedAllPreExistingMessages)
            {
                //FIXME this timeout is here as a debugging fix to see if we're somehow missing incoming message notifications
                successfullyAwaitedForSemaphore = await _receiveSemaphore.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }

            var database = _databaseFunc();
            var redisValue = await database.ListLeftPopAsync(_redisKey);
            if (!redisValue.HasValue)
            {
                _logger.Debug("No waiting messages.");
                _haveFetchedAllPreExistingMessages = true;
                return null;
            }

            //FIXME debugging hack. Remove.
            if (!successfullyAwaitedForSemaphore)
            {
                _logger.Warn("Uh oh. We dropped through our semaphore on a timeout but still somehow received a waiting message. This isn't supposed to happen.");
            }

            var message = (NimbusMessage) _serializer.Deserialize(redisValue, typeof (NimbusMessage));
            return message;
        }
    }
}