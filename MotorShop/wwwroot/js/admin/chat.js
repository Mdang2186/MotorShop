document.addEventListener('DOMContentLoaded', function () {
    const els = {
        body: document.getElementById('chatBody'),
        input: document.getElementById('chatInput'),
        sendBtn: document.getElementById('sendBtn'),
        uploadBtn: document.getElementById('imgUpload'),
        threadId: document.getElementById('activeThreadId'),
        avatar: document.getElementById('currentAvatar')?.value,
        initial: document.getElementById('currentInitial')?.value,
        // Info Toggle
        btnInfo: document.getElementById('btnToggleInfo'),
        infoBar: document.getElementById('infoSidebar'),
        search: document.getElementById('searchBox'),
        threadList: document.getElementById('threadListContainer') // Lấy container list
    };

    let currentId = parseInt(els.threadId?.value || '0');

    // 1. INFO SIDEBAR TOGGLE
    if (els.btnInfo && els.infoBar) {
        els.btnInfo.addEventListener('click', () => {
            els.infoBar.classList.toggle('open');
            // Đổi màu nút khi active
            if (els.infoBar.classList.contains('open')) els.btnInfo.classList.add('active');
            else els.btnInfo.classList.remove('active');
        });
    }

    // 2. TIME FORMAT
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

    document.querySelectorAll('.local-time, .local-time-preview').forEach(el => {
        const utc = el.getAttribute('data-utc');
        if (utc) el.textContent = formatTime(utc);
    });

    if (els.body) els.body.scrollTop = els.body.scrollHeight;

    // 3. SEARCH FUNCTION (LỌC TÊN KHÁCH HÀNG)
    els.search?.addEventListener('input', function () {
        const keyword = this.value.toLowerCase();
        const items = document.querySelectorAll('.thread-item');

        items.forEach(item => {
            // Lấy tên khách hàng từ class .t-name
            const name = item.querySelector('.t-name').textContent.toLowerCase();
            // Ẩn/Hiện dựa trên từ khóa
            item.style.display = name.includes(keyword) ? 'flex' : 'none';
        });
    });

    // 4. AUTO RESIZE INPUT
    els.input?.addEventListener('input', () => {
        els.sendBtn.disabled = els.input.value.trim() === '';
        els.input.style.height = 'auto';
        els.input.style.height = Math.min(els.input.scrollHeight, 120) + 'px';
    });

    // 5. SIGNALR
    if (typeof signalR === 'undefined') return;
    const connection = new signalR.HubConnectionBuilder().withUrl('/chathub').withAutomaticReconnect().build();

    connection.on("ReceiveMessage", (data) => {
        const threadItem = document.querySelector(`.thread-item[data-thread-id="${data.threadId}"]`);
        if (threadItem) {
            threadItem.querySelector('.preview-txt').textContent = data.content.includes('/images/chat/') ? 'Đã gửi ảnh' : data.content;
            threadItem.querySelector('.local-time-preview').textContent = formatTime(data.sentAt);
            if (data.threadId !== currentId) threadItem.classList.add('unread');
        }

        if (data.threadId === currentId) {
            appendMessage(data.isFromStaff, data.content, data.sentAt);
        }
    });

    connection.start().then(() => {
        if (currentId > 0) connection.invoke("JoinThreadGroup", currentId);
    }).catch(console.error);

    // 6. SEND TEXT
    async function sendMessage() {
        const text = els.input.value.trim();
        if (!text) return;

        els.sendBtn.disabled = true;
        const oldVal = els.input.value;
        els.input.value = ''; els.input.focus(); els.input.style.height = 'auto';

        try {
            await connection.invoke("SendStaffMessage", currentId, text);
        } catch (err) {
            els.input.value = oldVal;
            alert("Lỗi gửi tin.");
        } finally { els.sendBtn.disabled = false; }
    }

    els.sendBtn?.addEventListener('click', sendMessage);
    els.input?.addEventListener('keydown', (e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(); } });

    // 7. UPLOAD IMAGES
    els.uploadBtn?.addEventListener('change', async function () {
        if (this.files.length === 0) return;
        const files = Array.from(this.files);
        const urls = [];
        const tempDiv = document.createElement('div');
        tempDiv.className = 'msg-row me';
        tempDiv.innerHTML = `<div class="msg-bubble" style="background:#f0f2f5; color:#666">Đang tải...</div>`;
        els.body.appendChild(tempDiv); els.body.scrollTop = els.body.scrollHeight;

        try {
            for (const file of files) {
                const fd = new FormData(); fd.append('file', file);
                const res = await fetch('/Admin/Chat/UploadImage', { method: 'POST', body: fd });
                if (res.ok) {
                    const data = await res.json();
                    if (data.url) urls.push(data.url);
                }
            }
            if (urls.length > 0) await connection.invoke("SendStaffMessage", currentId, urls.join(','));
        } catch (e) { console.error(e); }
        finally { tempDiv.remove(); this.value = ''; }
    });

    // 8. RENDER
    function appendMessage(isMe, content, timeIso) {
        if (!els.body) return;
        const div = document.createElement('div');
        div.className = `msg-row ${isMe ? 'me' : 'other'}`;

        let inner = '';
        if (!isMe) {
            let avtHtml = els.initial;
            if (els.avatar) avtHtml = `<img src="${els.avatar}" />`;
            inner += `<div class="avatar-sm" style="width:30px; height:30px; font-size:12px; margin:0;">${avtHtml}</div>`;
        }

        let bubbleClass = 'msg-bubble';
        let contentHtml = '';

        if (content.includes('/images/chat/')) {
            bubbleClass += ' is-gallery';
            const imgs = content.split(',').filter(s => s.trim().startsWith('/images/chat/'));
            contentHtml += `<div class="grid-img" data-count="${imgs.length}">`;
            imgs.forEach(u => { contentHtml += `<img src="${u.trim()}" onclick="window.open(this.src)" />`; });
            contentHtml += `</div>`;
        } else {
            contentHtml = escapeHtml(content).replace(/\n/g, '<br>');
        }

        inner += `<div class="msg-content">
                        <div class="${bubbleClass}">${contentHtml}</div>
                        <span class="msg-time" style="text-align:${isMe ? 'right' : 'left'}">${formatTime(timeIso)}</span>
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