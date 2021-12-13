using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TeamsChannelTester.Components;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TeamsChannelTester.Controllers
{
    [Route("api")]
    public sealed class MessagesController : Controller
    {
        private readonly TeamsActivityReceiver _activityReceiver;

        public MessagesController(TeamsActivityReceiver activityReceiver)
        {
            _activityReceiver = activityReceiver ?? throw new ArgumentNullException(nameof(activityReceiver));
        }

        /// <summary>
        /// <para>Sink for an MS Teams activity to process.</para>
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("messages", Name = "post-message")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task Post([FromBody] Microsoft.Bot.Schema.Activity activity, CancellationToken cancellationToken)
        {
            return _activityReceiver.ProcessTeamsActivity(activity, cancellationToken);
        }
    }
}
