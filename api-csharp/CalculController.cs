using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;

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
        public IActionResult PostResult([FromBody] CalculDto dto)
        {
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

                    // string query = "INSERT INTO calcul_results (nombre, pair, premier, parfait, created_at) VALUES (@nombre, @pair, @premier, @parfait, @created_at)";
                    // using (var command = new MySqlCommand(query, connection))
                    // {
                    //     command.Parameters.Add(new MySqlParameter("@nombre", dto.Nombre));
                    //     command.Parameters.Add(new MySqlParameter("@pair", dto.Pair));
                    //     command.Parameters.Add(new MySqlParameter("@premier", dto.Premier));
                    //     command.Parameters.Add(new MySqlParameter("@parfait", dto.Parfait));
                    //     command.Parameters.Add(new MySqlParameter("@created_at", DateTime.UtcNow));

                    //     int rowsAffected = command.ExecuteNonQuery();

                    //     if (rowsAffected > 0)
                    //     {
                    //         return Ok(new { message = "Résultats reçus et stockés", result = dto });
                    //     }
                    //     else
                    //     {
                    //         return BadRequest(new { error = "Insertion échouée" });
                    //     }
                    // }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Erreur lors de la sauvegarde en base de données", details = ex.Message });
            }
        }
    }

    public class CalculDto
    {
        public int Nombre { get; set; }
        public bool Pair { get; set; }
        public bool Premier { get; set; }
        public bool Parfait { get; set; }
    }
}
