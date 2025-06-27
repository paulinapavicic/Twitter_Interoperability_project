using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SoapCore;
using System.Text;
using Twitter_Interoperability_project.Interfaces;
using Twitter_Interoperability_project.Service;

var builder = WebApplication.CreateBuilder(args);
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];

builder.Services.AddSingleton(provider =>
{
    var supabase = new Supabase.Client(supabaseUrl, supabaseKey);
    supabase.InitializeAsync().Wait();
    return supabase;
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; 
    options.Cookie.HttpOnly = true;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
           
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();



builder.Services.AddScoped<TwitterXmlService>();
builder.Services.AddScoped<IJobPostingSoapService, JobPostingSoapService>();

builder.Services.AddSoapCore();
builder.Configuration.AddJsonFile("appsettings.json");

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.UseSoapEndpoint<IJobPostingSoapService>(
        "/JobPostingSoap.svc",
        new SoapCore.SoapEncoderOptions(),
        SoapCore.SoapSerializer.XmlSerializer
    );
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});




app.Run();
