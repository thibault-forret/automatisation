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


        /// <summary>
        /// Méthode qui reçoit une requête HTTP POST et tente de sauvegarder les données dans MinIO et dans la base de données.
        /// </summary>
        /// <param name="dto">L'objet contenant les données à traiter et à sauvegarder.</param>
        /// <returns>Retourne un statut HTTP selon le succès ou l'échec de l'opération.</returns>
        [HttpPost]
        public async Task<IActionResult> PostResult([FromBody] CalculDto dto)
        {
            try 
            {
                // Sauvegarder dans le bucket MinIO
                await SaveIntoBucket(dto);
                
                // Sauvegarder dans la base de données
                SaveIntoDatabase(dto);

                // Si tout se passe bien, retourner l'objet DTO avec succès
                return Ok(new { dto });
            } 
            catch (Exception ex) 
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Sauvegarde les données dans un bucket MinIO.
        /// </summary>
        /// <param name="dto">L'objet contenant les données à sauvegarder.</param>
        private async Task SaveIntoBucket(CalculDto dto) 
        {
            try 
            {
                // Configuration du client MinIO
                var minioClient = new MinioClient()
                    .WithEndpoint(_minioSettings.Endpoint)
                    .WithCredentials(_minioSettings.AccessKey, _minioSettings.SecretKey)
                    .Build();
                
                // Vérifier si le bucket existe, sinon le créer
                await IfBucketDoesntExistsCreateIt(minioClient, _minioSettings.BucketName);

                // Stocker le fichier dans le bucket
                await SaveFileIntoBucket(minioClient, _minioSettings.BucketName, dto);
            } 
            catch (Exception ex) 
            {
                throw new Exception("Erreur lors du téléchargement dans MinIO", ex);
            } 
        }

        /// <summary>
        /// Vérifie si le bucket existe dans MinIO, sinon il le crée.
        /// </summary>
        /// <param name="minioClient">Le client MinIO pour interagir avec MinIO.</param>
        /// <param name="bucketName">Le nom du bucket à vérifier ou à créer.</param>
        static private async Task IfBucketDoesntExistsCreateIt(IMinioClient minioClient, string bucketName)
        {
            bool found = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!found)
            {
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            }
        }

        /// <summary>
        /// Sauvegarde un fichier dans un bucket MinIO avec le nom basé sur le nombre, et le contenu comme chaîne de caractères séparée par des virgules.
        /// </summary>
        /// <param name="minioClient">Le client MinIO pour interagir avec MinIO.</param>
        /// <param name="bucketName">Le nom du bucket où sauvegarder le fichier.</param>
        /// <param name="dto">L'objet contenant les données à sauvegarder dans le fichier.</param>
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

        /// <summary>
        /// Sauvegarde les données dans la base de données MySQL.
        /// </summary>
        /// <param name="dto">L'objet contenant les données à sauvegarder dans la base de données.</param>
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

        /// <summary>
        /// Vérifie si les données (en particulier le nombre) sont déjà présentes dans la base de données.
        /// </summary>
        /// <param name="connection">La connexion MySQL pour interroger la base de données.</param>
        /// <param name="dto">L'objet contenant les données à vérifier.</param>
        /// <returns>Retourne true si les données sont déjà présentes, sinon false.</returns>
        static private bool VerifyIfDataAlreadySave(MySqlConnection connection, CalculDto dto) 
        {
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

        /// <summary>
        /// Insère les données dans la table de résultats de calculs dans la base de données.
        /// </summary>
        /// <param name="connection">La connexion MySQL pour exécuter l'insertion.</param>
        /// <param name="dto">L'objet contenant les données à insérer dans la base de données.</param>
        /// <returns>Retourne true si l'insertion est réussie, sinon false.</returns>
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