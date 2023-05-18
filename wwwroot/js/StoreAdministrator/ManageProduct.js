$('.picture-delete').on("click", MarkToDelete);
$('.picture-up').on("click", MovePictureUp);
$('.picture-down').on("click", MovePictureDown);

function MarkToDelete()
{
	$(this).closest('.picture-galery').attr("DeletionMark", '');
	$(this).closest('.picture-galery').attr("hidden", '');
}

function MovePictureUp()
{
	NormalizeGalery();
	let currPicture = this.closest('.picture-galery');

	let currentOrderNumber = GetOrderNumberOfPicture(currPicture);
	if(currentOrderNumber == 1)
	{
		return;
	}
	let pictureToSwapOrderNumber = this.closest('.picture-galery').parentNode.querySelector(`.order-${currentOrderNumber - 1}`);

	currPicture.classList.remove(`order-${currentOrderNumber}`);
	pictureToSwapOrderNumber.classList.remove(`order-${currentOrderNumber - 1}`);

	currPicture.classList.add(`order-${currentOrderNumber - 1}`);
	pictureToSwapOrderNumber.classList.add(`order-${currentOrderNumber}`);
}

function MovePictureDown()
{
	NormalizeGalery();
	let currPicture = this.closest('.picture-galery');

	let currentOrderNumber = GetOrderNumberOfPicture(currPicture);
	let pictureToSwapOrderNumber = this.closest('.picture-galery').parentNode.querySelector(`.order-${currentOrderNumber + 1}`);

	if(pictureToSwapOrderNumber == null)
	{
		return;
	}

	currPicture.classList.remove(`order-${currentOrderNumber}`);
	pictureToSwapOrderNumber.classList.remove(`order-${currentOrderNumber + 1}`);

	currPicture.classList.add(`order-${currentOrderNumber + 1}`);
	pictureToSwapOrderNumber.classList.add(`order-${currentOrderNumber}`);
}

function NormalizeGalery()
{
	let galery = document.querySelectorAll('.picture-galery');

	let picturesOrderNumbers = GetOrderNumberOfAllPictures(galery);

	let pictureOrderIndexes = ConvertNumbersToTheirOrderIndexes(picturesOrderNumbers);

	GetRidOfRepeatingIndexes(pictureOrderIndexes);

	ApplyOrderIndexesToPictureElements(galery, pictureOrderIndexes)
}

function ApplyOrderIndexesToPictureElements(pictureElements, pictureOrderIndexes)
{
	for(let i = 0; i < pictureElements.length; i++)
	{
		pictureElements[i].classList.forEach(className => 
		{
			if(className.startsWith('order-'))
			{
				pictureElements[i].classList.remove(className);
			}
		});

		pictureElements[i].classList.add(`order-${pictureOrderIndexes[i]}`);
	}
}

function GetRidOfRepeatingIndexes(array)
{
	let amountOfRepeatedElemsInRow = 0;
	let lastElement = -1;

	for (let i = 0; i < array.length; i++) 
	{
		amountOfRepeatedElemsInRow = array[i] == lastElement ? amountOfRepeatedElemsInRow + 1 : 0;

		array[i] += amountOfRepeatedElemsInRow;
		lastElement = array[i];
	}
}

function ConvertNumbersToTheirOrderIndexes(array)
{
	let arraySorted = new Int32Array(array).sort();
	let result = []

	for(let i = 0; i < array.length; i++)
	{
		for(let j = 0 ; j < array.length; j++)
		{
			if(array[i] == arraySorted[j])
			{
				result.push(j + 1);
				break;
			}
		}
	}

	return result
}

function GetOrderNumberOfAllPictures(pictureElements)
{
	picturesOrderNumbers = [];

	for(let i = 0; i < pictureElements.length; i++)
	{
		let orderNumber = GetOrderNumberOfPicture(pictureElements[i])

		if(orderNumber <= 0)
		{
			orderNumber = 9999;
		}

		picturesOrderNumbers.push(orderNumber);
	}

	return picturesOrderNumbers;
}

function GetOrderNumberOfPicture(element)
{
	let answer = -1;

	for(let className of Array.from(element.classList))
	{
		if(!className.startsWith("order-"))
		{
			continue;
		}

		let testRegExp = new RegExp("order-[0-9]+$");
		let matchRegExp = new RegExp("[0-9]+");

		if(testRegExp.test(className))
		{
			answer = +className.match(matchRegExp);
		}
	}

	return answer;
}