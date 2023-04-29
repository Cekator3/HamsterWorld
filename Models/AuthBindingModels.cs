namespace HamsterWorld.Models
{
	public class SignUpBindingModel
	{
		public string Login { get; set; } = "";
		public string Email { get; set; } = "";
		public string Password { get; set; } = "";
		public string PasswordConfirm { get; set; } = "";

		public string? ReturnUrl { get; set; } = "";
		public object Captcha { get; set; } = "";
	}

	public class LoginBindingModel
	{

		public string Login { get; set; } = "";
		public string Password { get; set; } = "";

		public string? ReturnUrl { get; set; } = "";
		public object Captcha { get; set; } = "";
	}
}