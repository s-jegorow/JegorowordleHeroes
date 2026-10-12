using JegoroWordleHeroes.Areas.Identity.Data;
using JegoroWordleHeroes.Hubs;
using JegoroWordleHeroes.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("JegorowordleHeroesContextConnection")
    ?? throw new InvalidOperationException("Connection string 'JegorowordleHeroesContextConnection' not found.");

builder.Services.AddDbContext<JegorowordleHeroesContext>(options => options.UseSqlite(connectionString));

builder.Services.AddDefaultIdentity<JegorowordleHeroesUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequiredLength = 4;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._äöüÄÖÜß";
    })
    .AddEntityFrameworkStores<JegorowordleHeroesContext>()
    .AddErrorDescriber<GermanIdentityErrorDescriber>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

builder.Services.AddSingleton<WordService>(); 
builder.Services.AddSingleton<GameRegistry>(); 

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.MapHub<GameHub>("/gamehub");

app.Run();