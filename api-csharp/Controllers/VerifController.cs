using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Options;
using System;
using System.IO; 
using Minio;
using Minio.DataModel.Args;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MonProjet.Models;

// Commenter tous le code
// Faire de meilleurs messages de retour
// Changer le nom du namespace ?
namespace MonProjet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerifController : ControllerBase
    {

        private readonly DatabaseSettings _databaseSettings;
        private readonly MinioSettings _minioSettings;

        public VerifController(IOptions<DatabaseSettings> databaseSettings, IOptions<MinioSettings> minioSettings)
        {
            _databaseSettings = databaseSettings.Value;
            _minioSettings = minioSettings.Value;
        }

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

        private async Task<List<int>> VerifyInBucket(int number) 
        {
            try {
                // Configuration du client MinIO
                var minioClient = new MinioClient()
                    .WithEndpoint(_minioSettings.Endpoint)
                    .WithCredentials(_minioSettings.AccessKey, _minioSettings.SecretKey)
                    .Build();

                // Vérifier si le bucket existe
                var bucketExists = await VerifyIfBucketExists(minioClient, _minioSettings.BucketName);

                if (bucketExists) {
                    var dataExists = await VerifyIfDataSaveInBucket(minioClient, _minioSettings.BucketName, number);

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

        private Dictionary<string, bool> VerifyInDatabase(int number)
        {
            string connectionString = _databaseSettings.Connection;

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