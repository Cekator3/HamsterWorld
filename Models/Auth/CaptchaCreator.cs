using SixLaborsCaptcha.Core;
using SixLabors.ImageSharp;

namespace HamsterWorld.CaptchaCreator
{
	public static class Captcha
	{
		public static string GenerateAnswer()
		{
			return new Random(DateTime.Now.Millisecond).Next(1111, 9999).ToString();
		}

		public static byte[] GenerateImage(string Answer)
		{
			var slc = new SixLaborsCaptchaModule(new SixLaborsCaptchaOptions
			{
				DrawLines = 5,
				TextColor = new Color[] { Color.Blue, Color.Black }
			});

			byte[] result = slc.Generate(Answer);

			return result;
		}
	}
}