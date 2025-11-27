using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using MauiApp2.Models;

namespace MauiApp2.Services
{
    public class AdafruitService
    {
        private readonly HttpClient _httpClient = new();

        // Feed đọc sensor cũ
        private const string FeedUrl =
            "https://io.adafruit.com/api/v2/nhatminh1710/feeds/jsonpayload";

        // Feed button để ghi dữ liệu
        private const string ButtonFeedUrl =
            "https://io.adafruit.com/api/v2/nhatminh1710/feeds/button/data";

        // Đặt AIO key thật của bạn ở đây
        private const string AioKey = "aio_RyQq59HxwAEMsU55EKetxPRGNNrY";

        public async Task<SensorPayload?> GetLastSensorValueAsync()
        {
            try
            {
                var feed = await _httpClient.GetFromJsonAsync<AdafruitFeed>(FeedUrl);

                if (feed == null || string.IsNullOrWhiteSpace(feed.LastValue))
                    return null;

                var payload = JsonSerializer.Deserialize<SensorPayload>(feed.LastValue);
                return payload;
            }
            catch
            {
                return null;
            }
        }

        // Gửi giá trị button lên feed "button"
        public async Task<bool> SendButtonValueAsync(int value)
        {
            if (string.IsNullOrWhiteSpace(AioKey))
                return false;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, ButtonFeedUrl);

                // Header xác thực
                request.Headers.Add("X-AIO-Key", AioKey);

                // Payload theo format của Adafruit IO: {"value":"1"}
                var body = new { value = value.ToString() };
                request.Content = JsonContent.Create(body);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
