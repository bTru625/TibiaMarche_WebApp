// DEVELOPER FEATURE ONLY (navbar guest / user / admin switcher).
// TODO(DEV ONLY - DELETE BEFORE RELEASE): delete this file with the rest of the dev role switcher
// (see Controllers/DevRoleController.cs for the full list).
// Click listener: pick the role, then post the form. The server stores it in a cookie and reloads the page.
document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('dev-role-form');
    const value = document.getElementById('dev-role-value');
    if (!form || !value) { return; }

    form.querySelectorAll('[data-dev-role]').forEach(function (button) {
        button.addEventListener('click', function () {
            value.value = button.dataset.devRole;
            form.requestSubmit();
        });
    });
});
