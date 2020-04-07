using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cw3.Services;
using System.IO;
using System.Text;

namespace Cw3.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext httpContext, IStudentsDbService service)
        {
            httpContext.Request.EnableBuffering();
            if (httpContext.Request != null)
            {
                var bodyStream = string.Empty;

                using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    bodyStream = await reader.ReadToEndAsync();
                }

                string log = DateTime.Now + " "  +httpContext.Request.Path + " " +httpContext.Request.QueryString.ToString() + " " + httpContext.Request.Method.ToString() + " " + bodyStream;
                string path = "requestLogs.txt";
                if (!File.Exists(path))
                {
                    using(StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine(log);
                    }
                }
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(log);
                }
                //File.WriteAllText("requestLogs.txt", log);
            }
            //Our code
            
            httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            await _next(httpContext);
        }
    }
}
