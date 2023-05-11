namespace HamsterWorld.Models
{
	public class ProductBindingModel
	{
		public int Id { get; set; }
		public string? PictureSrc { get; set; } = "";
		public string Model { get; set; } = "";
		public decimal Price { get; set; } = 0;
		public int Amount { get; set; } = 0;
	}
	public class CPUBindingModel : ProductBindingModel
	{
		public ushort ClockRate { get; set; } = 0;
		public ushort NumberOfCores { get; set; } = 0; 
		public string Socket { get; set; } = "";

		public CPUBindingModel(){}
		public CPUBindingModel(CPU cpu)
		{
			Id= cpu.Id;
			Amount = cpu.Assortments!.Select(e => e.Amount).First();
			ClockRate = cpu.ClockRate;
			Model = cpu.Model;
			NumberOfCores = cpu.NumberOfCores;
			PictureSrc = cpu.Pictures!.First().Path;
			Price = cpu.Price;
			Socket = cpu.Socket;
		}
		public override string ToString()
		{
			return $"Процессор {Model} [Тактовая частота - {ClockRate}, Число ядер - {NumberOfCores}, Сокет - {Socket}]";
		}
	}
}