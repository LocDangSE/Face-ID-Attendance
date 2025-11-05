using FaceIdBackend.Application.Mappings;
using Microsoft.Extensions.Options;
using FaceIdBackend.Application.Services;
using FaceIdBackend.Application.Services.Interfaces;
using FaceIdBackend.Infrastructure.Configuration;
using FaceIdBackend.Infrastructure.Context;
using FaceIdBackend.Infrastructure.Services;
using FaceIdBackend.Infrastructure.Services.Interfaces;
using FaceIdBackend.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Database Context
builder.Services.AddDbContext<AttendanceSystemContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Configuration Settings
builder.Services.Configure<FileStorageSettings>(
    builder.Configuration.GetSection("FileStorage"));
builder.Services.Configure<FlaskApiSettings>(
    builder.Configuration.GetSection("FlaskApi"));
builder.Services.Configure<SupabaseSettings>(
    builder.Configuration.GetSection("Supabase"));

// Infrastructure Services
builder.Services.AddScoped<FileStorageService>(); // Needed for local fallback
// Register enhanced Supabase storage implementation
builder.Services.AddScoped<ISupabaseStorageService, SupabaseStorageServiceEnhanced>();
builder.Services.AddScoped<IFileStorageService, HybridFileStorageService>();

// Flask API client (typed) used by application services. Keep existing FlaskFaceRecognitionService registration for backward compatibility.
builder.Services.AddHttpClient<IFlaskApiClient, FlaskApiClient>()
    .ConfigureHttpClient((sp, client) =>
    {
        var settings = sp.GetRequiredService<IOptions<FlaskApiSettings>>().Value;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    });

builder.Services.AddHttpClient<IFlaskFaceRecognitionService, FlaskFaceRecognitionService>();

// Application Services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFaceDatabaseSyncService, FaceDatabaseSyncService>();
builder.Services.AddScoped<IAttendanceSessionJobScheduler, AttendanceSessionJobScheduler>();
builder.Services.AddScoped<ICacheSyncService, CacheSyncService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Hangfire Configuration
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.ServerName = "FaceAttendanceServer";
    options.WorkerCount = 5; // Number of background workers
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Face Recognition Attendance API",
        Version = "v1",
        Description = "API for Face Recognition based Attendance System using Flask/DeepFace API"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

// Add custom middleware
app.UseMiddleware<FaceIdBackend.Middleware.RequestLoggingMiddleware>();
app.UseMiddleware<FaceIdBackend.Middleware.ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Face Recognition Attendance API v1");
    });
}

// Enable CORS
app.UseCors("AllowAll");

// Serve static files (for uploaded photos)
app.UseStaticFiles();

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new FaceIdBackend.HangfireAuthorizationFilter() },
    DashboardTitle = "Face Attendance System - Background Jobs",
    StatsPollingInterval = 2000
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
