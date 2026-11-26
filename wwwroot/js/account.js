// Account page (/Account): click listeners for the currency list, Set password, Change email and Log out.
document.addEventListener('DOMContentLoaded', function () {

    // Currency: saves as soon as another one is chosen.
    const currency = document.getElementById('currency-select');
    if (currency) {
        currency.addEventListener('change', function () {
            window.tm.submitForm('currency-form');
        });
    }

    // Set password: same rules as the server (6+ characters, both new passwords match).
    const passwordSave = document.getElementById('password-save-btn');
    passwordSave.addEventListener('click', function () {
        const error = document.getElementById('password-error');
        const oldPassword = document.getElementById('old-password').value;
        const newPassword = document.getElementById('new-password').value;
        const confirmPassword = document.getElementById('confirm-password').value;

        if (!oldPassword || !newPassword) {
            error.textContent = 'Enter your current password and a new password.';
        } else if (newPassword.length < 6) {
            error.textContent = 'The new password must be at least 6 characters.';
        } else if (newPassword !== confirmPassword) {
            error.textContent = 'The new password and its confirmation do not match.';
        } else {
            error.textContent = '';
            window.tm.submitForm('password-form');
        }
    });

    // Change email.
    const emailSave = document.getElementById('email-save-btn');
    emailSave.addEventListener('click', function () {
        const error = document.getElementById('email-error');
        const password = document.getElementById('email-password').value;
        const newEmail = document.getElementById('new-email');

        if (!password || !newEmail.value.trim()) {
            error.textContent = 'Enter your password and the new email address.';
        } else if (!newEmail.checkValidity()) {
            error.textContent = 'Enter a valid email address.';
        } else {
            error.textContent = '';
            window.tm.submitForm('email-form');
        }
    });

    // Log out.
    // TODO(J - S2 "[Web] Auth"): once AuthController.Logout exists, replace the notice below with
    //   window.tm.submitForm('logout-form');
    // and point the form at that action (see Views/Account/Index.cshtml). Until then there is nothing to log out of.
    document.getElementById('logout-btn').addEventListener('click', function () {
        window.tm.showNotice('Log out is coming with the login feature (sprint 2, J).');
    });
});
