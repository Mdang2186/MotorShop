// === 1. GENTLE WATER RIPPLE (Sóng nhẹ) ===
const canvas = document.getElementById('magnetic-canvas');
const ctx = canvas.getContext('2d');
let w, h, particles = [];
const config = { spacing: 24, baseSize: 1.5, waveSpeed: 0.03, amplitude: 10, color: '0, 113, 227' };
let mouse = { x: -1000, y: -1000 };
let time = 0;
window.addEventListener('mousemove', (e) => { mouse.x = e.clientX; mouse.y = e.clientY; });
window.addEventListener('mouseout', () => { mouse.x = -1000; mouse.y = -1000; });
function initCanvas() {
    w = canvas.width = window.innerWidth;
    h = canvas.height = window.innerHeight; particles = [];
    for (let x = 0; x < w; x += config.spacing) {
        for (let y = 0; y < h; y += config.spacing) {
            particles.push({ x: x, y: y, originX: x, originY: y });
        }
    }
}

function animate() {
    ctx.fillStyle = 'rgba(255, 255, 255, 0.8)';
    ctx.fillRect(0, 0, w, h);
    time += config.waveSpeed;
    for (let i = 0; i < particles.length; i++) {
        let p = particles[i];
        let dx = mouse.x - p.originX, dy = mouse.y - p.originY;
        let dist = Math.sqrt(dx * dx + dy * dy);
        let waveInteraction = 0, size = config.baseSize, opacity = 0.05;
        if (dist < 900) {
            let decay = (1 - dist / 900);
            let wave = Math.sin(dist / 80 - time);
            waveInteraction = wave * config.amplitude * decay;
            size = config.baseSize + (wave * 0.8 * decay);
            opacity = 0.05 + (wave * 0.1 * decay);
        }
        if (size < 0.5) size = 0.5;
        ctx.fillStyle = `rgba(${config.color}, ${opacity})`;
        ctx.beginPath(); ctx.arc(p.originX, p.originY + waveInteraction, size, 0, Math.PI * 2); ctx.fill();
    }
    requestAnimationFrame(animate);
}
window.addEventListener('resize', initCanvas); initCanvas(); animate();

// === 2. CHAT LOGIC ===
const ui = {
    input: document.getElementById('ai-input'),
    sendBtn: document.getElementById('ai-send'),
    hero: document.getElementById('hero-view'),
    chat: document.getElementById('chat-content'),
    scroll: document.getElementById('scroll-wrapper'),
    loader: document.getElementById('loading-spinner')
};
let currentConversationId = null;
let currentProductList = []; // [NEW] Lưu danh sách sản phẩm

async function sendMessage() {
    const txt = ui.input.value.trim();
    if (!txt) return;

    ui.hero.style.display = 'none';
    ui.input.value = '';

    // User Msg (Right Aligned)
    const userDiv = document.createElement('div');
    userDiv.className = 'msg-row user';
    userDiv.innerHTML = `<div class="bubble user">${txt}</div>`;
    ui.chat.appendChild(userDiv);

    ui.loader.style.display = 'block';
    scrollToBottom();

    try {
        const res = await fetch('/ai/chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: txt, conversationId: currentConversationId })
        });
        const data = await res.json();
        if (data.conversationId) currentConversationId = data.conversationId;

        ui.loader.style.display = 'none';
        const botDiv = document.createElement('div');
        let html = `<div style="width:100%">`;

        const isSearchResult = data.items && data.items.length > 0;
        if (isSearchResult) {
            currentProductList = data.items; // [NEW] Cập nhật list sản phẩm

            botDiv.className = 'msg-row bot results';
            if (data.insight) html += `<div style="font-size: 0.75rem; font-weight: 700; color: #0071e3; text-transform: uppercase; margin-bottom: 8px;"><i class="fa-solid fa-lightbulb"></i> ${data.insight}</div>`;
            html += `<div class="bubble transparent">Đây là các mẫu xe phù hợp:</div>`;

            html += `<div class="ios-grid">`;
            // [UPDATED] Render card với onclick gọi showSidebar
            data.items.forEach((p, index) => {
                const price = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(p.price);
                html += `
                        <div class="widget-card" onclick="showSidebar(${index})">
                            <div class="widget-img-box">
                                <img src="${p.imageUrl || '/images/default-moto.png'}" class="widget-img">
                                <div class="widget-price">${price}</div>
                            </div>
                            <div class="widget-info">
                                <div class="widget-name">${p.name}</div>
                                <div class="widget-desc" style="display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;">${p.reason}</div>
                            </div>
                        </div>`;
            });
            html += `</div>`;

        } else {
            botDiv.className = 'msg-row bot';
            let content = "";
            if (data.insight) content += `<b>${data.insight}</b><br>`;
            content += "Không tìm thấy xe phù hợp hoặc tôi chưa hiểu ý bạn.";
            html += `<div class="bubble bot">${content}</div>`;
        }

        html += `</div>`;
        botDiv.innerHTML = html;
        ui.chat.appendChild(botDiv);
        scrollToBottom();

    } catch (err) {
        console.error(err);
        ui.loader.style.display = 'none';
    }
}

