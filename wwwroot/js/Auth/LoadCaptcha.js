let captchaImg = document.querySelector('#captcha-img');
document.addEventListener("DOMContentLoaded", LoadCaptcha);

async function LoadCaptcha()
{
   let response = await fetch('/Auth/GetCaptchaImg');
   let blob = await response.blob();

   captchaImg.src = URL.createObjectURL(blob);
}
