using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using TwitchLiveNotifications.Helpers;
using TwitchLiveNotifications.Models;

namespace TwitchLiveNotifications.EventSubFunctions;

public class RegisterSubscription
{
    private readonly ILogger _logger;
    private readonly IApiClient _apiClient;
    private readonly TableClient _configTable;
    private readonly QueueServiceClient _queueClientService;

    public RegisterSubscription(ILoggerFactory loggerFactory, IApiClient apiClient, TableClient configTable, QueueServiceClient queueClientService)
    {
        _logger = loggerFactory.CreateLogger<RegisterSubscription>();
        _apiClient = apiClient;
        _configTable = configTable;
        _queueClientService = queueClientService;
    }

    [Function("RegisterSubscription")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        HttpResponseData response;
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var configs = JsonSerializer.Deserialize<List<SubscriptionConfig>>(requestBody);
        foreach (var config in configs)
        {
            var users = await _apiClient.Helix.Users.GetUsersAsync(logins: new List<string>() { config.TwitchName });
            if (users.Successfully != 1)
            {
                response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "text; charset=utf-8");
                response.WriteString($"Invalid twitch user: {config.TwitchName}");
                return response;
            }

            var user = users.Users.FirstOrDefault();
            config.TwitchId = user.Id;
            SubscriptionConfig.SetTwitchSubscriptionConfiguration(config, _configTable);

            var subscription = new TwitchSubscription()
            {
                Type = EventSubTypes.StreamOnline,
                Value = config.TwitchId
            };
            var message = JsonSerializer.Serialize(subscription);
            _logger.LogInformation("Posting message: {message}", message);
            QueueHelpers.SendMessage(_logger, _queueClientService, "queueAddSubscription", message);
        }
        response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString("Users registered.");

        return response;
    }
}
