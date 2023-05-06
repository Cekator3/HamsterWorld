using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HamsterWorld.Models
{
   public class ApplicationContext : DbContext
   {
      public DbSet<User> Users { get; set; } = null!;
      public DbSet<Role> Roles { get; set; } = null!;
      public DbSet<UserWithChangedRole> Blacklist { get; set; } = null!;
      public DbSet<Store> Stores { get; set; } = null!;
      public DbSet<Country> Countries { get; set; } = null!;
      public DbSet<CPU> CPUs { get; set; } = null!;
      public DbSet<GPU> GPUs { get; set; } = null!;
      public DbSet<RAM> RAMs { get; set; } = null!;
      public DbSet<ProductPicture> ProductsPictures { get; set; } = null!;
      public DbSet<ShoppingList> ShoppingLists { get; set; } = null!;
      public DbSet<CommentToProduct> CommentsToProducts { get; set; } = null!;
      public DbSet<VoteToComment> CommentsVotes { get; set; } = null!;

      public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
      {
            // Database.EnsureDeleted();
            // Database.EnsureCreated();
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
            //При работе с цепочкой наследования будет использоваться подход TPH (Table per hierarchy)
            modelBuilder.Entity<Product>().UseTphMappingStrategy();  
            modelBuilder.Entity<Comment>().UseTphMappingStrategy();  

            ConfigurePrimaryKeys(modelBuilder);
            ConfigureForeignKeys(modelBuilder);
            ConfigureIndexes(modelBuilder);
            ConfigureMoneyTypes(modelBuilder);
            ConfigureDefaultValues(modelBuilder);
            InitializeDatabaseWithValues(modelBuilder);
      }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
            optionsBuilder.LogTo(Console.WriteLine, new [] {RelationalEventId.CommandExecuted});
      }



      void ConfigurePrimaryKeys(ModelBuilder modelBuilder)
      {
            modelBuilder.Entity<User>()
                  .HasKey(u => u.Id);

            modelBuilder.Entity<Role>()
                  .HasKey(r => r.Id);

            modelBuilder.Entity<UserWithChangedRole>()
                  .HasKey(u => u.UserId);

            modelBuilder.Entity<Store>()
                  .HasKey(u => u.Id);

            modelBuilder.Entity<Country>()
                  .HasKey(c => c.Name);

            modelBuilder.Entity<Product>()
                  .HasKey(p => p.Id);

            modelBuilder.Entity<ProductPicture>()
                  .HasKey(p => p.Id);

            modelBuilder.Entity<ShoppingList>()
                  .HasKey(s => s.Id);

            modelBuilder.Entity<ItemOfShoppingList>()
                  .HasKey(i => new {i.ShoppingListId, i.ProductId});

            modelBuilder.Entity<Comment>()
                  .HasKey(c => c.Id);

            modelBuilder.Entity<VoteToComment>()
                  .HasKey(k => new {k.AuthorId, k.CommentId});
      }

      void ConfigureForeignKeys(ModelBuilder modelBuilder)
      {
            //Зависимая сущность User связана с главной сущностью Role в отношении одна Role ко многим User.
            modelBuilder.Entity<User>()
                  .HasOne<Role>(e => e.Role)
                  .WithMany(e => e.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            //Главная сущность User связана с зависимой сущностью UserWithChangedRole в отношении один к одному.
            modelBuilder.Entity<User>()
                  .HasOne<UserWithChangedRole>()
                  .WithOne()
                  .HasForeignKey<UserWithChangedRole>(u => u.UserId);

            //Сущности User и Store связаны в отношении многие ко многим. Промежуточная таблица называется storesAdministrators
            modelBuilder.Entity<User>()
                  .HasMany<Store>(e => e.AdministratingStores)
                  .WithMany(e => e.Administrators)
                  .UsingEntity(t => t.ToTable("storesAdministrators"));

            //Главная сущность Country связана с зависимой сущностью Product в отношении один Country ко многим Product. 
            modelBuilder.Entity<Country>()
                  .HasMany<Product>(e => e.ProductsFromThisCountry)
                  .WithOne()
                  .HasForeignKey(e => e.CountryName)
                  .OnDelete(DeleteBehavior.Restrict);

            //Главная сущность ShoppingList связана с зависимой сущностью ItemOfShoppingList в отношении один ShoppingList ко многим Item. 
            modelBuilder.Entity<ShoppingList>()
                  .HasMany<ItemOfShoppingList>(e => e.Buyings)
                  .WithOne()
                  .HasForeignKey(e => e.ShoppingListId);

            //Главная сущность Product связана с зависимой сущностью Item в отношении один Product ко многим Item.
            modelBuilder.Entity<Product>()
                  .HasMany<ItemOfShoppingList>()
                  .WithOne(e => e.Product)
                  .HasForeignKey(e => e.ProductId);

            //Главная сущность Product связана с зависимой сущностью Pictures в отношении один Product ко многим Picture.
            modelBuilder.Entity<Product>()
                  .HasMany<ProductPicture>(p => p.Pictures)
                  .WithOne()
                  .HasForeignKey(e => e.ProductId);

            //Главная сущность Product связана с зависимой сущностью CommentToProduct в отношении один Product ко многим Comment.
            modelBuilder.Entity<Product>()
                  .HasMany<CommentToProduct>(e => e.Comments)
                  .WithOne()
                  .HasForeignKey(e => e.ProductId);

            //Главная сущность User связана с зависимой сущностью Comment в отношении один User ко многим Comment. 
             modelBuilder.Entity<User>()
                   .HasMany<Comment>(e => e.Comments)
                   .WithOne(e => e.Author)
                   .HasForeignKey(e => e.AuthorId)
                   .OnDelete(DeleteBehavior.SetNull);

            //Главная сущность Comment связана с зависимой сущностью AnswerToComment в отношении многие AnswerToComment к одному Answer.
            modelBuilder.Entity<Comment>()
                  .HasMany<AnswerToComment>(e => e.ChildrenComments)
                  .WithOne()
                  .HasForeignKey(e => e.ParentCommentId);
            
            modelBuilder.Entity<Comment>()
                  .HasMany<VoteToComment>()
                  .WithOne()
                  .HasForeignKey(e => e.CommentId);

            modelBuilder.Entity<User>()
                  .HasMany<VoteToComment>()
                  .WithOne()
                  .HasForeignKey(e => e.AuthorId);
      }

      void ConfigureIndexes(ModelBuilder modelBuilder)
      {
            modelBuilder.Entity<User>()
                  .HasIndex(u => u.Login);

            modelBuilder.Entity<User>()
                  .HasIndex(u => u.Email);

            modelBuilder.Entity<Product>()
                  .HasIndex(u => u.Model);

            modelBuilder.Entity<Comment>()
                  .HasIndex(u => u.WritingDate);

            modelBuilder.Entity<Store>()
                  .HasIndex(u => u.Address)
                  .IsUnique();
      }

      void ConfigureMoneyTypes(ModelBuilder modelBuilder)
      {
            modelBuilder.Entity<User>()
                  .Property(u => u.Money)
                  .HasColumnType("money");

            modelBuilder.Entity<Product>()
                  .Property(p => p.Price)
                  .HasColumnType("money");

            modelBuilder.Entity<ShoppingList>()
                  .Property(l => l.FinalPrice)
                  .HasColumnType("money");
      }

      void ConfigureDefaultValues(ModelBuilder modelBuilder)
      {
            modelBuilder.Entity<Comment>()
                  .Property(c => c.WritingDate)
                  .HasDefaultValue(DateTimeOffset.UtcNow.ToUniversalTime());
      }

      void InitializeDatabaseWithValues(ModelBuilder modelBuilder)
      {
            modelBuilder.Entity<Role>().HasData(
                  new Role() { Id=Role.ADMIN, Name=Role.AdminRoleName },
                  new Role() { Id=Role.STORE_ADMIN, Name = Role.StoreAdminRoleName },
                  new Role() { Id=Role.USER, Name = Role.UserRoleName },
                  new Role() { Id=Role.BANNED, Name = Role.BannedUserRoleName }
            );

            modelBuilder.Entity<User>().HasData(
                  new User() {Id=1, Login = "admin", RoleId = Role.ADMIN, Email = "Hamsterdreams@inbox.ru", PasswordHash=BCrypt.Net.BCrypt.HashPassword("1"), Money=99999 }
            );
            modelBuilder.Entity<User>().HasData(
                  new User() {Id=2, Login = "test2", RoleId = Role.STORE_ADMIN, Email = "Hamsterdreams1@inbox.ru", PasswordHash=BCrypt.Net.BCrypt.HashPassword("1"), Money=99999 }
            );
            modelBuilder.Entity<User>().HasData(
                  new User() {Id=3, Login = "test3", RoleId = Role.STORE_ADMIN, Email = "Hamsterdreams2@inbox.ru", PasswordHash=BCrypt.Net.BCrypt.HashPassword("1"), Money=99999 }
            );
            modelBuilder.Entity<User>().HasData(
                  new User() {Id=4, Login = "test4", RoleId = Role.STORE_ADMIN, Email = "Hamsterdreams3@inbox.ru", PasswordHash=BCrypt.Net.BCrypt.HashPassword("1"), Money=99999 }
            );
            modelBuilder.Entity<User>().HasData(
                  new User() {Id=5, Login = "test5", RoleId = Role.STORE_ADMIN, Email = "Hamsterdreams4@inbox.ru", PasswordHash=BCrypt.Net.BCrypt.HashPassword("1"), Money=99999 }
            );

            modelBuilder.Entity<Country>().HasData(
                  new Country() {Name="RU"},
                  new Country() {Name="JP"},
                  new Country() {Name="CN"}
            );
      }
   }
}
