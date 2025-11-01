using Microsoft.EntityFrameworkCore;
// using PreschoolEnrollmentSystem.Infrastructure.Data;  ← REMOVE THIS LINE
using PreschoolEnrollmentSystem.Infrastructure.Firebase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database Context - COMMENTED OUT until you create it
/*
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
*/

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Initialize Firebase
try
{
    FirebaseInitializer.Initialize(builder.Configuration);
    Console.WriteLine("✓ Firebase initialized successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Firebase initialization failed: {ex.Message}");
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }