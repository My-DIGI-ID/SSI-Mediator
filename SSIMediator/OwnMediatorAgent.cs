using SSIMediator.Agent;
using Hyperledger.Aries.Agents;
using System;

namespace SSIMediator
{
    /// <inheritdoc />
    public class OwnMediatorAgent : AgentBase
    {
        /// <inheritdoc />
        public OwnMediatorAgent(IServiceProvider provider) : base(provider)
        {
        }

        /// <inheritdoc />
        protected override void ConfigureHandlers()
        {
            AddConnectionHandler();
            AddHandler<CustomRoutingInboxHandler>();
            AddHandler<CustomMediatorForwardHandler>();
            AddTrustPingHandler();
            AddBasicMessageHandler();
        }
    }
}
