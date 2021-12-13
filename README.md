Sample project to reproduce an authentication issue with Bot Framework SDK and MS Teams, in a corporate environment (**with a dedicated tenant**).

# Required to run this project

- `appsettings.json`: 
  - MicrosoftAppId: app ID for bot in MS Teams
  - MicrosoftAppPassword: app password for bot in MS Teams
  - ChannelAuthTenant: tenant id or tenant name (MS Teams)

# How it works

Run **TeamsChannelTester**, go to [https://](https://localhost:5001/swagger/index.html), paste or create an MS Teams bot activity in "Try It Out" operation "/api/messages". 

This will trigger a request to MS Teams to get conversation members from received activity.

Expected: an access token is fetched and sent with request to MS Teams.

Actual behavior: with Microsoft.Bot.Connector v4.7.3, no access token is fetched, request is sent without access token, response is HTTP 401. With Microsoft.Bot.Connector v4.7.15 (last version), an access token is fetched and sent with request, but response still is HTTP 401. Response body is always `{ "message": "Authorization has been denied for this request." }`

