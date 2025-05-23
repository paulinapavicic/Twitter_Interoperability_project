using SoapCore;
using Twitter_Interoperability_project.Interfaces;
using Twitter_Interoperability_project.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//builder.Services.AddSoapCore();
builder.Services.AddScoped<TwitterXmlService>();
builder.Services.AddScoped<IJobPostingSoapService, JobPostingSoapService>();

builder.Services.AddSoapCore();


builder.Configuration.AddJsonFile("appsettings.json");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add this after UseRouting, before MapControllerRoute and Run
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


app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
