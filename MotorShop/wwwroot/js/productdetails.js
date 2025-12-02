document.addEventListener('DOMContentLoaded', () => {
    
    // 1. TABS ĐỊA ĐIỂM (Đã cập nhật logic mới)
    const locTabs = document.querySelectorAll('.loc-tab-btn');
    const locLists = document.querySelectorAll('.loc-list');
    
    if(locTabs.length > 0) {
        locTabs.forEach(btn => {
            btn.addEventListener('click', () => {
                // Reset style tất cả các tab về trạng thái chưa chọn
                locTabs.forEach(t => {
                    t.classList.remove('active', 'text-slate-900', 'bg-white', 'shadow-sm');
                    t.classList.add('text-slate-500');
                });
                
                // Ẩn tất cả danh sách
                locLists.forEach(l => l.classList.add('hidden'));
                
                // Active tab được click
                btn.classList.remove('text-slate-500');
                btn.classList.add('active', 'text-slate-900', 'bg-white', 'shadow-sm');
                
                // Hiển thị danh sách tương ứng
                const targetId = 'list-' + btn.getAttribute('data-target');
                const targetList = document.getElementById(targetId);
                if(targetList) {
                    targetList.classList.remove('hidden');
                    // Hiệu ứng fade in nhẹ
                    targetList.animate([
                        { opacity: 0, transform: 'translateY(5px)' },
                        { opacity: 1, transform: 'translateY(0)' }
                    ], {
                        duration: 300,
                        easing: 'ease-out'
                    });
                }
            });
        });
        
        // Kích hoạt tab đầu tiên (mặc định)
        if(locTabs[0]) locTabs[0].click();
    }

    // 2. MODAL THÔNG SỐ
    const modal = document.getElementById('specModal');
    const openBtn = document.getElementById('specModalBtn');
    const closeBtns = [document.getElementById('closeSpecModalBtn'), document.getElementById('closeSpecModalBackdrop')];

    if(modal && openBtn) {
        openBtn.addEventListener('click', () => {
            modal.classList.remove('hidden');
            document.body.style.overflow = 'hidden';
        });
        closeBtns.forEach(b => {
            if(b) b.addEventListener('click', () => {
                modal.classList.add('hidden');
                document.body.style.overflow = '';
            });
        });
    }

    // 3. ẢNH GALLERY
    const mainImg = document.getElementById('mainImg');
    const thumbs = document.querySelectorAll('.pd-thumb-btn');
    
    thumbs.forEach(t => {
        t.addEventListener('click', function() {
            thumbs.forEach(x => x.classList.remove('active', 'border-[#00D1E4]'));
            thumbs.forEach(x => x.classList.add('border-transparent'));
            
            this.classList.remove('border-transparent');
            this.classList.add('active', 'border-[#00D1E4]');
            
            const src = this.getAttribute('data-img');
            if(mainImg && src) {
                mainImg.style.opacity = '0.5';
                setTimeout(() => {
                    mainImg.src = src;
                    mainImg.style.opacity = '1';
                }, 150);
            }
        });
    });

    // 4. ĐỌC THÊM BÀI VIẾT
    const content = document.getElementById('descriptionContent');
    const btnRead = document.getElementById('toggleContentBtn');
    
    if(content && btnRead) {
        if(content.scrollHeight <= 600) {
            btnRead.style.display = 'none';
            content.classList.remove('content-mask');
        }
        btnRead.addEventListener('click', () => {
            const isExp = content.classList.contains('expanded');
            if(isExp) {
                content.classList.remove('expanded');
                btnRead.innerHTML = 'Đọc thêm <i class="fa-solid fa-chevron-down ml-1"></i>';
                content.scrollIntoView({behavior: 'smooth', block: 'center'});
            } else {
                content.classList.add('expanded');
                btnRead.innerHTML = 'Thu gọn <i class="fa-solid fa-chevron-up ml-1"></i>';
            }
        });
    }

    // 5. STICKY NAV ANIMATION
    const nav = document.getElementById('sticky-nav');
    window.addEventListener('scroll', () => {
        if(window.scrollY > 600) nav.classList.remove('-translate-y-full');
        else nav.classList.add('-translate-y-full');
    });

    // 6. LOGIC MUA HÀNG (AJAX)
    window.handleBuyAction = async function(productId, isBuyNow) {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';
        const btn = event.currentTarget;
        const originalHtml = btn.innerHTML;

        btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i>';
        btn.disabled = true;

        try {
            const response = await fetch('/Cart/AddToCart', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ productId: parseInt(productId), quantity: 1 })
            });

            const result = await response.json();

            if (response.ok) {
                if (isBuyNow) {
                    window.location.href = '/Cart'; 
                } else {
                    alert(result.message || 'Đã thêm sản phẩm vào giỏ hàng!');
                    btn.innerHTML = originalHtml;
                    btn.disabled = false;
                }
            } else {
                alert(result.message || 'Không thể thêm vào giỏ.');
                btn.innerHTML = originalHtml;
                btn.disabled = false;
            }
        } catch (error) {
            console.error(error);
            alert('Lỗi kết nối server.');
            btn.innerHTML = originalHtml;
            btn.disabled = false;
        }
    }
}); document.addEventListener('DOMContentLoaded', () => {

    // =========================================================
    // XỬ LÝ NÚT "ĐỌC THÊM" / "THU GỌN" BÀI VIẾT
    // =========================================================
    const content = document.getElementById('descriptionContent');
    const btnRead = document.getElementById('toggleContentBtn');
    const gradient = document.getElementById('desc-gradient');

    // Chiều cao mặc định (phải khớp với style trong HTML)
    const DEFAULT_HEIGHT = 600;

    if (content && btnRead) {
        // 1. Kiểm tra nếu nội dung ngắn hơn chiều cao mặc định 
        // -> Tự động ẩn nút "Đọc thêm" và lớp mờ
        if (content.scrollHeight <= DEFAULT_HEIGHT) {
            btnRead.style.display = 'none';
            if (gradient) gradient.style.display = 'none';
            content.style.maxHeight = 'none'; // Hiển thị hết luôn
        }
        else {
            // 2. Nếu nội dung dài, bắt sự kiện click
            btnRead.addEventListener('click', () => {
                // Kiểm tra xem đang đóng hay mở
                // Nếu max-height là 'none' nghĩa là đang mở to -> cần thu nhỏ
                const isExpanded = content.style.maxHeight === 'none';

                if (isExpanded) {
                    // --- HÀNH ĐỘNG: THU GỌN LẠI ---
                    content.style.maxHeight = DEFAULT_HEIGHT + 'px'; // Gán lại giới hạn
                    if (gradient) gradient.style.opacity = '1'; // Hiện lớp mờ

                    // Đổi tên nút
                    btnRead.innerHTML = '<span>Đọc thêm</span> <i class="fa-solid fa-chevron-down text-xs ml-2"></i>';

                    // Cuộn nhẹ lên đầu bài viết để khách đỡ bị hẫng
                    const sectionTop = document.getElementById('bai-viet');
                    if (sectionTop) {
                        sectionTop.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }
                }
                else {
                    // --- HÀNH ĐỘNG: MỞ RỘNG RA (HIỂN THỊ TOÀN BỘ) ---
                    content.style.maxHeight = 'none'; // Bỏ giới hạn chiều cao
                    if (gradient) gradient.style.opacity = '0'; // Ẩn lớp mờ đi

                    // Đổi tên nút
                    btnRead.innerHTML = '<span>Thu gọn</span> <i class="fa-solid fa-chevron-up text-xs ml-2"></i>';
                }
            });
        }
    }

    // ... (Giữ nguyên các code khác như Tabs, Modal, Gallery nếu có)
});