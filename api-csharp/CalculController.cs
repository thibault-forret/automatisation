using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO; 
using System.Text;   
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Threading.Tasks;
using System.Collections.Generic;

// Commenter tous le code
// Factoriser le code

namespace MonProjetAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalculController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CalculController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> PostResult([FromBody] CalculDto dto)
        {
            // Configuration MinIO
            var endpoint = "minio:9000"; // Adresse MinIO
            var accessKey = "admin";         // Identifiant
            var secretKey = "admin123";      // Mot de passe
            var bucketName = "syracuse";     // Nom du bucket

            // Configuration du client MinIO
            var minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .Build();

            // Vérifier si le bucket existe, sinon le créer
            bool found = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!found)
            {
                Console.WriteLine($"Bucket '{bucketName}' not found. Creating...");
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            }

            var fileName = $"{dto.Nombre}.txt"; // Nom du fichier basé sur le nombre
            var fileContent = string.Join(", ", dto.Syracuse); // Contenu du fichier
            var fileBytes = Encoding.UTF8.GetBytes(fileContent);

            // Stocker le fichier
            using (var memoryStream = new MemoryStream(fileBytes))
            {
                Console.WriteLine($"Uploading {fileName} to bucket {bucketName}...");
                await minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(fileName)
                    .WithStreamData(memoryStream)
                    .WithObjectSize(memoryStream.Length)
                    .WithContentType("text/plain"));
            }

            // -------------------- BDD --------------------

            //Se connecter au serveur
            string connectionString = "Server=db;Port=3306;Database=calculs;User=calcul_user;Password=1234;";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Vérifier si le nombre existe déjà
                    string checkQuery = "SELECT COUNT(*) FROM calcul_results WHERE nombre = @nombre";
                    using (var checkCommand = new MySqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.Add(new MySqlParameter("@nombre", dto.Nombre));
                        int count = Convert.ToInt32(checkCommand.ExecuteScalar()); 

                        if (count > 0)
                        {
                            return Ok(new { message = "Résultat reçu mais non stocké (existe déjà)", result = dto });
                        }
                    }

                    // Insérer le nouveau résultat
                    string insertQuery = "INSERT INTO calcul_results (nombre, pair, premier, parfait, created_at) " +
                                        "VALUES (@nombre, @pair, @premier, @parfait, @created_at)";
                    using (var insertCommand = new MySqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.Add(new MySqlParameter("@nombre", dto.Nombre));
                        insertCommand.Parameters.Add(new MySqlParameter("@pair", dto.Pair));
                        insertCommand.Parameters.Add(new MySqlParameter("@premier", dto.Premier));
                        insertCommand.Parameters.Add(new MySqlParameter("@parfait", dto.Parfait));
                        insertCommand.Parameters.Add(new MySqlParameter("@created_at", DateTime.UtcNow));

                        int rowsAffected = insertCommand.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok(new { message = "Résultat reçu et stocké avec succès", result = dto });
                        }
                        else
                        {
                            return BadRequest(new { error = "Échec de l'insertion dans la base de données" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Erreur lors de la sauvegarde en base de données", details = ex.Message });
            }
        }

        public class CalculDto
        {
            public int Nombre { get; set; }
            public bool Pair { get; set; }
            public bool Premier { get; set; }
            public bool Parfait { get; set; }
            public List<int> Syracuse { get; set; }
        }
    }
}


        //     try
        //     {
        //         // 1. Sauvegarde dans MinIO
        //         await SaveToMinioAsync(dto);

        //         // 2. Sauvegarde dans la base de données
        //         await SaveToDatabaseAsync(dto);

        //         return Ok(new { message = "Données traitées avec succès", result = dto });
        //     }
        //     catch (Exception ex)
        //     {
        //         return BadRequest(new { error = "Erreur lors du traitement", details = ex.Message });
        //     }

        // }

        // // Méthode asynchrone pour sauvegarder les données dans MinIO
        // private async Task SaveToMinioAsync(CalculDto dto)
        // {
        //     var endpoint = "localhost:9000";
        //     var accessKey = "admin";
        //     var secretKey = "admin123";
        //     var bucketName = "syracuse";
        //     var fileName = $"{dto.Nombre}.txt";
        //     var fileContent = string.Join(", ", dto.Syracuse);
        //     var contentType = "text/plain"; // Type MIME pour fichier texte

        //     try
        //     {
        //         var minio = new MinioClient()
        //             .WithEndpoint(endpoint)
        //             .WithCredentials(accessKey, secretKey)
        //             .Build();

        //         // Vérifier si le bucket existe, sinon le créer
        //         bool found = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
        //         if (!found)
        //         {
        //             await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
        //         }

        //         // Préparer le fichier à télécharger
        //         var fileBytes = Encoding.UTF8.GetBytes(fileContent);
        //         using (var memoryStream = new MemoryStream(fileBytes))
        //         {
        //             var putObjectArgs = new PutObjectArgs()
        //                 .WithBucket(bucketName)
        //                 .WithObject(fileName)
        //                 .WithStreamData(memoryStream)
        //                 .WithObjectSize(memoryStream.Length)
        //                 .WithContentType(contentType);

        //             await minio.PutObjectAsync(putObjectArgs);
        //         }

                

        //         Console.WriteLine("Fichier téléchargé avec succès dans MinIO.");
        //     }
        //     catch (Exception ex)
        //     {
        //         throw new Exception("Erreur lors du téléchargement dans MinIO", ex);
        //     }
        // }


        // // Méthode asynchrone pour sauvegarder les résultats dans la base de données MySQL
        // private async Task SaveToDatabaseAsync(CalculDto dto)
        // {
        //     string connectionString = "Server=db;Port=3306;Database=calculs;User=calcul_user;Password=1234;";
        //     try
        //     {
        //         using (var connection = new MySqlConnection(connectionString))
        //         {
        //             await connection.OpenAsync();

        //             // Vérifier si le nombre existe déjà dans la base de données
        //             string checkQuery = "SELECT COUNT(*) FROM calcul_results WHERE nombre = @nombre";
        //             using (var checkCommand = new MySqlCommand(checkQuery, connection))
        //             {
        //                 checkCommand.Parameters.Add(new MySqlParameter("@nombre", dto.Nombre));
        //                 int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

        //                 if (count > 0)
        //                 {
        //                     throw new Exception("Résultat déjà existant pour ce nombre");
        //                 }
        //             }

        //             // Insérer un nouveau résultat dans la base de données
        //             string insertQuery = "INSERT INTO calcul_results (nombre, pair, premier, parfait, created_at) " +
        //                                  "VALUES (@nombre, @pair, @premier, @parfait, @created_at)";
        //             using (var insertCommand = new MySqlCommand(insertQuery, connection))
        //             {
        //                 insertCommand.Parameters.Add(new MySqlParameter("@nombre", dto.Nombre));
        //                 insertCommand.Parameters.Add(new MySqlParameter("@pair", dto.Pair));
        //                 insertCommand.Parameters.Add(new MySqlParameter("@premier", dto.Premier));
        //                 insertCommand.Parameters.Add(new MySqlParameter("@parfait", dto.Parfait));
        //                 insertCommand.Parameters.Add(new MySqlParameter("@created_at", DateTime.UtcNow));

        //                 int rowsAffected = await insertCommand.ExecuteNonQueryAsync();

        //                 if (rowsAffected == 0)
        //                 {
        //                     throw new Exception("Échec de l'insertion dans la base de données");
        //                 }
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine("Erreur BDD: " + ex.Message);
        //         throw new Exception("Erreur lors de la sauvegarde en base de données", ex);
        //     }
        // }
