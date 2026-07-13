using System.Security.Cryptography;
using System.Text;
using WorkQueue.Domain.Entities;
using WorkQueue.Domain.Enums;

namespace WorkQueue.Infrastructure
{
    public static class DataSeeder
    {
        public static void Initialize(WorkQueueDbContext context)
        {
            // Если данные уже есть, выходим
            if (context.Organizations.Any()) return;

            var orgA = new Organization { Id = Guid.NewGuid(), Name = "Organization A" };
            var orgB = new Organization { Id = Guid.NewGuid(), Name = "Organization B" };

            context.Organizations.AddRange(orgA, orgB);

            // ТЗ: Passwords may be seeded with a simple development-only approach.
            string Hash(string password)
            {
                using var sha256 = SHA256.Create();
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }

            var devPassword = Hash("dev_password");

            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Name = "Manager A", Email = "manager.a@test.com", PasswordHash = devPassword, Role = UserRole.Manager, OrganizationId = orgA.Id },
                new User { Id = Guid.NewGuid(), Name = "Member A", Email = "member.a@test.com", PasswordHash = devPassword, Role = UserRole.Member, OrganizationId = orgA.Id },
                new User { Id = Guid.NewGuid(), Name = "Manager B", Email = "manager.b@test.com", PasswordHash = devPassword, Role = UserRole.Manager, OrganizationId = orgB.Id },
                new User { Id = Guid.NewGuid(), Name = "Member B", Email = "member.b@test.com", PasswordHash = devPassword, Role = UserRole.Member, OrganizationId = orgB.Id }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
        }
    }
}