namespace HamsterWorld.Models
{
	public class UserBindingModel
	{
		public string Login { get; set; } = "";
		public string RoleName { get; set; } = "";

		public UserBindingModel(string login, string roleName)
		{
			Login = login;
			RoleName = roleName;
		}
	}	
}