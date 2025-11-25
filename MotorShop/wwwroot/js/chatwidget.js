document.addEventListener('DOMContentLoaded', function () {
    const els = {
        widget: document.getElementById('chatWidget'),
        toggle: document.getElementById('chatToggle'),
        close: document.getElementById('closeChat'),
        body: document.getElementById('chatBody'),
        input: document.getElementById('chatInput'),
        sendBtn: document.getElementById('sendBtn'),
        threadId: document.getElementById('threadIdInput'),
        customerId: document.getElementById('customerIdInput'),
        uploadInput: document.getElementById('imgUpload'),
        badge: document.getElementById('notiBadge')
    };

    let currentThreadId = parseInt(els.threadId.value || '0');
    const customerId = els.customerId.value;

    // Time Format
    function formatTime(dateInput) {
        if (!dateInput) return '';
        // Đảm bảo là UTC để convert đúng giờ VN
        if (!dateInput.endsWith('Z')) dateInput += 'Z';

        const date = new Date(dateInput);
        const now = new Date();

        // Lấy giờ:phút
        const timeStr = date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

        // Lấy ngày/tháng
        const dateStr = date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });

        // Logic hiển thị thông minh:
        // Nếu là tin nhắn hôm nay -> Chỉ hiện giờ (10:30)
        // Nếu là tin nhắn ngày khác -> Hiện giờ + ngày (10:30 20/11)
        if (date.toDateString() === now.toDateString()) {
            return timeStr;
        } else {
            return `${timeStr} ${dateStr}`;
        }
    }

    document.querySelectorAll('.local-time').forEach(el => {
        const utc = el.getAttribute('data-utc');
        if (utc) el.textContent = formatTime(utc);
    });

    if (els.body) els.body.scrollTop = els.body.scrollHeight;

    // UI Handlers
    els.toggle?.addEventListener('click', () => {
        els.widget.classList.toggle('open');
        if (els.widget.classList.contains('open')) {
            els.body.scrollTop = els.body.scrollHeight;
            if (els.badge) els.badge.style.display = 'none';
            els.input.focus();
        }
        if (connection.state === signalR.HubConnectionState.Disconnected) startSignalR();
    });
    els.close?.addEventListener('click', () => els.widget.classList.remove('open'));

    els.input?.addEventListener('input', () => {
        els.sendBtn.disabled = els.input.value.trim() === '';
        els.input.style.height = 'auto';
        els.input.style.height = Math.min(els.input.scrollHeight, 100) + 'px';
    });

    // SignalR
    if (typeof signalR === 'undefined') return;
    const connection = new signalR.HubConnectionBuilder().withUrl('/chathub').withAutomaticReconnect().build();

    connection.on('ReceiveMessage', (payload) => {
        if (!payload || !payload.threadId) return;
        if (payload.threadId === currentThreadId || currentThreadId === 0) {
            appendMessage(!payload.isFromStaff, payload.content, payload.sentAt);
            if (!els.widget.classList.contains('open') && payload.isFromStaff) {
                if (els.badge) els.badge.style.display = 'block';
            }
        }
    });

    connection.on('UpdateThreadId', (newId) => {
        currentThreadId = newId;
        els.threadId.value = newId;
    });

    async function startSignalR() {
        try {
            await connection.start();
            if (currentThreadId > 0) await connection.invoke('JoinThreadGroup', currentThreadId);
        } catch (err) { console.error(err); }
    }
    startSignalR();

    // Send Text
    async function sendMessage() {
        const text = els.input.value.trim();
        if (!text || !customerId) {
            if (!customerId) alert("Vui lòng đăng nhập.");
            return;
        }
        els.sendBtn.disabled = true;
        const oldVal = els.input.value;
        els.input.value = ''; els.input.focus();
        try {
            await connection.invoke('SendCustomerMessage', currentThreadId, text);
        } catch (err) {
            els.input.value = oldVal;
        } finally { els.sendBtn.disabled = false; }
    }
    els.sendBtn?.addEventListener('click', sendMessage);
    els.input?.addEventListener('keydown', (e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(); } });

    // Send Images
    els.uploadInput?.addEventListener('change', async function () {
        if (this.files.length === 0 || !customerId) return;
        const files = Array.from(this.files);
        const urls = [];

        const tempDiv = document.createElement('div');
        tempDiv.className = 'msg-row me';
        tempDiv.innerHTML = `<div class="msg-bubble" style="background:#f2f2f2; color:#888">Đang gửi ảnh...</div>`;
        els.body.appendChild(tempDiv); els.body.scrollTop = els.body.scrollHeight;

        try {
            for (const file of files) {
                const fd = new FormData(); fd.append('file', file);
                const res = await fetch('/Chat/UploadImage', { method: 'POST', body: fd });
                if (res.ok) {
                    const data = await res.json();
                    if (data.url) urls.push(data.url);
                }
            }
            if (urls.length > 0) await connection.invoke('SendCustomerMessage', currentThreadId, urls.join(','));
        } catch (e) { console.error(e); }
        finally {
            tempDiv.remove();
            this.value = '';
        }
    });

    // Render Helper
    function appendMessage(isMe, content, timeIso) {
        if (!els.body) return;
        const div = document.createElement('div');
        div.className = `msg-row ${isMe ? 'me' : 'other'}`;

        let inner = '';
        // Avatar Shop
        if (!isMe) inner += `<div class="shop-logo" style="width:28px; height:28px; font-size:12px; flex-shrink:0; background:#555;"><i class="fa-solid fa-headset"></i></div>`;

        let msgContent = '';
        if (content.includes('/images/chat/')) {
            const imgs = content.split(',').filter(s => s.trim().startsWith('/images/chat/'));
            // Grid thông minh
            msgContent = `<div class="msg-bubble is-gallery">
                                <div class="gallery-grid" data-count="${imgs.length}">
                                    ${imgs.map(u => `<img src="${u.trim()}" onclick="window.open(this.src)" />`).join('')}
                                </div>
                              </div>`;
        } else {
            msgContent = `<div class="msg-bubble">${escapeHtml(content).replace(/\n/g, '<br>')}</div>`;
        }

        inner += `<div class="msg-content-wrap">
                        ${msgContent}
                        <span class="msg-time" style="text-align:${isMe ? 'right' : 'left'}; color:${isMe ? 'rgba(0,0,0,0.6)' : '#888'}">${formatTime(timeIso)}</span>
                      </div>`;

        div.innerHTML = inner;
        els.body.appendChild(div);
        els.body.scrollTop = els.body.scrollHeight;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
});