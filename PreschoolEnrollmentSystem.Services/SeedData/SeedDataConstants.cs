using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Services.SeedData
{
    public static class SeedDataConstants
    {
        // Common password for all seed users (for testing only!)
        public const string DEFAULT_PASSWORD = "SeedUser123!@#";

        // Seed User Data
        public static readonly List<SeedUserData> Users = new()
        {
            // Admin User
            new SeedUserData
            {
                Email = "admin@preschool.edu.vn",
                FirstName = "Nguyễn",
                LastName = "Quản Trị",
                Phone = "+84901234567",
                Role = UserRole.Admin,
                Username = "admin"
            },

            // Staff Users
            new SeedUserData
            {
                Email = "staff1@preschool.edu.vn",
                FirstName = "Trần",
                LastName = "Thu Hà",
                Phone = "+84901234568",
                Role = UserRole.Staff,
                Username = "staff1"
            },
            new SeedUserData
            {
                Email = "staff2@preschool.edu.vn",
                FirstName = "Lê",
                LastName = "Minh Tuấn",
                Phone = "+84901234569",
                Role = UserRole.Staff,
                Username = "staff2"
            },

            // Teacher Users
            new SeedUserData
            {
                Email = "teacher1@preschool.edu.vn",
                FirstName = "Phạm",
                LastName = "Thị Lan",
                Phone = "+84901234570",
                Role = UserRole.Teacher,
                Username = "teacher1"
            },
            new SeedUserData
            {
                Email = "teacher2@preschool.edu.vn",
                FirstName = "Hoàng",
                LastName = "Văn Nam",
                Phone = "+84901234571",
                Role = UserRole.Teacher,
                Username = "teacher2"
            },
            new SeedUserData
            {
                Email = "teacher3@preschool.edu.vn",
                FirstName = "Vũ",
                LastName = "Thị Hoa",
                Phone = "+84901234572",
                Role = UserRole.Teacher,
                Username = "teacher3"
            },

            // Parent Users
            new SeedUserData
            {
                Email = "parent1@gmail.com",
                FirstName = "Nguyễn",
                LastName = "Văn An",
                Phone = "+84901234573",
                Role = UserRole.Parent,
                Username = "parent1"
            },
            new SeedUserData
            {
                Email = "parent2@gmail.com",
                FirstName = "Trần",
                LastName = "Thị Bình",
                Phone = "+84901234574",
                Role = UserRole.Parent,
                Username = "parent2"
            },
            new SeedUserData
            {
                Email = "parent3@gmail.com",
                FirstName = "Lê",
                LastName = "Hoàng Cường",
                Phone = "+84901234575",
                Role = UserRole.Parent,
                Username = "parent3"
            },
            new SeedUserData
            {
                Email = "parent4@gmail.com",
                FirstName = "Phạm",
                LastName = "Thị Dung",
                Phone = "+84901234576",
                Role = UserRole.Parent,
                Username = "parent4"
            },
            new SeedUserData
            {
                Email = "parent5@gmail.com",
                FirstName = "Hoàng",
                LastName = "Văn Em",
                Phone = "+84901234577",
                Role = UserRole.Parent,
                Username = "parent5"
            }
        };

        // Classroom Data
        public static readonly List<SeedClassroomData> Classrooms = new()
        {
            new SeedClassroomData
            {
                Name = "Lớp Mầm",
                Capacity = 25,
                Grade = "Mầm"
            },
            new SeedClassroomData
            {
                Name = "Lớp Chồi",
                Capacity = 30,
                Grade = "Chồi"
            },
            new SeedClassroomData
            {
                Name = "Lớp Lá",
                Capacity = 30,
                Grade = "Lá"
            }
        };

        // Children/Student Data (for Parents)
        public static readonly List<SeedChildData> Children = new()
        {
            // Parent 1's children
            new SeedChildData
            {
                ParentEmail = "parent1@gmail.com",
                FullName = "Nguyễn Minh Anh",
                Birthdate = new DateTime(2021, 3, 15),
                Gender = Gender.Female,
                Address = "123 Nguyễn Trãi, Q.1, TP.HCM",
                Grade = "Mầm"
            },
            new SeedChildData
            {
                ParentEmail = "parent1@gmail.com",
                FullName = "Nguyễn Hoàng Bảo",
                Birthdate = new DateTime(2020, 7, 22),
                Gender = Gender.Male,
                Address = "123 Nguyễn Trãi, Q.1, TP.HCM",
                Grade = "Chồi"
            },

            // Parent 2's child
            new SeedChildData
            {
                ParentEmail = "parent2@gmail.com",
                FullName = "Trần Thảo Chi",
                Birthdate = new DateTime(2021, 5, 10),
                Gender = Gender.Female,
                Address = "456 Lê Lợi, Q.3, TP.HCM",
                Grade = "Mầm"
            },

            // Parent 3's children
            new SeedChildData
            {
                ParentEmail = "parent3@gmail.com",
                FullName = "Lê Minh Đức",
                Birthdate = new DateTime(2020, 9, 8),
                Gender = Gender.Male,
                Address = "789 Trần Hưng Đạo, Q.5, TP.HCM",
                Grade = "Chồi"
            },
            new SeedChildData
            {
                ParentEmail = "parent3@gmail.com",
                FullName = "Lê Thu Hà",
                Birthdate = new DateTime(2019, 11, 25),
                Gender = Gender.Female,
                Address = "789 Trần Hưng Đạo, Q.5, TP.HCM",
                Grade = "Lá"
            },

            // Parent 4's child
            new SeedChildData
            {
                ParentEmail = "parent4@gmail.com",
                FullName = "Phạm Gia Huy",
                Birthdate = new DateTime(2021, 1, 30),
                Gender = Gender.Male,
                Address = "321 Võ Thị Sáu, Q.3, TP.HCM",
                Grade = "Mầm"
            },

            // Parent 5's children
            new SeedChildData
            {
                ParentEmail = "parent5@gmail.com",
                FullName = "Hoàng Khánh Linh",
                Birthdate = new DateTime(2020, 6, 18),
                Gender = Gender.Female,
                Address = "654 Pasteur, Q.1, TP.HCM",
                Grade = "Chồi"
            },
            new SeedChildData
            {
                ParentEmail = "parent5@gmail.com",
                FullName = "Hoàng Minh Khôi",
                Birthdate = new DateTime(2019, 12, 5),
                Gender = Gender.Male,
                Address = "654 Pasteur, Q.1, TP.HCM",
                Grade = "Lá"
            }
        };

        // Application Status scenarios
        public static readonly List<SeedApplicationData> Applications = new()
        {
            // Payment completed, pending approval
            new SeedApplicationData
            {
                ChildFullName = "Nguyễn Minh Anh",
                Status = ApplicationStatus.PaymentCompleted
            },
            new SeedApplicationData
            {
                ChildFullName = "Trần Thảo Chi",
                Status = ApplicationStatus.PaymentCompleted
            },

            // Approved applications
            new SeedApplicationData
            {
                ChildFullName = "Nguyễn Hoàng Bảo",
                Status = ApplicationStatus.Approved
            },
            new SeedApplicationData
            {
                ChildFullName = "Lê Minh Đức",
                Status = ApplicationStatus.Approved
            },
            new SeedApplicationData
            {
                ChildFullName = "Lê Thu Hà",
                Status = ApplicationStatus.Approved
            },
            new SeedApplicationData
            {
                ChildFullName = "Hoàng Khánh Linh",
                Status = ApplicationStatus.Approved
            },

            // Payment pending
            new SeedApplicationData
            {
                ChildFullName = "Phạm Gia Huy",
                Status = ApplicationStatus.PaymentPending
            },

            // Rejected (with reason)
            new SeedApplicationData
            {
                ChildFullName = "Hoàng Minh Khôi",
                Status = ApplicationStatus.Rejected,
                RejectionReason = "Không đủ độ tuổi theo quy định nhà trường"
            }
        };
    }

    public class SeedUserData
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class SeedClassroomData
    {
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Grade { get; set; } = string.Empty;
    }

    public class SeedChildData
    {
        public string ParentEmail { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime Birthdate { get; set; }
        public Gender Gender { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
    }

    public class SeedApplicationData
    {
        public string ChildFullName { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public string? RejectionReason { get; set; }
    }
}
