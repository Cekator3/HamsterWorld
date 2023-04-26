using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace HamsterWorld.Models
{
   public class User
   {
      // Первичный ключ
      public string Login { get; set; } = "";
      
      //Внешний ключ
      public string RoleName { get; set; } = "";
      //Навигационные свойства
      public Role Role { get; set; } = null!;
      public List<CommentToProduct>? Comments { get; set; }

      //Столбцы
      public string Email { get; set; } = "";
      public string PasswordHash { get; set; } = "";
      [Column(TypeName = "decimal(18,8)")]
      public decimal money { get; set; }
      public string? UserPicture { get; set; }
   }
   public class Role
   {
      //Первичный ключ
      public string Name { get; set; } = "";
      
      //Навигационное свойство
      public List<User>? Users { get; set; }
   }
   //Люди, попавшие сюда, должны заново пройти авторизацию.
   public class UserWithChangedRole
   {
      //Внешний ключ (и первичный)
      public string UserLogin { get; set; } = "";

      //Навигационное свойство
      public User User { get; set; } = null!;
   }

   public class Store
   {
      //Первичный ключ
      public int Id { get; set; }

      //Столбцы
      public string Name { get; set; } = "";
      public TimeOnly OpenningTime { get; set; }
      public TimeOnly ClosingTime { get; set; }
      public Point Coordinates { get; set; } = null!;

      //Навигационное свойство
      public List<StoreAdministrator>? Administrators;
   }
   public class StoreAdministrator
   {
      //Внешние ключи (и первичные)
      public string UserLogin { get; set; } = "";
      public int StoreId { get; set; }

      //Навигационные свойства
      public User User { get; set; } = null!;
      public Store Store { get; set; } = null!;
   }


   //Страна производитель
   public class Country
   {
      public string Name { get; set; } = "";

      public List<Product>? ProductsFromThisCountry { get; set; }
   }
   public class Product
   {
      //Первичный ключ
      public int Id { get; set; }

      //Внешний ключ
      public string? CountryName { get; set; }
      //Навигационные свойства
      public List<CommentToProduct>? Comments { get; set; }
      public List<ProductPicture>? Pictures { get; set; }

      //Столбцы
      public string Model { get; set; } = "";
      public string Description { get; set; } = "";
      [Column(TypeName = "decimal(18,8)")]
      public decimal Price { get; set; }
   }
   public class CPU : Product
   {
      public string Socket { get; set; } = "";
      public ushort NumberOfCores { get; set; }
      public ushort ClockRate { get; set; }
   }
   public class GPU : Product
   {
      public int VRAM { get; set; }
      public string MemoryType { get; set; } = "";
      public int AmountOfMemory { get; set; }
   }
   public class RAM : Product
   {
      public string MemoryType { get; set; } = "";
      public int AmountOfMemory { get; set; }
   }
   public class ProductPicture
   {
      //Первичный ключ
      public int Id { get; set; }

      //Внешний ключ
      public int ProductId { get; set; }

      //Столбцы
      public string Path { get; set; } = "";
      public int OrderNumber { get; set; }
   }

   public class ShoppingList
   {
      //Первичный ключ
      public int Id { get; set; }

      //Навигационное свойство
      public List<ItemOfShoppingList>? Buyings;

      //Столбцы
      public DateTime? TimeOfSale { get; set; }
      [Column(TypeName = "decimal(18,8)")]
      public decimal FinalPrice { get; set; }
   }
   public class ItemOfShoppingList
   {
      //Внешние ключи
      public int ShoppingListId { get; set; }
      public int ProductId { get; set; }
      //Навигационное свойство
      public Product Product { get; set; } = null!;
      public ShoppingList ShoppingList { get; set; } = null!;

      // Столбец
      public int Amount { get; set; }
   }
   
   public class CommentToProduct
   {
      //Первичный ключ
      public int Id { get; set; }

      //Внешние ключи
      public string? UserLogin { get; set; }
      public int ProductId { get; set; }
      public int? ParentCommentId { get; set; }

      //Навигационное свойство
      public CommentToProduct? ParentComment { get; set; }
      public List<CommentToProduct>? ChildrenComments { get; set; }

      //Столбцы
      public string Content { get; set; } = "";
      public DateTime WritingDate { get; set; } = DateTime.UtcNow;
   }
}
