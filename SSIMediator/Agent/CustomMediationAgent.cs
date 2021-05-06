using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SSIMediator.AriesFrameworkCustom
{

    ///<inheritdoc/>
    public class CustomMediationAgent
    {
        private readonly RequestDelegate _requestDelegate;


        ///<inheritdoc/>
        public CustomMediationAgent(RequestDelegate requestDelegate) => _requestDelegate = requestDelegate;


        ///<inheritdoc/>
        public async Task Invoke(HttpContext context, IAgentProvider agentProvider)
        {
            if
            (
              !HttpMethods.IsPost(context.Request.Method) ||
              !(context.Request.ContentType?.Equals(DefaultMessageService.AgentWireMessageMimeType) ?? false)
            )
            {
                await _requestDelegate(context);
                return;
            }

            if (context.Request.ContentLength == null)
            {
                throw new Exception("No content length");
            }

            using StreamReader stream = new(context.Request.Body);
            string body = await stream.ReadToEndAsync();

            IAgent agent = await agentProvider.GetAgentAsync();

            MessageContext response =
              await agent.ProcessAsync
              (
                context: await agentProvider.GetContextAsync(),
                messageContext: new PackedMessageContext(body.GetUTF8Bytes())
              );

            context.Response.StatusCode = (int)HttpStatusCode.OK;

            if (response != null)
            {
                context.Response.ContentType = DefaultMessageService.AgentWireMessageMimeType;
                await context.Response.WriteAsync(response.Payload.GetUTF8String());
            }
            else
            {
                await context.Response.WriteAsync(string.Empty);
            }
        }
    }
}
