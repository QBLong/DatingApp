using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Errors
{
    public class ApiException : Exception
    {
        public ApiException(int statusCode, string messages, string details) 
        {
            this.StatusCode = statusCode;
            this.Messages = messages;
            this.Details = details;
        }
        public int StatusCode { get; set; }
        public string Messages { get; set; }
        public string Details { get; set; } 
    }
}