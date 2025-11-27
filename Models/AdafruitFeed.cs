using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;


namespace MauiApp2.Models
{
    public class AdafruitFeed
    {
        [JsonPropertyName("last_value")]
        public string LastValue { get; set; } = string.Empty;
    }

    // Map với JSON bên trong last_value: {"t": 32.9, "h": 45, "lx": 54}
    public class SensorPayload
    {
        [JsonPropertyName("t")]
        public double Temperature { get; set; }

        [JsonPropertyName("h")]
        public double Humidity { get; set; }

        [JsonPropertyName("lx")]
        public double Lux { get; set; }
    }
}
