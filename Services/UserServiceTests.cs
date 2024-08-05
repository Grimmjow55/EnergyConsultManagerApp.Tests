using EnergyConsultManagerApp.Data;
using EnergyConsultManagerApp.DTO.Users;
using EnergyConsultManagerApp.Models;
using EnergyConsultManagerApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EnergyConsultManagerApp.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private ApplicationDbContext _contextMock;
        private UserService _userService;

        public UserServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            var roleStore = new Mock<IRoleStore<ApplicationRole>>();
            _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(roleStore.Object, null, null, null, null);

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;


        }
        private void SeedRoles(IEnumerable<ApplicationRole> roles)
        {
            _contextMock.Roles.AddRange(roles);
            _contextMock.SaveChanges();
        }

        private void InitializeDatabase()
        {
            _contextMock = new ApplicationDbContext(_options);
            _contextMock.Database.EnsureDeleted();
            _contextMock.Database.EnsureCreated();
            _userService = new UserService(_userManagerMock.Object, _roleManagerMock.Object, _contextMock);
        }

        [Fact]
        public void GetAllUsers_ReturnsAllUsers()
        {
            // Arrange
            InitializeDatabase();
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", UserName = "user1" },
                new ApplicationUser { Id = "2", UserName = "user2" }
            };

            _contextMock.Users.AddRange(users);
            _contextMock.SaveChanges();

            // Act
            var result = _userService.GetAllUsers();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, u => u.UserName == "user1");
            Assert.Contains(result, u => u.UserName == "user2");
        }

        [Fact]
        public void GetAllUsersWithTheirCompany_ReturnsAllUsersWithCompany()
        {
            // Arrange
            InitializeDatabase();
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", UserName = "user1", Company = new Company { Name = "Company1" }},
                new ApplicationUser { Id = "2", UserName = "user2", Company = new Company { Name = "Company2" }}
            };

            _contextMock.Users.AddRange(users);
            _contextMock.SaveChanges();

            // Act
            var result = _userService.GetAllUsersWithTheirCompany();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, u => u.Company.Name == "Company1");
            Assert.Contains(result, u => u.Company.Name == "Company2");
        }

        [Fact]
        public async Task GetUserByIdAsync_UserExists_ReturnsUser()
        {
            // Arrange
            InitializeDatabase();
            var userId = "test-user-id";
            var expectedUser = new ApplicationUser { Id = userId, UserName = "testuser" };
            _userManagerMock.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(expectedUser);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.Id, result.Id);
            Assert.Equal(expectedUser.UserName, result.UserName);
        }

        [Fact]
        public async Task CreateUserAsync_ValidUser_ReturnsSuccess()
        {
            // Arrange
            InitializeDatabase();
            var createUserDto = new CreateUserDTO
            {
                Email = "newuser@example.com",
                Password = "Password123!",
                Firstname = "New",
                Lastname = "User"
            };

            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), createUserDto.Password))
                            .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.CreateUserAsync(createUserDto);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task DeleteUserAsync_UserExists_ReturnsSuccess()
        {
            // Arrange
            InitializeDatabase();
            var userId = "test-user-id";
            var user = new ApplicationUser { Id = userId, UserName = "testuser" };
            _userManagerMock.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string>());
            _userManagerMock.Setup(um => um.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UpdateUserAsync_ValidUser_ReturnsSuccess()
        {
            // Arrange
            InitializeDatabase();
            var updateUserDto = new UpdateUserDTO
            {
                Id = "test-user-id",
                Firstname = "UpdatedFirstName",
                Lastname = "UpdatedLastName",
            };

            var user = new ApplicationUser { Id = updateUserDto.Id, UserName = "testuser" };

            _contextMock.Users.Add(user);
            _contextMock.SaveChanges();

            _userManagerMock.Setup(um => um.FindByIdAsync(updateUserDto.Id)).ReturnsAsync(user);
            _userManagerMock.Setup(um => um.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.UpdateUserAsync(updateUserDto);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task GetAllRolesAsync_ReturnsAllRoles()
        {
            // Arrange
            InitializeDatabase();
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole { Id = "1", Name = "Admin" },
                new ApplicationRole { Id = "2", Name = "User" }
            };

            SeedRoles(roles);

            _roleManagerMock.Setup(r => r.Roles).Returns(_contextMock.Roles);

            // Act
            var result = await _userService.GetAllRolesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Name == "Admin");
            Assert.Contains(result, r => r.Name == "User");
        }

        [Fact]
        public async Task GetAllActivitiesAsync_ReturnsAllActivities()
        {
            // Arrange
            InitializeDatabase();
            var activities = new List<CompanyActivity>
            {
                new CompanyActivity { Id = 1, Description = "Activity1" },
                new CompanyActivity { Id = 2, Description = "Activity2" }
            };

            _contextMock.CompaniesActivities.AddRange(activities);
            await _contextMock.SaveChangesAsync();

            // Act
            var result = await _userService.GetAllActivitiesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, a => a.Description == "Activity1");
            Assert.Contains(result, a => a.Description == "Activity2");
        }

        [Fact]
        public async Task GetCompanyByUserAsync_UserHasCompany_ReturnsCompany()
        {
            // Arrange
            InitializeDatabase();
            var company = new Company { Id = 1, Name = "Test Company" };
            var user = new ApplicationUser { Id = "test-user-id", CompanyId = company.Id, Company = company };

            _contextMock.Companies.Add(company);
            _contextMock.Users.Add(user);
            await _contextMock.SaveChangesAsync();

            // Act
            var result = await _userService.GetCompanyByUserAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(company.Id, result.Id);
            Assert.Equal(company.Name, result.Name);
        }

        [Fact]
        public void GetAllUsersWithTheirCompany_WithUsersList_ReturnsUsersWithCompany()
        {
            // Arrange
            InitializeDatabase();
            var company1 = new Company { Id = 1, Name = "Company1" };
            var company2 = new Company { Id = 2, Name = "Company2" };
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", UserName = "user1", Company = company1 },
                new ApplicationUser { Id = "2", UserName = "user2", Company = company2 }
            };

            _contextMock.Companies.AddRange(company1, company2);
            _contextMock.Users.AddRange(users);
            _contextMock.SaveChanges();

            // Act
            var result = _userService.GetAllUsersWithTheirCompany(users).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, u => u.Company.Name == "Company1");
            Assert.Contains(result, u => u.Company.Name == "Company2");
        }

        [Fact]
        public async Task GetUsersByRole_ReturnsUsersInRole()
        {
            // Arrange
            InitializeDatabase();
            var role = "Admin";
            var company1 = new Company { Id = 1, Name = "Company1" };
            var company2 = new Company { Id = 2, Name = "Company2" };
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", UserName = "user1", Company = company1 },
                new ApplicationUser { Id = "2", UserName = "user2", Company = company2 }
            };

            _contextMock.Companies.AddRange(company1, company2);
            _contextMock.Users.AddRange(users);
            _contextMock.SaveChanges();

            _userManagerMock.Setup(um => um.GetUsersInRoleAsync(role)).ReturnsAsync(users);

            // Act
            var result = _userService.GetUsersByRole(role).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, u => u.UserName == "user1");
            Assert.Contains(result, u => u.UserName == "user2");
            Assert.Contains(result, u => u.Company.Name == "Company1");
            Assert.Contains(result, u => u.Company.Name == "Company2");
        }

        [Fact]
        public async Task GetUsersByRoles_ReturnsUsersInRoles()
        {
            // Arrange
            InitializeDatabase();
            var roles = new[] { "Admin", "Manager" };
            var company1 = new Company { Id = 1, Name = "Company1" };
            var company2 = new Company { Id = 2, Name = "Company2" };
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", UserName = "user1", Company = company1 },
                new ApplicationUser { Id = "2", UserName = "user2", Company = company2 }
            };

            _contextMock.Companies.AddRange(company1, company2);
            _contextMock.Users.AddRange(users);
            _contextMock.SaveChanges();

            _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Admin")).ReturnsAsync(users.Where(u => u.UserName == "user1").ToList());
            _userManagerMock.Setup(um => um.GetUsersInRoleAsync("Manager")).ReturnsAsync(users.Where(u => u.UserName == "user2").ToList());

            // Act
            var result = _userService.GetUsersByRoles(roles).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, u => u.UserName == "user1");
            Assert.Contains(result, u => u.UserName == "user2");
            Assert.Contains(result, u => u.Company.Name == "Company1");
            Assert.Contains(result, u => u.Company.Name == "Company2");
        }

        [Fact]
        public void GetRolesByUser_ReturnsRoles()
        {
            // Arrange
            InitializeDatabase();
            var user = new ApplicationUser { Id = "1", UserName = "user1" };
            var roles = new List<string> { "Admin", "Manager" };

            _contextMock.Users.Add(user);
            _contextMock.SaveChanges();

            _userManagerMock.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(roles);

            // Act
            var result = _userService.GetRolesByUser(user).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("Admin", result);
            Assert.Contains("Manager", result);
        }

        [Fact]
        public void SearchByActivity_ReturnsUsersInActivity()
        {
            // Arrange
            InitializeDatabase();

            var activity = new CompanyActivity { Id = 1, Description = "Consulting" };
            var company1 = new Company { Id = 1, Name = "Company1" };
            var company2 = new Company { Id = 2, Name = "Company2" };

            var user1 = new ApplicationUser { Id = "1", UserName = "user1", CompanyId = company1.Id, Company = company1 };
            var user2 = new ApplicationUser { Id = "2", UserName = "user2", CompanyId = company2.Id, Company = company2 };

            var companyActivityLink1 = new CompanyActivityLink { CompanyId = company1.Id, ActivityId = activity.Id, Company = company1, Activity = activity };
            var companyActivityLink2 = new CompanyActivityLink { CompanyId = company2.Id, ActivityId = activity.Id, Company = company2, Activity = activity };

            _contextMock.CompaniesActivities.Add(activity);
            _contextMock.Companies.AddRange(company1, company2);
            _contextMock.Users.AddRange(user1, user2);
            _contextMock.CompaniesActivitiesLinks.AddRange(companyActivityLink1, companyActivityLink2);
            _contextMock.SaveChanges();

            // Act
            var result = _userService.SearchByActivity("Consulting").ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, u => u.UserName == "user1");
            Assert.Contains(result, u => u.UserName == "user2");
        }

        [Fact]
        public void SearchByLastName_ReturnsMatchingUsers()
        {
            // Arrange
            InitializeDatabase();
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", UserName = "user1", LastName = "Smith" },
                new ApplicationUser { Id = "2", UserName = "user2", LastName = "Doe" },
                new ApplicationUser { Id = "3", UserName = "user3", LastName = "Smith" }
            };

            _contextMock.Users.AddRange(users);
            _contextMock.SaveChanges();

            // Act
            var result = _userService.SearchByLastName("Smith").ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, u => u.LastName == "Smith" && u.UserName == "user1");
            Assert.Contains(result, u => u.LastName == "Smith" && u.UserName == "user3");
        }

        [Fact]
        public void SearchByCompanyName_ReturnsMatchingUsers()
        {
            // Arrange
            InitializeDatabase();
            var company1 = new Company { Id = 1, Name = "Company1", ApplicationUserId = "1" };
            var company2 = new Company { Id = 2, Name = "Company2", ApplicationUserId = "2" };

            var users = new List<ApplicationUser>
    {
        new ApplicationUser { Id = "1", UserName = "user1", Company = company1, CompanyId = company1.Id },
        new ApplicationUser { Id = "2", UserName = "user2", Company = company2, CompanyId = company2.Id },
        new ApplicationUser { Id = "3", UserName = "user3", Company = null, CompanyId = null }
    };

            _contextMock.Companies.AddRange(company1, company2);
            _contextMock.Users.AddRange(users);
            _contextMock.SaveChanges();

            // Act
            var result = _userService.SearchByCompanyName("Company1").ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Contains(result, u => u.UserName == "user1");
            Assert.DoesNotContain(result, u => u.UserName == "user2");
            Assert.DoesNotContain(result, u => u.UserName == "user3");
        }

        [Fact]
        public async Task GetRoleByIdAsync_RetourneLeRole()
        {
            // Arrange
            InitializeDatabase();
            var roleId = "test-role-id";
            var roleAttendu = new ApplicationRole { Id = roleId, Name = "Admin" };
            _roleManagerMock.Setup(rm => rm.FindByIdAsync(roleId)).ReturnsAsync(roleAttendu);

            // Act
            var resultat = await _userService.GetRoleByIdAsync(roleId);

            // Assert
            Assert.NotNull(resultat);
            Assert.Equal(roleAttendu.Id, resultat.Id);
            Assert.Equal(roleAttendu.Name, resultat.Name);
        }

    }
}
