namespace HamsterWorld.Models
{
	public class UserInfoBindingModel
	{
		public string Login { get; set; } = "";
		public string Email { get; set; } = "";
		public string Role { get; set; } = "";
		static public List<string> AllRoles { get; set; } = new List<string>();
	}	
}