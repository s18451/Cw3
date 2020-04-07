using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Cw3.DTOs.Requests;
using Cw3.DTOs.Responses;
using System.Threading.Tasks;


namespace Cw3.Services
{
    public class SqlServerDbService : IStudentsDbService
    {
        private String connection = "Data Source=db-mssql;Initial Catalog=s18451;Integrated Security=True";

        public bool checkStudentID(string index)
        {
            using (var con = new SqlConnection(connection))
            {
                using (var com = new SqlCommand())
                {
                    com.Connection = con;

                    con.Open();

                    com.CommandText = "SELECT COUNT(*) AS COUNT FROM Student WHERE IndexNumber=@ID";
                    com.Parameters.AddWithValue("ID", index);
                    var dr = com.ExecuteReader();
                    if (dr.Read())
                    {
                        if (Int32.Parse(dr[dr.GetName(0)].ToString()) > 0)
                        {
                            dr.Close();
                            return true;
                        }
                    }
                    dr.Close();
                    return false;
                }

            }
        }
        public EnrollStudentResponse EnrollStudent(EnrollStudentRequest request)
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
                            throw new NotImplementedException("Studia nie istnieją");
                        }
                        int studies = Int32.Parse(dr["IdStudy"].ToString());
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
                                newID = Int32.Parse(dr[dr.GetName(0)].ToString());
                            }
                            newID++;
                            com.CommandText = "INSERT INTO Enrollment VALUES(@newID, 1, @studies, @date)";
                            com.Parameters.AddWithValue("newID", newID);
                            com.Parameters.AddWithValue("studies", studies);
                            com.Parameters.AddWithValue("date", DateTime.Now);
                            IdEnrollment = newID;
                            dr.Close();
                            com.ExecuteNonQuery();
                        }
                        else
                        {
                            IdEnrollment = Int32.Parse(dr["IdEnrollment"].ToString());
                        }
                        dr.Close();

                        //Pamietamy o tym, aby sprawdzic czy indeks podany przez studenta jest unikalny. W przeciwnym wypadku zgłaszamy bład.
                        com.CommandText = "SELECT COUNT(*) FROM STUDENT WHERE IndexNumber = @IndexNumber";
                        com.Parameters.AddWithValue("IndexNumber", request.IndexNumber);

                        dr = com.ExecuteReader();
                        if (dr.Read())
                        {
                            if (Int32.Parse(dr[dr.GetName(0)].ToString()) > 0)
                            {
                                dr.Close();
                                tran.Rollback();
                                throw new NotImplementedException("Indeks nie jest unikalny");
                            }
                        }
                        else
                        {
                            dr.Close();
                            tran.Rollback();
                            throw new NotImplementedException("Cannot read data");
                        }
                        dr.Close();

                        //Na koncu dodajemy wpis w tabeli Students
                        com.CommandText = "INSERT INTO Student VALUES (\'" + request.IndexNumber + "\',\'" + request.FirstName + "\',\'" + request.LastName + "\',\'" + request.BirthDate.ToString("yyyy-MM-dd") + "\'," + IdEnrollment + ")";

                        com.ExecuteNonQuery();

                        tran.Commit();

                    }
                    catch (SqlException ex)
                    {
                        tran.Rollback();
                        throw new NotImplementedException(ex.ToString());
                    }
                }
            }
            var response = new EnrollStudentResponse();
            response.LastName = request.LastName;
            response.Semester = 1;
            response.StartDate = DateTime.Now;

            return response;
        }

        public EnrollPromoteResponse PromoteStudent(EnrollPromoteRequest request)
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
                        throw new NotImplementedException("Nie znaleziono danych");
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
            return response;
        }
    }
}
