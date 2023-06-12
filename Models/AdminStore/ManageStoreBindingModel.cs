using System.ComponentModel.DataAnnotations;

namespace HamsterWorld.Models
{
	public class ManageStoreBindingModel
	{
		public short? StoreId { get; set; } = null;

		[Required(ErrorMessage = "Введите название филиала")]
		public string Name { get; set; } = "";

		[Required(ErrorMessage = "Введите время открытия магазина")]
      public TimeOnly OpeningTime { get; set; } = new TimeOnly(8, 0);

		[Required(ErrorMessage = "Введите время закрытия магазина")]
      public TimeOnly ClosingTime { get; set; } = new TimeOnly(20, 0);

		[Required(ErrorMessage = "Введите адрес магазина")]
      public string Address { get; set; } = "";

		public ManageStoreBindingModel() {}
		public ManageStoreBindingModel(Store store)
		{
			this.StoreId = store.Id;
			this.Name = store.Name;
			this.OpeningTime = store.OpeningTime;
			this.ClosingTime = store.ClosingTime;
			this.Address = store.Address;
		}
	}	
}