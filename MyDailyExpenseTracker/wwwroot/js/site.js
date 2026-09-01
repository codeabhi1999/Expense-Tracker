/**
 * My Daily Expense Tracker — Main JavaScript
 * Handles: theme toggle, sidebar, notifications, delete confirms, chart helpers
 */

(function () {
    'use strict';

    /* ── Theme Management ──────────────────────────────────────────────────── */
    const themeKey = 'mdet-theme';

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(themeKey, theme);
        const icon = document.getElementById('theme-icon');
        if (icon) {
            icon.className = theme === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-fill';
        }
    }

    function initTheme() {
        const serverTheme = document.getElementById('user-theme')?.value;
        const saved = serverTheme || localStorage.getItem(themeKey) ||
            (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
        applyTheme(saved);
    }

    function toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme') || 'light';
        const next = current === 'dark' ? 'light' : 'dark';
        applyTheme(next);
        fetch('/Profile/SetTheme', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: 'theme=' + next
        }).catch(() => {});
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
                if (!list) return;

                if (data.length === 0) {
                    list.innerHTML = '<div class="text-center p-4 text-muted"><i class="bi bi-bell-slash fs-3 d-block mb-2"></i>No new notifications</div>';
                } else {
                    list.innerHTML = data.map(n =>
                        '<div class="notification-item">' +
                        '<div class="flex-grow-1">' +
                        '<p class="mb-0 small">' + escapeHtml(n.message) + '</p>' +
                        '<span class="text-muted" style="font-size:11px">' + n.createdDate + '</span>' +
                        '</div></div>'
                    ).join('');
                }
            } catch (err) {
                console.error('Failed to load notifications:', err);
            }
        });
    }

    /* ── Chart.js Global Defaults ──────────────────────────────────────────── */
    function initChartDefaults() {
        if (typeof Chart === 'undefined') return;
        const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
        Chart.defaults.color             = isDark ? '#94A3B8' : '#64748B';
        Chart.defaults.borderColor       = isDark ? 'rgba(255,255,255,.06)' : 'rgba(0,0,0,.06)';
        Chart.defaults.font.family       = "'Inter', sans-serif";
        Chart.defaults.font.size         = 12;
    }

    /* ── Utilities ─────────────────────────────────────────────────────────── */
    function escapeHtml(str) {
        const d = document.createElement('div');
        d.appendChild(document.createTextNode(str));
        return d.innerHTML;
    }

    function getAntiForgeryToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    /* ── Init ──────────────────────────────────────────────────────────────── */
    document.addEventListener('DOMContentLoaded', () => {
        initTheme();
        initSidebar();
        initAlerts();
        initDeleteConfirm();
        initCategoryRefresh();
        initNotifications();
        initChartDefaults();

        document.getElementById('theme-toggle')?.addEventListener('click', () => {
            toggleTheme();
            setTimeout(initChartDefaults, 100);
        });
    });

    window.MDET = { getAntiForgeryToken, escapeHtml, applyTheme };
})();
