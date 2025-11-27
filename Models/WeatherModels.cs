using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MauiApp2.Models
{
    public class WeatherMain
    {
        [JsonPropertyName("temp")]
        public double Temp { get; set; }
    }

    // THÊM MỚI: thông tin thời tiết tổng quát
    public class WeatherInfo
    {
        [JsonPropertyName("main")]
        public string Main { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    public class WeatherResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("main")]
        public WeatherMain Main { get; set; } = new();

        // THÊM MỚI: mảng weather
        [JsonPropertyName("weather")]
        public List<WeatherInfo> Weather { get; set; } = new();
    }
}
