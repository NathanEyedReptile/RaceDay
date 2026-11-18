using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<AuthService>();

// Session storage
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// HTTP client for communicating with the RaceDay API
builder.Services.AddHttpClient("RaceDayApi", client =>
{
    var apiUrl = builder.Configuration["ApiSettings:BaseUrl"];

    client.BaseAddress = new Uri(apiUrl!);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configure HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();