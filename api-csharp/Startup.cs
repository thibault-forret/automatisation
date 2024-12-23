using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MonProjet.Models;

namespace MonProjet;
public class Startup
{
    public IConfiguration Configuration { get; }  // Propriété pour stocker la configuration

    // Ajoutez un constructeur pour injecter la configuration
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Ajouter la configuration de la base de données et de MinIO
        services.Configure<DatabaseSettings>(Configuration.GetSection("DatabaseSettings"));
        services.Configure<MinioSettings>(Configuration.GetSection("MinioSettings"));

        services.AddControllers();
        
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder => builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader());
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseCors("AllowAll");

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }



}
