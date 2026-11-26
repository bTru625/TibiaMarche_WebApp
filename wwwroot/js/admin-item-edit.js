// Edit Item (/Admin/ItemEdit/{id}): click listeners for Check, Save, Cancel and the hold button, plus the
// live break-even / profit calculator. The server recalculates nothing: it only saves Level, Price and
// Gold paid. The calculator is a helper for choosing the price.
document.addEventListener('DOMContentLoaded', function () {
    const calculator = document.getElementById('calculator');
    const goldInput = document.getElementById('gold-input');
    const profitInput = document.getElementById('profit-input');
    const breakEvenOut = document.getElementById('break-even');
    const profitOut = document.getElementById('profit-price');
    const priceInput = document.getElementById('price-input');
    const levelInput = document.getElementById('level-input');

    // CAD per 1,000,000 gold (see GoldCalculator.cs: change both together).
    const rate = parseFloat(calculator.dataset.rate);
    const goldUnit = parseFloat(calculator.dataset.goldUnit) || 1000000;
    const haveRate = rate > 0;

    function money(value) { return '$ ' + value.toFixed(2) + ' [CAD]'; }

    // Returns the break-even price, or null when it cannot be worked out.
    function calculate() {
        const gold = parseFloat(goldInput.value);
        if (!haveRate || !(gold >= 0)) {
            breakEvenOut.textContent = 'n/a';
            profitOut.textContent = 'n/a';
            return null;
        }
        const breakEven = gold / goldUnit * rate;
        const profit = parseFloat(profitInput.value);
        breakEvenOut.textContent = money(breakEven);
        profitOut.textContent = profit >= 0 ? money(breakEven * (1 + profit / 100)) : 'n/a';
        return { breakEven: breakEven, withProfit: profit >= 0 ? breakEven * (1 + profit / 100) : null };
    }

    goldInput.addEventListener('input', calculate);
    profitInput.addEventListener('input', calculate);
    calculate();

    // Check: work out the prices and offer the profit price as the new price.
    document.getElementById('check-btn').addEventListener('click', function () {
        const result = calculate();
        if (!result) {
            window.tm.showNotice(haveRate
                ? 'Enter the gold paid first.'
                : 'The gold rate is not set yet, so prices cannot be calculated.', 'danger');
            return;
        }
        if (result.withProfit !== null && !priceInput.disabled) {
            priceInput.value = result.withProfit.toFixed(2);
            window.tm.showNotice('Price set to ' + money(result.withProfit) + '. Press Save to keep it.', 'success');
        }
    });

    // Save: check the three fields, then post.
    document.getElementById('save-btn').addEventListener('click', function () {
        const level = parseInt(levelInput.value, 10);
        const price = parseFloat(priceInput.value);
        const gold = parseFloat(goldInput.value);
        if (!(level >= 1 && level <= 5000)) {
            window.tm.showNotice('Level must be between 1 and 5000.', 'danger');
            levelInput.focus();
        } else if (!(price > 0)) {
            window.tm.showNotice('Price must be greater than 0.', 'danger');
            priceInput.focus();
        } else if (!(gold >= 0)) {
            window.tm.showNotice('Gold paid cannot be negative.', 'danger');
            goldInput.focus();
        } else {
            window.tm.submitForm('edit-form');
        }
    });

    // Cancel: back to the list without saving.
    const cancel = document.getElementById('cancel-btn');
    cancel.addEventListener('click', function () {
        window.location.href = cancel.dataset.url;
    });

    // Hold / Release: the server checks the status again before changing it.
    const hold = document.getElementById('hold-btn');
    if (hold) {
        hold.addEventListener('click', function () {
            window.tm.submitForm(hold.dataset.submitForm);
        });
    }
});
