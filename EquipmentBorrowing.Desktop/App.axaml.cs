using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Data;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
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

            // The window opens immediately. The database is migrated/seeded in the background
            // (MainWindowViewModel.StartAsync) so a slow or unreachable PostgreSQL never freezes
            // the UI; every screen then shows its own loading / error / empty state.
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = mainViewModel };
            _ = mainViewModel.StartAsync(() => DatabaseInitializer.InitializeAsync(Services));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var connectionString = ResolveConnectionString();

        // Register DbContext with Transient lifetime so each resolved service/scope gets an isolated context
        // avoiding concurrent async DbContext access in desktop ViewModels
        services.AddDbContext<BorrowingDbContext>(
            options => options.UseNpgsql(connectionString),
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Singleton);

        // Repositories (Transient to pair cleanly with isolated DbContext instances)
        services.AddTransient<IStudentRepository, StudentRepository>();
        services.AddTransient<IEquipmentRepository, EquipmentRepository>();
        services.AddTransient<IBorrowingRepository, BorrowingRepository>();
        services.AddTransient<IBorrowingQueryRepository, BorrowingQueryRepository>();

        // One short-lived context + one PostgreSQL transaction per business operation
        // (issue / return / multi-item checkout). Uses the singleton DbContextOptions above.
        services.AddSingleton<IBorrowingTransactionFactory, BorrowingTransactionFactory>();

        // Application services
        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();
        services.AddTransient<StudentManagementService>();
        services.AddTransient<EquipmentManagementService>();
        services.AddTransient<CheckoutMultipleEquipmentService>();
        services.AddTransient<BorrowingQueryService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<BorrowingsViewModel>();
        services.AddTransient<StudentManagementViewModel>();
        services.AddTransient<EquipmentManagementViewModel>();
        services.AddTransient<BorrowingSearchViewModel>();
    }

    private static string ResolveConnectionString()
    {
        // 1. Check environment variable first (no secrets in source code)
        var envConn = Environment.GetEnvironmentVariable("EBS_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConn))
            return envConn;

        // 2. Check local untracked appsettings.json if present (copy appsettings.example.json)
        var localConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(localConfigPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(localConfigPath));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("DefaultConnection", out var defaultConn) &&
                    !string.IsNullOrWhiteSpace(defaultConn.GetString()))
                {
                    return defaultConn.GetString()!;
                }
            }
            catch (Exception ex)
            {
                // A malformed file must not stop the app from opening.
                ErrorLog.Write(ex);
            }
        }

        // 3. Secret-free template fallback (local developer sets PGPASSWORD or appsettings.json)
        return "Host=localhost;Port=5432;Database=EquipmentBorrowingDb;Username=postgres";
    }
}
