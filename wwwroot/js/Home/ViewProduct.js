$('.rating__star').on('click', UpdateSelectedRatingValue);
$('#newFeedback').on("submit", SendFeedback);
let submitFeedbackErrorMessageField = $("#newFeedbackSubmitError");
let submitFeedbackSucceedMessageField = $("#newFeedbackSubmitSucceed");

function UpdateSelectedRatingValue()
{  
   let selectedValue = $(this).attr('rating-value')
   $(this).parent().attr('rating-selected-value', selectedValue);
}

function SendFeedback(event)
{
   event.preventDefault();
   let productId = $('#productId').val();
   let feedbackText = $('#feedbackText').val();
   let feedbackRating = $('#rating').attr('rating-selected-value');
   let showMessageToUserTimeAmount = 5000;

   if(feedbackRating === undefined)
   {
      ShowFeedbackSubmitErrorMessage("Укажите количество звёзд (оценку) этому товару", showMessageToUserTimeAmount);
      return;
   }
   if(feedbackText === "")
   {
      ShowFeedbackSubmitErrorMessage("Укажите текст отзыва", showMessageToUserTimeAmount);
      return;
   }
   if(!(feedbackRating >= 1 && feedbackRating <= 5))
   {
      ShowFeedbackSubmitErrorMessage("Неверное значение количества звёзд", showMessageToUserTimeAmount);
      return;
   }
   if(productId === "")
   {
      ShowFeedbackSubmitErrorMessage("Id товара не указано", showMessageToUserTimeAmount);
      return;
   }

   let jqxhr = $.ajax({
		type: "POST",
		url: "/Home/AddNewFeedback",
		data: {'feedbackRating': +feedbackRating, 'feedbackText': feedbackText, 'productId': productId},
	});

   jqxhr.done(() => 
	{ 
      ShowFeedbackSubmitSucceedMessage("Отзыв успешно опубликован", showMessageToUserTimeAmount);
	});
	jqxhr.fail(() => 
	{ 
      if(jqxhr.status === 404)
      {
         ShowFeedbackSubmitErrorMessage("Чтобы добавить отзыв, необходимо авторизоваться", showMessageToUserTimeAmount);
         return;
      }
      ShowFeedbackSubmitErrorMessage(jqxhr.responseText, showMessageToUserTimeAmount);
	});
}

function ShowFeedbackSubmitErrorMessage(textMessage, showMessageToUserTimeAmount)
{
   submitFeedbackErrorMessageField.text(textMessage);

   setTimeout(function()
   {
      submitFeedbackErrorMessageField.text("");
   }, showMessageToUserTimeAmount)
}

function ShowFeedbackSubmitSucceedMessage(textMessage, showMessageToUserTimeAmount)
{
   submitFeedbackSucceedMessageField.text(textMessage);

   setTimeout(function()
   {
      submitFeedbackSucceedMessageField.text("");
   }, showMessageToUserTimeAmount)
}