using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cw3.DTOs.Requests;
using Cw3.DTOs.Responses;
using Cw3.Models;
using Cw3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cw3.Controllers
{
    [ApiController]
    [Route("api/enrollments")]
    public class EnrollmentsController : ControllerBase
    {
        private IStudentsDbService _service;

        private String connection = "Data Source=db-mssql;Initial Catalog=s18451;Integrated Security=True";

        public IConfiguration Configuration { get; set; }

        public EnrollmentsController(IStudentsDbService service, IConfiguration configuration)
        {
            _service = service;
            Configuration = configuration;
        }


        [HttpPost]
        [Authorize(Roles = "employee")]
        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {
            try
            {
                var response = _service.EnrollStudent(request);
                return Created("", response);
            } catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpPost]
        [Route("promotions")]
        [Authorize(Roles = "employee")]
        public IActionResult PromoteStudent(EnrollPromoteRequest request)
        {
            try
            {
                var response = _service.PromoteStudent(request);
                return Created("", response);
            } catch(Exception ex)
            {
                return NotFound(ex);
            }
        }
        [HttpPost]
        [Route("login")]
        public IActionResult Login(LoginRequestDTO request)
        {

            var salt = EnrollmentsController.CreateSalt();
            var password = EnrollmentsController.Create("das8dha8dadha8", salt);

            string login;
            string name;
            using (var con = new SqlConnection(connection))
            using (var com = new SqlCommand())
            {

                com.Connection = con;
                com.CommandText = "SELECT * FROM student WHERE indexnumber = @indexnumber";
                com.Parameters.AddWithValue("indexnumber", request.Login);


                con.Open();

                var dr = com.ExecuteReader();

                if (!dr.Read())
                {
                    return BadRequest("Incorrect login or password");
                }
                if (!Validate(request.Pass, dr["salt"].ToString(), dr["password"].ToString()))
                {
                    return Unauthorized("Incorrect login or password");
                }
                login = dr["IndexNumber"].ToString();
                name = dr["FirstName"].ToString();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, login),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, "employee"),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken
            (
                issuer: "Admin",
                audience: "Employees",
                claims: claims,
                expires: DateTime.Now.AddMinutes(10),
                signingCredentials: creds
            );

            var refreshtoken = Guid.NewGuid();
            setRefreshToken(refreshtoken.ToString(), login);
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                refreshtoken
            });
        }

        [HttpPost]
        [Route("refreshtoken/{token}")]
        public IActionResult RefreshToken(string _token)
        {
            string login = null;
            string imie = null;
            using (var client = new SqlConnection(connection))
            using (var comm = new SqlCommand())
            {
                client.Open();
                comm.Connection = client;
                comm.CommandText = "SELECT indexnumber, firstname FROM student WHERE refreshtoken=@refreshtoken";
                comm.Parameters.AddWithValue("refreshtoken", _token);
                var dr = comm.ExecuteReader();
                if (!dr.Read())
                {
                    return Unauthorized("Incorrect Token");
                }
                login = dr["indexnumber"].ToString();
                imie = dr["firstname"].ToString();
            }
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, login),
                new Claim(ClaimTypes.Name, imie),
                new Claim(ClaimTypes.Role, "employee")
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken
            (
                issuer: "Admin",
                audience: "Employees",
                claims: claims,
                expires: DateTime.Now.AddMinutes(10),
                signingCredentials: creds
            );

            var refreshtoken = Guid.NewGuid();
            setRefreshToken(refreshtoken.ToString(), login);
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                refreshtoken
            });
        }
        public void setRefreshToken(string _token, string login)
        {
            using (var client = new SqlConnection(connection))
            using (var comm = new SqlCommand())
            {
                client.Open();
                comm.Connection = client;
                comm.CommandText = "UPDATE STUDENT SET Refreshtoken = @token WHERE indexnumber = @index";
                comm.Parameters.AddWithValue("index", login);
                comm.Parameters.AddWithValue("token", _token);
                comm.ExecuteNonQuery();
            }
        }


        public static string Create(string value, string salt)
        {
            var valueBytes = KeyDerivation.Pbkdf2(
                password: value,
                salt: Encoding.UTF8.GetBytes(salt),
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 1000,
                numBytesRequested: 256 / 8);
            return Convert.ToBase64String(valueBytes);
        }

        public static bool Validate(string value, string salt, string hash) => Create(value, salt) == hash;

        public static string CreateSalt()
        {
            byte[] randomBytes = new byte[128 / 8];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }
    }
}