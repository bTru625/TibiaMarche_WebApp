// Admin item list (/Admin/Items): click listeners for Add item, Edit, Hold/Release, Delete, the row
// expand arrow and Update Price. Buttons never submit by themselves; each handler validates first.
document.addEventListener('DOMContentLoaded', function () {

    // Add item.
    // TODO(J - S3 "[Web] Admin: Add Item"): navigate to the Add Item page. No page exists yet.
    const addItem = document.getElementById('add-item-btn');
    if (addItem) {
        addItem.addEventListener('click', function () {
            window.tm.comingSoon('Add item', 'sprint 3');
        });
    }

    // Edit: open the edit page (/Admin/ItemEdit/{id}).
    document.querySelectorAll('[data-edit-url]').forEach(function (button) {
        button.addEventListener('click', function () {
            window.location.href = button.dataset.editUrl;
        });
    });

    // Hold / Release: the server checks the status again before changing it.
    document.querySelectorAll('[data-submit-form]').forEach(function (button) {
        button.addEventListener('click', function () {
            window.tm.submitForm(button.dataset.submitForm);
        });
    });

    // Delete: always ask first.
    document.querySelectorAll('[data-delete-form]').forEach(function (button) {
        button.addEventListener('click', function () {
            const name = button.dataset.itemName || 'this item';
            if (window.confirm('Delete "' + name + '"? This cannot be undone.')) {
                window.tm.submitForm(button.dataset.deleteForm);
            }
        });
    });

    // Expand arrow: show or hide the gold paid / break-even / new price row.
    document.querySelectorAll('[data-expand]').forEach(function (button) {
        button.addEventListener('click', function () {
            const row = document.getElementById(button.dataset.expand);
            if (!row) { return; }
            const opening = row.hidden;
            row.hidden = !opening;
            button.setAttribute('aria-expanded', String(opening));
            button.innerHTML = opening ? '&#9652;' : '&#9662;';   // up / down arrow
        });
    });

    // Update Price: check the number first; the server checks the status again.
    document.querySelectorAll('[data-price-form]').forEach(function (button) {
        const input = document.getElementById(button.dataset.priceInput);
        function submitPrice() {
            const price = parseFloat(input.value);
            if (!(price > 0)) {
                window.tm.showNotice('Enter a new price greater than 0.', 'danger');
                input.focus();
                return;
            }
            window.tm.submitForm(button.dataset.priceForm);
        }
        button.addEventListener('click', submitPrice);
        input.addEventListener('keydown', function (event) {
            if (event.key === 'Enter') { event.preventDefault(); submitPrice(); }
        });
    });
});
