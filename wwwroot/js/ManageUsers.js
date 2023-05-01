let amountOfLoadedRows = 0;
loadRowsToTable();
window.addEventListener('scroll', checkIfRowsNeedToBeLoaded);

function checkIfRowsNeedToBeLoaded()
{
	let windowRelativeBottom = document.documentElement.getBoundingClientRect().bottom;

	if(windowRelativeBottom < document.documentElement.clientHeight + 100)
	{
		loadRowsToTable();
	}
}

async function loadRowsToTable()
{
	//Выполнение останавливается, пока не придёт ответ (await), а поток идёт делать свои дела в других местах
	let response = await fetch(`/Admin/GetUsersRows?startPosition=${amountOfLoadedRows + 1}`);
	if(!response.ok)
	{
		return;
	}

	let htmlCode = await response.text();

	//Check if we downloaded all rows
	if(htmlCode.length == 0)
	{
		window.removeEventListener('scroll', checkIfRowsNeedToBeLoaded);
		return;
	}

	//Insert table
	let table = document.querySelector("tbody");
	table.insertAdjacentHTML('beforeend', htmlCode);

	//Update loaded rows number
	amountOfLoadedRows = +document.querySelector("tr:last-child .position").innerText
}