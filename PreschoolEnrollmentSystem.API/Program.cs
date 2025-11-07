using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PreschoolEnrollmentSystem.API.Mapping;
using PreschoolEnrollmentSystem.API.Middleware;
using PreschoolEnrollmentSystem.Infrastructure.Data;
using PreschoolEnrollmentSystem.Infrastructure.Firebase;
using PreschoolEnrollmentSystem.Services.Implementation;
using PreschoolEnrollmentSystem.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Register the DbContext with the dependency injection container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1. Add Controllers
builder.Services.AddControllers();

// 2. Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your Firebase ID token. Example: 'Bearer eyJhbGc...'"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 3. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",      // React Native dev
            "http://localhost:19006",     // Expo dev
            "capacitor://localhost",      // Capacitor iOS
            "ionic://localhost"           // Ionic
        )
        .AllowAnyMethod()                 // GET, POST, PUT, DELETE, etc.
        .AllowAnyHeader()                 // Allow any headers
        .AllowCredentials();              // Allow cookies/auth headers
    });
});

// 4. Add Authentication
builder.Services.AddAuthentication()
    .AddJwtBearer();  // Even though Firebase handles it, we need this for the framework

// 5. Add Authorization
builder.Services.AddAuthorization();

// 6. Initialize Firebase
// Why: Must be done before the app starts to verify tokens
try
{
    FirebaseInitializer.Initialize(builder.Configuration);
}
catch (Exception ex)
{
    // Why: Log the error but don't crash the app immediately
    // This allows the app to start and show a proper error message
    Console.WriteLine($"CRITICAL: Failed to initialize Firebase: {ex.Message}");
    Console.WriteLine("The API will start but authentication will not work.");
}

// 7. Register HttpClientFactory for FirebaseAuthService
builder.Services.AddHttpClient();

// 8. Register Memory Cache
builder.Services.AddMemoryCache();

// 9. Register Repositories
builder.Services.AddScoped<PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces.IUserRepository,
    PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation.UserRepository>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces.IApplicationRepository,
    PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation.ApplicationRepository>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces.IChildRepository,
    PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation.ChildRepository>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces.IStudentRepository,
    PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation.StudentRepository>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces.IClassroomRepository,
    PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation.ClassroomRepository>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces.IPaymentRepository,
    PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation.PaymentRepository>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces.IBlogRepository,
    PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation.BlogRepository>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces.ITagRepository,
    PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation.TagRepository>();

// 10. Register Services
builder.Services.AddScoped<PreschoolEnrollmentSystem.Services.Interfaces.IAuthService,
    PreschoolEnrollmentSystem.Services.Implementation.FirebaseAuthService>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Services.Interfaces.IEmailService,
    PreschoolEnrollmentSystem.Services.Implementation.EmailService>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Services.Interfaces.IFirebaseStorageService,
    PreschoolEnrollmentSystem.Services.Implementation.FirebaseStorageService>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Services.Interfaces.IFirebaseNotificationService,
    PreschoolEnrollmentSystem.Services.Implementation.FirebaseNotificationService>();
builder.Services.AddScoped<PreschoolEnrollmentSystem.Services.Interfaces.IDataSeedingService,
    PreschoolEnrollmentSystem.Services.Implementation.DataSeedingService>();
builder.Services.AddScoped<IParentService, ParentService>();
builder.Services.AddScoped<IStudentService, StudentService>();

builder.Services.AddAutoMapper(typeof(MappingProfile));

var app = builder.Build();

// Enable Swagger in all environments (including Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Preschool Enrollment API v1");
    c.RoutePrefix = "swagger"; // Access at /swagger
});

app.UseHttpsRedirection();
app.UseCors("AllowMobileApp");
app.UseAuthentication();
app.UseMiddleware<FirebaseAuthMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
public partial class Program { }