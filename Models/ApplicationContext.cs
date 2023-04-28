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
      public DbSet<StoreAdministrator> StoresAdministrators { get; set; } = null!;
      public DbSet<Country> Countries { get; set; } = null!;
      public DbSet<CPU> CPUs { get; set; } = null!;
      public DbSet<GPU> GPUs { get; set; } = null!;
      public DbSet<RAM> RAMs { get; set; } = null!;
      public DbSet<ProductPicture> ProductsPictures { get; set; } = null!;
      public DbSet<ShoppingList> ShoppingLists { get; set; } = null!;
      public DbSet<CommentToProduct> CommentsToProducts { get; set; } = null!;

      public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
      {
         Database.EnsureDeleted();
         Database.EnsureCreated();
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         //При работе с цепочкой наследования будет использоваться подход TPH (Table per hierarchy)
         modelBuilder.Entity<Product>().UseTphMappingStrategy();  
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
               .HasKey(u => u.Login);

         modelBuilder.Entity<Role>()
               .HasKey(r => r.Id);

         modelBuilder.Entity<UserWithChangedRole>()
               .HasKey(u => u.UserLogin);

         modelBuilder.Entity<Store>()
               .HasKey(u => u.Id);

         modelBuilder.Entity<StoreAdministrator>()
               .HasKey(a => new { a.UserLogin, a.StoreId });

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

         modelBuilder.Entity<CommentToProduct>()
               .HasKey(c => c.Id);
      }

      void ConfigureForeignKeys(ModelBuilder modelBuilder)
      {
         //Зависимая сущность User связана с главной сущностью Role в отношении одна Role ко многим User. У зависимой сущности вторичный ключ - u.RoleName
         modelBuilder.Entity<User>()
               .HasOne(e => e.Role)
               .WithMany(e => e.Users)
               .HasForeignKey(u => u.RoleId)
               .OnDelete(DeleteBehavior.Restrict);

         //Главная сущность User связана с зависимой сущностью UserWithChangedRole в отношении один к одному. Зависимая сущность имеет вторичный ключ - u.UserLogin
         modelBuilder.Entity<User>()
               .HasOne<UserWithChangedRole>()
               .WithOne(u => u.User)
               .HasForeignKey<UserWithChangedRole>(u => u.UserLogin);

         //Главная сущность User связана с зависимой сущностю StoreAdmin в отношении один User ко многим StoreAdministrator. Зависимая сущность имеет вторичный ключ - a.UserLogin
         modelBuilder.Entity<User>()
               .HasMany<StoreAdministrator>()
               .WithOne(e => e.User)
               .HasForeignKey(a => a.UserLogin);

         //Главная сущность Store связана с зависимой сущностю StoreAdmin в отношении один Store ко многим StoreAdmin. Зависимая сущность имеет вторичный ключ - s.UserLogin
         modelBuilder.Entity<Store>()
               .HasMany<StoreAdministrator>(e => e.Administrators)
               .WithOne(e => e.Store)
               .HasForeignKey(s => s.StoreId);

         //Главная сущность Country связана с зависимой сущностью Product в отношении один Country ко многим Product. Зависимая сущность имеет вторичный ключ - e.CountryName
         modelBuilder.Entity<Country>()
               .HasMany<Product>(e => e.ProductsFromThisCountry)
               .WithOne()
               .HasForeignKey(e => e.CountryName)
               .OnDelete(DeleteBehavior.Restrict);

         //Главная сущность ShoppingList связана с зависимой сущностью ItemOfShoppingList в отношении один ShoppingList ко многим Item. Зависимая сущностью имеет вторичный ключ - e.ShoppingListId
         modelBuilder.Entity<ShoppingList>()
               .HasMany<ItemOfShoppingList>(e => e.Buyings)
               .WithOne(e => e.ShoppingList)
               .HasForeignKey(e => e.ShoppingListId);

         //Главная сущность Product связана с зависимой сущностью Item в отношении один Product ко многим Item. Зависимая сущность имеет вторичный ключ - e.ProductId
          modelBuilder.Entity<Product>()
               .HasMany<ItemOfShoppingList>()
               .WithOne(e => e.Product)
               .HasForeignKey(e => e.ProductId);

          //Главная сущность Product связана с зависимой сущностью Pictures в отношении один Product ко многим Picture. Зависимая сущность имеет вторичный ключ - e.ProductId
          modelBuilder.Entity<Product>()
               .HasMany(p => p.Pictures)
               .WithOne()
               .HasForeignKey(e => e.ProductId);

          //Главная сущность Product связана с зависимой сущностью CommentToProduct в отношении один Product ко многим Comment. Зависимая сущность имеет вторичный клю - e.ProductId
         modelBuilder.Entity<Product>()
               .HasMany<CommentToProduct>(e => e.Comments)
               .WithOne()
               .HasForeignKey(e => e.ProductId);

          //Главная сущность User связана с зависимой сущностью CommentToProduct в отношении один User ко многим Comment. Зависимая сущность имеет вторичный клю - e.UserLogin
         modelBuilder.Entity<User>()
               .HasMany<CommentToProduct>(e => e.Comments)
               .WithOne()
               .HasForeignKey(e => e.UserLogin)
               .OnDelete(DeleteBehavior.SetNull);

          //Главная сущность CommentToProduct связана с зависимой сущностью CommentToProduct в отношении один комментарий может быть родителем многих комментариев. Зависимая сущность имеет вторичный ключ - e.ParentCommentId
         modelBuilder.Entity<CommentToProduct>()
               .HasMany<CommentToProduct>(e => e.ChildrenComments)
               .WithOne(e => e.ParentComment)
               .HasForeignKey(e => e.ParentCommentId);
      }

      void ConfigureIndexes(ModelBuilder modelBuilder)
      {
         modelBuilder.Entity<User>()
               .HasIndex(u => u.Email);

         modelBuilder.Entity<Product>()
               .HasIndex(u => u.Model);
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
         modelBuilder.Entity<UserWithChangedRole>()
               .Property(u => u.RoleChangingTime)
               .HasDefaultValue(DateTime.UtcNow.ToUniversalTime());

         modelBuilder.Entity<CommentToProduct>()
               .Property(c => c.WritingDate)
               .HasDefaultValue(DateTimeOffset.UtcNow.ToUniversalTime());
      }

      void InitializeDatabaseWithValues(ModelBuilder modelBuilder)
      {
            
            Role adminRole = new Role() {Id=1, Name="Admin"};
            modelBuilder.Entity<Role>().HasData(
                  adminRole,
                  new Role() {Id=2, Name = "StoreAdmin"},
                  new Role() {Id=3, Name = "User"}
         );

         //TODO Password Hash для админа
         modelBuilder.Entity<User>().HasData(
               new User() {Login = "Admin", RoleId = adminRole.Id, Email = "Hamsterdreams@inbox.ru", PasswordHash="", Money=99999, }
         );

         modelBuilder.Entity<Country>().HasData(
               new Country() {Name="Россия"},
               new Country() {Name="Япония"},
               new Country() {Name="Китай"}
         );
      }
   }
}
