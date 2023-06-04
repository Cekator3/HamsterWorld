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
			decimal? MinPrice = GetProductMinPrice(bindingContext);
			decimal? MaxPrice = GetProductMaxPrice(bindingContext);

			if(category == Product.Categorys.CPU)
			{
				CatalogCpuFilter filter = new CatalogCpuFilter()
				{
					filterType = (Product.Categorys)category,
					Model = Model,
					MinPrice = MinPrice,
					MaxPrice = MaxPrice,
					ClockRateMin = GetClockRateMinOfCpu(bindingContext),
					ClockRateMax = GetClockRateMaxOfCpu(bindingContext),
					NumberOfCoresMin = GetMinNumberOfCoresOfCpu(bindingContext),
					NumberOfCoresMax = GetMaxNumberOfCoresOfCpu(bindingContext),
					Socket = GetSocketOfCpu(bindingContext)
				};

				bindingContext.Result = ModelBindingResult.Success(filter);
			}
			else if(category == Product.Categorys.GPU)
			{
				CatalogGpuFilter filter = new CatalogGpuFilter()
				{
					filterType = (Product.Categorys)category,
					Model = Model,
					MinPrice = MinPrice,
					MaxPrice = MaxPrice,
					VramMin = GetVRamMinOfGpu(bindingContext),
					VramMax = GetVRamMaxOfGpu(bindingContext),
					MemoryType = GetMemoryTypeOfGpu(bindingContext)
				};

				bindingContext.Result = ModelBindingResult.Success(filter);
			}
			else if(category == Product.Categorys.RAM)
			{
				CatalogRamFilter filter = new CatalogRamFilter()
				{
					filterType = (Product.Categorys)category,
					Model = Model,
					MinPrice = MinPrice,
					MaxPrice = MaxPrice,
					AmountOfMemoryMin = GetMinAmountOfMemoryOfRam(bindingContext),
					AmountOfMemoryMax = GetMaxAmountOfMemoryOfRam(bindingContext),
					MemoryType = GetMemoryTypeOfRam(bindingContext)
				};

				bindingContext.Result = ModelBindingResult.Success(filter);
			}

			return Task.CompletedTask;
		}

		Product.Categorys? GetFilterType(ModelBindingContext bindingContext)
		{
			string? categoryIdStr = bindingContext.ValueProvider.GetValue("Filter.filterType").FirstValue
											?? bindingContext.ValueProvider.GetValue("filterType").FirstValue;

			if(Enum.TryParse(categoryIdStr, out Product.Categorys category))
			{
				return category;
			}

			return null;
		}

		string? GetProductModel(ModelBindingContext bindingContext)
		{
			return bindingContext.ValueProvider.GetValue("Filter.Model").FirstValue;
		}

		decimal? GetProductMinPrice(ModelBindingContext bindingContext)
		{
			string? MinPriceStr = bindingContext.ValueProvider.GetValue("Filter.MinPrice").FirstValue;

			BindModelHelper.ParseNullable(MinPriceStr, out decimal? MinPrice, decimal.TryParse);

			return MinPrice;
		}

		decimal? GetProductMaxPrice(ModelBindingContext bindingContext)
		{
			string? maxPriceStr = bindingContext.ValueProvider.GetValue("Filter.MaxPrice").FirstValue;

			BindModelHelper.ParseNullable(maxPriceStr, out decimal? MaxPrice, decimal.TryParse);

			return MaxPrice;
		}

		int? GetClockRateMinOfCpu(ModelBindingContext bindingContext)
		{
			string? ClockRateMinStr = bindingContext.ValueProvider.GetValue("cpuFilter.ClockRateMin").FirstValue;

			BindModelHelper.ParseNullable(ClockRateMinStr, out int? ClockRateMin, int.TryParse);

			return ClockRateMin;
		}

		int? GetClockRateMaxOfCpu(ModelBindingContext bindingContext)
		{
			string? ClockRateMaxStr = bindingContext.ValueProvider.GetValue("cpuFilter.ClockRateMax").FirstValue;

			BindModelHelper.ParseNullable(ClockRateMaxStr, out int? ClockRateMax, int.TryParse);

			return ClockRateMax;
		}

		ushort? GetMinNumberOfCoresOfCpu(ModelBindingContext bindingContext)
		{
			string? numberOfCoresMinStr = bindingContext.ValueProvider.GetValue("cpuFilter.NumberOfCoresMin").FirstValue;

			BindModelHelper.ParseNullable(numberOfCoresMinStr, out ushort? NumberOfCores, ushort.TryParse);

			return NumberOfCores;
		}

		ushort? GetMaxNumberOfCoresOfCpu(ModelBindingContext bindingContext)
		{
			string? maxNumberOfCoresStr = bindingContext.ValueProvider.GetValue("cpuFilter.NumberOfCoresMax").FirstValue;

			BindModelHelper.ParseNullable(maxNumberOfCoresStr, out ushort? NumberOfCoresMax, ushort.TryParse);

			return NumberOfCoresMax;
		}
		

		string? GetSocketOfCpu(ModelBindingContext bindingContext)
		{
			return bindingContext.ValueProvider.GetValue("cpuFilter.Socket").FirstValue;
		}

		int? GetVRamMinOfGpu(ModelBindingContext bindingContext)
		{
			string? VRamMinStr = bindingContext.ValueProvider.GetValue("gpuFilter.VramMax").FirstValue;

			BindModelHelper.ParseNullable(VRamMinStr, out int? VRamMin, int.TryParse);

			return VRamMin;
		}

		int? GetVRamMaxOfGpu(ModelBindingContext bindingContext)
		{
			string? VRamMaxStr = bindingContext.ValueProvider.GetValue("gpuFilter.VramMin").FirstValue;

			BindModelHelper.ParseNullable(VRamMaxStr, out int? VRamMax, int.TryParse);

			return VRamMax;
		}

		string? GetMemoryTypeOfGpu(ModelBindingContext bindingContext)
		{
			return bindingContext.ValueProvider.GetValue("gpuFilter.MemoryType").FirstValue;
		}

		int? GetMinAmountOfMemoryOfRam(ModelBindingContext bindingContext)
		{
			string? AmountOfMemoryMinStr = bindingContext.ValueProvider.GetValue("ramFilter.AmountOfMemoryMin").FirstValue;

			BindModelHelper.ParseNullable(AmountOfMemoryMinStr, out int? AmountOfMemoryMin, int.TryParse);

			return AmountOfMemoryMin;
		}

		int? GetMaxAmountOfMemoryOfRam(ModelBindingContext bindingContext)
		{
			string? AmountOfMemoryMaxStr = bindingContext.ValueProvider.GetValue("ramFilter.AmountOfMemoryMax").FirstValue;

			BindModelHelper.ParseNullable(AmountOfMemoryMaxStr, out int? AmountOfMemoryMax, int.TryParse);

			return AmountOfMemoryMax;
		}

		string? GetMemoryTypeOfRam(ModelBindingContext bindingContext)
		{
			return bindingContext.ValueProvider.GetValue("ramFilter.MemoryType").FirstValue;
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