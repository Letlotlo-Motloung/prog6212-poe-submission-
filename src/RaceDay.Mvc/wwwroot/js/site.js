// Small progressive enhancements for the RaceDay MVC front-end.
(function () {
    // Auto-dismiss alerts after a while.
    document.querySelectorAll('.alert').forEach(function (el) {
        setTimeout(function () {
            el.style.transition = 'opacity .4s ease, transform .4s ease';
            el.style.opacity = '0';
            el.style.transform = 'translateY(-6px)';
            setTimeout(function () { el.remove(); }, 420);
        }, 6000);
    });

    // Live preview when an image file is chosen.
    document.querySelectorAll('input[type="file"][data-preview]').forEach(function (input) {
        input.addEventListener('change', function () {
            var target = document.querySelector(input.getAttribute('data-preview'));
            if (!target || !input.files || !input.files[0]) return;
            var reader = new FileReader();
            reader.onload = function (e) {
                if (target.tagName === 'IMG') { target.src = e.target.result; target.classList.remove('hidden'); }
                else { target.style.backgroundImage = 'url(' + e.target.result + ')'; }
            };
            reader.readAsDataURL(input.files[0]);
        });
    });

    // Confirm destructive actions.
    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (!window.confirm(form.getAttribute('data-confirm'))) e.preventDefault();
        });
    });

    // Reveal the inline result editor for a row.
    document.querySelectorAll('[data-toggle-target]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var el = document.querySelector(btn.getAttribute('data-toggle-target'));
            if (el) el.classList.toggle('hidden');
        });
    });
})();
