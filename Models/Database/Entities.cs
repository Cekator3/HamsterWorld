using NetTopologySuite.Geometries;

namespace HamsterWorld.Models
{
   public class User
   {
      // Первичный ключ
      public int Id { get; set; }

      
      //Внешний ключ
      public ushort RoleId { get; set; }
      //Навигационные свойства
      public Role Role { get; set; } = null!;
      public List<Comment>? Comments { get; set; }
      public List<Store>? AdministratingStores { get; set; }

      //Столбцы
      public string Login { get; set; } = "";
      public string Email { get; set; } = "";
      public string PasswordHash { get; set; } = "";
      public decimal Money { get; set; }
      public string? UserPicture { get; set; }
   }
   public class Role
   {
      //Первичный ключ (в бинарном формате)
      public ushort Id { get; set; }
      
      //Навигационное свойство
      public List<User>? Users { get; set; }

      //Столбец
      public string Name { get; set; } = "";

      //Константы
      public const int BANNED = 1;
      public const int USER = 0b10;
      public const int STORE_ADMIN = 0b100;
      public const int ADMIN = 0b1000;
      public const string BannedUserRoleName = "Banned";
      public const string UserRoleName = "User";
      public const string StoreAdminRoleName = "StoreAdmin";
      public const string AdminRoleName = "Admin";
   }
   //Люди, попавшие сюда, должны заново пройти авторизацию.
   public class UserWithChangedRole
   {
      //Внешний ключ (и первичный)
      public int UserId { get; set; }
   }

   public class Store
   {
      //Первичный ключ
      public short Id { get; set; }

      //Столбцы
      public string Name { get; set; } = "";
      public TimeOnly OpeningTime { get; set; }
      public TimeOnly ClosingTime { get; set; }
      public Point Coordinates { get; set; } = null!;
      public string Address { get; set; } = "";

      //Навигационное свойство
      public List<User>? Administrators;
      public List<GPU>? GPUs;
      public List<CPU>? CPUs;
      public List<RAM>? RAMs;
   }

   public class Assortment
   {
      //Внешние (и первичные) ключи
      public short StoreId { get; set; }
      public int ProductId { get; set; }
      //Столбец
      public int Amount { get; set; }
   }

   //Страна производитель
   public class Country
   {
      //Первичный ключ
      public string Name { get; set; } = "";
      public const string Russia = "RU";
      public const string Japan = "JP";
      public const string China = "CN";
   }
   public class Product
   {
      //Первичный ключ
      public int Id { get; set; }

      //Внешний ключ
      public string Country { get; set; } = "";
      //Навигационные свойства
      public List<CommentToProduct>? Comments { get; set; }
      public List<ProductPicture>? Pictures { get; set; }

      //Столбцы
      public string Model { get; set; } = "";
      public string Description { get; set; } = "";
      public decimal Price { get; set; }

      public enum Categorys : byte
      {
         CPU,
         GPU,
         RAM
      }
   }
   public class CPU : Product
   {
      public string Socket { get; set; } = "";
      public ushort NumberOfCores { get; set; }
      public ushort ClockRate { get; set; }

      //Навигационные свойства
      public List<Assortment>? Assortments;
   }
   public class GPU : Product
   {
      public int VRAM { get; set; }
      public string MemoryType { get; set; } = "";

      //Навигационные свойства
      public List<Assortment>? Assortments;
   }
   public class RAM : Product
   {
      public string MemoryType { get; set; } = "";
      public int AmountOfMemory { get; set; }

      //Навигационные свойства
      public List<Assortment>? Assortments;
   }
   public class ProductPicture
   {
      //Первичный ключ
      public int Id { get; set; }

      //Внешний ключ
      public int ProductId { get; set; }

      //Столбцы
      public string FileName { get; set; } = "";
      public ushort OrderNumber { get; set; }

      //Константа
      public const string PATH = "/Images/Products/";
   }

   public class ShoppingList
   {
      //Первичный ключ
      public int Id { get; set; }

      //Навигационное свойство
      public List<ItemOfShoppingList>? Buyings;

      //Столбцы
      public DateTime? TimeOfSale { get; set; }
      public decimal FinalPrice { get; set; }
   }
   public class ItemOfShoppingList
   {
      //Внешние (и первичные) ключи
      public int ShoppingListId { get; set; }
      public int ProductId { get; set; }
      //Навигационное свойство
      public Product Product { get; set; } = null!;

      // Столбец
      public int Amount { get; set; }
   }

   public class Comment
   {
      //Первичный ключ
      public int Id { get; set; }


      //Внешний ключ (Если AuthorId == null, тогда коммент считается удалённым)
      public int? AuthorId { get; set; }
      //Навигационное свойство
      public User Author { get; set; } = null!;


      //Столбцы
      public string Content { get; set; } = "";
      public DateTimeOffset WritingDate { get; set; }
      public int Likes { get; set; }
      public int Dislikes { get; set; }
      //Навигационное свойство
      public List<AnswerToComment>? ChildrenComments { get; set; }
   }
   
   public class CommentToProduct : Comment 
   {
      //Внешний ключ
      public int ProductId { get; set; }

      //Столбец
      public ushort AmountOfStars { get; set; }
   }

   public class AnswerToComment : Comment
   {
      //Внешний ключ
      public int ParentCommentId { get; set; }
   }

   public class VoteToComment
   {
      //Внешние ключи (и первичные)
      public int CommentId { get; set; }
      public int AuthorId { get; set; }

      //Столбец
      public bool Type { get; set; }

      //Константы
      public const bool LIKE = true;
      public const bool DISLIKE = false;
   }
}
