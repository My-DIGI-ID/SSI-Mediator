using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Routing;
using Hyperledger.Aries.Storage;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSIMediator.Agent
{

    ///<inheritdoc/>
    public class CustomRoutingInboxHandler : IMessageHandler
    {
        private readonly IWalletRecordService recordService;
        private readonly IWalletService walletService;
        private readonly IRoutingStore routingStore;
        private readonly AgentOptions options;
        private readonly ILogger<CustomRoutingInboxHandler> logger;


        ///<inheritdoc/>
        public CustomRoutingInboxHandler(
            IWalletRecordService recordService,
            IWalletService walletService,
            IRoutingStore routingStore,
            IOptions<AgentOptions> options,
            ILogger<CustomRoutingInboxHandler> logger)
        {
            this.recordService = recordService;
            this.walletService = walletService;
            this.routingStore = routingStore;
            this.options = options.Value;
            this.logger = logger;
        }

        public IEnumerable<MessageType> SupportedMessageTypes => new MessageType[]
        {
            RoutingTypeNames.CreateInboxMessage,
            RoutingTypeNames.AddRouteMessage,
            RoutingTypeNames.AddDeviceInfoMessage,
            RoutingTypeNames.GetInboxItemsMessage,
            RoutingTypeNames.DeleteInboxItemsMessage
        };


        ///<inheritdoc/>
        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            if (messageContext.Connection == null ||
                messageContext.Connection.MultiPartyInvitation ||
                messageContext.Connection.State != ConnectionState.Connected)
            {
                throw new InvalidOperationException("Connection missing or invalid");
            }

            switch (messageContext.GetMessageType())
            {
                case RoutingTypeNames.CreateInboxMessage:
                    return await CreateInboxAsync(agentContext, messageContext.Connection, messageContext.GetMessage<CreateInboxMessage>());

                case RoutingTypeNames.AddRouteMessage:
                    await AddRouteAsync(agentContext, messageContext.Connection, messageContext.GetMessage<AddRouteMessage>());
                    break;

                case RoutingTypeNames.GetInboxItemsMessage:
                    return await GetInboxItemsAsync(agentContext, messageContext.Connection, messageContext.GetMessage<GetInboxItemsMessage>());

                case RoutingTypeNames.DeleteInboxItemsMessage:
                    await DeleteInboxItemsAsync(agentContext, messageContext.Connection, messageContext.GetMessage<DeleteInboxItemsMessage>());
                    break;

                case RoutingTypeNames.AddDeviceInfoMessage:
                    await AddDeviceInfoAsync(agentContext, messageContext.Connection, messageContext.GetMessage<AddDeviceInfoMessage>());
                    break;

                default:
                    break;
            }

            return null;
        }

        private async Task AddDeviceInfoAsync(IAgentContext agentContext, ConnectionRecord connection, AddDeviceInfoMessage addDeviceInfoMessage)
        {
            string inboxId = connection.GetTag("InboxId");
            if (inboxId == null)
            {
                throw new InvalidOperationException("No INBOX found. Creating new one");
            }

            CustomDeviceInfoRecord deviceRecord = new()
            {
                InboxId = inboxId,
                DeviceId = addDeviceInfoMessage.DeviceId,
                DeviceVendor = addDeviceInfoMessage.DeviceVendor,
                Metadata = addDeviceInfoMessage.DeviceMetadata
            };
            try
            {
                await recordService.AddAsync(agentContext.Wallet, deviceRecord);
                addDeviceInfoMessage.DeviceMetadata.TryGetValue("Push", out string pushService);
                Log.Information("Added new device");
                Log.Debug($"DeviceId: {addDeviceInfoMessage.DeviceId}");
                Log.Debug($"DeviceVendor: {addDeviceInfoMessage.DeviceVendor}");
                Log.Debug($"PushService: {pushService}");
            }
            catch (WalletItemAlreadyExistsException)
            {
            }
            catch (Exception e)
            {
                Log.Error($"Failed to register device: {e.Message}");
            }
        }

        private async Task DeleteInboxItemsAsync(IAgentContext agentContext, ConnectionRecord connection, DeleteInboxItemsMessage deleteInboxItemsMessage)
        {
            string inboxId = connection.GetTag("InboxId");
            InboxRecord inboxRecord = await recordService.GetAsync<InboxRecord>(agentContext.Wallet, inboxId);

            Wallet edgeWallet = await walletService.GetWalletAsync(inboxRecord.WalletConfiguration, inboxRecord.WalletCredentials);

            foreach (string itemId in deleteInboxItemsMessage.InboxItemIds)
            {
                try
                {
                    await recordService.DeleteAsync<InboxItemRecord>(edgeWallet, itemId);
                }
                catch (Exception e)
                {
                    Log.Error($"Couldn't delete inbox item {itemId}: {e.Message}");
                }
            }
        }

        private async Task<GetInboxItemsResponseMessage> GetInboxItemsAsync(IAgentContext agentContext, ConnectionRecord connection, GetInboxItemsMessage getInboxItemsMessage)
        {
            string inboxId = connection.GetTag("InboxId");
            InboxRecord inboxRecord = await recordService.GetAsync<InboxRecord>(agentContext.Wallet, inboxId);

            Wallet edgeWallet = await walletService.GetWalletAsync(inboxRecord.WalletConfiguration, inboxRecord.WalletCredentials);

            List<InboxItemRecord> items = await recordService.SearchAsync<InboxItemRecord>(edgeWallet);
            return new GetInboxItemsResponseMessage
            {
                Items = items
                    .OrderBy(x => x.Timestamp)
                    .Select(x => new InboxItemMessage { Id = x.Id, Data = x.ItemData, Timestamp = x.Timestamp })
                    .ToList()
            };
        }

        private async Task AddRouteAsync(IAgentContext _, ConnectionRecord connection, AddRouteMessage addRouteMessage)
        {
            string inboxId = connection.GetTag("InboxId");

            await routingStore.AddRouteAsync(addRouteMessage.RouteDestination, inboxId);
        }

        private async Task<CreateInboxResponseMessage> CreateInboxAsync(IAgentContext agentContext, ConnectionRecord connection, CreateInboxMessage createInboxMessage)
        {
            string mobileSecret = "";
            try
            {
                mobileSecret = createInboxMessage.Metadata.Where(x => x.Key.Equals("Mobile-Secret")).FirstOrDefault().Value;
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Can't find Mobile-Secret");
            }


            if (string.IsNullOrEmpty(mobileSecret))
            {
                throw new InvalidOperationException("Can't find Mobile-Secret");
            }

            List<string> mobileSecretList = Environment.GetEnvironmentVariable("MOBILE_SECRETS").Split(",").ToList();
            if (!mobileSecretList.Contains(mobileSecret))
            {
                throw new UnauthorizedAccessException("Invalid Mobile-Secret");
            }

            if (connection.State != ConnectionState.Connected)
            {
                throw new InvalidOperationException("Can't create inbox if connection is not in final state");
            }

            string inboxId = $"Edge{Guid.NewGuid().ToString("N")}";
            string inboxKey = Guid.NewGuid().ToString();

            InboxRecord inboxRecord = new InboxRecord
            {
                Id = inboxId,
                WalletConfiguration = new WalletConfiguration
                {
                    Id = inboxId,
                    StorageType = options.WalletConfiguration?.StorageType ?? "default"
                },
                WalletCredentials = new WalletCredentials { Key = inboxKey }
            };
            connection.SetTag("InboxId", inboxId);

            await walletService.CreateWalletAsync(
                configuration: inboxRecord.WalletConfiguration,
                credentials: inboxRecord.WalletCredentials);

            await recordService.AddAsync(agentContext.Wallet, inboxRecord);
            await recordService.UpdateAsync(agentContext.Wallet, connection);

            return new CreateInboxResponseMessage
            {
                InboxId = inboxId,
                InboxKey = inboxKey
            };
        }
    }
}
