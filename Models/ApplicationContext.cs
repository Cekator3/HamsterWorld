using Microsoft.EntityFrameworkCore;

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
      public DbSet<ShoppingList> ShoppingLists { get; set; } = null!;
      public DbSet<CommentToProduct> CommentsToProducts { get; set; } = null!;

      public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
      {
         Database.EnsureDeleted();
         Database.EnsureCreated();
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         //Использование fluent API
         base.OnModelCreating(modelBuilder);

         //При работе с цепочкой наследования будет использоваться подход TPH (Table per hierarchy)
         modelBuilder.Entity<Product>().UseTphMappingStrategy();  
         ConfigurePrimaryKeys(modelBuilder);
         ConfigureForeignKeys(modelBuilder);
      }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
         optionsBuilder.LogTo(Console.WriteLine);
      }



      void ConfigurePrimaryKeys(ModelBuilder modelBuilder)
      {
         modelBuilder.Entity<User>()
               .HasKey(u => u.Login);

         modelBuilder.Entity<Role>()
               .HasKey(r => r.Name);

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
               .HasForeignKey(u => u.RoleName)
               .OnDelete(DeleteBehavior.SetNull);

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
               .HasForeignKey(e => e.CountryName);

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

          //Главная сущность Product связана с зависимой сущностью CommentToProduct в отношении один Product ко многим Comment. Зависимая сущность имеет вторичный клю - e.ProductId
         modelBuilder.Entity<Product>()
               .HasMany<CommentToProduct>(e => e.Comments)
               .WithOne()
               .HasForeignKey(e => e.ProductId);

          //Главная сущность User связана с зависимой сущностью CommentToProduct в отношении один User ко многим Comment. Зависимая сущность имеет вторичный клю - e.UserLogin
         modelBuilder.Entity<User>()
               .HasMany<CommentToProduct>(e => e.Comments)
               .WithOne()
               .HasForeignKey(e => e.UserLogin);

          //Главная сущность CommentToProduct связана с зависимой сущностью CommentToProduct в отношении один комментарий может быть родителем многих комментариев. Зависимая сущность имеет вторичный ключ - e.ParentCommentId
         modelBuilder.Entity<CommentToProduct>()
               .HasMany<CommentToProduct>(e => e.ChildrenComments)
               .WithOne(e => e.ParentComment)
               .HasForeignKey(e => e.ParentCommentId);
      }
   }
}
