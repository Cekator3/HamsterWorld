using HamsterWorld.ModelBinders;
using Microsoft.AspNetCore.Mvc;

namespace HamsterWorld.Models
{
	[ModelBinder(BinderType = typeof(CatalogItemsModelBinder))]
	abstract public class CatalogFilter
	{
		//Нужен, чтобы знать, какой фильтр использовать
		public Product.Categorys filterType { get; set; }
		public string? Model { get; set; } = "";
		public decimal MinPrice { get; set; } = -1;
		public decimal? MaxPrice { get; set; }
	}

	public class CatalogCpuFilter : CatalogFilter
	{
		public int ClockRateMin { get; set; } = 0;
		public int? ClockRateMax { get; set; } = null;
		public List<ushort> AllowedNumbersOfCores { get; set; } = new List<ushort>();
		public List<string> AllowedSockets { get; set; } = new List<string>();
	}

	public class CatalogGpuFilter : CatalogFilter
	{
		public List<int> AllowedVRAMs { get; set; } = new List<int>();
		public string? MemoryType { get; set; } = null;
	}

	public class CatalogRamFilter : CatalogFilter
	{
		public List<int> AllowedAmountsOfMemory { get; set; } = new List<int>();
		public string? MemoryType { get; set; } = null;
	}
}