/**
 * My Daily Expense Tracker — Main JavaScript
 * Handles: theme toggle, sidebar, notifications, delete confirms, chart helpers
 */

(function () {
    'use strict';

    /* ── Theme Management ──────────────────────────────────────────────────── */
    const themeKey = 'mdet-theme';

    /* ── Chart.js Theme Defaults & Dynamic Adapter ──────────────────────────── */
    function initChartDefaults() {
        if (typeof Chart === 'undefined') return;
        const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
        const isDark = currentTheme === 'dark';
        const textColor = isDark ? '#94A3B8' : '#475569';
        const gridColor = isDark ? 'rgba(255, 255, 255, 0.08)' : 'rgba(0, 0, 0, 0.06)';

        try {
            if (Chart.defaults) {
                Chart.defaults.color = textColor;
                Chart.defaults.borderColor = gridColor;
            }

            if (Chart.instances) {
                Object.values(Chart.instances).forEach(chart => {
                    try {
                        if (chart.options && chart.options.scales) {
                            Object.values(chart.options.scales).forEach(scale => {
                                if (scale.grid) scale.grid.color = gridColor;
                                if (scale.ticks) scale.ticks.color = textColor;
                            });
                        }
                        if (chart.options && chart.options.plugins && chart.options.plugins.legend && chart.options.plugins.legend.labels) {
                            chart.options.plugins.legend.labels.color = textColor;
                        }
                        chart.update('none');
                    } catch (_) {}
                });
            }
        } catch (_) {}
    }

    function applyTheme(theme) {
        const targetTheme = (theme === 'dark') ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', targetTheme);
        try {
            localStorage.setItem(themeKey, targetTheme);
        } catch (_) {}

        // Update theme toggle icons and tooltips
        const icons = document.querySelectorAll('#theme-icon, .theme-icon');
        icons.forEach(icon => {
            icon.className = targetTheme === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-fill';
        });

        const toggleBtns = document.querySelectorAll('#theme-toggle, [data-action="toggle-theme"]');
        toggleBtns.forEach(btn => {
            const label = targetTheme === 'dark' ? 'Switch to Day Mode' : 'Switch to Night Mode';
            btn.title = label;
            btn.setAttribute('aria-label', label);
        });

        // Sync profile select dropdown if present
        const select = document.getElementById('theme-select');
        if (select && select.value !== targetTheme) {
            select.value = targetTheme;
        }

        // Sync hidden input
        const userThemeInput = document.getElementById('user-theme');
        if (userThemeInput) {
            userThemeInput.value = targetTheme;
        }

        initChartDefaults();
    }

    function initTheme() {
        let initialTheme = 'light';
        try {
            const local = localStorage.getItem(themeKey);
            const serverTheme = document.getElementById('user-theme')?.value;
            if (local === 'dark' || local === 'light') {
                initialTheme = local;
            } else if (serverTheme === 'dark' || serverTheme === 'light') {
                initialTheme = serverTheme;
            } else if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                initialTheme = 'dark';
            }
        } catch (_) {}
        applyTheme(initialTheme);
    }

    function toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme') || 'light';
        const next = current === 'dark' ? 'light' : 'dark';
        applyTheme(next);

        // Sync with server if authenticated
        try {
            const token = getAntiForgeryToken();
            const body = new URLSearchParams();
            body.append('theme', next);
            if (token) body.append('__RequestVerificationToken', token);

            fetch('/Profile/SetTheme', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': token
                },
                body: body.toString()
            }).catch(() => {});
        } catch (_) {}
    }

    /* ── Sidebar Toggle ────────────────────────────────────────────────────── */
    function initSidebar() {
        const sidebar  = document.getElementById('sidebar');
        const overlay  = document.getElementById('sidebar-overlay');
        const toggle   = document.getElementById('sidebar-toggle');
        if (!sidebar) return;

        function openSidebar() {
            sidebar.classList.add('open');
            overlay?.classList.add('active');
            document.body.style.overflow = 'hidden';
        }

        function closeSidebar() {
            sidebar.classList.remove('open');
            overlay?.classList.remove('active');
            document.body.style.overflow = '';
        }

        toggle?.addEventListener('click', () => {
            sidebar.classList.contains('open') ? closeSidebar() : openSidebar();
        });
        overlay?.addEventListener('click', closeSidebar);
        window.addEventListener('resize', () => {
            if (window.innerWidth >= 992) closeSidebar();
        });
    }

    /* ── Auto-dismiss Alerts ───────────────────────────────────────────────── */
    function initAlerts() {
        document.querySelectorAll('.alert-auto-dismiss').forEach(alert => {
            setTimeout(() => {
                alert.style.transition = 'opacity .5s ease';
                alert.style.opacity = '0';
                setTimeout(() => alert.remove(), 500);
            }, 4000);
        });
    }

    /* ── Delete Confirmation Modal ─────────────────────────────────────────── */
    function initDeleteConfirm() {
        const modal = document.getElementById('deleteModal');
        if (!modal) return;
        let targetForm = null;

        document.querySelectorAll('[data-delete-form]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                targetForm = document.getElementById(btn.getAttribute('data-delete-form'));
                const label = btn.getAttribute('data-delete-label') || 'this item';
                const msgEl = document.getElementById('delete-confirm-msg');
                if (msgEl) msgEl.textContent = 'Are you sure you want to delete "' + label + '"? This action cannot be undone.';
                new bootstrap.Modal(modal).show();
            });
        });

        document.getElementById('confirm-delete-btn')?.addEventListener('click', () => {
            if (targetForm) targetForm.submit();
        });
    }

    /* ── AJAX Category Refresh ─────────────────────────────────────────────── */
    function initCategoryRefresh() {
        const typeSelect = document.getElementById('Type');
        const catSelect  = document.getElementById('CategoryId');
        if (!typeSelect || !catSelect) return;

        typeSelect.addEventListener('change', async function () {
            const type = this.value;
            try {
                const res  = await fetch('/Transactions/GetCategoriesByType?type=' + type);
                const data = await res.json();
                catSelect.innerHTML = '<option value="">Select Category</option>';
                data.forEach(c => {
                    const opt = document.createElement('option');
                    opt.value = c.value;
                    opt.text  = c.text;
                    catSelect.appendChild(opt);
                });
            } catch (err) {
                console.error('Failed to load categories:', err);
            }
        });
    }

    /* ── Notifications Dropdown ────────────────────────────────────────────── */
    function initNotifications() {
        const btn = document.getElementById('notifications-btn');
        if (!btn) return;

        btn.addEventListener('click', async () => {
            try {
                const res  = await fetch('/Notifications/GetUnread');
                const data = await res.json();
                const list = document.getElementById('notification-list');
                const badge = document.getElementById('notification-badge');
                if (badge) {
                    if (data.length > 0) {
                        badge.textContent = data.length;
                        badge.classList.remove('d-none');
                    } else {
                        badge.classList.add('d-none');
                    }
                }
                if (!list) return;

                if (data.length === 0) {
                    list.innerHTML = '<div class="text-center p-4 text-muted"><i class="bi bi-bell-slash fs-3 d-block mb-2"></i>No new notifications</div>';
                } else {
                    list.innerHTML = data.map(n =>
                        '<div class="notification-item d-flex align-items-start justify-content-between p-2 border-bottom border-subtle-custom">' +
                        '<div class="flex-grow-1 pe-2">' +
                        '<p class="mb-0 small fw-500">' + escapeHtml(n.message) + '</p>' +
                        '<span class="text-muted" style="font-size:11px">' + n.createdDate + '</span>' +
                        '</div>' +
                        '<button type="button" class="btn btn-sm btn-link p-0 text-muted-custom mark-read-btn" data-id="' + n.notificationId + '" title="Mark as read">' +
                        '<i class="bi bi-check2"></i>' +
                        '</button>' +
                        '</div>'
                    ).join('');

                    list.querySelectorAll('.mark-read-btn').forEach(b => {
                        b.addEventListener('click', async (e) => {
                            e.stopPropagation();
                            const notifId = b.getAttribute('data-id');
                            try {
                                const fd = new FormData();
                                fd.append('id', notifId);
                                fd.append('__RequestVerificationToken', getAntiForgeryToken());
                                await fetch('/Notifications/MarkRead', {
                                    method: 'POST',
                                    body: fd
                                });
                                b.closest('.notification-item')?.remove();
                                const currentCount = list.querySelectorAll('.notification-item').length;
                                if (badge) {
                                    if (currentCount > 0) {
                                        badge.textContent = currentCount;
                                    } else {
                                        badge.classList.add('d-none');
                                        list.innerHTML = '<div class="text-center p-4 text-muted"><i class="bi bi-bell-slash fs-3 d-block mb-2"></i>No new notifications</div>';
                                    }
                                }
                            } catch (err) {
                                console.error('Failed to mark read', err);
                            }
                        });
                    });
                }
            } catch (err) {
                console.error('Failed to load notifications:', err);
            }
        });
    }

    /* ── Utilities ─────────────────────────────────────────────────────────── */
    function escapeHtml(str) {
        const d = document.createElement('div');
        d.appendChild(document.createTextNode(str || ''));
        return d.innerHTML;
    }

    function getAntiForgeryToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        if (el && el.value) return el.value;
        const meta = document.querySelector('meta[name="csrf-token"]');
        return meta ? meta.getAttribute('content') || '' : '';
    }

    /* ── Global setType for transaction form toggle ───────────────────────── */
    window.setType = function(type) {
        const typeHidden = document.getElementById('TypeHidden');
        if (typeHidden) typeHidden.value = type;
        const btnExp = document.getElementById('btn-expense');
        const btnInc = document.getElementById('btn-income');

        if (type === 'Expense') {
            if (btnExp) btnExp.className = 'btn flex-grow-1 py-2 fw-600 btn-expense';
            if (btnInc) btnInc.className = 'btn flex-grow-1 py-2 fw-600 btn-outline-secondary';
        } else {
            if (btnInc) btnInc.className = 'btn flex-grow-1 py-2 fw-600 btn-income';
            if (btnExp) btnExp.className = 'btn flex-grow-1 py-2 fw-600 btn-outline-secondary';
        }

        fetch('/Transactions/GetCategoriesByType?type=' + encodeURIComponent(type))
            .then(r => r.json())
            .then(data => {
                const sel = document.getElementById('CategoryId');
                if (!sel) return;
                const cur = sel.value;
                sel.innerHTML = '<option value="">Select Category</option>';
                data.forEach(c => {
                    const opt = document.createElement('option');
                    opt.value = c.value;
                    opt.text = c.text;
                    if (c.value === cur) opt.selected = true;
                    sel.appendChild(opt);
                });
            })
            .catch(err => console.error('Failed to update categories:', err));
    };

    /* ── Init ──────────────────────────────────────────────────────────────── */
    document.addEventListener('DOMContentLoaded', () => {
        initTheme();
        initSidebar();
        initAlerts();
        initDeleteConfirm();
        initCategoryRefresh();
        initNotifications();
        initChartDefaults();

        // Delegated listener for all theme toggles (prevents missed listeners and handles dynamically added toggles)
        document.addEventListener('click', (e) => {
            const btn = e.target.closest('#theme-toggle, [data-action="toggle-theme"]');
            if (btn) {
                e.preventDefault();
                toggleTheme();
            }
        });

        // Global shortcut Ctrl+K / Cmd+K to open AI Assistant
        document.addEventListener('keydown', (e) => {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
                e.preventDefault();
                if (typeof window.openAiChat === 'function') {
                    window.openAiChat();
                }
            }
        });
    });

    window.MDET = { getAntiForgeryToken, escapeHtml, applyTheme, toggleTheme, initChartDefaults };
})();
