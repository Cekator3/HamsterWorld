using HamsterWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace HamsterWorld.DatabaseUtilities
{
	public static class DbUsageTools
	{
		public static async Task<(bool, string)> TryAddNewProductToDatabase(ApplicationContext context, CPU cpu)
		{
			if(await context.CPUs.AnyAsync(e => e.Model == cpu.Model))
			{
				return (false, "Процессор с такой же моделью уже существует");
			}

			await context.CPUs.AddAsync(cpu);

			//Saving changes to aquire id of added cpu
			await context.SaveChangesAsync();

			await AddMissingProductToAssortmentTable(context, cpu.Id);

			return (true, "");
		}

		public static async Task<(bool, string)> TryAddNewProductToDatabase(ApplicationContext context, GPU gpu)
		{
			//Почти точное повторение кода из первого метода
			if(await context.GPUs.AnyAsync(e => e.Model == gpu.Model))
			{
				return (false, "Видеокарта с такой же моделью уже существует");
			}

			await context.GPUs.AddAsync(gpu);
			await context.SaveChangesAsync();
			await AddMissingProductToAssortmentTable(context, ram.Id);
			return (true, "");
		}

		public static async Task<(bool, string)> TryAddNewProductToDatabase(ApplicationContext context, RAM ram)
		{
			//Почти точное повторение кода из первого метода
			if(await context.RAMs.AnyAsync(e => e.Model == ram.Model))
			{
				return (false, "Оперативная память с такой же моделью уже существует");
			}

			await context.RAMs.AddAsync(ram);
			await context.SaveChangesAsync();
			await AddMissingProductToAssortmentTable(context, ram.Id);
			return (true, "");
		}
		public async static Task RemoveProductFromDatabase(ApplicationContext context, int productId)
		{
			Product? product = await context.Products.FindAsync(productId);

			if(product != null)
			{
				context.Products.Remove(product);
				await context.Assortments.Where(e => e.ProductId == productId).ExecuteDeleteAsync();
				await context.SaveChangesAsync();
			}
		}

		//Добавить недостающие элементы в промежуточную таблицу (Product-Store)
		private static async Task AddMissingProductToAssortmentTable(ApplicationContext context, int productId)
		{
			//Getting id list of all existing stores
			var storeIDs = await context.Stores.AsNoTracking()
															.Select(e => e.Id)
															.ToListAsync();

			List<Assortment> addedItems = new List<Assortment>();
			foreach (var storeId in storeIDs)
			{
				Assortment item = new Assortment()
				{
					StoreId = storeId,
					ProductId = productId
				};

				addedItems.Add(item);
			}
			await context.Assortments.AddRangeAsync(addedItems);
		}


		public async static Task<(bool, string)> TryAddStoreToDatabase(ApplicationContext context, Store store)
		{
			if(await IsStoreWithThatAddressExistsInDatabase(context, store))
			{
            return (false, "Такой магазин уже существует");
			}

			await context.AddAsync(store);
			//Сохранение позволяет узнать id эл-та при помощи store.Id
			await context.SaveChangesAsync();

			await AddMissingStoreToAssortmentTable(context, store.Id);

			return (true, "");		
		}

		private static async Task<bool> IsStoreWithThatAddressExistsInDatabase(ApplicationContext context, Store store)
		{
			return await context.Stores.AnyAsync(e => e.Coordinates == store.Coordinates);
		}

		public async static Task RemoveStoreFromDatabase(ApplicationContext context, short storeId)
		{
			Store? store = await context.Stores.FindAsync(storeId);

			if(store != null)
			{
				context.Stores.Remove(store);
				await context.Assortments.Where(e => e.ProductId == storeId).ExecuteDeleteAsync();
				await context.SaveChangesAsync();
			}
		}
		//Добавить недостающие элементы в промежуточную таблицу (Product-Store)
		private static async Task AddMissingStoreToAssortmentTable(ApplicationContext context, short storeId)
		{
			//Getting id list of all existing products
			var productIDs = await context.Products.AsNoTracking()
																.Select(e => e.Id)
																.ToListAsync();

			List<Assortment> addedItems = new List<Assortment>();
			foreach (var productId in productIDs)
			{
				Assortment item = new Assortment()
				{
					StoreId = storeId,
					ProductId = productId
				};

				addedItems.Add(item);
			}
			await context.Assortments.AddRangeAsync(addedItems);
		}
	}
}