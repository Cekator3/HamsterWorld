namespace HamsterWorld.Models
{
   public class ViewProductBindingModel
   {
      public Product Product { get; set; }
      public double AverageMark { get; set; }
      public bool IsInUsersBuyingsList { get; set; }

      public ViewProductBindingModel(Product product)
      {
         this.Product = product;
      }

      public string GetProductTitle()
      {
         return Product.ToString()!;
      }
   }
}