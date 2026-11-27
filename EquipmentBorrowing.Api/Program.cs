using System.Text.Json;
using EquipmentBorrowing.Api;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Queries;
using EquipmentBorrowing.Application.Results;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Data;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connection = Environment.GetEnvironmentVariable("EBS_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Set EBS_CONNECTION_STRING or ConnectionStrings:DefaultConnection.");
builder.Services.AddDbContext<BorrowingDbContext>(o => o.UseNpgsql(connection), optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
builder.Services.AddScoped<IBorrowingRepository, BorrowingRepository>();
builder.Services.AddScoped<IBorrowingQueryRepository, BorrowingQueryRepository>();
builder.Services.AddSingleton<IBorrowingTransactionFactory, BorrowingTransactionFactory>();
builder.Services.AddScoped<BorrowEquipmentService>();
builder.Services.AddScoped<ReturnEquipmentService>();
builder.Services.AddScoped<CheckoutMultipleEquipmentService>();
builder.Services.AddScoped<StudentManagementService>();
builder.Services.AddScoped<EquipmentManagementService>();
builder.Services.AddScoped<BorrowingQueryService>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
var app = builder.Build();
app.UseExceptionHandler();
app.MapOpenApi();
app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));
app.MapEquipmentApi();
app.MapStudentApi();
app.MapBorrowingApi();
app.Run();

public partial class Program { }
