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

        /// <summary>
        /// Méthode qui reçoit une requête HTTP POST, vérifie si les données sont présentes dans la base de données et le bucket MinIO, 
        /// et renvoie un objet CalculDto si les données sont trouvées.
        /// </summary>
        /// <param name="number">Le nombre à rechercher dans la base de données et dans MinIO.</param>
        /// <returns>Retourne un statut HTTP avec l'objet CalculDto si les données sont trouvées, sinon un statut de non trouvé.</returns>
        [HttpPost]
        public async Task<IActionResult> PostResult([FromBody] long number)
        {
            try 
            {
                // Vérifier les données dans la base de données
                var databaseResult = VerifyInDatabase(number);

                if (databaseResult == null) 
                {
                    return Ok(new { found = false});
                }
                
                // Vérifier les données dans le bucket MinIO
                List<string> syracuseList = await VerifyInBucket(number);
                if (syracuseList == null)
                {
                    return Ok(new { found = false });
                }

                // Créer un objet CalculDto avec les résultats
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
            catch (Exception ex) 
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Vérifie la présence des données dans un bucket MinIO en recherchant un fichier associé au nombre donné.
        /// </summary>
        /// <param name="number">Le nombre à rechercher dans MinIO.</param>
        /// <returns>Retourne une liste d'entiers extraite du fichier dans le bucket, ou null si le fichier n'est pas trouvé.</returns>
        private async Task<List<string>> VerifyInBucket(long number) 
        {
            try 
            {
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

            } 
            catch (Exception ex) 
            {
                throw new Exception("Erreur lors de la vérification dans MinIO", ex);
            } 
        }

        /// <summary>
        /// Vérifie si un bucket existe dans MinIO.
        /// </summary>
        /// <param name="minioClient">Le client MinIO pour interagir avec MinIO.</param>
        /// <param name="bucketName">Le nom du bucket à vérifier.</param>
        /// <returns>Retourne true si le bucket existe, sinon false.</returns>
        static private async Task<bool> VerifyIfBucketExists(IMinioClient minioClient, string bucketName)
        {
            var bucketExists = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));

            return bucketExists;
        }

        /// <summary>
        /// Vérifie si des données associées au nombre sont présentes dans un fichier dans le bucket MinIO.
        /// </summary>
        /// <param name="minioClient">Le client MinIO pour interagir avec MinIO.</param>
        /// <param name="bucketName">Le nom du bucket contenant le fichier.</param>
        /// <param name="number">Le nombre utilisé pour déterminer le nom du fichier à rechercher dans le bucket.</param>
        /// <returns>Retourne une liste d'entiers si les données sont trouvées dans le fichier, sinon null.</returns>
        static private async Task<List<string>> VerifyIfDataSaveInBucket(IMinioClient minioClient, string bucketName, long number)
        {
            var fileName = $"{number}.txt"; // Nom du fichier basé sur le nombre

            try
            {
                var dataList = new List<string>();

                // Méthode de traitement de flux pour lire et transformer le contenu en une liste d'entiers
                void ProcessStream(Stream stream)
                {
                    using var reader = new StreamReader(stream);
                    var fileContent = reader.ReadToEnd();

                    // Transformer le contenu en une liste d'entiers
                    dataList = fileContent
                        .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(value => value.Trim())                      
                        .ToList();
                }

                // Arguments pour récupérer l'objet
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(fileName)
                    .WithCallbackStream(ProcessStream);

                // Récupère l'objet depuis le bucket MinIO
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

        /// <summary>
        /// Vérifie la présence des données dans la base de données MySQL associées au nombre donné.
        /// </summary>
        /// <param name="number">Le nombre à rechercher dans la base de données.</param>
        /// <returns>Retourne un dictionnaire avec les résultats de la base de données (pair, premier, parfait) ou null si les données ne sont pas trouvées.</returns>
        private Dictionary<string, bool> VerifyInDatabase(long number)
        {
            // Configuration du client MySQL
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
                throw new Exception("Erreur lors de la vérification dans la base de données", ex);
            }
        }

        /// <summary>
        /// Vérifie si des données associées au nombre sont présentes dans la table de résultats de calculs de la base de données.
        /// </summary>
        /// <param name="connection">La connexion MySQL pour exécuter la requête.</param>
        /// <param name="number">Le nombre à rechercher dans la base de données.</param>
        /// <returns>Retourne un dictionnaire avec les résultats (pair, premier, parfait) si trouvés, sinon null.</returns>
        static private Dictionary<string, bool> VerifyIfDataSaveInDatabase(MySqlConnection connection, long number) 
        {
            // Préparation de la requête
            string checkQuery = "SELECT pair, premier, parfait FROM calcul_results WHERE nombre = @nombre";

            using var checkCommand = new MySqlCommand(checkQuery, connection);

            checkCommand.Parameters.Add(new MySqlParameter("@nombre", number));

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

                return result;
            }

            return null; 
        }
    }
}