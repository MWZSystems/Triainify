(function () {
    'use strict';

    if (typeof Swal === 'undefined') {
        return;
    }

    // Same contextual palette as the AppStack admin theme (Outline alerts / Colored modals),
    // so SweetAlert2 popups match the rest of the app's look instead of using Swal2's defaults.
    var THEME_COLORS = {
        primary: '#3f80ea',
        secondary: '#495057',
        success: '#4bbf73',
        info: '#1f9bcf',
        warning: '#e5a54b',
        danger: '#d9534f'
    };

    function themeColorForIcon(icon) {
        switch (icon) {
            case 'success': return THEME_COLORS.success;
            case 'error': return THEME_COLORS.danger;
            case 'warning': return THEME_COLORS.warning;
            case 'info': return THEME_COLORS.info;
            case 'question': return THEME_COLORS.primary;
            default: return THEME_COLORS.primary;
        }
    }

    // Modals (confirmAndRun / swalAlert) use the AppStack "Default modal" look: white
    // background, split into 3 bordered sections like a Bootstrap modal-header/body/footer.
    // Only the icon + title (the "header" section) carry the contextual color, and the icon
    // sits inline with the title (small, same row) instead of stacked above it, so the popup
    // stays compact/rectangular instead of tall — height is never fixed, just no longer wasted
    // on the old full-width centered icon block.
    function styleThreeSectionModal(themeColor) {
        var icon = Swal.getIcon();
        var title = Swal.getTitle();

        if (icon && title && icon.parentNode) {
            var header = document.createElement('div');
            header.style.display = 'flex';
            header.style.alignItems = 'center';
            header.style.justifyContent = 'center';
            header.style.gap = '.6rem';
            header.style.borderBottom = '1px solid #e9ecef';
            header.style.padding = '1.25rem 1.6rem 1rem';
            header.style.margin = '0';

            icon.parentNode.insertBefore(header, icon);
            header.appendChild(icon);
            header.appendChild(title);

            icon.style.margin = '0';
            icon.style.fontSize = '0.45em';
            icon.style.flexShrink = '0';

            title.style.color = themeColor;
            title.style.margin = '0';
            title.style.padding = '0';
            title.style.borderBottom = 'none';
            title.style.flex = '0 1 auto';
        }

        var htmlContainer = Swal.getHtmlContainer();
        if (htmlContainer) {
            htmlContainer.style.color = '#495057';
        }

        var actions = Swal.getActions();
        if (actions) {
            actions.style.borderTop = '1px solid #e9ecef';
            actions.style.paddingTop = '1.25rem';
            actions.style.marginTop = '1rem';
            actions.style.width = '100%';
        }

        var confirmBtn = Swal.getConfirmButton();
        if (confirmBtn) {
            confirmBtn.style.backgroundColor = themeColor;
            confirmBtn.style.color = '#fff';
            confirmBtn.style.boxShadow = 'none';
        }
        var cancelBtn = Swal.getCancelButton();
        if (cancelBtn) {
            cancelBtn.style.backgroundColor = THEME_COLORS.secondary;
            cancelBtn.style.color = '#fff';
            cancelBtn.style.boxShadow = 'none';
        }
    }

    function confirmAndRun(message, title, icon, onConfirm) {
        var resolvedIcon = icon || 'warning';
        var themeColor = themeColorForIcon(resolvedIcon);

        Swal.fire({
            title: title || 'Are you sure?',
            text: message,
            icon: resolvedIcon,
            iconColor: themeColor,
            background: '#fff',
            showCancelButton: true,
            showCloseButton: true,
            confirmButtonText: 'Yes',
            cancelButtonText: 'Cancel',
            reverseButtons: true,
            didOpen: function () {
                styleThreeSectionModal(themeColor);
            }
        }).then(function (result) {
            if (result.isConfirmed) {
                onConfirm();
            }
        });
    }

    // Forms marked with class="js-confirm-submit" data-confirm-message="..." get a
    // SweetAlert2 confirmation instead of a native confirm() before actually submitting.
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form.classList || !form.classList.contains('js-confirm-submit')) {
            return;
        }
        if (form.dataset.confirmed === 'true') {
            return;
        }
        e.preventDefault();
        var message = form.getAttribute('data-confirm-message') || 'Are you sure?';
        var title = form.getAttribute('data-confirm-title') || undefined;
        var icon = form.getAttribute('data-confirm-icon') || 'warning';
        confirmAndRun(message, title, icon, function () {
            form.dataset.confirmed = 'true';
            HTMLFormElement.prototype.submit.call(form);
        });
    }, true);

    // Links marked with class="js-confirm-link" data-confirm-message="..." get a
    // SweetAlert2 confirmation instead of a native confirm() before navigating.
    document.addEventListener('click', function (e) {
        var link = e.target.closest ? e.target.closest('.js-confirm-link') : null;
        if (!link) {
            return;
        }
        e.preventDefault();
        var message = link.getAttribute('data-confirm-message') || 'Are you sure?';
        var title = link.getAttribute('data-confirm-title') || undefined;
        var icon = link.getAttribute('data-confirm-icon') || 'warning';
        confirmAndRun(message, title, icon, function () {
            window.location.href = link.href;
        });
    }, true);

    // Toasts use the AppStack "Outline alert with contextual icon" look instead: white
    // background, a colored icon and a matching colored left accent border.
    window.swalToast = function (message, type) {
        var iconMap = { success: 'success', danger: 'error', error: 'error', warning: 'warning', info: 'info', default: 'info' };
        var icon = iconMap[type] || 'info';
        var accentColor = themeColorForIcon(icon);

        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: icon,
            iconColor: accentColor,
            title: message,
            background: '#fff',
            color: '#495057',
            showConfirmButton: false,
            showCloseButton: true,
            timer: type === 'danger' || type === 'error' ? 7000 : 5000,
            timerProgressBar: true,
            didOpen: function (toastEl) {
                toastEl.style.borderLeft = '4px solid ' + accentColor;
                toastEl.style.marginTop = '70px';
            }
        });
    };

    // Simple one-button alerts are still a "modal", so they get the same 3-section look.
    window.swalAlert = function (message, icon, title) {
        var resolvedIcon = icon || 'warning';
        var themeColor = themeColorForIcon(resolvedIcon);

        Swal.fire({
            icon: resolvedIcon,
            iconColor: themeColor,
            title: title || undefined,
            text: message,
            background: '#fff',
            showCloseButton: true,
            confirmButtonText: 'OK',
            didOpen: function () {
                styleThreeSectionModal(themeColor);
            }
        });
    };
})();
