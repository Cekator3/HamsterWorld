using HamsterWorld.ModelBinders;
using Microsoft.AspNetCore.Mvc;

namespace HamsterWorld.Models
{
	[ModelBinder(BinderType = typeof(CatalogItemsModelBinder))]
	abstract public class CatalogFilter
	{
		//Нужен, чтобы знать, какой фильтр использовать
		public Product.Categorys filterType { get; set; }
		public string? Model { get; set; } = null;
		public decimal? MinPrice { get; set; } = null;
		public decimal? MaxPrice { get; set; } = null;
	}

	public class CatalogCpuFilter : CatalogFilter
	{
		public int? ClockRateMin { get; set; } = null;
		public int? ClockRateMax { get; set; } = null;
		public ushort? NumberOfCoresMin { get; set; } = null;
		public ushort? NumberOfCoresMax { get; set; } = null;
		public string? Socket { get; set; } = null;
	}

	public class CatalogGpuFilter : CatalogFilter
	{
		public int? VramMin { get; set; } = null;
		public int? VramMax { get; set; } = null;
		public string? MemoryType { get; set; } = null;
	}

	public class CatalogRamFilter : CatalogFilter
	{
		public int? AmountOfMemoryMin { get; set; } = null;
		public int? AmountOfMemoryMax { get; set; } = null;
		public string? MemoryType { get; set; } = null;
	}
}