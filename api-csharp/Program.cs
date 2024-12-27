using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace MonProjet;

public class Program
{
    public static void Main(string[] args)
    {
        // Crée l'hôte et démarre l'exécution de l'application
        CreateHostBuilder(args).Build().Run();
    }

    /// <summary>
    /// Crée et configure un hôte pour l'application web.
    /// </summary>
    /// <param name="args">Arguments passés à l'application.</param>
    /// <returns>Le constructeur de l'hôte configuré.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>(); // Utilise la classe Startup pour la configuration de l'application
            });
}
