using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO; 
using System.Text;   
using Minio;
using Minio.DataModel.Args;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

// Commenter tous le code
// Faire de meilleurs messages de retour
// Changer le nom du namespace ?
namespace MonProjetAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerifController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public VerifController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Revoir tous les return etc
        // Voir a quoi sert _configuration ?

        [HttpPost]
        public async Task<IActionResult> PostResult([FromBody] int number)
        {

            try {
                var databaseResult = VerifyInDatabase(number);

                if (databaseResult != null) 
                {
                    List<int> syracuseList = await VerifyInBucket(number);

                    if (syracuseList != null)
                    {
                        // Créer un objet CalculDto
                        CalculDto dto = new()
                        {
                            Number = number,
                            IsEven = databaseResult["Pair"],
                            IsPrime = databaseResult["Premier"],
                            IsPerfect = databaseResult["Parfait"],
                            Syracuse = syracuseList,
                        };

                        return Ok(new { found = true, dto });
                    }
                }
                return Ok(new { found = false });
            } catch (Exception) {
                return Ok(new { found = false });
            }
        }

        // Faire le summary
        static private async Task<List<int>> VerifyInBucket(int number) 
        {
            var endpoint = "minio:9000"; // Adresse MinIO
            var accessKey = "admin";         // Identifiant
            var secretKey = "admin123";      // Mot de passe
            var bucketName = "syracuse";     // Nom du bucket

            try {
                // Configuration du client MinIO
                var minioClient = new MinioClient()
                    .WithEndpoint(endpoint)
                    .WithCredentials(accessKey, secretKey)
                    .Build();

                // Vérifier si le bucket existe
                var bucketExists = await VerifyIfBucketExists(minioClient, bucketName);

                if (bucketExists) {
                    var dataExists = await VerifyIfDataSaveInBucket(minioClient, bucketName, number);

                    if (dataExists != null)
                        return dataExists;
                }
                
                return null;

            } catch (Exception ex) {
                throw new Exception("Erreur lors de la recherche dans MinIO", ex);
            } 
        }

        static private async Task<bool> VerifyIfBucketExists(IMinioClient minioClient, string bucketName)
        {
            var bucketExists = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));

            return bucketExists;
        }

        static private async Task<List<int>> VerifyIfDataSaveInBucket(IMinioClient minioClient, string bucketName, int number)
        {
            var fileName = $"{number}.txt"; // Nom du fichier basé sur le nombre

            try
            {
                var dataList = new List<int>();

                void memoryStream(Stream stream)
                {
                    using var reader = new StreamReader(stream);
                    var fileContent = reader.ReadToEnd();

                    // Transformer le contenu en une liste d'entiers
                    dataList = fileContent
                        .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToList();
                }

                // Arguments pour récupérer l'objet
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(fileName)
                    .WithCallbackStream(memoryStream);

                // Récupère l'objet
                var objectStat = await minioClient.GetObjectAsync(getObjectArgs);

                return dataList;
            } 
            catch (Minio.Exceptions.ObjectNotFoundException) 
            {
                // Si l'objet n'est pas trouvé, retourner null
                return null;
            } 
            catch (Exception ex) 
            {
                throw new Exception("Erreur lors de la vérification du fichier dans MinIO", ex);
            }
        }

        static private Dictionary<string, bool> VerifyInDatabase(int number)
        {
            string connectionString = "Server=db;Port=3306;Database=calculs;User=calcul_user;Password=1234;";

            try
            {
                using var connection = new MySqlConnection(connectionString);

                connection.Open();

                // Vérifier si les informations sont déjà stockées dans la BDD
                var result = VerifyIfDataSaveInDatabase(connection, number);

                if (result != null) {
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la recherche dans la base de données", ex);
            }
        }

        static private Dictionary<string, bool> VerifyIfDataSaveInDatabase(MySqlConnection connection, int number) {
            string checkQuery = "SELECT pair, premier, parfait FROM calcul_results WHERE nombre = @nombre";

            using var checkCommand = new MySqlCommand(checkQuery, connection);

            checkCommand.Parameters.Add(new MySqlParameter("@nombre", number));

            try
            {
                using var reader = checkCommand.ExecuteReader();

                // Si une ligne est retournée
                if (reader.HasRows)
                {
                    reader.Read();

                    // Récupérer les données
                    var result = new Dictionary<string, bool>
                    {
                        { "Pair", (bool)reader["pair"] },
                        { "Premier", (bool)reader["premier"] },
                        { "Parfait", (bool)reader["parfait"] }
                    };

                    return result;  // Retourne l'objet avec les résultats
                }

                return null; 
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la vérification dans la base de données", ex);
            }
        }
    }
}