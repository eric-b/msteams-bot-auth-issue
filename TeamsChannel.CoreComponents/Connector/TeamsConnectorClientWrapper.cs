using Microsoft.Bot.Connector;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;

namespace TeamsChannel.CoreComponents.Connector
{
    public sealed class TeamsConnectorClientWrapper : IConnectorClient, IDisposable
    {
        private readonly IDisposable[] _disposableResources;
        private readonly ConnectorClient _connectorClient;

        public TeamsConnectorClientWrapper(ConnectorClient connectorClient, params IDisposable[] disposableResources)
        {
            _connectorClient = connectorClient ?? throw new ArgumentNullException(nameof(connectorClient));
            _disposableResources = disposableResources ?? throw new ArgumentNullException(nameof(disposableResources));
        }

        public static implicit operator ConnectorClient?(TeamsConnectorClientWrapper? instance) => instance?._connectorClient;

        Uri IConnectorClient.BaseUri { get => _connectorClient.BaseUri; set => _connectorClient.BaseUri = value; }

        JsonSerializerSettings IConnectorClient.SerializationSettings => _connectorClient.SerializationSettings;

        JsonSerializerSettings IConnectorClient.DeserializationSettings => _connectorClient.DeserializationSettings;

        ServiceClientCredentials IConnectorClient.Credentials => _connectorClient.Credentials;

        IAttachments IConnectorClient.Attachments => _connectorClient.Attachments;

        IConversations IConnectorClient.Conversations => _connectorClient.Conversations;

        public void Dispose()
        {
            _connectorClient.Dispose();
            foreach (IDisposable item in _disposableResources)
                item.Dispose();
        }
    }
}
