namespace FreelancePlatform.Domain.Enums;

[Flags]
public enum UserRole
{
    None = 0,
    Client = 1,
    Freelancer = 2,
    Admin = 4
}