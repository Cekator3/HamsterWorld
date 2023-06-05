$('.productItem').on('click', UpdateMapSrc);

$('.productItem').first().click();

function UpdateMapSrc()
{
	let coordinatesX = $(this).attr('storeCoordinatesX');
	let coordinatesY = $(this).attr('storeCoordinatesY');

	let jqxhr = $.ajax(
		{
			type: "Get",
			url: "/Home/GetYandexMapQuery",
			data: 
			{
				"coordinatesX": coordinatesX,
				"coordinatesY": coordinatesY
			},
		});

	jqxhr.done(() => 
	{ 
		$('#map img').attr('src', jqxhr.responseText);
	});
}