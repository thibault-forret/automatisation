using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Options;
using System;
using System.IO; 
using System.Text;   
using Minio;
using Minio.DataModel.Args;
using System.Threading.Tasks;
using MonProjet.Models;

// Commenter tous le code
// Faire de meilleurs messages de retour
namespace MonProjet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SaveController : ControllerBase
    {
        private readonly DatabaseSettings _databaseSettings;
        private readonly MinioSettings _minioSettings;

        public SaveController(IOptions<DatabaseSettings> databaseSettings, IOptions<MinioSettings> minioSettings)
        {
            _databaseSettings = databaseSettings.Value;
            _minioSettings = minioSettings.Value;
        }

        [HttpPost]
        public async Task<IActionResult> PostResult([FromBody] CalculDto dto)
        {
            try {
                await SaveIntoBucket(dto);

                SaveIntoDatabase(dto);
            } catch (Exception ex) {
                return BadRequest(new { error = ex.Message });
            }

            return Ok(new { dto });
        }

        // Faire le summary
        private async Task SaveIntoBucket(CalculDto dto) 
        {
            try {
                // Configuration du client MinIO
                var minioClient = new MinioClient()
                    .WithEndpoint(_minioSettings.Endpoint)
                    .WithCredentials(_minioSettings.AccessKey, _minioSettings.SecretKey)
                    .Build();
                
                // Vérifier si le bucket existe, sinon le créer
                await IfBucketDoesntExistsCreateIt(minioClient, _minioSettings.BucketName);

                // Stocker le fichier dans le bucket
                await SaveFileIntoBucket(minioClient, _minioSettings.BucketName, dto);
            } catch (Exception ex) {
                throw new Exception("Erreur lors du téléchargement dans MinIO", ex);
            } 
        }

        static private async Task IfBucketDoesntExistsCreateIt(IMinioClient minioClient, string bucketName)
        {
            bool found = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!found)
            {
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            }
        }

        static private async Task SaveFileIntoBucket(IMinioClient minioClient, string bucketName, CalculDto dto)
        {
            // Nom du fichier basé sur le nombre
            var fileName = $"{dto.Number}.txt";

            // Contenu du fichier
            var fileContent = string.Join(", ", dto.Syracuse);
            var fileBytes = Encoding.UTF8.GetBytes(fileContent);

            // Stocker le fichier
            using var memoryStream = new MemoryStream(fileBytes);

            await minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName)
                .WithStreamData(memoryStream)
                .WithObjectSize(memoryStream.Length)
                .WithContentType("text/plain"));
        }

        private void SaveIntoDatabase(CalculDto dto)
        {
            string connectionString = _databaseSettings.Connection;

            try
            {
                using var connection = new MySqlConnection(connectionString);

                connection.Open();

                // Vérifier si les informations sont déjà stockées dans la BDD
                var result = VerifyIfDataAlreadySave(connection, dto);

                // Insérer si ce n'est pas stocker, sinon ne rien faire
                if (!result) {
                    var insert = InsertDataIntoDatabase(connection, dto);

                    // Vérifie si l'inserstion s'est bien dérouler
                    if (!insert)
                        throw new Exception("Un problème est survenu lors de l'insertion");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la sauvegarde en base de données", ex);
            }
        }

        static private bool VerifyIfDataAlreadySave(MySqlConnection connection, CalculDto dto) {
            // Préparation de la requête
            string checkQuery = "SELECT COUNT(*) FROM calcul_results WHERE nombre = @nombre";

            using var checkCommand = new MySqlCommand(checkQuery, connection);

            checkCommand.Parameters.Add(new MySqlParameter("@nombre", dto.Number));

            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            // Vérifie si une ligne est retournée
            if (count > 0)
                return true;

            return false;
        }

        static private bool InsertDataIntoDatabase(MySqlConnection connection, CalculDto dto)
        {
            // Préparation de la requête
            string insertQuery = "INSERT INTO calcul_results (nombre, pair, premier, parfait, created_at) " +
                                    "VALUES (@nombre, @pair, @premier, @parfait, @created_at)";

            using var insertCommand = new MySqlCommand(insertQuery, connection);

            // Ajout des paramètres
            insertCommand.Parameters.Add(new MySqlParameter("@nombre", dto.Number));
            insertCommand.Parameters.Add(new MySqlParameter("@pair", dto.IsEven));
            insertCommand.Parameters.Add(new MySqlParameter("@premier", dto.IsPrime));
            insertCommand.Parameters.Add(new MySqlParameter("@parfait", dto.IsPerfect));
            insertCommand.Parameters.Add(new MySqlParameter("@created_at", DateTime.UtcNow));

            int rowsAffected = insertCommand.ExecuteNonQuery();

            // Vérifier si ça a bien été insérer
            if (rowsAffected > 0)
                return true;
            else
                return false;
        }
    }
}