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

                    // Vérifier si il est déjà stocker

                    string query = "INSERT INTO calcul_results (nombre, pair, premier, parfait, created_at) VALUES (@nombre, @pair, @premier, @parfait, @created_at)";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add(new MySqlParameter("@nombre", dto.Nombre));
                        command.Parameters.Add(new MySqlParameter("@pair", dto.Pair));
                        command.Parameters.Add(new MySqlParameter("@premier", dto.Premier));
                        command.Parameters.Add(new MySqlParameter("@parfait", dto.Parfait));
                        command.Parameters.Add(new MySqlParameter("@created_at", DateTime.UtcNow));

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok(new { message = "Résultats reçus et stockés", result = dto });
                        }
                        else
                        {
                            return BadRequest(new { error = "Insertion échouée" });
                        }
                    }
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
