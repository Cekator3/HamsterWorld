using System.ComponentModel.DataAnnotations;

namespace HamsterWorld.Models
{
	public class SignUpBindingModel
	{
		[Required(ErrorMessage = "Придумайте логин")]
		public string Login { get; set; } = "";

		[Required(ErrorMessage = "Введите свою электронную почту")]
		[DataType(DataType.EmailAddress)]
		public string Email { get; set; } = "";

		[Required(ErrorMessage = "Придумайте пароль для себя")]
		[DataType(DataType.Password)]
		public string Password { get; set; } = "";

		[Required(ErrorMessage = "Введите подтверждение пароля")]
		[Compare("Password", ErrorMessage = "Пароли не совпадают")]
		[DataType(DataType.Password)]
		public string PasswordConfirm { get; set; } = "";

		[Required(ErrorMessage = "Введите ответ на капчу")]
		public string CaptchaUserAnswer { get; set; } = "";
	}

	public class LoginBindingModel
	{
		[Required(ErrorMessage = "Введите логин")]
		public string Login { get; set; } = "";
		[Required(ErrorMessage = "Введите пароль")]
		[DataType(DataType.Password)]
		public string Password { get; set; } = "";

		public string? ReturnUrl { get; set; } = "";
	}
}