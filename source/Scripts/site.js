var $source = $('#MainContent_source'), sourceContents = $source.val();
var $table = $('#foundPropertiesHeading');

if ($table.length) {
    scrollTo($table);
} else if (sourceContents.length) {
    scrollTo($source);
}

$('.cardinalities').on('change', function () {
    $('input[data-index="' + $(this).data('index') + '"]').val('');
});

$('#fineTune').on('click', function () {
    $(this).val(($(this).val() == $(this).data('label1')) ? $(this).data('label2') : $(this).data('label1'));
    $('.toggleContents').slideToggle();
});

$source.on('keyup', function () {
    if ($source.val() != sourceContents && $('#MainContent_Button1').length === 0) {
        $('#MainContent_Button2').show();
    } else {
        $('#MainContent_Button2').hide();
    }
});

function scrollTo($element) {
    $('html, body').animate({
        scrollTop: $element.offset().top - 60
    }, 100);
}