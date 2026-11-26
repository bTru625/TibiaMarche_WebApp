// Admin requests table (/Admin/Requests): "Mark fulfilled" closes every open request for that
// item + server, so ask first.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-fulfill-form]').forEach(function (button) {
        button.addEventListener('click', function () {
            const count = parseInt(button.dataset.count, 10) || 0;
            const message = 'Mark all ' + count + ' request' + (count === 1 ? '' : 's') +
                ' for "' + button.dataset.itemName + '" as fulfilled?';
            if (window.confirm(message)) {
                window.tm.submitForm(button.dataset.fulfillForm);
            }
        });
    });
});
