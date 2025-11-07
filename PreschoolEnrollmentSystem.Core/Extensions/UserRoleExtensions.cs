using System;
using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Core.Extensions
{
    public static class UserRoleExtensions
    {
        public static string ToRoleString(this UserRole role)
        {
            return role switch
            {
                UserRole.Parent => "Parent",
                UserRole.Staff => "Staff",
                UserRole.Admin => "Admin",
                UserRole.Teacher => "Teacher",
                _ => throw new ArgumentException($"Unknown role: {role}")
            };
        }

        public static UserRole ParseRole(string roleString)
        {
            if (string.IsNullOrWhiteSpace(roleString))
                throw new ArgumentException("Role string cannot be null or empty", nameof(roleString));

            return roleString.Trim().ToLower() switch
            {
                "parent" => UserRole.Parent,
                "staff" => UserRole.Staff,
                "admin" => UserRole.Admin,
                "teacher" => UserRole.Teacher,
                _ => throw new ArgumentException($"Invalid role string: {roleString}")
            };
        }
    }
}
