using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO; 
using System.Text;   
using Minio;
using Minio.DataModel.Args;
using System.Threading.Tasks;

// Commenter tous le code
// Factoriser le code
// Faire de meilleurs messages de retour
// Changer le nom du namespace ?
namespace MonProjetAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SaveController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public SaveController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> PostResult([FromBody] CalculDto dto)
        {

            try {
                await SaveIntoBucket(dto);

                SaveIntoDatabase(dto);
            } catch (Exception ex) {
                return BadRequest(new { error = ex });
            }

            return Ok(new { dto });
        }

        // Faire le summary
        static private async Task SaveIntoBucket(CalculDto dto) 
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

                // Vérifier si le bucket existe, sinon le créer
                await IfBucketDoesntExistsCreateIt(minioClient, bucketName);

                // Stocker le fichier dans le bucket
                await SaveFileIntoBucket(minioClient, bucketName, dto);
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

        // Si déjà stocker, ne pas upload le fichier (même si ca remplace le fichier d'origine sur le bucket)
        // -> Le récupérer
        static private async Task SaveFileIntoBucket(IMinioClient minioClient, string bucketName, CalculDto dto)
        {
            var fileName = $"{dto.Number}.txt"; // Nom du fichier basé sur le nombre
            var fileContent = string.Join(", ", dto.Syracuse); // Contenu du fichier
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

        // Faire le summary
        static private void SaveIntoDatabase(CalculDto dto)
        {
            string connectionString = "Server=db;Port=3306;Database=calculs;User=calcul_user;Password=1234;";

            try
            {
                using var connection = new MySqlConnection(connectionString);

                connection.Open();

                // Vérifier si les informations sont déjà stockées dans la BDD
                var result = VerifyIfDataAlreadySave(connection, dto);

                // Insérer si ce n'est pas stocker, sinon ne rien faire
                if (!result) {
                    // Insérer le nouveau résultat
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

        // Vérifier si le nombre est déjà stocker dans la BDD
        static private bool VerifyIfDataAlreadySave(MySqlConnection connection, CalculDto dto) {
            string checkQuery = "SELECT COUNT(*) FROM calcul_results WHERE nombre = @nombre";

            using var checkCommand = new MySqlCommand(checkQuery, connection);

            checkCommand.Parameters.Add(new MySqlParameter("@nombre", dto.Number));

            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count > 0) // Vérifie si une ligne est retournée
                return true;

            return false; // Si aucune donnée n'est trouvée
        }

        static private bool InsertDataIntoDatabase(MySqlConnection connection, CalculDto dto)
        {
            string insertQuery = "INSERT INTO calcul_results (nombre, pair, premier, parfait, created_at) " +
                                    "VALUES (@nombre, @pair, @premier, @parfait, @created_at)";

            using var insertCommand = new MySqlCommand(insertQuery, connection);

            insertCommand.Parameters.Add(new MySqlParameter("@nombre", dto.Number));
            insertCommand.Parameters.Add(new MySqlParameter("@pair", dto.IsEven));
            insertCommand.Parameters.Add(new MySqlParameter("@premier", dto.IsPrime));
            insertCommand.Parameters.Add(new MySqlParameter("@parfait", dto.IsPerfect));
            insertCommand.Parameters.Add(new MySqlParameter("@created_at", DateTime.UtcNow));

            int rowsAffected = insertCommand.ExecuteNonQuery();

            if (rowsAffected > 0)
                return true;
            else
                return false;
        }
    }
}