namespace HamsterWorld.Models
{
	public class ProductDetailsBindingModel
	{
		public int Id { get; set; } = 0;
		public string Country { get; set; } = "";
		public string Model { get; set; } = "";
		public string Description { get; set; } = "";
		public decimal Price { get; set; } = 0;
		public List<ProductPicture> Pictures { get; set; } = new List<ProductPicture>();
		public byte[]? NewPhotos { get; set; } = null!;

		public CPUDetails? CpuDetails { get; set; } = null!;
		public GPUDetails? GpuDetails { get; set; } = null!;
		public RAMDetails? RamDetails { get; set; } = null!;

		public ProductDetailsBindingModel(){}
		public ProductDetailsBindingModel(Product product)
		{
			Id= product.Id;
			Country = product.Country;
			Model = product.Model;
			Description = product.Description;
			Price = product.Price;

			List<ProductPicture> Pictures = product.Pictures ?? new List<ProductPicture>();
		}
		public ProductDetailsBindingModel(CPU cpu) : this(cpu as Product)
		{
			CpuDetails = new CPUDetails(cpu);
		}
		public ProductDetailsBindingModel(GPU gpu) : this(gpu as Product)
		{
			GpuDetails = new GPUDetails(gpu);
		}
		public ProductDetailsBindingModel(RAM ram) : this(ram as Product)
		{
			RamDetails = new RAMDetails(ram);
		}
	}

	public class CPUDetails
	{
		public ushort ClockRate { get; set; } = 0;
		public ushort NumberOfCores { get; set; } = 0; 
		public string Socket { get; set; } = "";

		public CPUDetails(){}
		public CPUDetails (CPU cpu)
		{
			ClockRate = cpu.ClockRate;
			NumberOfCores = cpu.NumberOfCores;
			Socket = cpu.Socket;
		}
	}

	public class GPUDetails
	{
		public int VRAM { get; set; } = 0;
		public string MemoryType { get; set; } = ""; 

		public GPUDetails(){}
		public GPUDetails(GPU gpu)
		{
			VRAM = gpu.VRAM;
			MemoryType = gpu.MemoryType;
		}
	}

	public class RAMDetails
	{
		public int AmountOfMemory { get; set; } = 0;
		public string MemoryType { get; set; } = ""; 

		public RAMDetails(){}
		public RAMDetails(RAM ram)
		{
			AmountOfMemory = ram.AmountOfMemory;
			MemoryType = ram.MemoryType;
		}
	}
}