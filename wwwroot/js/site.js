// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ===== Shared helpers (used by the page scripts: admin-*.js, request-item.js, account.js) =====
window.tm = window.tm || {};

// Shows a short message at the top of the page (same look as the server-side banners).
// kind: "info" (default), "success" or "danger".
window.tm.showNotice = function (message, kind) {
    const area = document.getElementById('tm-notice-area');
    if (!area) { return; }
    const box = document.createElement('div');
    box.className = 'tm-alert' + (kind === 'success' ? ' tm-alert-success' : kind === 'danger' ? ' tm-alert-danger' : '');
    box.setAttribute('role', 'status');
    box.textContent = message;          // textContent, never innerHTML: the text may contain item names
    area.replaceChildren(box);
    box.scrollIntoView({ block: 'nearest' });
};

// For buttons whose feature is built in a later sprint.
window.tm.comingSoon = function (what, sprint) {
    window.tm.showNotice(what + ' is coming in ' + sprint + '.');
};

// Submits a form after the page script has validated it (requestSubmit keeps the antiforgery field).
window.tm.submitForm = function (formId) {
    const form = document.getElementById(formId);
    if (form) { form.requestSubmit(); }
};
