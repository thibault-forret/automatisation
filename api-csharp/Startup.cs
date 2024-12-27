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

    /// <summary>
    /// Constructeur pour injecter la configuration dans l'application.
    /// </summary>
    /// <param name="configuration">Configuration de l'application.</param>
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// Configure les services de l'application.
    /// </summary>
    /// <param name="services">Collection de services à configurer.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // Configuration de la base de données et de MinIO
        services.Configure<DatabaseSettings>(Configuration.GetSection("DatabaseSettings"));
        services.Configure<MinioSettings>(Configuration.GetSection("MinioSettings"));

        // Ajoute les contrôleurs pour gérer les requêtes HTTP
        services.AddControllers();
        
        // Configuration CORS (Cross-Origin Resource Sharing) pour autoriser uniquement les requêtes depuis "http://api-python:5000"
        services.AddCors(options =>
        {
            options.AddPolicy("RestrictToApiPython",
                builder => builder.WithOrigins("http://api-python:5000") // Limite les origines autorisées
                                .AllowAnyMethod() // Autorise toutes les méthodes HTTP (GET, POST, PUT, DELETE, etc.)
                                .AllowAnyHeader()); // Autorise tous les types d'en-têtes HTTP
        });
    }

    /// <summary>
    /// Configure les middlewares de l'application.
    /// </summary>
    /// <param name="app">Application pour configurer les middlewares.</param>
    /// <param name="env">Environnement dans lequel l'application s'exécute.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Si l'application est en mode développement, affiche les erreurs détaillées
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Ajoute le routage pour rediriger les requêtes vers les bons contrôleurs
        app.UseRouting();

        // Active la politique CORS définie plus tôt
        app.UseCors("RestrictToApiPython");

        // Associe les contrôleurs aux routes définies dans l'application
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
