using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Cw3.DTOs.Requests;
using Cw3.DTOs.Responses;
using Cw3.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cw3.Controllers
{
    [ApiController]
    [Route("api/enrollments")]
    public class EnrollmentsController : Controller
    {
        private String connection = "Data Source=db-mssql;Initial Catalog=s18451;Integrated Security=True";

        [HttpPost]
        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {


            using (var con = new SqlConnection(connection))
            {
                using (var com = new SqlCommand())
                {
                    com.Connection = con;

                    con.Open();
                    var tran = con.BeginTransaction();
                    com.Transaction = tran;
                    try
                    {
                        com.CommandText = "select IdStudy from studies where Name=@name";
                        com.Parameters.AddWithValue("@name", request.Studies);


                        var dr = com.ExecuteReader();

                        if (!dr.Read())
                        {
                            dr.Close();
                            tran.Rollback();
                            return BadRequest("Studia nie istnieją");
                        }
                        int studies = (int)dr["IdStudy"];
                        dr.Close();
                        
                        com.CommandText = "SELECT IdEnrollment FROM Enrollment WHERE IdStudy=@idStudy AND Semester=1";
                        com.Parameters.AddWithValue("idStudy", studies);

                        dr = com.ExecuteReader();

                        int IdEnrollment = 0;

                        //Jesli tak wpis nie istnieje to dodajemy go do bazy danych (StartDate ustawiamyna aktualna date)
                        if (!dr.Read())
                        {
                            dr.Close();
                            com.CommandText = "SELECT MAX(IdEnrollment) FROM Enrollment";
                            dr = com.ExecuteReader();
                            int newID = 0;
                            if (dr.Read())
                            {
                                newID = (int)dr[dr.GetName(0)];
                            }
                            newID++;
                            com.CommandText = "INSERT INTO Enrollment VALUES(@newID, 1, @studies, @date)";
                            com.Parameters.AddWithValue("newID", newID);
                            com.Parameters.AddWithValue("studies", studies);
                            com.Parameters.AddWithValue("date", DateTime.Now);
                            IdEnrollment = newID;
                            dr.Close();
                            com.ExecuteNonQuery();
                        } else
                        {
                            IdEnrollment = (int)dr["IdEnrollment"];
                        }
                        dr.Close();
                        
                        //Pamietamy o tym, aby sprawdzic czy indeks podany przez studenta jest unikalny. W przeciwnym wypadku zgłaszamy bład.
                        com.CommandText = "SELECT COUNT(*) FROM STUDENT WHERE IndexNumber = @IndexNumber";
                        com.Parameters.AddWithValue("IndexNumber", request.IndexNumber);

                        dr = com.ExecuteReader();
                        if (dr.Read())
                        {
                            if ((int)dr[dr.GetName(0)] > 0)
                            {
                                dr.Close();
                                tran.Rollback();
                                return BadRequest("Indeks nie jest unikalny");
                            }
                        } else
                        {
                            dr.Close();
                            tran.Rollback();
                            return BadRequest("Cannot read data");
                        }
                        dr.Close();

                        //Na koncu dodajemy wpis w tabeli Students
                        com.CommandText = "INSERT INTO Student VALUES (\'"+request.IndexNumber+"\',\'"+request.FirstName+"\',\'"+request.LastName+ "\',\'" +request.BirthDate.ToString("yyyy-MM-dd")+ "\'," +IdEnrollment+")";

                        com.ExecuteNonQuery();

                        tran.Commit();

                    }catch (SqlException ex)
                    {
                        tran.Rollback();
                        return BadRequest("Blad");
                    }
                }
            }
            var response = new EnrollStudentResponse();
            response.LastName = request.LastName;
            response.Semester = 1;
            response.StartDate = DateTime.Now;

            return Created(request.ToString(), response);
        }
        [HttpPost]
        [Route("promotions")]
        public IActionResult PromoteStudent(EnrollPromoteRequest request)
        {
            var response = new EnrollPromoteResponse();
            using (var con = new SqlConnection(connection))
            {
                using (var com = new SqlCommand())
                {
                    con.Open();
                    com.Connection = con;



                    com.CommandText = "SELECT * FROM Enrollment JOIN Studies ON enrollment.IdStudy=studies.IdStudy WHERE Name = @name AND Semester=@semester";
                    com.Parameters.AddWithValue("@name", request.Studies);
                    com.Parameters.AddWithValue("@semester", request.Semester);

                    var dr = com.ExecuteReader();

                    if (!dr.Read())
                    {
                        return NotFound("Nie znaleziono danych");
                    }
                    dr.Close();

                    com.CommandType = System.Data.CommandType.StoredProcedure;
                    com.CommandText = "PromoteStudents";

                    com.Parameters.Clear();

                    com.Parameters.AddWithValue("@Studies", request.Studies);
                    com.Parameters.AddWithValue("@Semester", request.Semester);

                    com.ExecuteNonQuery();
                }
            }

            response.Studies = request.Studies;
            response.Semester = request.Semester + 1;
            return Created("", response);
        }


    }
}