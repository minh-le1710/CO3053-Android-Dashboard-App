using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MauiApp2.Models;

namespace MauiApp2.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient = new();

        // API key thật của bạn
        private const string ApiKey = "0c9200bef9bcccf47c9a7969608a7f42";

        // Hồ Chí Minh, đơn vị °C
        private const string UrlTemplate =
            "https://api.openweathermap.org/data/2.5/weather?q=Ho%20Chi%20Minh,VN&appid={0}&units=metric";

        public async Task<WeatherResponse?> GetCurrentWeatherAsync()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                return null;

            var url = string.Format(UrlTemplate, ApiKey);

            try
            {
                return await _httpClient.GetFromJsonAsync<WeatherResponse>(url);
            }
            catch
            {
                return null;
            }
        }
    }
}
