$('.amount-of-product').on('change', UpdatePriceFields);
$('.remove-product-from-cart').on('click', RemoveProductFromShoppingCart);

function UpdatePriceFields()
{
   let productPrices = UpdateTotalPriceOfProduct($(this));

   let oldProductPrice = productPrices.oldProductPrice;
   let newProductPrice = productPrices.newProductPrice;

   UpdateFinalPriceOfShoppingCart(oldProductPrice, newProductPrice);
}

function RemoveProductFromShoppingCart()
{
   let productId = +$(this).attr('product-id');

   let jqxhr = $.ajax(
   {
		type: "DELETE",
		url: "/Home/RemoveProductFromUserShoppingList",
		data: {'productId': productId},
	});

   jqxhr.done(() => 
	{ 
      window.location.reload();
	});
}


function UpdateTotalPriceOfProduct(amountOfProductEl)
{
   let totalPriceForProductField = amountOfProductEl.closest('.shopping-item').children('.price-div').children('.total-price-for-product');

   let amountOfProduct = +amountOfProductEl.val();
   let baseProductPrice = +totalPriceForProductField.attr('base-price');
   let oldProductPriceTxt = totalPriceForProductField.text();
   let oldProductPrice = GetNumberFromStringWithCurrencyPostfix(oldProductPriceTxt);
   let newProductPrice = baseProductPrice * amountOfProduct;

   totalPriceForProductField.text(`${newProductPrice} руб.`);

   return {
      oldProductPrice: oldProductPrice,
      newProductPrice: newProductPrice
   }
}

function UpdateFinalPriceOfShoppingCart(oldProductPrice, newProductPrice)
{
   let oldTotalPriceTxt = $('#final-price').text();
   let oldTotalPrice = GetNumberFromStringWithCurrencyPostfix(oldTotalPriceTxt);

   let newTotalPrice = oldTotalPrice - oldProductPrice + newProductPrice;

   $('#final-price').text(`${newTotalPrice} руб.`);
}

//Используется, чтобы извлечь цену из полей с постфиксом руб.
function GetNumberFromStringWithCurrencyPostfix(str)
{
   let i = 0;
   let temp = "";
   while(str[i] != ' ')
   {
      temp += str[i];
      i++;
   }

   return +temp;
}