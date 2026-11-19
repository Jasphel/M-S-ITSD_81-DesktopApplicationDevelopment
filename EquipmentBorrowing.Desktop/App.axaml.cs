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

            // Safe startup seeding: only inserts initial data if tables are empty
            try
            {
                Task.Run(async () => await SeedDemoDataAsync(Services)).Wait();
            }
            catch
            {
                // Allow UI to launch even if DB is offline so Views can show connection error states (BR-18/BR-19)
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
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

        // Existing Application Services (UC-01 to UC-05)
        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();

        // Application Services
        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();
        services.AddTransient<StudentManagementService>();
        services.AddTransient<EquipmentManagementService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<BorrowingsViewModel>();
        services.AddTransient<StudentManagementViewModel>();
        services.AddTransient<EquipmentManagementViewModel>();
    }

    private static string ResolveConnectionString()
    {
        // 1. Check environment variable first (no secrets in source code)
        var envConn = Environment.GetEnvironmentVariable("EBS_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConn))
            return envConn;

        // 2. Check local untracked appsettings.json if present
        var localConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(localConfigPath))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(localConfigPath));
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                cs.TryGetProperty("DefaultConnection", out var defaultConn) &&
                !string.IsNullOrWhiteSpace(defaultConn.GetString()))
            {
                return defaultConn.GetString()!;
            }
        }

        // 3. Secret-free template fallback (local developer sets PGPASSWORD or appsettings.json)
        return "Host=localhost;Port=5432;Database=EquipmentBorrowingDb;Username=postgres";
    }

    private static async Task SeedDemoDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
        var equipmentRepo = scope.ServiceProvider.GetRequiredService<IEquipmentRepository>();

        var existingStudents = await studentRepo.GetAllAsync();
        if (!existingStudents.Any())
        {
            await studentRepo.AddAsync(new Student(0, "2024-0001", "Claire", "claire@cmu.edu.ph", true));
            await studentRepo.AddAsync(new Student(0, "2024-0002", "Jasper", "jasper@cmu.edu.ph", true));
            await studentRepo.AddAsync(new Student(0, "2024-0003", "Mancawan", null, true));
            await studentRepo.AddAsync(new Student(0, "2024-0004", "Jack", null, false)); // Restricted for testing
        }

        var existingEquipment = await equipmentRepo.GetAllAsync();
        if (!existingEquipment.Any())
        {
            await equipmentRepo.AddAsync(new Equipment(0, "EQ-1001", "Laptop", "Dell Latitude 14-inch"));
            await equipmentRepo.AddAsync(new Equipment(0, "EQ-1002", "Projector", "Epson HDMI Portable Projector"));
            await equipmentRepo.AddAsync(new Equipment(0, "EQ-1003", "HDMI Cable", "2-meter braided HDMI 2.1"));
            await equipmentRepo.AddAsync(new Equipment(0, "EQ-1004", "DSLR Camera", "Canon EOS with 18-55mm lens"));
            await equipmentRepo.AddAsync(new Equipment(0, "EQ-1005", "Tripod", "Heavy-duty aluminum tripod"));
        }
    }
}