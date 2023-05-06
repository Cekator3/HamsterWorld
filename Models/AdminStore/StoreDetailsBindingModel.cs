using System.ComponentModel.DataAnnotations;

namespace HamsterWorld.Models
{
	public class AddStoreBindingModel
	{
		[Required(ErrorMessage = "Введите название филиала")]
		public string Name { get; set; } = "";

		[Required(ErrorMessage = "Введите время открытия магазина")]
      public TimeOnly OpeningTime { get; set; }

		[Required(ErrorMessage = "Введите время закрытия магазина")]
      public TimeOnly ClosingTime { get; set; }

		[Required(ErrorMessage = "Введите адрес магазина")]
      public string Address { get; set; } = "";
	}	
	public class ChangeStoreInfoBindingModel : AddStoreBindingModel
	{
		public short Id { get; set; }
	}
}