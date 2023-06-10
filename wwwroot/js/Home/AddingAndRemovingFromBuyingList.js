$('.add-to-shopping-list').on('click', TryAddProductToShoppingList);
$('.remove-from-shopping-list').on('click', TryRemoveProductFromShoppingList);

function TryAddProductToShoppingList()
{
   let productId = +$(this).attr('productId');

   let jqxhr = $.ajax(
   {
		type: "POST",
		url: "/Home/AddProductToUserShoppingList",
		data: {'productId': productId},
	});

   jqxhr.done(() => 
	{ 
      $(this).attr('hidden', true);
      $(this).siblings('.remove-from-shopping-list').removeAttr('hidden');
	});
   jqxhr.fail(() => 
	{ 
      if(jqxhr.status === 404)
      {
         alert("Для использования корзины необходимо авторизоваться");
         return;
      }
	});
}

function TryRemoveProductFromShoppingList()
{
   let productId = +$(this).attr('productId');

   let jqxhr = $.ajax(
   {
		type: "DELETE",
		url: "/Home/RemoveProductFromUserShoppingList",
		data: {'productId': productId},
	});

   jqxhr.done(() => 
	{ 
      $(this).attr('hidden', true);
      $(this).siblings('.add-to-shopping-list').removeAttr('hidden');
	});
}