using Cw3.DTOs.Requests;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cw3.Services
{
    interface IStudentsDbService
    {
        IActionResult EnrollStudent(EnrollStudentRequest request);
        IActionResult PromoteStudent(EnrollPromoteRequest request);
    }
}
