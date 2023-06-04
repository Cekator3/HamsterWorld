using Microsoft.AspNetCore.Mvc.ModelBinding;
using HamsterWorld.Models;

namespace HamsterWorld.ModelBinders
{
	public class CatalogItemsModelBinder : IModelBinder
	{
		public Task BindModelAsync(ModelBindingContext bindingContext)
		{
			Product.Categorys? category = GetFilterType(bindingContext);
			if(category == null)
			{
				return Task.CompletedTask;
			}

			string? Model = GetProductModel(bindingContext);
			decimal MinPrice = GetProductMinPrice(bindingContext);
			decimal? MaxPrice = GetProductMaxPrice(bindingContext);

			if(category == Product.Categorys.CPU)
			{
				int ClockRateMin = GetClockRateMin(bindingContext);
				int? ClockRateMax = GetClockRateMax(bindingContext);
				List<ushort> AllowedNumbersOfCores = GetAllowedNumbersOfCores(bindingContext);
				List<string> AllowedSockets = GetAllowedSockets(bindingContext);

				CatalogCpuFilter filter = new CatalogCpuFilter()
				{
					filterType = (Product.Categorys)category,
					Model = Model,
					MinPrice = MinPrice,
					MaxPrice = MaxPrice,
					ClockRateMin = ClockRateMin,
					ClockRateMax = ClockRateMax,
					AllowedNumbersOfCores = AllowedNumbersOfCores,
					AllowedSockets = AllowedSockets
				};

				bindingContext.Result = ModelBindingResult.Success(filter);
			}
			else if(category == Product.Categorys.GPU)
			{
				List<int> AllowedVRAMs = GetAllowedVRAMs(bindingContext);
				string? MemoryType = GetMemoryType(bindingContext);

				CatalogGpuFilter filter = new CatalogGpuFilter()
				{
					filterType = (Product.Categorys)category,
					Model = Model,
					MinPrice = MinPrice,
					MaxPrice = MaxPrice,
					AllowedVRAMs = AllowedVRAMs,
					MemoryType = MemoryType
				};

				bindingContext.Result = ModelBindingResult.Success(filter);
			}
			else if(category == Product.Categorys.RAM)
			{
				List<int> AllowedAmountsOfMemory = GetAllowedAmountsOfMemory(bindingContext);
				string? MemoryType = GetMemoryType(bindingContext);

				CatalogRamFilter filter = new CatalogRamFilter()
				{
					filterType = (Product.Categorys)category,
					Model = Model,
					MinPrice = MinPrice,
					MaxPrice = MaxPrice,
					AllowedAmountsOfMemory = AllowedAmountsOfMemory,
					MemoryType = MemoryType
				};

				bindingContext.Result = ModelBindingResult.Success(filter);
			}

			return Task.CompletedTask;
		}

		Product.Categorys? GetFilterType(ModelBindingContext bindingContext)
		{
			string? categoryIdStr = bindingContext.ValueProvider.GetValue("filterType").FirstValue;

			if(Enum.TryParse(categoryIdStr, out Product.Categorys category))
			{
				return category;
			}

			return null;
		}

		string? GetProductModel(ModelBindingContext bindingContext)
		{
			return bindingContext.ValueProvider.GetValue("Model").FirstValue;
		}

		decimal GetProductMinPrice(ModelBindingContext bindingContext)
		{
			string? MinPriceStr = bindingContext.ValueProvider.GetValue("MinPrice").FirstValue;

			decimal.TryParse(MinPriceStr, out decimal MinPrice);

			return MinPrice;
		}

		decimal? GetProductMaxPrice(ModelBindingContext bindingContext)
		{
			string? maxPriceStr = bindingContext.ValueProvider.GetValue("MaxPrice").FirstValue;

			BindModelHelper.ParseNullable(maxPriceStr, out decimal? MaxPrice, decimal.TryParse);

			return MaxPrice;
		}

		int GetClockRateMin(ModelBindingContext bindingContext)
		{
			string? ClockRateMinStr = bindingContext.ValueProvider.GetValue("ClockRateMin").FirstValue;

			int.TryParse(ClockRateMinStr, out int ClockRateMin);

			return ClockRateMin;
		}

		int? GetClockRateMax(ModelBindingContext bindingContext)
		{
			string? ClockRateMaxStr = bindingContext.ValueProvider.GetValue("ClockRateMax").FirstValue;

			BindModelHelper.ParseNullable(ClockRateMaxStr, out int? ClockRateMax, int.TryParse);

			return ClockRateMax;
		}

		List<ushort> GetAllowedNumbersOfCores(ModelBindingContext bindingContext)
		{
			List<string> AllowedNumbersOfCoresStrs = bindingContext.ValueProvider.GetValue("ClockRateMax").ToList();

			List<ushort> result = new List<ushort>();
			foreach(string numberOfCores in AllowedNumbersOfCoresStrs)
			{
				if(ushort.TryParse(numberOfCores, out ushort tmp))
				{
					result.Add(tmp);
				}
			}

			return result;
		}

		List<string> GetAllowedSockets(ModelBindingContext bindingContext)
		{
			return bindingContext.ValueProvider.GetValue("AllowedSockets").ToList();
		}

		List<int> GetAllowedVRAMs(ModelBindingContext bindingContext)
		{
			List<string> AllowedVRAMsStrs = bindingContext.ValueProvider.GetValue("AllowedVRAMs").ToList();

			List<int> result = new List<int>();
			foreach(string VRAM in AllowedVRAMsStrs)
			{
				if(int.TryParse(VRAM, out int tmp))
				{
					result.Add(tmp);
				}
			}

			return result;
		}

		string? GetMemoryType(ModelBindingContext bindingContext)
		{
			return bindingContext.ValueProvider.GetValue("MemoryType").FirstValue;
		}

		List<int> GetAllowedAmountsOfMemory(ModelBindingContext bindingContext)
		{
			List<string> AllowedAmountsOfMemoryStrs = bindingContext.ValueProvider.GetValue("AllowedAmountsOfMemory").ToList();

			List<int> result = new List<int>();
			foreach(string amountOfMemory in AllowedAmountsOfMemoryStrs)
			{
				if(int.TryParse(amountOfMemory, out int tmp))
				{
					result.Add(tmp);
				}
			}

			return result;
		}
	}

	internal class BindModelHelper
	{
		public delegate bool TryDelegate<T>(string? s, out T result);

		internal static T? ParseNullable<T>(string? s, out T? result, TryDelegate<T> tryDelegate) where T : struct
		{
			result = null;

			if(tryDelegate(s, out T tmp))
			{
				result = tmp;
			}

			return result;
		}
	}
}