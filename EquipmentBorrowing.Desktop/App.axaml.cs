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

            SeedDemoData(Services);

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
        services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();
        services.AddSingleton<IEquipmentRepository, InMemoryEquipmentRepository>();
        services.AddSingleton<IBorrowingRepository, InMemoryBorrowingRepository>();

        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<BorrowingsViewModel>();
    }

    /// <summary>
    /// Same demo data as the Laboratory Activity 1 console project
    /// (EquimentBorrowingSys/Program.cs), just seeded through the
    /// repositories that this app's container resolves, so the desktop
    /// app has something to show without needing its own data source.
    /// </summary>
    private static void SeedDemoData(IServiceProvider services)
    {
        var studentRepository = (InMemoryStudentRepository)services.GetRequiredService<IStudentRepository>();
        var equipmentRepository = (InMemoryEquipmentRepository)services.GetRequiredService<IEquipmentRepository>();

        studentRepository.Add(new Student(1, "Claire", true));
        studentRepository.Add(new Student(2, "Jasper", true));
        studentRepository.Add(new Student(3, "Mancawan", true));
        studentRepository.Add(new Student(4, "Jack", false)); // not allowed to borrow

        equipmentRepository.Add(new Equipment(1, "Laptop"));
        equipmentRepository.Add(new Equipment(2, "Projector"));
        equipmentRepository.Add(new Equipment(3, "HDMI Cable"));
        equipmentRepository.Add(new Equipment(4, "DSLR Camera"));
        equipmentRepository.Add(new Equipment(5, "Tripod"));
    }
}
