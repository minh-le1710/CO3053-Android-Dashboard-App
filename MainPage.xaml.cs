using System;
using System.Threading.Tasks;
using MauiApp2.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;   // để dùng Color / Colors
using System.Linq;

namespace MauiApp2;

public partial class MainPage : ContentPage
{
    private readonly AdafruitService _adafruitService = new();
    private readonly WeatherService _weatherService = new();

    private readonly IDispatcherTimer _timer;
    private bool _isLoading;

    // Trạng thái hai button điều khiển
    private bool _button1On;
    private bool _button2On;

    private double? _minTemperature;
    private double? _maxTemperature;
    private double? _minHumidity;
    private double? _maxHumidity;
    public MainPage()
    {
        InitializeComponent();

        // Ẩn thanh navigation nếu dùng NavigationPage
        NavigationPage.SetHasNavigationBar(this, false);

        // Timer cập nhật dữ liệu Adafruit mỗi 1 giây
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += async (s, e) => await LoadDataAsync();

        // Khởi tạo giao diện button
        UpdateButtonVisuals();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _timer.Start();
        _ = InitLocationAsync();   // map
        _ = LoadWeatherAsync();    // thời tiết OpenWeatherMap
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer.Stop();
    }

    private async void RefreshButton_Clicked(object sender, EventArgs e)
    {
        await LoadDataAsync();
        await LoadWeatherAsync();
    }

    // ==================== WEATHER (OpenWeatherMap) ====================

    private async Task LoadWeatherAsync()
    {
        try
        {
            var weather = await _weatherService.GetCurrentWeatherAsync();
            if (weather == null)
            {
                CityWeatherLabel.Text = "Ho Chi Minh City • -- °C, không lấy được dữ liệu thời tiết";
                return;
            }

            var city = string.IsNullOrWhiteSpace(weather.Name)
                ? "Ho Chi Minh City"
                : weather.Name;

            var temp = weather.Main?.Temp ?? double.NaN;

            // Lấy phần tử weather đầu tiên (nếu có)
            var info = weather.Weather?.FirstOrDefault();
            var conditionText = ConvertConditionToVietnamese(info?.Main, info?.Description);

            if (double.IsNaN(temp))
            {
                CityWeatherLabel.Text = $"{city} • -- °C, {conditionText}";
            }
            else
            {
                // Kết quả cuối cùng: "Ho Chi Minh City: 27.3 °C, trời quang mây tạnh"
                CityWeatherLabel.Text = $"{city} • {temp:F1} °C - {conditionText}";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadWeatherAsync] {ex.Message}");
            CityWeatherLabel.Text = "Ho Chi Minh City • -- °C, lỗi khi lấy thời tiết";
        }
    }

    private string ConvertConditionToVietnamese(string main, string description)
    {
        main = (main ?? string.Empty).ToLowerInvariant();
        description = (description ?? string.Empty).ToLowerInvariant();

        // Ưu tiên theo main
        switch (main)
        {
            case "clear":
                return "Trời quang đãng";
            case "clouds":
                // mây ít / mây thưa
                if (description.Contains("few") || description.Contains("scattered"))
                    return "Có vài đám mây";
                // broken/overcast
                return "Mây đen u ám";
            case "rain":
            case "drizzle":
                return "Mưa rào";
            case "thunderstorm":
                return "Mưa to sấm sét";
            case "snow":
                return "tuyết rơi";
            case "mist":
            case "fog":
            case "haze":
                return "Có sương mù";
            default:
                return "Thời tiết âm u";
        }
    }


    // ==================== MAP LOCATION ====================

    private async Task InitLocationAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return;

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.GetLocationAsync(request);

            if (location == null)
                return;

            var center = new Location(location.Latitude, location.Longitude);

