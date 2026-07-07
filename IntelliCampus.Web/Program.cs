using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Presentation.Hubs;
using IntelliCampus.Presistence.Data.Contexts;
using IntelliCampus.Presistence.Data.DataSeeding;
using IntelliCampus.Presistence.Repositories;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Settings;
using IntelliCampus.Web.CustomMiddleWares;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for larger file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50 MB
});

// Configure form options for file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }); builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContexts with pooling
builder.Services.AddDbContextPool<IntelliCampusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Configure JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<TurnstileSettings>(builder.Configuration.GetSection("Turnstile"));

// Configure Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };

    // Read token from cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("token", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddFixedWindowLimiter("GetCredentials", config =>
    {
        config.PermitLimit = 5;
        config.Window = TimeSpan.FromMinutes(15);
        config.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("ForgotPassword", config =>
    {
        config.PermitLimit = 3;
        config.Window = TimeSpan.FromMinutes(15);
        config.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("SendVerificationCode", config =>
    {
        config.PermitLimit = 3;
        config.Window = TimeSpan.FromMinutes(15);
        config.QueueLimit = 0;
    });
});

// VAPID settings for Native Web Push
builder.Services.Configure<VapidSettings>(builder.Configuration.GetSection("Vapid"));

builder.Services.AddSingleton<IPushSender, WebPushSender>();

builder.Services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>();

// Register services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ICodeGenerationService, CodeGenerationService>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddHttpClient<IRoutingClientService, RoutingClientService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["RoutingService:BaseUrl"] ?? "http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IFaheemAiService, FaheemAiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["FaheemAi:BaseUrl"] ?? "http://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(int.TryParse(builder.Configuration["FaheemAi:RequestTimeoutSeconds"], out var s) ? s : 60);
});
builder.Services.AddSingleton<IFahimUserService, FahimUserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ICredentialRetrievalService, CredentialRetrievalService>();
builder.Services.AddScoped<IAccountRecoveryService, AccountRecoveryService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IInstructorService, InstructorService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IInstructorReminderService, InstructorReminderService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IExamScheduleService, ExamScheduleService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAttendanceExcuseService, AttendanceExcuseService>();
builder.Services.AddScoped<IFacultyService, FacultyService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<INotificationStreamService, NotificationStreamService>();
//builder.Services.AddHostedService<RouterInitializerService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<UrlResolver>();
builder.Services.AddScoped<IBylawService, BylawService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IAutoExamSchedulingService, AutoExamSchedulingService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IPdfExportService, PdfExportService>();
builder.Services.AddScoped<IChartExportService, ChartExportService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddScoped<IElectiveBucketService, ElectiveBucketService>();
builder.Services.AddScoped<IDataSeed, DataSeed>();
builder.Services.AddScoped<IInstructorScheduleService, InstructorScheduleService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAdminAnalysisService, AdminAnalysisService>();
builder.Services.AddScoped<IInstructorAnalyticsService, InstructorAnalyticsService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IInternalMessageService, InternalMessageService>();
builder.Services.AddScoped<IInboxHubService, InboxHubService>();
builder.Services.AddScoped<IDepartmentPreferenceService, DepartmentPreferenceService>();
builder.Services.AddScoped<IDepartmentAllocationService, DepartmentAllocationService>();
builder.Services.AddHostedService<IntelliCampus.Service.DepartmentAllocationHostedService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentAdminContext, CurrentAdminContext>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = actionContext =>
    {
        var Errors = actionContext.ModelState
            .Where(E => E.Value.Errors.Count > 0)
            .ToDictionary(
                X => X.Key,
                X => X.Value.Errors.Select(X => X.ErrorMessage).ToArray()
            );

        var Problem = new ProblemDetails()
        {
            Title = "Validation Errors",
            Detail = "One or more validation errors occurred",
            Status = StatusCodes.Status400BadRequest,
            Extensions = { { "Errors", Errors } }
        };

        return new BadRequestObjectResult(Problem);
    };
});


var app = builder.Build();




app.UseMiddleware<ExceptionHandlerMiddleWare>();
app.UseMiddleware<LogoutCookieMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
   
}

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IntelliCampusDbContext>();
    // Set RESET_DB=true as environment variable to drop and recreate the database on next startup
    if (Environment.GetEnvironmentVariable("RESET_DB") == "true")
    {
        await context.Database.EnsureDeletedAsync();
    }
    await context.Database.MigrateAsync();
    var dataSeed = scope.ServiceProvider.GetRequiredService<IDataSeed>();
    await dataSeed.SeedDataAsync();
}


// app.UseHttpsRedirection(); // Disabled in dev — use http://localhost:5122

app.UseStaticFiles(); // Enable serving static files (for material downloads)

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<MustChangePasswordMiddleware>();

app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<InboxHub>("/hubs/inbox");
app.MapControllers();

app.Run();

public partial class Program { }