function scrollToBottom() { ui.scroll.scrollTop = ui.scroll.scrollHeight; }
window.usePrompt = (t) => { ui.input.value = t; sendMessage(); }
ui.sendBtn.addEventListener('click', sendMessage);
ui.input.addEventListener('keydown', (e) => { if (e.key === 'Enter') sendMessage(); });
document.addEventListener("DOMContentLoaded", () => {
    const p = new URLSearchParams(window.location.search);
    if (p.get('q')) { ui.input.value = p.get('q'); sendMessage(); }
});

// === 3. [NEW] SIDEBAR LOGIC ===
const sidebar = document.getElementById('product-sidebar');
const backdrop = document.getElementById('side-backdrop');

// === 3. UPDATE SIDEBAR LOGIC ===
function showSidebar(index) {
    const p = currentProductList[index];
    if (!p) return;

    // --- 1. Xử lý Ảnh & Brand ---
    document.getElementById('sb-image').src = p.imageUrl || '/images/default-moto.png';
    const brandName = p.brandName || "MotorShop";
    document.getElementById('sb-brand-text').innerText = brandName;
    document.getElementById('sb-category-small').innerText = p.categoryName || "Xe máy";
    document.getElementById('sb-name').innerText = p.name;

    // --- 2. Xử lý Giá & Giảm giá (Logic UX) ---
    const formatter = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' });
    const currentPrice = p.price;
    // Giả sử logic: Nếu có originalPrice và nó lớn hơn price thì là đang giảm giá
    // (Lưu ý: API Controller của bạn cần trả về thêm OriginalPrice nếu muốn dùng tính năng này,
    //  hiện tại chúng ta dùng tạm Price làm giá chính).

    document.getElementById('sb-price').innerText = formatter.format(currentPrice);

    // Ẩn tạm các thẻ giảm giá (mở lại nếu bạn map thêm trường OriginalPrice từ BE)
    document.getElementById('sb-original-price').style.display = 'none';
    document.getElementById('sb-discount-tag').style.display = 'none';

    // --- 3. Xử lý Thông số (Icons) ---
    document.getElementById('sb-year').innerText = p.year || "Mới";
    document.getElementById('sb-sku').innerText = p.sku || "N/A";
    document.getElementById('sb-category-val').innerText = p.categoryName || "Xe máy";

    // Tình trạng kho màu sắc
    const stockEl = document.getElementById('sb-stock');
    if (p.stockQuantity > 0) {
        stockEl.innerText = 'Sẵn sàng giao';
        stockEl.style.color = '#34c759'; // Xanh lá
    } else {
        stockEl.innerText = 'Tạm hết';
        stockEl.style.color = '#ff3b30'; // Đỏ
    }

    // --- 4. AI Insight & Description ---
    document.getElementById('sb-reason').innerText = `"${p.reason || 'Sản phẩm phù hợp với nhu cầu của bạn.'}"`;

    const defaultDesc = `<p>Sản phẩm chính hãng <strong>${p.name}</strong> từ thương hiệu ${brandName}.</p>`;
    document.getElementById('sb-desc').innerHTML = p.description || defaultDesc;

    // --- 5. Action Button ---
    const btn = document.getElementById('sb-btn-link');
    btn.onclick = () => window.open(`/products/${p.productId}`, '_blank');

    // --- 6. Active Sidebar ---
    sidebar.classList.add('active');
    backdrop.classList.add('active');
}
function closeSidebar() {
    sidebar.classList.remove('active');
    backdrop.classList.remove('active');
}

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeSidebar();
});