const maxAmountOfPictures = 30;
const styleEl = document.createElement('style');

for(let i = 0; i < maxAmountOfPictures; i++)
{
	styleEl.innerHTML += `
	.order-${i + 1}
	{
		order: ${i + 1}
	}`;
}

document.head.append(styleEl);