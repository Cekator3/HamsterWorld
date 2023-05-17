$('.amountOfProduct').change(SaveChanges);

function SaveChanges()
{
	let ancestor = this.closest('.row');
	let storeId = ancestor.getAttribute('storeId');
	let productId = ancestor.getAttribute('productId');

	let amount = $(this).val();
	let goodAnswerField = $(this).closest('.row').find('.ok');
	let badAnswerField = $(this).closest('.row').find('.error');


	$.post("/StoreAdministrator/ChangeProductAmount", {'storeId': storeId, 'productId': productId, 'amount': amount},
		function (data, textStatus, jqXHR) 
		{
			if(jqXHR.status == 200)
			{
				goodAnswerField.text("Изменения сохранены");
				setTimeout(function()
				{
					goodAnswerField.text("");
				}, 3000);
			}		
			else
			{
				badAnswerField.text(textStatus);
				setTimeout(function()
				{
					badAnswerField.text("");
				}, 3000);
			}
		},
	);
}