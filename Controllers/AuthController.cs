using Microsoft.AspNetCore.Authentication;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using HamsterWorld.Models;
using HamsterWorld.CaptchaCreator;
using Bcrypt = BCrypt.Net.BCrypt;
using Microsoft.EntityFrameworkCore;

namespace HamsterWorld.Controllers
{
	public class AuthController : Controller
	{
		private readonly ApplicationContext _context;

		public AuthController(ApplicationContext context)
		{
			_context = context;
		}

		[HttpGet]
		public FileResult GetCaptchaImg()
		{
			string answer;
			if (HttpContext.Session.Keys.Contains("CaptchaRealAnswer"))
			{
				answer = HttpContext.Session.GetString("CaptchaRealAnswer")!;
			}
			else
			{
				//Такое произойдёт только, если пользователь напрямую обратиться по адресу /Auth/GetCaptchaImg
				answer = "You shouldn't see this";
			}

			byte[] img = Captcha.GenerateImage(answer);
			return File(img, "Image/Png");
		}

		[HttpGet]
		public IActionResult SignUp()
		{
			SignUpBindingModel model = new SignUpBindingModel();
			HttpContext.Session.SetString("CaptchaRealAnswer", Captcha.GenerateAnswer());

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SignUp(SignUpBindingModel model)
		{
			string captchaRealAnswer = HttpContext.Session.GetString("CaptchaRealAnswer")!;
			if (model.CaptchaUserAnswer != captchaRealAnswer)
			{
				ModelState.AddModelError(nameof(model.CaptchaUserAnswer), "Неправильный ответ на капчу");

				//Regenerate captcha
				model.CaptchaUserAnswer = "";
				HttpContext.Session.SetString("CaptchaRealAnswer", Captcha.GenerateAnswer());
			}
			if (await IsLoginExist(model.Login))
			{
				ModelState.AddModelError(nameof(model.Login), "Пользователь с таким логином уже существует");
			}
			if (await IsEmailExist(model.Email))
			{
				ModelState.AddModelError(nameof(model.Email), "Пользователя с такой электронной почтой уже существует");
			}
			if (ModelState.IsValid)
			{
				//Adding user to Database
				User user = new User()
				{
					Login = model.Login,
					RoleId = Role.USER,
					Email = model.Email,
					PasswordHash = GeneratePasswordHash(model.Password),
					Money = 0
				};
				await _context.Users.AddAsync(user);
				await _context.SaveChangesAsync();

				await SendAuthCookiesToUser(user);

				return Redirect("/");
			}

			return View(model);
		}

		[HttpGet]
		public IActionResult Login(string? returnUrl)
		{
			LoginBindingModel model = new LoginBindingModel() { ReturnUrl = returnUrl };
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginBindingModel model)
		{
			//Get user with that login
			User? user = await _context.Users.AsNoTracking()
														.FirstOrDefaultAsync(u => u.Login == model.Login);

			if (user == null)
			{
				ModelState.AddModelError(nameof(model.Login), "Пользователя с таким логином не существует");
			}
			else if (!IsPasswordValid(model.Password, user.PasswordHash))
			{
				ModelState.AddModelError(nameof(model.Password), "Неверный пароль");
			}
			if (ModelState.IsValid)
			{
				await SendAuthCookiesToUser(user!);
				if(string.IsNullOrEmpty(model.ReturnUrl))
				{
					return Redirect("/");
				}
				return Redirect(model.ReturnUrl);
			}

			return View(model);
		}

		public async Task<IActionResult> Logout()
		{
			await HttpContext.SignOutAsync("Cookies");
			return Redirect("/");
		}


		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}


		private async Task<bool> IsLoginExist(string login)
		{
			return await _context.Users.AsNoTracking().AnyAsync(u => u.Login == login);
		}
		private async Task<bool> IsEmailExist(string email)
		{
			return await _context.Users.AsNoTracking().AnyAsync(u => u.Email == email);
		}

		private string GeneratePasswordHash(string password)
		{
			return Bcrypt.HashPassword(password);
		}
		private bool IsPasswordValid(string text, string passwordHash)
		{
			return Bcrypt.Verify(text, passwordHash);
		}

		private async Task SendAuthCookiesToUser(User user)
		{
			List<Claim> claims = new List<Claim>()
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Name, user.Login),
				new Claim(ClaimTypes.Role, user.RoleId.ToString()),
				new Claim(ClaimTypes.Email, user.Email)
			};
			ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Cookies");
			ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

			await HttpContext.SignInAsync("Cookies", claimsPrincipal);
		}
	}
}
