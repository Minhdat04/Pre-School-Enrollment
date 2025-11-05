using System;

namespace PreschoolEnrollmentSystem.Core.Enums
{
    public enum UserRole
    {
        Parent = 1,
        Staff = 2,
        Admin = 3
    }

    public static class UserRoleExtensions
    {
        public static string ToRoleString(this UserRole role)
        {
            return role switch
            {
                UserRole.Parent => "Parent",
                UserRole.Staff => "Staff",
                UserRole.Admin => "Admin",
                _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unknown role: {role}")
            };
        }
        public static UserRole ParseRole(string roleString)
        {
            return roleString?.ToLower() switch
            {
                "parent" => UserRole.Parent,
                "staff" => UserRole.Staff,
                "admin" => UserRole.Admin,
                _ => throw new ArgumentException($"Invalid role: {roleString}", nameof(roleString))
            };
        }
        public static bool IsAdmin(this UserRole role)
        {
            return role == UserRole.Admin;
        }
        public static bool IsStaffOrAdmin(this UserRole role)
        {
            return role == UserRole.Staff || role == UserRole.Admin;
        }
        public static string GetDisplayName(this UserRole role)
        {
            return role switch
            {
                UserRole.Parent => "Parent/Guardian",
                UserRole.Staff => "Staff Member",
                UserRole.Admin => "Administrator",
                _ => "Unknown Role"
            };
        }
        public static string GetDescription(this UserRole role)
        {
            return role switch
            {
                UserRole.Parent => "Can register children and submit enrollment applications",
                UserRole.Staff => "Can manage assigned classes and view student information",
                UserRole.Admin => "Has full system access and can manage all aspects",
                _ => "Unknown role"
            };
        }
    }
}