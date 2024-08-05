using EnergyConsultManagerApp.Data;
using EnergyConsultManagerApp.DTO.Roles;
using EnergyConsultManagerApp.Models;
using EnergyConsultManagerApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EnergyConsultManagerApp.Tests.Services
{
    public class RoleServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private ApplicationDbContext _contextMock;
        private RoleService _roleService;

        public RoleServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            var roleStore = new Mock<IRoleStore<ApplicationRole>>();
            _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(roleStore.Object, null, null, null, null);

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
        }

        private void InitializeDatabase()
        {
            _contextMock = new ApplicationDbContext(_options);
            _contextMock.Database.EnsureDeleted();
            _contextMock.Database.EnsureCreated();
            _roleService = new RoleService(_userManagerMock.Object, _roleManagerMock.Object, _contextMock);
        }

        [Fact]
        public async Task CreateRoleAsync_ValidRole_ReturnsSuccess()
        {
            // Arrange
            InitializeDatabase();
            var createRoleDto = new CreateRoleDTO { RoleName = "NewRole" };
            _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _roleService.CreateRoleAsync(createRoleDto);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UpdateRoleAsync_ValidRole_ReturnsSuccess()
        {
            // Arrange
            InitializeDatabase();
            var updateRoleDto = new UpdateRoleDTO { RoleId = "1", RoleName = "UpdatedRole" };
            var role = new ApplicationRole { Id = "1", Name = "OldRole" };
            _roleManagerMock.Setup(r => r.FindByIdAsync(updateRoleDto.RoleId)).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.UpdateAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _roleService.UpdateRoleAsync(updateRoleDto);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal("UpdatedRole", role.Name);
        }

        [Fact]
        public async Task UpdateRoleAsync_RoleNotFound_ReturnsFailure()
        {
            // Arrange
            InitializeDatabase();
            var updateRoleDto = new UpdateRoleDTO { RoleId = "1", RoleName = "UpdatedRole" };
            _roleManagerMock.Setup(r => r.FindByIdAsync(updateRoleDto.RoleId)).ReturnsAsync((ApplicationRole)null);

            // Act
            var result = await _roleService.UpdateRoleAsync(updateRoleDto);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description == $"Role with ID {updateRoleDto.RoleId} not found.");
        }

        [Fact]
        public async Task DeleteRoleAsync_ValidRole_ReturnsSuccess()
        {
            // Arrange
            InitializeDatabase();
            var role = new ApplicationRole { Id = "1", Name = "RoleToDelete" };
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { UserName = "user1" },
                new ApplicationUser { UserName = "user2" }
            };

            _roleManagerMock.Setup(r => r.FindByIdAsync(role.Id)).ReturnsAsync(role);
            _userManagerMock.Setup(u => u.GetUsersInRoleAsync(role.Name)).ReturnsAsync(users);
            _userManagerMock.Setup(u => u.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), role.Name)).ReturnsAsync(IdentityResult.Success);
            _roleManagerMock.Setup(r => r.DeleteAsync(It.IsAny<ApplicationRole>())).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _roleService.DeleteRoleAsync(role.Id);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task DeleteRoleAsync_RoleNotFound_ReturnsFailure()
        {
            // Arrange
            InitializeDatabase();
            var roleId = "1";
            _roleManagerMock.Setup(r => r.FindByIdAsync(roleId)).ReturnsAsync((ApplicationRole)null);

            // Act
            var result = await _roleService.DeleteRoleAsync(roleId);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description == $"Role with ID {roleId} not found.");
        }
    }
}
