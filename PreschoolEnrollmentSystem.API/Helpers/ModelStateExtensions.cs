using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PreschoolEnrollmentSystem.API.Helpers
{
    public static class ModelStateExtensions
    {
        public static string ToErrorString(this ModelStateDictionary modelState)
        {
            var errors = modelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return string.Join("; ", errors);
        }
    }
}
