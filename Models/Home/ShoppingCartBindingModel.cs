namespace HamsterWorld.Models
{
   public class ShoppingCartBindingModel
   {
      public List<ShoppingItem> ShoppingItems { get; set; } = new List<ShoppingItem>();
      public decimal TotalPrice { get; set; }
   }
   public class ShoppingItem
   {
      public int ProductId { get; set; }
      public string ProductName { get; set; } = "";
      public string? ProductPictureSrc { get; set; }
      public int Amount { get; set; }
      public decimal Price { get; set; }
   }
}