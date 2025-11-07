using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Core.DTOs.Shared
{
    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
    }
    public class SuccessResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
