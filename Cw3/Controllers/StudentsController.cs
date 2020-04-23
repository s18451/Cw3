using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cw3.DAL;
using Cw3.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Cw3.DTOs.Requests;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace Cw3.Controllers
{

    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
        private readonly IDbService _dbService;

        private String connection = "Data Source=db-mssql;Initial Catalog=s18451;Integrated Security=True";

        public IConfiguration Configuration { get; set; }

        public StudentsController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public StudentsController(IDbService dbService)
        {
            _dbService = dbService;
        }
        [HttpGet]
        public IActionResult GetStudents()
        {
            string id = "s1234";
            using (var con = new SqlConnection(connection))
            {
                using(var com = new SqlCommand())
                {
                    com.Connection = con;
                    com.CommandText = "select * from Student where IndexNumber=@id";
                    com.Parameters.AddWithValue("id", id);

                    con.Open();
                    var dr = com.ExecuteReader();

                    var studentList = new List<Student>();
                    while (dr.Read())
                    {
                        var st = new Student();
                        st.IndexNumber = dr["IndexNumber"].ToString();
                        st.FirstName = dr["FirstName"].ToString();
                        st.LastName = dr["LastName"].ToString();
                        st.BirthDate = DateTime.Parse(dr["BirthDate"].ToString());
                        st.IdEnrollment = int.Parse(dr["IdEnrollment"].ToString());
                        studentList.Add(st);
                    }
                    return Ok(studentList);
                }
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetStudent(string id)
        {
            using (var con = new SqlConnection(connection))
            {
                using (var com = new SqlCommand())
                {
                    com.Connection = con;
                    com.CommandText = "select * from Enrollment WHERE IdEnrollment=(SELECT IdEnrollment FROM Student WHERE IndexNumber=\'"+id+"\')";

                    con.Open();
                    var dr = com.ExecuteReader();

                    var enrollmentList = new List<string>();
                    while (dr.Read())
                    {
                        enrollmentList.Add(dr["Semester"].ToString());
                    }
                    return Ok(enrollmentList);
                }
            }
        }

        [HttpPost]
        public IActionResult CreateStudent(Student student)
        {
            student.IndexNumber = $"s{new Random().Next(1, 20000)}";
            return Ok(student);
        }
        [HttpPut("{id}")]
        public IActionResult PutStudent(int id)
        {
            return Ok("Aktualizacja dokończona");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteStudent(int id)
        {
            return Ok("Ususuwanie ukończone");
        }


        [HttpPost]
        public IActionResult Login(LoginRequestDTO requst)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "1"),
                new Claim(ClaimTypes.Role, "employee"),
                new Claim(ClaimTypes.Role, "student")
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

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                refreshtoken=Guid.NewGuid()
            });
        }
    }
    
}