namespace HamsterWorld.Models
{
   public abstract class ViewProductBindingModel
   {
      public Product Product { get; set; }
      public double AverageMark { get; set; }
      public bool IsInUsersBuyingsList { get; set; }

      public ViewProductBindingModel(Product product)
      {
         this.Product = product;
      }

      public abstract string GetTopName();
   }

   public class ViewCpuBindingModel : ViewProductBindingModel
   {
      public ViewCpuBindingModel(CPU cpu) : base(cpu)
      {
      }

      public override string GetTopName()
      {
         return $"Процессор {Product.Model}";
      }
   }

   public class ViewGpuBindingModel : ViewProductBindingModel
   {
      public ViewGpuBindingModel(GPU gpu) : base(gpu)
      {
      }

      public override string GetTopName()
      {
         return $"Видеокарта {Product.Model}";
      }
   }

   public class ViewRamBindingModel : ViewProductBindingModel
   {
      public ViewRamBindingModel(RAM ram) : base(ram)
      {
      }

      public override string GetTopName()
      {
         return $"Видеокарта {Product.Model}";
      }
   }
}