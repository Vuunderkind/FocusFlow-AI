namespace NexesPass.Models;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AvatarInitials => $"{FirstName.FirstOrDefault()}{LastName.FirstOrDefault()}".ToUpper();
    public string FullName => $"{FirstName} {LastName}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsVerified { get; set; } = true;
    public string PinHash { get; set; } = string.Empty;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
