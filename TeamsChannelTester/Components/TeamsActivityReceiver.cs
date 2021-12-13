using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamsChannel.CoreComponents.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TeamsChannelTester.Components
{
    public sealed class TeamsActivityReceiver
    {
        private readonly TeamsConnectorClientProvider _teamConnectorClientProvider;

        private readonly ILogger _logger;

        public TeamsActivityReceiver(TeamsConnectorClientProvider teamConnectorClientProvider, ILogger<TeamsActivityReceiver> logger)
        {
            _teamConnectorClientProvider = teamConnectorClientProvider ?? throw new ArgumentNullException(nameof(teamConnectorClientProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private IConnectorClient CreateConnectorClient(Activity sourceActivity = null)
        {
            if (sourceActivity?.ServiceUrl != null)
            {
                return _teamConnectorClientProvider.CreateConnector(new Uri(sourceActivity.ServiceUrl, UriKind.Absolute));
            }
            else
            {
                return _teamConnectorClientProvider.CreateConnector(_teamConnectorClientProvider.DefaultServiceUrl);
            }
        }

        public async Task ProcessTeamsActivity(Activity activity, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Received Teams activity {JsonConvert.SerializeObject(activity)}");
            IList<ChannelAccount> members = await GetConversationMembers(activity, cancellationToken);
            _logger.LogInformation($"Members in conversation {activity.Conversation.Id} are:{Environment.NewLine}{JsonConvert.SerializeObject(members)}");

            if (activity.Type == ActivityTypes.Message)
            {
                Activity replyActivity = activity.CreateReply($"You said: {activity.Text}");
                await PostActivity(replyActivity, activity, cancellationToken);
            }
            
        }

        private async Task<IList<ChannelAccount>> GetConversationMembers(Activity sourceActivity, CancellationToken cancellationToken)
        {
            using (IConnectorClient connector = CreateConnectorClient(sourceActivity))
            {
                return await connector.Conversations.GetConversationMembersAsync(sourceActivity.Conversation.Id, cancellationToken);
            }
        }

        private async Task PostActivity(Activity activity, Activity parentActivity, CancellationToken cancellationToken)
        {
            using (IConnectorClient connector = CreateConnectorClient(parentActivity))
            {
                ResourceResponse resourceResponse;
                if (string.IsNullOrEmpty(activity.ReplyToId) || activity.ReplyToId == "0" || activity.ReplyToId == "-1")
                    resourceResponse = await connector.Conversations.SendToConversationAsync(activity.Conversation.Id, activity, cancellationToken);
                else
                    resourceResponse = await connector.Conversations.ReplyToActivityAsync(activity.Conversation.Id, activity.ReplyToId, activity, cancellationToken);

                _logger.LogInformation($"Activity sent: {resourceResponse.Id}");
            }
        }

    }
}
