namespace HamsterWorld.Models
{
	public class ProductAmountBindingModel
	{
		public int Id { get; set; } = 0;
		public string? PictureSrc { get; set; } = "";
		public string ProductName { get; set; } = "";
		public decimal Price { get; set; } = 0;
		public int Amount { get; set; } = 0;

		protected ProductAmountBindingModel(){}
		protected ProductAmountBindingModel(Product product)
		{
			Id = product.Id;
			PictureSrc = $"{ProductPicture.PATH}{product.Pictures!.FirstOrDefault()?.FileName}";
			ProductName = product.ToString()!;
			Price = product.Price;
		}
	}
	public class CPUAmountBindingModel : ProductAmountBindingModel
	{
		public ushort ClockRate { get; set; } = 0;
		public ushort NumberOfCores { get; set; } = 0; 
		public string Socket { get; set; } = "";

		public CPUAmountBindingModel(){}
		public CPUAmountBindingModel(CPU cpu) : base(cpu)
		{
			ClockRate = cpu.ClockRate;
			NumberOfCores = cpu.NumberOfCores;
			Socket = cpu.Socket;
			Amount = cpu.Assortments!.Select(e => e.Amount).First();
		}
		public override string ToString()
		{
			return $"{ProductName} [Тактовая частота - {ClockRate}, Число ядер - {NumberOfCores}, Сокет - {Socket}]";
		}
	}
	public class GPUAmountBindingModel : ProductAmountBindingModel
	{
		public int VRAM { get; set; } = 0;
		public string MemoryType { get; set; } = ""; 

		public GPUAmountBindingModel(){}
		public GPUAmountBindingModel(GPU gpu) : base(gpu)
		{
			VRAM = gpu.VRAM;
			MemoryType = gpu.MemoryType;
			Amount = gpu.Assortments!.Select(e => e.Amount).First();
		}
		public override string ToString()
		{
			return $"{ProductName} [Объём видеопамяти - {VRAM}, Тип памяти - {MemoryType}]";
		}
	}
	public class RAMAmountBindingModel : ProductAmountBindingModel
	{
		public int AmountOfMemory { get; set; } = 0;
		public string MemoryType { get; set; } = ""; 

		public RAMAmountBindingModel(){}
		public RAMAmountBindingModel(RAM ram) : base(ram)
		{
			AmountOfMemory = ram.AmountOfMemory;
			MemoryType = ram.MemoryType;
			Amount = ram.Assortments!.Select(e => e.Amount).First();
		}
		public override string ToString()
		{
			return $"{ProductName} [Объём - {AmountOfMemory}, Тип памяти - {MemoryType}]";
		}
	}
}