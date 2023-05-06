roleCells = document.querySelectorAll('.newRoleName');

roleCells.forEach(cell => {
	cell.addEventListener('change', SaveChanges)
});

function SaveChanges()
{
	let login = this.getAttribute('login');
	let newRole = this.value;

	let xhr = new XMLHttpRequest();
	xhr.open('POST', `/AdminUser/ManageUsers?login=${login}&newRole=${newRole}`);
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