// Request Item (/Request): click listeners for the two "notify me" options, the item box (preview card)
// and Submit. A guest who presses Submit is sent to Login by the server and returns to this form.
document.addEventListener('DOMContentLoaded', function () {
    const itemInput = document.getElementById('item-input');
    const serverSelect = document.getElementById('server-select');
    const useAccount = document.getElementById('notify-account');
    const useAlt = document.getElementById('notify-alt');
    const altEmail = document.getElementById('alt-email-input');
    const previewName = document.getElementById('item-preview-name');
    const previewImage = document.getElementById('item-preview-image');
    const submit = document.getElementById('request-submit-btn');

    // Notify me: the alternative email box is only active when its option is chosen.
    function syncEmailChoice() {
        altEmail.disabled = !useAlt.checked;
        if (useAlt.checked) { altEmail.focus(); }
    }
    useAccount.addEventListener('click', syncEmailChoice);
    useAlt.addEventListener('click', syncEmailChoice);

    // Item box: show the picture and name of the chosen item when it is one we know.
    function updatePreview() {
        const typed = itemInput.value.trim();
        previewName.textContent = typed || 'Your item';

        let img = '';
        document.querySelectorAll('#known-items option').forEach(function (option) {
            if (option.value.toLowerCase() === typed.toLowerCase()) { img = option.dataset.img || ''; }
        });

        previewImage.replaceChildren();
        // Only http(s) pictures are shown; the address comes from the database.
        if (/^https?:\/\//i.test(img)) {
            const picture = document.createElement('img');
            picture.src = img;
            picture.alt = typed;
            picture.className = 'tm-request-img';
            previewImage.appendChild(picture);
        } else {
            const placeholder = document.createElement('span');
            placeholder.className = 'tm-muted';
            placeholder.textContent = '[Image]';
            previewImage.appendChild(placeholder);
        }
    }
    itemInput.addEventListener('input', updatePreview);
    itemInput.addEventListener('change', updatePreview);
    updatePreview();

    // Submit: check the form, then post. The server checks everything again.
    submit.addEventListener('click', function () {
        if (!itemInput.value.trim()) {
            window.tm.showNotice('Enter the item you are looking for.', 'danger');
            itemInput.focus();
        } else if (!serverSelect.value) {
            window.tm.showNotice('Choose a server.', 'danger');
            serverSelect.focus();
        } else if (useAlt.checked && !altEmail.checkValidity()) {
            window.tm.showNotice('Enter a valid email address.', 'danger');
            altEmail.focus();
        } else if (useAlt.checked && !altEmail.value.trim()) {
            window.tm.showNotice('Enter the email address to notify.', 'danger');
            altEmail.focus();
        } else {
            window.tm.submitForm('request-form');
        }
    });
});
