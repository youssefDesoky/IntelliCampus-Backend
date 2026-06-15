using System.Text;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Settings;
using IntelliCampus.Presistence.Data.Contexts;
using IntelliCampus.Presistence.Data.DataSeeding;
using IntelliCampus.Presentation.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Presistence.Repositories;

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
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<IntelliCampusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

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

// Register services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddHttpClient<IRoutingClientService, RoutingClientService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["RoutingService:BaseUrl"] ?? "http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IInstructorService, InstructorService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
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
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<INotificationStreamService, NotificationStreamService>(); 
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<UrlResolver>();
builder.Services.AddScoped<IBylawService, BylawService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IAutoExamSchedulingService, AutoExamSchedulingService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IPdfExportService, PdfExportService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDataSeed, DataSeed>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IntelliCampusDbContext>();
    await context.Database.MigrateAsync();
    var dataSeed = scope.ServiceProvider.GetRequiredService<IDataSeed>();
    await dataSeed.SeedDataAsync();
}


// app.UseHttpsRedirection(); // Disabled in dev — use http://localhost:5122

app.UseStaticFiles(); // Enable serving static files (for material downloads)

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/hubs/chat");
app.MapControllers();

app.Run();
