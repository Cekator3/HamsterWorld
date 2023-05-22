$('.picture-delete').on("click", MarkToDelete);
$('.picture-up').on("click", MovePictureUp);
$('.picture-down').on("click", MovePictureDown);
$('#save-changes').on("click", SaveShanges);

async function SaveShanges() {
	NormalizeGalery();

	await SendRequestToDeletePicturesWithDeletionMark()

	await SendRequestToSaveCurrentPicturesOrder();
}

function MarkToDelete() {
	MakeActiveSaveButton();
	$(this).closest('.picture-galery').attr("to-delete", '');
	$(this).closest('.picture-galery').attr("hidden", '');
}

function MovePictureUp() {
	MakeActiveSaveButton();
	NormalizeGalery();
	let currPicture = this.closest('.picture-galery');

	let currentOrderNumber = GetOrderNumberOfPicture(currPicture);
	if (currentOrderNumber == 1) {
		return;
	}
	let pictureToSwapOrderNumber = this.closest('.picture-galery').parentNode.querySelector(`.order-${currentOrderNumber - 1}`);

	currPicture.classList.remove(`order-${currentOrderNumber}`);
	pictureToSwapOrderNumber.classList.remove(`order-${currentOrderNumber - 1}`);

	currPicture.classList.add(`order-${currentOrderNumber - 1}`);
	pictureToSwapOrderNumber.classList.add(`order-${currentOrderNumber}`);
}

function MovePictureDown() {
	MakeActiveSaveButton();
	NormalizeGalery();
	let currPicture = this.closest('.picture-galery');

	let currentOrderNumber = GetOrderNumberOfPicture(currPicture);
	let pictureToSwapOrderNumber = this.closest('.picture-galery').parentNode.querySelector(`.order-${currentOrderNumber + 1}`);

	if (pictureToSwapOrderNumber == null) {
		return;
	}

	currPicture.classList.remove(`order-${currentOrderNumber}`);
	pictureToSwapOrderNumber.classList.remove(`order-${currentOrderNumber + 1}`);

	currPicture.classList.add(`order-${currentOrderNumber + 1}`);
	pictureToSwapOrderNumber.classList.add(`order-${currentOrderNumber}`);
}

function NormalizeGalery() {
	let galery = document.querySelectorAll('.picture-galery');

	let picturesOrderNumbers = GetOrderNumberOfAllPictures(galery);

	let pictureOrderIndexes = ConvertNumbersToTheirOrderIndexes(picturesOrderNumbers);

	GetRidOfRepeatingIndexes(pictureOrderIndexes);

	ApplyOrderIndexesToPictureElements(galery, pictureOrderIndexes)
}

function ApplyOrderIndexesToPictureElements(pictureElements, pictureOrderIndexes) {
	for (let i = 0; i < pictureElements.length; i++) {
		pictureElements[i].classList.forEach(className => {
			if (className.startsWith('order-')) {
				pictureElements[i].classList.remove(className);
			}
		});

		pictureElements[i].classList.add(`order-${pictureOrderIndexes[i]}`);
	}
}

function GetRidOfRepeatingIndexes(array) {
	let amountOfRepeatedElemsInRow = 0;
	let lastElement = -1;

	for (let i = 0; i < array.length; i++) {
		amountOfRepeatedElemsInRow = array[i] == lastElement ? amountOfRepeatedElemsInRow + 1 : 0;

		array[i] += amountOfRepeatedElemsInRow;
		lastElement = array[i];
	}
}

function ConvertNumbersToTheirOrderIndexes(array) {
	let arraySorted = new Int32Array(array).sort();
	let result = []

	for (let i = 0; i < array.length; i++) {
		for (let j = 0; j < array.length; j++) {
			if (array[i] == arraySorted[j]) {
				result.push(j + 1);
				break;
			}
		}
	}

	return result
}

function GetOrderNumberOfAllPictures(pictureElements) {
	picturesOrderNumbers = [];

	for (let i = 0; i < pictureElements.length; i++) {
		let orderNumber = GetOrderNumberOfPicture(pictureElements[i])

		if (orderNumber <= 0) {
			orderNumber = 9999;
		}

		picturesOrderNumbers.push(orderNumber);
	}

	return picturesOrderNumbers;
}

function GetOrderNumberOfPicture(element) {
	let answer = -1;

	for (let className of Array.from(element.classList)) {
		if (!className.startsWith("order-")) {
			continue;
		}

		let testRegExp = new RegExp("order-[0-9]+$");
		let matchRegExp = new RegExp("[0-9]+");

		if (testRegExp.test(className)) {
			answer = +className.match(matchRegExp);
		}
	}

	return answer;
}

function MakeActiveSaveButton() {
	$('#save-changes').removeAttr('disabled');
}


async function SendRequestToDeletePicturesWithDeletionMark() {
	let picturesToDelete = [];
	let picturesWithDeletionMark = $('.picture-galery[to-delete]');
	for (let picture of picturesWithDeletionMark) {
		picturesToDelete.push(+picture.getAttribute('pictureId'));
	}

	if (picturesToDelete.length == 0) {
		return;
	}

	let jqxhr = $.ajax({
		type: "DELETE",
		url: "/StoreAdministrator/ManagePictures",
		data: { "picturesToDelete": picturesToDelete },
	});

	jqxhr.done(() => 
	{ 
		$('#delete-ok').text("Удаление произошло успешно");	
		setTimeout(function()
		{
			$('#delete-ok').text("");
		}, 5000);
	});
	jqxhr.fail(() => 
	{ 
		$('#delete-error').text(jqxhr.responseText);	
		setTimeout(function()
		{
			$('#delete-error').text("");
		}, 5000);
	});
}

async function SendRequestToSaveCurrentPicturesOrder()
{
	let picturesOrderInfo = [];

	let pictures = $('.picture-galery').filter(':not([to-delete])');
	for (let picture of pictures) 
	{
		let id = +picture.getAttribute('pictureId');
		let orderNumber = GetOrderNumberOfPicture(picture);

		picturesOrderInfo.push(
		{
			Id: id,
			OrderNumber: orderNumber
		});
	}

	let jqxhr = $.ajax({
		type: "PUT",
		url: "/StoreAdministrator/ManagePictures",
		data: { "picturesOrderInfo": picturesOrderInfo },
	});

	jqxhr.done(() => 
	{ 
		$('#update-ok').text("Порядок изображений изменён успешно");	
		setTimeout(function()
		{
			$('#update-ok').text("");
		}, 5000);
	});
	jqxhr.fail(() => 
	{ 
		$('#update-error').text(jqxhr.responseText);	
		setTimeout(function()
		{
			$('#update-error').text("");
		}, 5000);
	});
}