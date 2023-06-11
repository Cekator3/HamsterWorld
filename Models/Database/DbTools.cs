using HamsterWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace HamsterWorld.DatabaseUtilities
{
	public static class DbUsageTools
	{
		public static async Task<(bool, string)> TryAddNewProductToDatabase<T>(ApplicationContext context, T product) where T: Product
		{
			if(product is CPU cpu)
			{
				return await TryAddNewProductToDatabase(context.CPUs, context, cpu);
			}
			if(product is GPU gpu)
			{
				return await TryAddNewProductToDatabase(context.GPUs, context, gpu);
			}
			if(product is RAM ram)
			{
				return await TryAddNewProductToDatabase(context.RAMs, context, ram);
			}

			throw new NotImplementedException();
		}
		public static async Task<(bool, string)> TryAddNewProductToDatabase<T>(DbSet<T> dbSet, ApplicationContext context, T product) where T: Product
		{
			if(await dbSet.AnyAsync(e => e.Model == product.Model))
			{
				return (false, "Товар с такой же моделью уже существует");
			}

			await dbSet.AddAsync(product);

			//Saving changes to aquire id of added cpu
			await context.SaveChangesAsync();

			await AddMissingProductToAssortmentTable(context, product.Id);

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
			await context.SaveChangesAsync();
		}


		public async static Task<(bool, string)> TryAddStoreToDatabase(ApplicationContext context, Store store)
		{
			if(await IsStoreWithThatAddressAlreadyExist(context, store))
			{
            return (false, "Такой магазин уже существует");
			}

			await context.AddAsync(store);
			//Сохранение позволяет узнать id эл-та при помощи store.Id
			await context.SaveChangesAsync();

			await AddMissingStoreToAssortmentTable(context, store.Id);

			return (true, "");		
		}

		private static async Task<bool> IsStoreWithThatAddressAlreadyExist(ApplicationContext context, Store store)
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
			await context.SaveChangesAsync();
		}
	}
}