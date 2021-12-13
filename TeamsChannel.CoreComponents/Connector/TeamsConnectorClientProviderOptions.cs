namespace TeamsChannel.CoreComponents.Connector
{
    public sealed class TeamsConnectorClientProviderOptions
    {
        public string? MicrosoftAppId { get; set; }

        public string? MicrosoftAppPassword { get; set; } 

        public string? ChannelAuthTenant { get; set; }

        public string? DefaultServiceUrl { get; set; }
    }
}
