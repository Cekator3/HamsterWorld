namespace HamsterWorld.Models
{
	public class ChangeStoreAdministratorsBindingModel
	{
		public int AdminId { get; set; }
		public string Login { get; set; } = null!;
		public string Email { get; set; } = null!;
		public bool IsAdminOfThisStore { get; set; }
	}
}