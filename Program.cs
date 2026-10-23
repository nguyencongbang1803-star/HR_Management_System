using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using QLNS.Data;
using QLNS.Models.Entities;
using QLNS.Services;

var builder = WebApplication.CreateBuilder(args);

// Không dùng Windows Event Log: môi trường phát triển thông thường không có quyền ghi nguồn .NET Runtime.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")))
    .SetApplicationName("TIC.HRM");

builder.Services.AddControllersWithViews(options =>
{
    // Mọi request thay đổi dữ liệu đều phải có antiforgery token.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(options =>
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always);
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = long.MaxValue);
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = null);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServer => sqlServer.EnableRetryOnFailure()));
builder.Services.AddIdentity<TaiKhoan, IdentityRole<int>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<HrmClaimsPrincipalFactory>();
builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "TIC.HRM.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<LeaveService>();
builder.Services.AddScoped<PayrollCalculationService>();
builder.Services.AddScoped<PersonalNotificationService>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Route cho Areas — phải đặt TRƯỚC route mặc định
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await DbSeeder.InitializeAsync(app.Services, app.Environment, app.Logger);

if (args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase))
    return;

app.Run();
