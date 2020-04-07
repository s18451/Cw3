using Cw3.DTOs.Requests;
using Cw3.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.Services
{
    public interface IStudentsDbService
    {
        EnrollStudentResponse EnrollStudent(EnrollStudentRequest request);
        EnrollPromoteResponse PromoteStudent(EnrollPromoteRequest request);
        bool checkStudentID(string index);
    }
}