            LocationMap.MoveToRegion(
                MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(0.5)));

            LocationMap.Pins.Clear();
            LocationMap.Pins.Add(new Pin
            {
                Label = "Vị trí hiện tại",
                Type = PinType.Place,
                Location = center
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InitLocationAsync] {ex.Message}");
        }
    }

    // ==================== ADAFRUIT DATA ====================

    private async Task LoadDataAsync()
    {
        if (_isLoading) return;
        _isLoading = true;

        try
        {
            var payload = await _adafruitService.GetLastSensorValueAsync();

            if (payload == null)
            {
                StatusLabel.Text = "Không lấy được dữ liệu từ API.";

                TempValue.Text = "-- °C";
                HumValue.Text = "-- %";

                LuxLabel.Text = "-- lx";
                TempBar.Progress = 0;
                HumBar.Progress = 0;
                LuxSlider.Value = 0;
                TempRangeLabel.Text = "Min: --  |  Max: --";
                HumRangeLabel.Text = "Min: --  |  Max: --";

            }
            else
            {
                // Tiles chi tiết
                TempValue.Text = $"{payload.Temperature:F1} °C";
                HumValue.Text = $"{payload.Humidity:F1} %";
                LuxLabel.Text = $"{payload.Lux:F0} lx";

                StatusLabel.Text = "Cập nhật lúc: " + DateTime.Now.ToString("HH:mm:ss");

                // Cập nhật Min / Max kể từ lúc mở app
                if (!_minTemperature.HasValue || payload.Temperature < _minTemperature.Value)
                    _minTemperature = payload.Temperature;
                if (!_maxTemperature.HasValue || payload.Temperature > _maxTemperature.Value)
                    _maxTemperature = payload.Temperature;

                if (!_minHumidity.HasValue || payload.Humidity < _minHumidity.Value)
                    _minHumidity = payload.Humidity;
                if (!_maxHumidity.HasValue || payload.Humidity > _maxHumidity.Value)
                    _maxHumidity = payload.Humidity;

                // Cập nhật text Min / Max
                if (_minTemperature.HasValue && _maxTemperature.HasValue)
                    TempRangeLabel.Text =
                        $"Min: {_minTemperature.Value:F1}  |  Max: {_maxTemperature.Value:F1}";
                else
                    TempRangeLabel.Text = "Min: --  |  Max: --";

                if (_minHumidity.HasValue && _maxHumidity.HasValue)
                    HumRangeLabel.Text =
                        $"Min: {_minHumidity.Value:F1}  |  Max: {_maxHumidity.Value:F1}";
                else
                    HumRangeLabel.Text = "Min: --  |  Max: --";

                // Progress nhiệt độ 18–35 °C
                double tempMin = 18;
                double tempMax = 40;
                TempBar.Progress = Normalize(payload.Temperature, tempMin, tempMax);

                // Progress độ ẩm 40–70 %
                double humMin = 40;
                double humMax = 100;
                HumBar.Progress = Normalize(payload.Humidity, humMin, humMax);

                // Lux slider 0–1000 lx
                LuxSlider.Value = Math.Clamp(payload.Lux, 0, 6000);
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private double Normalize(double value, double min, double max)
    {
        if (value <= min) return 0;
        if (value >= max) return 1;
        return (value - min) / (max - min);
    }

    // ==================== BUTTON 1 & BUTTON 2 ====================

    private void UpdateButtonVisuals()
    {
        // Button 1
        if (_button1On)
        {
            Button1.Text = "Công tắc 1: BẬT";
            Button1.BackgroundColor = Color.FromArgb("#22c55e");
            Button1.TextColor = Colors.Black;
        }
        else
        {
            Button1.Text = "Công tắc 1: TẮT";
            Button1.BackgroundColor = Color.FromArgb("#1e293b");
            Button1.TextColor = Colors.White;
        }

        // Button 2
        if (_button2On)
        {
            Button2.Text = "Công tắc 2: BẬT";
            Button2.BackgroundColor = Color.FromArgb("#f97316");
            Button2.TextColor = Colors.Black;
        }
        else
        {
            Button2.Text = "Công tắc 2: TẮT";
            Button2.BackgroundColor = Color.FromArgb("#1e293b");
            Button2.TextColor = Colors.White;
        }
    }

    private async void Button1_Clicked(object sender, EventArgs e)
    {
        // Đảo trạng thái
        _button1On = !_button1On;

        // Bật -> 1, Tắt -> 0
        int value = _button1On ? 1 : 0;
        bool ok = await _adafruitService.SendButtonValueAsync(value);

        if (!ok)
        {
            // Nếu gửi lỗi thì rollback
            _button1On = !_button1On;
        }

        UpdateButtonVisuals();
    }

    private async void Button2_Clicked(object sender, EventArgs e)
    {
        // Đảo trạng thái
        _button2On = !_button2On;

        // Bật -> 3, Tắt -> 2
        int value = _button2On ? 3 : 2;
        bool ok = await _adafruitService.SendButtonValueAsync(value);

        if (!ok)
        {
            _button2On = !_button2On;
        }

        UpdateButtonVisuals();
    }
}
