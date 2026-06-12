using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WSPMigration.Services;

namespace WSPMigration
{
    /// <summary>
    /// Blazor WebAssembly Application Configuration
    /// Maps WSP services to modern Blazor dependency injection
    /// 
    /// WSP to Blazor Migration:
    /// - WSP runtime engine -> Blazor component model
    /// - WSP stored procedures -> IContactService
    /// - WSP template rendering -> Razor components
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            
            // Root component
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // Register HTTP client for API communication
            builder.Services.AddScoped(sp => new HttpClient 
            { 
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
            });

            // Register Contact Service
            // Maps to WSP stored procedures:
            // - up_contacts_tablist
            // - up_contacts_deleteContact
            // - up_asc_isValidRequest
            builder.Services.AddScoped<IContactService>(sp =>
            {
                var httpClient = sp.GetRequiredService<HttpClient>();
                var connectionString = "Server=YOUR_SERVER;Database=YOUR_DB;Integrated Security=true;";
                return new ContactService(connectionString);
            });

            // Add authentication if needed
            // builder.Services.AddAuthorizationCore();

            // Add localization if needed
            // builder.Services.AddLocalization();

            var host = builder.Build();
            await host.RunAsync();
        }
    }

    /// <summary>
    /// For Server-side Blazor (Blazor Server), use this Program.cs instead:
    /// </summary>
    public class ServerProgram
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Register Contact Service
            builder.Services.AddScoped<IContactService>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                return new ContactService(connectionString);
            });

            // Register HTTP Client for inter-service communication
            builder.Services.AddHttpClient();

            // Add controllers for API endpoints if needed
            builder.Services.AddControllers();

            // Add CORS if needed for cross-origin requests
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowASC", builder =>
                    builder
                        .WithOrigins("https://asc.yourdomain.com")
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("AllowASC");

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Map API controllers
            app.MapControllers();

            app.Run();
        }
    }
}
