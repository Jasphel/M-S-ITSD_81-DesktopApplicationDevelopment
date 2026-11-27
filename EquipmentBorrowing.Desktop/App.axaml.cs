using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Desktop.Api;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Desktop;

public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            Services = services.BuildServiceProvider();

            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = mainViewModel };
            _ = mainViewModel.StartAsync(() => Task.CompletedTask);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var apiBaseUrl = ResolveApiBaseUrl();

        services.AddSingleton(_ => new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl.EndsWith("/") ? apiBaseUrl : apiBaseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(15)
        });
        services.AddSingleton<BorrowingApiClient>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<BorrowingsViewModel>();
        services.AddTransient<StudentManagementViewModel>();
        services.AddTransient<EquipmentManagementViewModel>();
        services.AddTransient<BorrowingSearchViewModel>();
    }

    private static string ResolveApiBaseUrl()
    {
        var envUrl = Environment.GetEnvironmentVariable("EBS_API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(envUrl))
            return envUrl.Trim();

        var localConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(localConfigPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(localConfigPath));
                if (doc.RootElement.TryGetProperty("Api", out var apiSection) &&
                    apiSection.TryGetProperty("BaseUrl", out var baseUrlProp) &&
                    !string.IsNullOrWhiteSpace(baseUrlProp.GetString()))
                {
                    return baseUrlProp.GetString()!.Trim();
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
            }
        }

        return "http://localhost:5000";
    }
}