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
      public DbSet<VoteToComment> CommentsVotes { get; set; } = null!;

      public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
      {
            Database.EnsureDeleted();
            Database.EnsureCreated();
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

            modelBuilder.Entity<StoreAdministrator>()
                  .HasKey(a => new { a.UserId, a.StoreId });

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

            //Главная сущность User связана с зависимой сущностю StoreAdmin в отношении один User ко многим StoreAdministrator.
            modelBuilder.Entity<User>()
                  .HasMany<StoreAdministrator>()
                  .WithOne()
                  .HasForeignKey(a => a.UserId);

            //Главная сущность Store связана с зависимой сущностю StoreAdmin в отношении один Store ко многим StoreAdmin.
            modelBuilder.Entity<Store>()
                  .HasMany<StoreAdministrator>(e => e.Administrators)
                  .WithOne()
                  .HasForeignKey(s => s.StoreId);

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
                  new Role() {Id=1, Name = "User"},
                  new Role() {Id=1 << 1, Name = "StoreAdmin"},
                  new Role() {Id=1 << 2, Name = "Admin"}
            );


            //TODO Password Hash для админа
            modelBuilder.Entity<User>().HasData(
                  new User() {Id=1, Login = "Admin", RoleId = 0b100, Email = "Hamsterdreams@inbox.ru", PasswordHash="", Money=99999 }
            );

            modelBuilder.Entity<Country>().HasData(
                  new Country() {Name="RU"},
                  new Country() {Name="JP"},
                  new Country() {Name="CN"}
            );
      }
   }
}
