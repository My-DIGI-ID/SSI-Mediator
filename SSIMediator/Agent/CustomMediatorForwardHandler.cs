using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.Routing;
using Hyperledger.Aries.Routing;
using Hyperledger.Aries.Storage;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSIMediator.Agent
{

    ///<inheritdoc/>
    public class CustomMediatorForwardHandler : MessageHandlerBase<ForwardMessage>
    {
        private readonly IWalletRecordService _recordService;
        private readonly IWalletService _walletService;
        private readonly IRoutingStore _routingStore;
        private readonly IEventAggregator _eventAggregator;


        ///<inheritdoc/>
        public CustomMediatorForwardHandler(
            IWalletRecordService recordService,
            IWalletService walletService,
            IRoutingStore routingStore,
            IEventAggregator eventAggregator)
        {
            _recordService = recordService;
            _walletService = walletService;
            _routingStore = routingStore;
            _eventAggregator = eventAggregator;
        }

        public override IEnumerable<MessageType> SupportedMessageTypes => new MessageType[] { MessageTypes.Forward, MessageTypesHttps.Forward };


        ///<inheritdoc/>
        protected override async Task<AgentMessage> ProcessAsync(ForwardMessage message, IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            string inboxId = await _routingStore.FindRouteAsync(message.To);
            InboxRecord inboxRecord = await _recordService.GetAsync<InboxRecord>(agentContext.Wallet, inboxId);

            Hyperledger.Indy.WalletApi.Wallet edgeWallet = await _walletService.GetWalletAsync(inboxRecord.WalletConfiguration, inboxRecord.WalletCredentials);

            InboxItemRecord inboxItemRecord = new InboxItemRecord { ItemData = message.Message.ToJson(), Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds() };
            await _recordService.AddAsync(edgeWallet, inboxItemRecord);

            _eventAggregator.Publish(new InboxItemEvent
            {
                InboxId = inboxId,
                ItemId = inboxItemRecord.Id
            });

            CustomDeviceInfoRecord newestDevice = null;
            List<CustomDeviceInfoRecord> inboxDeviceInfo = (await _recordService.SearchAsync<CustomDeviceInfoRecord>(agentContext.Wallet, SearchQuery.Equal("InboxId", inboxId), count: 2147483647));
            if (inboxDeviceInfo.Count > 1)
            {
                Log.Debug($"{inboxDeviceInfo.Count} devices found.");
                try
                {
                    newestDevice = inboxDeviceInfo.OrderByDescending(x => long.Parse(x.Metadata.Where(y => y.Key.Equals("CreatedAt")).First().Value)).First();
                }
                catch (Exception ex)
                {
                    Log.Debug("DeviceMetadata error: " + ex.Message);
                    Log.Debug("Using last created device.");
                    newestDevice = inboxDeviceInfo.OrderByDescending(x => x.CreatedAtUtc).First();
                }
            }
            else
            {
                Log.Debug("Only one device found.");
                newestDevice = inboxDeviceInfo.FirstOrDefault();
            }

            string pushService = "";
            try
            {
                newestDevice.Metadata.TryGetValue("Push", out pushService);
                Log.Debug($"Push Metadata from newest device is {pushService}.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

            await SendPushNotification(newestDevice.DeviceId, newestDevice.DeviceVendor, pushService);
            return null;
        }

        private async Task SendPushNotification(string token, string targetOs, string pushService)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(pushService))
                {
                    pushService = "Polling";
                }

                switch (pushService)
                {
                    case "Polling":
                        break;
                    default:
                        throw new NotImplementedException("Push Service not supported");
                }
            });
        }
    }
}
