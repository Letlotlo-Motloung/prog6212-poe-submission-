using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Enrolment>().HasIndex(e => new { e.ParticipantID, e.CategoryID }).IsUnique();
            modelBuilder.Entity<Result>().HasIndex(r => r.EnrolmentID).IsUnique();

            // All seeded accounts share the password: Password123!
            const string pwd = "$2b$11$KXRPjd0OA/AVOYkJBQI51.n2c4pf3/Y5Sv6k9SfAJzb9dQLWYEI8y";
            var seedDate = new DateTime(2026, 1, 1);

            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = 1, RoleName = "Organiser" },
                new Role { RoleID = 2, RoleName = "Participant" }
            );

            modelBuilder.Entity<User>().HasData(
                new User { UserID = 1, RoleID = 1, FullName = "Thandiwe Mokoena", Email = "thandiwe.mokoena@gmail.co.za", PasswordHash = pwd, PhoneNumber = "0821234567", CreatedAt = seedDate },
                new User { UserID = 2, RoleID = 2, FullName = "Lindiwe Dlamini", Email = "lindiwe.dlamini@gmail.com", PasswordHash = pwd, PhoneNumber = "0731112222", CreatedAt = seedDate },
                new User { UserID = 3, RoleID = 2, FullName = "Sipho Nkosi", Email = "sipho.nkosi@gmail.com", PasswordHash = pwd, PhoneNumber = "0845551111", CreatedAt = seedDate },
                new User { UserID = 4, RoleID = 2, FullName = "Ayesha Patel", Email = "ayesha.patel@gmail.com", PasswordHash = pwd, PhoneNumber = "0764442222", CreatedAt = seedDate },
                new User { UserID = 5, RoleID = 2, FullName = "Johan van Wyk", Email = "johan.vanwyk@gmail.com", PasswordHash = pwd, PhoneNumber = "0837773333", CreatedAt = seedDate }
            );

            modelBuilder.Entity<Event>().HasData(
                new Event { EventID = 1, OrganiserID = 1, EventName = "Joburg City Marathon", EventDate = new DateTime(2027, 3, 15), Location = "Johannesburg", DistanceKM = 42.2m, EventType = "Run", RouteInfo = "Start at Mary Fitzgerald Square, finish at FNB Stadium.", Description = "The flagship annual road marathon through the heart of Johannesburg. A fast, flat, spectator-friendly route with full road closures and water points every 3km.", CreatedAt = seedDate },
                new Event { EventID = 2, OrganiserID = 1, EventName = "Cape Town Cycle Tour", EventDate = new DateTime(2027, 4, 12), Location = "Cape Town", DistanceKM = 109m, EventType = "Cycle", RouteInfo = "Cape Town CBD to Simon's Town and back along the coast.", Description = "The world's largest timed cycling event. A breathtaking 109km coastal route with the legendary Chapman's Peak climb.", CreatedAt = seedDate },
                new Event { EventID = 3, OrganiserID = 1, EventName = "Durban Beachfront 10K", EventDate = new DateTime(2027, 2, 20), Location = "Durban", DistanceKM = 10m, EventType = "Run", RouteInfo = "Golden Mile promenade, out-and-back.", Description = "A sunrise 10K along the Golden Mile. Flat, fast and perfect for a personal best, with ocean views the whole way.", CreatedAt = seedDate },
                new Event { EventID = 4, OrganiserID = 1, EventName = "Pretoria Trail Challenge", EventDate = new DateTime(2027, 5, 5), Location = "Pretoria", DistanceKM = 25m, EventType = "Trail", RouteInfo = "Groenkloof Nature Reserve loop.", Description = "A technical 25km trail run through the Groenkloof Nature Reserve with 800m of elevation gain and abundant wildlife.", CreatedAt = seedDate },
                new Event { EventID = 5, OrganiserID = 1, EventName = "Soweto Night Run", EventDate = new DateTime(2026, 10, 10), Location = "Soweto", DistanceKM = 15m, EventType = "Run", RouteInfo = "Vilakazi Street circuit.", Description = "A vibrant 15km night run through Soweto with live music at every water point.", CreatedAt = seedDate },
                new Event { EventID = 6, OrganiserID = 1, EventName = "Garden Route Walk", EventDate = new DateTime(2026, 9, 5), Location = "Knysna", DistanceKM = 8m, EventType = "Walk", RouteInfo = "Knysna Heads coastal path.", Description = "A relaxed 8km family walk along the Garden Route coastline.", CreatedAt = seedDate }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryID = 1, EventID = 1, CategoryName = "42km Marathon", CategoryType = "Distance", DistanceKM = 42.20m, MaxParticipants = 2000, EntryFee = 350.00m },
                new Category { CategoryID = 2, EventID = 1, CategoryName = "21km Half Marathon", CategoryType = "Distance", DistanceKM = 21.10m, MaxParticipants = 3000, EntryFee = 250.00m },
                new Category { CategoryID = 3, EventID = 1, CategoryName = "Junior 5km", CategoryType = "Age", DistanceKM = 5.00m, MaxParticipants = 500, EntryFee = 80.00m },
                new Category { CategoryID = 4, EventID = 2, CategoryName = "109km Open", CategoryType = "Distance", DistanceKM = 109.00m, MaxParticipants = 5000, EntryFee = 550.00m },
                new Category { CategoryID = 5, EventID = 2, CategoryName = "42km Short Route", CategoryType = "Distance", DistanceKM = 42.00m, MaxParticipants = 1500, EntryFee = 300.00m },
                new Category { CategoryID = 6, EventID = 3, CategoryName = "10km Open", CategoryType = "Distance", DistanceKM = 10.00m, MaxParticipants = 1200, EntryFee = 120.00m },
                new Category { CategoryID = 7, EventID = 3, CategoryName = "10km Junior (U18)", CategoryType = "Age", DistanceKM = 10.00m, MaxParticipants = 300, EntryFee = 60.00m },
                new Category { CategoryID = 8, EventID = 4, CategoryName = "25km Trail", CategoryType = "Distance", DistanceKM = 25.00m, MaxParticipants = 600, EntryFee = 280.00m },
                new Category { CategoryID = 9, EventID = 4, CategoryName = "12km Trail", CategoryType = "Distance", DistanceKM = 12.00m, MaxParticipants = 400, EntryFee = 180.00m },
                new Category { CategoryID = 10, EventID = 5, CategoryName = "15km Open", CategoryType = "Distance", DistanceKM = 15.00m, MaxParticipants = 1000, EntryFee = 150.00m },
                new Category { CategoryID = 11, EventID = 6, CategoryName = "8km Family Walk", CategoryType = "Distance", DistanceKM = 8.00m, MaxParticipants = 800, EntryFee = 50.00m }
            );

            modelBuilder.Entity<Enrolment>().HasData(
                new Enrolment { EnrolmentID = 1, ParticipantID = 2, CategoryID = 1, EnrolmentDate = new DateTime(2026, 11, 1), Status = "Confirmed" },
                new Enrolment { EnrolmentID = 2, ParticipantID = 3, CategoryID = 1, EnrolmentDate = new DateTime(2026, 11, 2), Status = "Confirmed" },
                new Enrolment { EnrolmentID = 3, ParticipantID = 4, CategoryID = 2, EnrolmentDate = new DateTime(2026, 11, 3), Status = "Confirmed" },
                new Enrolment { EnrolmentID = 4, ParticipantID = 5, CategoryID = 2, EnrolmentDate = new DateTime(2026, 11, 4), Status = "Pending" },
                new Enrolment { EnrolmentID = 5, ParticipantID = 2, CategoryID = 6, EnrolmentDate = new DateTime(2026, 11, 5), Status = "Confirmed" },
                new Enrolment { EnrolmentID = 6, ParticipantID = 2, CategoryID = 10, EnrolmentDate = new DateTime(2026, 9, 1), Status = "Confirmed" },
                new Enrolment { EnrolmentID = 7, ParticipantID = 3, CategoryID = 10, EnrolmentDate = new DateTime(2026, 9, 2), Status = "Confirmed" },
                new Enrolment { EnrolmentID = 8, ParticipantID = 4, CategoryID = 10, EnrolmentDate = new DateTime(2026, 9, 3), Status = "Confirmed" },
                new Enrolment { EnrolmentID = 9, ParticipantID = 2, CategoryID = 11, EnrolmentDate = new DateTime(2026, 8, 20), Status = "Confirmed" },
                new Enrolment { EnrolmentID = 10, ParticipantID = 5, CategoryID = 11, EnrolmentDate = new DateTime(2026, 8, 21), Status = "Confirmed" }
            );

            modelBuilder.Entity<Result>().HasData(
                new Result { ResultID = 1, EnrolmentID = 6, FinishTime = new TimeSpan(1, 12, 45), Position = 47, Status = "Finished", IsPublished = true, PublishedAt = new DateTime(2026, 10, 11) },
                new Result { ResultID = 2, EnrolmentID = 7, FinishTime = new TimeSpan(1, 8, 30), Position = 12, Status = "Finished", IsPublished = true, PublishedAt = new DateTime(2026, 10, 11) },
                new Result { ResultID = 3, EnrolmentID = 8, FinishTime = new TimeSpan(1, 25, 10), Position = 156, Status = "Finished", IsPublished = true, PublishedAt = new DateTime(2026, 10, 11) },
                new Result { ResultID = 4, EnrolmentID = 9, FinishTime = new TimeSpan(0, 58, 20), Position = 23, Status = "Finished", IsPublished = true, PublishedAt = new DateTime(2026, 9, 6) },
                new Result { ResultID = 5, EnrolmentID = 10, FinishTime = new TimeSpan(1, 4, 55), Position = 41, Status = "Finished", IsPublished = true, PublishedAt = new DateTime(2026, 9, 6) }
            );
        }
    }
}
