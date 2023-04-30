using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamsterWorld.Models
{
	public class SignUpBindingModel
	{
		[Required]
		public string? Login { get; set; }

		[Required]
		[DataType(DataType.EmailAddress)]
		public string? Email { get; set; }

		[Required]
		[DataType(DataType.Password)]
		public string? Password { get; set; }

		[Compare("Password")]
		[DataType(DataType.Password)]
		public string? PasswordConfirm { get; set; }

		[Required]
		[Display(Name = "adkfsj")]
		public string? CaptchaUserAnswer { get; set; }
	}

	public class LoginBindingModel
	{
		[Required(ErrorMessage = "lsakjf")]
		public string? Login { get; set; }
		[Required(ErrorMessage = "lakjfd")]
		[DataType(DataType.Password)]
		public string? Password { get; set; }

		public string? ReturnUrl { get; set; }
	}

	public class FormElement
	{
		public string Name { get; set; } = "";
		public string Inner { get; set; } = "";
	}
}