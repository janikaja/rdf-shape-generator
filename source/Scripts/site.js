var $table = $('#foundPropertiesHeading');
if ($table.length) {
    $('html, body').animate({
        scrollTop: $table.offset().top - 60
    }, 100);
}

$('.cardinalities').on('change', function () {
    $('input[data-index="' + $(this).data('index') + '"]').val('');
});