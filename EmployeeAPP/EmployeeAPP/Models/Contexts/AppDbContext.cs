using EmployeeAPP.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EmployeeAPP.Models.Contexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
    }
}
