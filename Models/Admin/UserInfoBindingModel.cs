namespace HamsterWorld.Models
{
	public class UserInfoBindingModel
	{
		public string Login { get; set; } = "";
		public string RoleName { get; set; } = "";
		//Нужен только для администраторов магазина
		public List<string>? StoresNames { get; set; }

		public UserInfoBindingModel(string login, string roleName, List<Store>? stores)
		{
			Login = login;
			RoleName = roleName;
			if(stores != null)
			{
				this.StoresNames = stores.Select(e => e.Name).ToList<String>();
			}
		}
	}	
}