using System.ComponentModel.DataAnnotations;

namespace HamsterWorld.Models
{
	public class ProductDetailsBindingModel
	{
		//Id существующего типа товара
		public int Id { get; set; } = -1;
		[Required(ErrorMessage = "Введите страну производителя")]
		public string Country { get; set; } = "";
		[Required(ErrorMessage = "Введите название модели")]
		public string Model { get; set; } = "";
		[Required(ErrorMessage = "Введите описание")]
		public string Description { get; set; } = "";
		[Required(ErrorMessage = "Введите цену товара")]
		public decimal Price { get; set; } = 0;
		public List<ProductPicture> Pictures { get; set; } = new List<ProductPicture>();
		public IFormFileCollection NewPhotos { get; set; } = new FormFileCollection();

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

			Pictures = product.Pictures ?? new List<ProductPicture>();
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
		[Required(ErrorMessage = "Введите тактовую частоту процессора")]
		public ushort ClockRate { get; set; } = 0;
		[Required(ErrorMessage = "Введите количество ядер процессора")]
		public ushort NumberOfCores { get; set; } = 0; 
		[Required(ErrorMessage = "Введите сокет")]
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
		[Required(ErrorMessage = "Введите количество видеопамяти")]
		public int VRAM { get; set; } = 0;
		[Required(ErrorMessage = "Введите тип памяти видеокарты")]
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
		[Required(ErrorMessage = "Введите количество оперативной памяти")]
		public int AmountOfMemory { get; set; } = 0;
		[Required(ErrorMessage = "Введите тип памяти")]
		public string MemoryType { get; set; } = ""; 

		public RAMDetails(){}
		public RAMDetails(RAM ram)
		{
			AmountOfMemory = ram.AmountOfMemory;
			MemoryType = ram.MemoryType;
		}
	}
}