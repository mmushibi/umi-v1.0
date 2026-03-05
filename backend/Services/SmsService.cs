using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace UmiHealthPOS.Services
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendBulkSmsAsync(List<string> phoneNumbers, string message);
        Task<bool> SendOtpAsync(string phoneNumber, string otp);
    }

    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;

        public SmsService(HttpClient httpClient, IConfiguration configuration, ILogger<SmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var provider = _configuration["SmsService:Provider"];
                
                switch (provider?.ToLower())
                {
                    case "africastalking":
                        return await SendViaAfricaTalkingAsync(phoneNumber, message);
                    case "twilio":
                        return await SendViaTwilioAsync(phoneNumber, message);
                    default:
                        _logger.LogWarning("SMS provider {Provider} not configured. Using fallback mode.", provider);
                        return await SendFallbackSmsAsync(phoneNumber, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendBulkSmsAsync(List<string> phoneNumbers, string message)
        {
            try
            {
                var provider = _configuration["SmsService:Provider"];
                
                switch (provider?.ToLower())
                {
                    case "africastalking":
                        return await SendBulkViaAfricaTalkingAsync(phoneNumbers, message);
                    case "twilio":
                        return await SendBulkViaTwilioAsync(phoneNumbers, message);
                    default:
                        _logger.LogWarning("SMS provider {Provider} not configured. Using fallback mode for bulk SMS.", provider);
                        var successCount = 0;
                        foreach (var phoneNumber in phoneNumbers)
                        {
                            if (await SendFallbackSmsAsync(phoneNumber, message))
                                successCount++;
                        }
                        return successCount == phoneNumbers.Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk SMS to {Count} recipients", phoneNumbers.Count);
                return false;
            }
        }

        public async Task<bool> SendOtpAsync(string phoneNumber, string otp)
        {
            var message = $"Your UmiHealth POS verification code is: {otp}. Valid for 10 minutes. Do not share this code.";
            return await SendSmsAsync(phoneNumber, message);
        }

        private async Task<bool> SendViaAfricaTalkingAsync(string phoneNumber, string message)
        {
            try
            {
                var apiKey = _configuration["SmsService:ApiKey"];
                var username = _configuration["SmsService:Username"];
                var baseUrl = _configuration["SmsService:BaseUrl"];

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(baseUrl))
                {
                    _logger.LogWarning("Africa Talking SMS configuration missing");
                    return false;
                }

                var requestBody = new
                {
                    username = username,
                    to = phoneNumber,
                    message = message,
                    from = "UmiHealthPOS"
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apiKey", apiKey);

                var response = await _httpClient.PostAsync($"{baseUrl}/messaging", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("SMS sent via Africa Talking to {PhoneNumber}", phoneNumber);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Africa Talking SMS API returned status {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS via Africa Talking to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        private async Task<bool> SendBulkViaAfricaTalkingAsync(List<string> phoneNumbers, string message)
        {
            try
            {
                var apiKey = _configuration["SmsService:ApiKey"];
                var username = _configuration["SmsService:Username"];
                var baseUrl = _configuration["SmsService:BaseUrl"];

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(baseUrl))
                {
                    _logger.LogWarning("Africa Talking SMS configuration missing for bulk SMS");
                    return false;
                }

                var requestBody = new
                {
                    username = username,
                    to = string.Join(",", phoneNumbers),
                    message = message,
                    from = "UmiHealthPOS"
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apiKey", apiKey);

                var response = await _httpClient.PostAsync($"{baseUrl}/messaging", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Bulk SMS sent via Africa Talking to {Count} recipients", phoneNumbers.Count);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Africa Talking bulk SMS API returned status {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk SMS via Africa Talking to {Count} recipients", phoneNumbers.Count);
                return false;
            }
        }

        private async Task<bool> SendViaTwilioAsync(string phoneNumber, string message)
        {
            try
            {
                var accountSid = _configuration["SmsService:Twilio:AccountSid"];
                var authToken = _configuration["SmsService:Twilio:AuthToken"];
                var fromNumber = _configuration["SmsService:Twilio:FromNumber"];

                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
                {
                    _logger.LogWarning("Twilio SMS configuration missing");
                    return false;
                }

                var baseUrl = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("To", phoneNumber),
                    new KeyValuePair<string, string>("From", fromNumber),
                    new KeyValuePair<string, string>("Body", message)
                });

                var byteArray = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = await _httpClient.PostAsync(baseUrl, requestBody);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SMS sent via Twilio to {PhoneNumber}", phoneNumber);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Twilio SMS API returned status {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS via Twilio to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        private async Task<bool> SendBulkViaTwilioAsync(List<string> phoneNumbers, string message)
        {
            var successCount = 0;
            foreach (var phoneNumber in phoneNumbers)
            {
                if (await SendViaTwilioAsync(phoneNumber, message))
                    successCount++;
            }
            return successCount == phoneNumbers.Count;
        }

        private async Task<bool> SendFallbackSmsAsync(string phoneNumber, string message)
        {
            // Fallback: Log SMS details for development/testing
            _logger.LogInformation("FALLBACK SMS - To: {PhoneNumber}, Message: {Message}", phoneNumber, message);
            await Task.Delay(100); // Simulate sending
            return true;
        }
    }
}
