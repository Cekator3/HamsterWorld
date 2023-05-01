using Microsoft.EntityFrameworkCore;
using HamsterWorld.Models;

namespace DatabaseUsageTools
{
	public static class DatabaseTools
	{
		public static async Task<User?> GetUser(string login, ApplicationContext context)
		{
			return await context.Users.FirstOrDefaultAsync(u => u.Login == login);
		}
	}
}