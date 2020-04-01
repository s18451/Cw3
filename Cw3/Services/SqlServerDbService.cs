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

        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {
            throw new NotImplementedException();
        }

        public IActionResult PromoteStudent(EnrollPromoteRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
