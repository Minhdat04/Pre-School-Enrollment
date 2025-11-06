using System.Security.Claims;

namespace PreschoolEnrollmentSystem.API.Helpers
{
    public static class ControllerHelper
    {
        public static string GetCurrentFirebaseUid(this ClaimsPrincipal user)
        {
            var firebaseUid = user.FindFirst("firebase_uid")?.Value;

            if (string.IsNullOrEmpty(firebaseUid))
            {
                throw new UnauthorizedAccessException("User not authenticated or firebase_uid claim is missing.");
            }

            return firebaseUid;
        }
    }
}
