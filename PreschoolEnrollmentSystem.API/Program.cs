using PreschoolEnrollmentSystem.API.Middleware;
using PreschoolEnrollmentSystem.Infrastructure.Firebase;

var builder = WebApplication.CreateBuilder(args);

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

// TODO: Add other services here (DbContext, Repositories, Services, etc.)
// builder.Services.AddDbContext<ApplicationDbContext>(...);
// builder.Services.AddScoped<IParentService, ParentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowMobileApp");
app.UseAuthentication();
app.UseMiddleware<FirebaseAuthMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
public partial class Program { }