using System.ComponentModel.DataAnnotations;

namespace HamsterWorld.Models
{
	public class UserInfoBindingModel
	{
		public string Login { get; set; } = "";
		public string Email { get; set; } = "";
		public string Role { get; set; } = "";
		public enum Filters
		{
			[Display(Name="Логин")]
			Login,
			[Display(Name="Email")]
			Email,
			[Display(Name="Роль")]
			Role
		}
		static public List<string> AllRoles { get; set; } = null!;
	}	
}