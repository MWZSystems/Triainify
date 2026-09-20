(function () {
    'use strict';

    var overlay = document.getElementById('pageLoadingOverlay');
    if (!overlay) {
        return;
    }

    function show() { overlay.classList.add('active'); }
    function hide() { overlay.classList.remove('active'); }

    // Full-page navigations: show right when the browser starts leaving this page,
    // hide once the new (or restored) page has finished loading.
    window.addEventListener('beforeunload', show);
    window.addEventListener('pageshow', hide);

    // AJAX calls (DataTables, Select2 remote data, form posts via fetch/jQuery, etc.).
    if (window.jQuery) {
        window.jQuery(document).on('ajaxStart', show).on('ajaxStop', hide);
    }
})();
