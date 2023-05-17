$('.newRoleName').change(SaveChanges);

function SaveChanges()
{
	let login = this.getAttribute('login');
	let newRole = this.value;
	let answerField = $('#xhrAnswer');

	$.post("/AdminUser/ManageUsers", {'login': login, 'newRole': newRole},
		function (data, textStatus, jqXHR) 
		{
			if(jqXHR.status == 200)
			{
				answerField.text("Изменения сохранены");
			}		
			else
			{
				answerField.text(textStatus);
			}

			setTimeout(function()
			{
				answerField.text("");
			}, 3000)
		},
	);
}