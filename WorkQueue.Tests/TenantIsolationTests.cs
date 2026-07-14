using Microsoft.EntityFrameworkCore;
using Moq;
using WorkQueue.Domain.Entities;
using WorkQueue.Infrastructure;
using Xunit;

namespace WorkQueue.Tests
{
    public class TenantIsolationTests
    {
        [Fact]
        public void DbContext_Should_Return_Only_WorkItems_For_Current_Organization()
        {
            // Arrange
            var orgIdA = Guid.NewGuid();
            var orgIdB = Guid.NewGuid();

            // Имитируем (Mock), что текущий пользователь из Организации А
            var currentUserMock = new Mock<ICurrentUserService>();
            currentUserMock.Setup(m => m.GetOrganizationId()).Returns(orgIdA);

            var options = new DbContextOptionsBuilder<WorkQueueDbContext>()
                .UseInMemoryDatabase(databaseName: "Test_Tenant_Db")
                .Options;

            using (var context = new WorkQueueDbContext(options, currentUserMock.Object))
            {
                // Заполняем базу задачами разных организаций
                context.WorkItems.Add(new WorkItem { Id = Guid.NewGuid(), Title = "Task A", OrganizationId = orgIdA, CreatedByUserId = Guid.NewGuid() });
                context.WorkItems.Add(new WorkItem { Id = Guid.NewGuid(), Title = "Task B", OrganizationId = orgIdB, CreatedByUserId = Guid.NewGuid() });
                context.SaveChanges();
            }

            // Act
            using (var context = new WorkQueueDbContext(options, currentUserMock.Object))
            {
                var tasks = context.WorkItems.ToList();

                // Assert: Должна вернуться только одна задача (Организации А)
                Assert.Single(tasks);
                Assert.Equal("Task A", tasks.First().Title);
            }
        }
    }
}