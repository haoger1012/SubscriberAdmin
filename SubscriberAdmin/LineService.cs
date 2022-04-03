using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace SubscriberAdmin
{
    public class LineService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public LineService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public string GetLoginUrl()
        {
            var queryString = new Dictionary<string, string>
            {
                { "response_type", "code" },
                { "client_id", _configuration["LineLogin:ChannelId"] },
                { "redirect_uri", $"{_configuration["Domain"]}/api/line-login-callback" },
                { "state", Guid.NewGuid().ToString() },
                { "scope", "profile openid email" }
            };

            return QueryHelpers.AddQueryString($"https://access.line.me/oauth2/v2.1/authorize", queryString);
        }

        public async Task<LineLoginResponse> GetLoginResponseAsync(string code)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.PostAsync("https://api.line.me/oauth2/v2.1/token", new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", $"{_configuration["Domain"]}/api/line-login-callback" },
                { "client_id", _configuration["LineLogin:ChannelId"] },
                { "client_secret", _configuration["LineLogin:ChannelSecret"] }
            }));

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
                var lineLoginResponse = await JsonSerializer.DeserializeAsync<LineLoginResponse>(contentStream);
                return lineLoginResponse;
            }

            return null;
        }

        public string GetNotifyUrl(string idToken)
        {
            var queryString = new Dictionary<string, string>
            {
                { "response_type", "code" },
                { "client_id", _configuration["LineNotify:ClientId"] },
                { "redirect_uri", $"{_configuration["Domain"]}/api/line-notify-callback" },
                { "state", idToken },
                { "scope", "notify" }
            };

            return QueryHelpers.AddQueryString($"https://notify-bot.line.me/oauth/authorize", queryString);
        }

        public async Task<LineNotifyResponse> GetNotifyResponseAsync(string code)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.PostAsync("https://notify-bot.line.me/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", $"{_configuration["Domain"]}/api/line-notify-callback" },
                { "client_id", _configuration["LineNotify:ClientId"] },
                { "client_secret", _configuration["LineNotify:ClientSecret"] }
            }));            

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
                var lineNotifyResponse = await JsonSerializer.DeserializeAsync<LineNotifyResponse>(contentStream);
                return lineNotifyResponse;
            }

            return null;
        }

        public async Task<LineNotifyStatus> GetNotificationStatus(string accessToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var httpResponseMessage = await httpClient.GetAsync("https://notify-api.line.me/api/status");

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                using var contentStream = await httpResponseMessage.Content.ReadAsStreamAsync();
                var lineNotifyStatus = await JsonSerializer.DeserializeAsync<LineNotifyStatus>(contentStream);
                return lineNotifyStatus;
            }

            return null;
        }

        public async Task<bool> RevokeNotification(string accessToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var httpResponseMessage = await httpClient.PostAsync("https://notify-api.line.me/api/revoke", null);
            return httpResponseMessage.IsSuccessStatusCode;
        }

        public async Task<bool> SendNotification(string accessToken, string message)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var httpResponseMessage = await httpClient.PostAsync("https://notify-api.line.me/api/notify", new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "message", message }
            }));
            return httpResponseMessage.IsSuccessStatusCode;
        }
    }
}