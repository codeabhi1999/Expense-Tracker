/**
 * AI Financial Assistant & Natural Language Quick-Add
 */
(function () {
    'use strict';

    // ── Helper to get AntiForgeryToken ─────────────────────────────
    function getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
               document.querySelector('meta[name="csrf-token"]')?.getAttribute('content') || '';
    }

    // ── 1. Spotlight Natural Language Quick-Add ────────────────────
    const spotlightInput = document.getElementById('ai-spotlight-input');
    const spotlightBtn   = document.getElementById('ai-spotlight-btn');
    const previewPanel   = document.getElementById('ai-preview-panel');
    const previewChips   = document.getElementById('ai-preview-chips');
    const confirmBtn     = document.getElementById('ai-confirm-btn');
    let currentParsedData = null;

    if (spotlightInput) {
        spotlightInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                triggerParse();
            }
        });
    }

    if (spotlightBtn) {
        spotlightBtn.addEventListener('click', function () {
            triggerParse();
        });
    }

    async function triggerParse() {
        const text = spotlightInput?.value?.trim();
        if (!text) return;

        spotlightBtn.disabled = true;
        spotlightBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status"></span> Parsing...';

        try {
            const resp = await fetch('/AiInsights/ParseQuickAdd', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({ text: text })
            });

            const data = await resp.json();
            if (data.success) {
                currentParsedData = data;
                renderPreview(data);
            } else {
                showToast(data.errorMessage || 'Could not understand transaction. Try specifying an amount like "₹500".', 'warning');
            }
        } catch (err) {
            console.error(err);
            showToast('Failed to parse with AI.', 'danger');
        } finally {
            spotlightBtn.disabled = false;
            spotlightBtn.innerHTML = '<i class="bi bi-stars"></i> Smart Log';
        }
    }

    function renderPreview(data) {
        if (!previewPanel || !previewChips) return;

        const isExpense = data.type === 'Expense';
        const typeClass = isExpense ? 'type-expense' : 'type-income';
        const typeIcon = isExpense ? 'bi-dash-circle-fill' : 'bi-plus-circle-fill';

        previewChips.innerHTML = `
            <span class="ai-chip amount">
                <i class="bi bi-currency-rupee"></i> ${Number(data.amount).toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </span>
            <span class="ai-chip ${typeClass}">
                <i class="bi ${typeIcon}"></i> ${data.type}
            </span>
            <span class="ai-chip">
                <i class="bi ${data.categoryIcon || 'bi-tag'}"></i> ${data.categoryName}
            </span>
            ${data.paymentMethodName ? `
            <span class="ai-chip">
                <i class="bi bi-credit-card-2-front"></i> ${data.paymentMethodName}
            </span>` : ''}
            <span class="ai-chip" style="font-weight:400;color:var(--text-secondary)">
                <i class="bi bi-card-text"></i> "${data.description}"
            </span>
        `;

        previewPanel.classList.add('active');
    }

    if (confirmBtn) {
        confirmBtn.addEventListener('click', async function () {
            if (!currentParsedData) return;

            confirmBtn.disabled = true;
            confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Saving...';

            try {
                const resp = await fetch('/AiInsights/ConfirmQuickAdd', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify(currentParsedData)
                });

                const res = await resp.json();
                if (res.success) {
                    showToast(res.message, 'success');
                    if (spotlightInput) spotlightInput.value = '';
                    if (previewPanel) previewPanel.classList.remove('active');
                    currentParsedData = null;

                    // Reload page smoothly to reflect newly logged transaction
                    setTimeout(() => {
                        window.location.reload();
                    }, 800);
                } else {
                    showToast(res.message || 'Error saving transaction.', 'danger');
                }
            } catch (err) {
                console.error(err);
                showToast('Failed to save transaction.', 'danger');
            } finally {
                confirmBtn.disabled = false;
                confirmBtn.innerHTML = '<i class="bi bi-check-lg me-1"></i> Confirm & Log';
            }
        });
    }

    // ── 2. AI Copilot Chat Drawer ──────────────────────────────────
    const chatDrawer   = document.getElementById('aiChatDrawer');
    const chatMessages = document.getElementById('ai-chat-messages');
    const chatInput    = document.getElementById('ai-chat-input');
    const chatSendBtn  = document.getElementById('ai-chat-send-btn');

    window.openAiChat = function (initialPrompt) {
        if (!chatDrawer) return;
        const bsOffcanvas = bootstrap.Offcanvas.getOrCreateInstance(chatDrawer);
        bsOffcanvas.show();

        if (initialPrompt) {
            setTimeout(() => {
                sendAiMessage(initialPrompt);
            }, 300);
        }
    };

    if (chatInput) {
        chatInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                const msg = chatInput.value.trim();
                if (msg) sendAiMessage(msg);
            }
        });
    }

    if (chatSendBtn) {
        chatSendBtn.addEventListener('click', function () {
            const msg = chatInput?.value?.trim();
            if (msg) sendAiMessage(msg);
        });
    }

    // Bind prompt chips
    document.addEventListener('click', function (e) {
        const chip = e.target.closest('.ai-prompt-chip');
        if (chip) {
            e.preventDefault();
            const query = chip.getAttribute('data-prompt') || chip.innerText.trim();
            openAiChat(query);
        }
    });

    async function sendAiMessage(messageText) {
        if (!chatMessages) return;

        // Append user message
        const userMsgDiv = document.createElement('div');
        userMsgDiv.className = 'chat-msg user';
        userMsgDiv.innerHTML = `
            <div class="chat-avatar user"><i class="bi bi-person"></i></div>
            <div class="chat-bubble">${escapeHtml(messageText)}</div>
        `;
        chatMessages.appendChild(userMsgDiv);

        if (chatInput) chatInput.value = '';

        // Append typing indicator
        const typingDiv = document.createElement('div');
        typingDiv.className = 'chat-msg ai typing-msg';
        typingDiv.innerHTML = `
            <div class="chat-avatar ai"><i class="bi bi-stars"></i></div>
            <div class="chat-bubble">
                <div class="typing-indicator">
                    <div class="typing-dot"></div>
                    <div class="typing-dot"></div>
                    <div class="typing-dot"></div>
                </div>
            </div>
        `;
        chatMessages.appendChild(typingDiv);
        scrollChatToBottom();

        try {
            const resp = await fetch('/AiInsights/Ask', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({ message: messageText })
            });

            const data = await resp.json();
            typingDiv.remove();

            const aiMsgDiv = document.createElement('div');
            aiMsgDiv.className = 'chat-msg ai';
            aiMsgDiv.innerHTML = `
                <div class="chat-avatar ai"><i class="bi bi-stars"></i></div>
                <div class="chat-bubble">
                    ${formatAiMarkdown(data.answer)}
                    ${renderFollowUps(data.followUpSuggestions)}
                </div>
            `;
            chatMessages.appendChild(aiMsgDiv);
            scrollChatToBottom();
        } catch (err) {
            console.error(err);
            typingDiv.remove();

            const errorDiv = document.createElement('div');
            errorDiv.className = 'chat-msg ai';
            errorDiv.innerHTML = `
                <div class="chat-avatar ai"><i class="bi bi-stars"></i></div>
                <div class="chat-bubble text-danger">
                    Unable to reach the AI Assistant right now. Please try again.
                </div>
            `;
            chatMessages.appendChild(errorDiv);
            scrollChatToBottom();
        }
    }

    function renderFollowUps(suggestions) {
        if (!suggestions || !suggestions.length) return '';
        let chips = suggestions.map(s => `
            <button type="button" class="ai-prompt-chip mt-2" data-prompt="${escapeHtml(s)}">
                <i class="bi bi-chat-dots"></i> ${escapeHtml(s)}
            </button>
        `).join(' ');
        return `<div class="d-flex flex-wrap gap-1 mt-2 pt-2 border-top border-subtle-custom">${chips}</div>`;
    }

    function scrollChatToBottom() {
        if (chatMessages) {
            chatMessages.scrollTop = chatMessages.scrollHeight;
        }
    }

    function formatAiMarkdown(text) {
        if (!text) return '';
        let html = escapeHtml(text);
        // Bold: **text**
        html = html.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
        // Italic: *text*
        html = html.replace(/\*(.*?)\*/g, '<em>$1</em>');
        // Bullets: • or -
        html = html.replace(/\n•\s*(.*?)(?=\n|$)/g, '<div class="ms-2 mb-1">• $1</div>');
        // Newlines
        html = html.replace(/\n\n/g, '<br/><br/>').replace(/\n/g, '<br/>');
        return html;
    }

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function showToast(message, type) {
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${type || 'primary'} alert-dismissible position-fixed bottom-0 end-0 m-4 shadow-lg animate-fade-in`;
        alertDiv.style.zIndex = '9999';
        alertDiv.style.borderRadius = 'var(--radius-md)';
        alertDiv.innerHTML = `
            <div class="d-flex align-items-center gap-2">
                <i class="bi bi-stars fs-5"></i>
                <span>${message}</span>
                <button type="button" class="btn-close ms-2" data-bs-dismiss="alert"></button>
            </div>
        `;
        document.body.appendChild(alertDiv);
        setTimeout(() => alertDiv.remove(), 4000);
    }
})();
