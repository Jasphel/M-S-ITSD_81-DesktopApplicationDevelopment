using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using EquipmentBorrowing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq; 

namespace EquipmentBorrowing.Desktop;

public partial class App : Avalonia.Application
{
    // Exposed so any ViewModel that needs to resolve something later
    // (not required by this activity, but keeps the composition root
    // discoverable) can reach the same container.
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

            // Task.Run(async () => await SeedDemoDataAsync(Services)).Wait();

            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// The single composition point for the whole desktop app (Part H).
    /// Repositories are registered as Singleton — the in-memory
    /// repositories only hold data because the *same instance* is reused
    /// for the life of the app. If these were Transient, every screen
    /// would silently get its own empty repository, and switching
    /// between Equipment and Active Borrowings would look like data kept
    /// disappearing.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // 1. Register DbContext connected to PostgreSQL
        services.AddDbContext<BorrowingDbContext>(options =>
            options.UseNpgsql("Host=localhost;Port=5432;Database=EquipmentBorrowingDb;Username=postgres;Password=admin123"));

        // 2. Swap In-Memory Repositories for EF Core Repositories
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        services.AddScoped<IBorrowingRepository, BorrowingRepository>();

        // 3. Preserve Application Services (Part I)
        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();

        // 4. ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<BorrowingsViewModel>();
    }
}

    /// <summary>
    /// Same demo data as the Laboratory Activity 1 console project
    /// (EquimentBorrowingSys/Program.cs), just seeded through the
    /// repositories that this app's container resolves, so the desktop
    /// app has something to show without needing its own data source.
    /// </summary>
    /*private static async Task SeedDemoDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
        var equipmentRepo = scope.ServiceProvider.GetRequiredService<IEquipmentRepository>();

        var existingStudents = await studentRepo.GetAllAsync();
        if (!existingStudents.Any())
        {
            await studentRepo.AddAsync(new Student(1, "Claire", true));
            await studentRepo.AddAsync(new Student(2, "Jasper", true));
            await studentRepo.AddAsync(new Student(3, "Mancawan", true));
            await studentRepo.AddAsync(new Student(4, "Jack", false));
        }

        var existingEquipment = await equipmentRepo.GetAllAsync();
        if (!existingEquipment.Any())
        {
            await equipmentRepo.AddAsync(new Equipment(1, "Laptop"));
            await equipmentRepo.AddAsync(new Equipment(2, "Projector"));
            await equipmentRepo.AddAsync(new Equipment(3, "HDMI Cable"));
            await equipmentRepo.AddAsync(new Equipment(4, "DSLR Camera"));
            await equipmentRepo.AddAsync(new Equipment(5, "Tripod"));
        }
    }*/