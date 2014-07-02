function IsValid(a)
{
	return ((a>=9)&& (Math.sqrt(a) % 1 == 0));
}

$('#copyright').children().first('li').html('<p>&copy; ' + new Date().getFullYear() + ' Taiyo Kato');

var $tinput = $('#tryinput');
var $validview = $('#validity').hide();
$tinput.keyup(function(){
	var res = IsValid($tinput.val());
	$validview.text( (res) ? "Size valid" : "Size invalid").fadeIn();
	
});
$tinput
	.focusout(function(){
	$validview.fadeOut();

	})
	.focusin(function(){
	$validview.fadeIn();
	});
