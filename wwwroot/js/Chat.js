$(document).ready(function () {
	$('#ChatSubmit').click(function (e) {
		e.preventDefault();
		var url = $(this).closest('form').attr('action');
        var data = $(this).closest('form').serialize();

        if ($('[name="Contents"]').val().trim() === '') {
            return;
        }

		$.ajax({
			url: url,
			type: 'post',
			data: data,
			success: function (data) {
				$('[name="Contents"]').val('');

				updateMessages();
			},
			error: function (data) {
				replyBox.html('An error has occurred');
			}
		});
    });

    $('.messageChain').stop().animate({
        scrollTop: $('.messageChain')[0].scrollHeight
    }, 800);

	setInterval(updateMessages, 5000);
});

function updateMessages() {
	var lastMessage = $('.messageChain').attr('data-lastmessageid');

	if (lastMessage === '0') {
		lastMessage = $('.messageChain').attr('data-sessionid');
	}

	$.ajax({
		url: '/Chat/Update/' + lastMessage + '?Ajax=true',
		type: 'get',
		success: function (data) {
			$('.messageChain').append(data);

			if ($('.messageChain').find('.message').length) {

				var newLastMessage = $('.messageChain').find('.message').last().attr('data-id');

                $('.messageChain').attr('data-lastmessageid', newLastMessage);

                if (lastMessage != newLastMessage) {
                    $('.messageChain').stop().animate({
                        scrollTop: $('.messageChain')[0].scrollHeight
                    }, 800);

                    $.titleAlert('New Chat Message', {
                        interval: 500,
                        originalTitleInterval: null,
                        duration: 0,
                        stopOnFocus: true,
                        requireBlur: true,
                        stopOnMouseMove: false
                    });
                }
			}
			
		},
		error: function (data) {
			replyBox.html('An error has occurred');
		}
	});
}