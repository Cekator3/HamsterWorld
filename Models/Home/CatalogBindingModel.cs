namespace HamsterWorld.Models
{
	public class CatalogBindingModel
	{
		public CatalogFilter Filter { get; set; } = null!;
		public List<CatalogItem> CatalogItems { get; set; } = new List<CatalogItem>();
		public List<int> ProductsFromUsersShoppingList { get; set; } = new List<int>();
	}

	abstract public class CatalogItem
	{
		public int Id { get; set; }
		public string? PictureSrc { get; set; } = "";
		public string Model { get; set; } = "";
		public decimal Price { get; set; } = 0;

		protected CatalogItem(){}
		protected CatalogItem(Product product)
		{
			Id = product.Id;
			PictureSrc = $"{ProductPicture.PATH}{product.Pictures!.FirstOrDefault()?.FileName}";
			Model = product.Model;
			Price = product.Price;
		}
	}

	public class CatalogCpuItem : CatalogItem
	{
		public ushort ClockRate { get; set; } = 0;
		public ushort NumberOfCores { get; set; } = 0; 
		public string Socket { get; set; } = "";

		public CatalogCpuItem(){}
		public CatalogCpuItem(CPU cpu) : base(cpu)
		{
			ClockRate = cpu.ClockRate;
			NumberOfCores = cpu.NumberOfCores;
			Socket = cpu.Socket;
		}

		public override string ToString()
		{
			return $"Процессор {Model} [Тактовая частота - {ClockRate}, Число ядер - {NumberOfCores}, Сокет - {Socket}]";
		}
	}

	public class CatalogGpuItem : CatalogItem
	{
		public int VRAM { get; set; } = 0;
		public string MemoryType { get; set; } = ""; 

		public CatalogGpuItem(){}
		public CatalogGpuItem(GPU gpu) : base(gpu)
		{
			VRAM = gpu.VRAM;
			MemoryType = gpu.MemoryType;
		}

		public override string ToString()
		{
			return $"Видеокарта {Model} [Объём видеопамяти - {VRAM}, Тип памяти - {MemoryType}]";
		}
	}

	public class CatalogRamItem : CatalogItem
	{
		public int AmountOfMemory { get; set; } = 0;
		public string MemoryType { get; set; } = ""; 

		public CatalogRamItem(){}
		public CatalogRamItem(RAM ram) : base(ram)
		{
			AmountOfMemory = ram.AmountOfMemory;
			MemoryType = ram.MemoryType;
		}

		public override string ToString()
		{
			return $"Оперативная память {Model} [Объём - {AmountOfMemory}, Тип памяти - {MemoryType}]";
		}
	}
}