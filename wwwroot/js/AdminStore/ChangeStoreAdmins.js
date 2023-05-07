IsAdminCell = document.querySelectorAll('.IsAdminOfThisStore');

IsAdminCell.forEach(cell => {
	cell.addEventListener('change', SaveChanges)
});

function SaveChanges()
{
	let AdminId = this.getAttribute('adminId');
	let StoreId = this.getAttribute('storeId');
	let IsAdminOfThisStore = this.checked;

	let xhr = new XMLHttpRequest();
	xhr.open('POST', `/AdminStore/ChangeStoreAdministrators?adminId=${AdminId}&storeId=${StoreId}&isBecomingAdmin=${IsAdminOfThisStore}`);
	xhr.send();

	let answerField = document.querySelector('#xhrAnswer')
	xhr.onload = function() 
	{
		if(xhr.status == 200)
		{
			answerField.innerHTML = "Изменения сохранены";
		}
		else
		{
			answerField.innerHTML = xhr.responseText;
		}
	}
}