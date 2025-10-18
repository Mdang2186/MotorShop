/**
 * Handles the "Add to Cart" button click from ANYWHERE (product card, detail page).
 * Sends an AJAX request to the server without reloading the page.
 * @param {Event} event - The click event.
 * @param {number} productId - The ID of the product to add.
 * @param {number} [quantity=1] - The quantity to add (defaults to 1).
 */
function handleAddToCart(event, productId, quantity = 1) {
    // Ngăn hành vi mặc định (ví dụ: submit form hoặc điều hướng link)
    event.preventDefault();
    event.stopPropagation(); // Ngăn sự kiện nổi bọt, quan trọng nếu nút nằm trong thẻ <a>

    const button = event.target.closest('button');
    if (!button) {
        console.error("Could not find the button element.");
        return;
    }

    const originalHtml = button.innerHTML;
    button.disabled = true; // Vô hiệu hóa nút
    button.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i>Đang thêm...'; // Hiển thị trạng thái loading

    const url = '/Cart/AddToCart'; // Đảm bảo URL này đúng

    fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            // !!! QUAN TRỌNG: Bỏ comment và đảm bảo có token nếu dùng [ValidateAntiForgeryToken]
            // 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value 
        },
        // Đảm bảo tên thuộc tính (ProductId, Quantity) khớp với C# Model
        body: JSON.stringify({ ProductId: productId, Quantity: quantity })
    })
        .then(response => {
            // Kiểm tra phản hồi mạng và kiểu nội dung trước khi parse
            const contentType = response.headers.get("content-type");
            if (!response.ok) {
                // Cố gắng đọc lỗi JSON từ server nếu có
                if (contentType && contentType.indexOf("application/json") !== -1) {
                    return response.json().then(errorData => {
                        throw new Error(errorData.message || `Lỗi ${response.status}`);
                    });
                } else {
                    // Nếu không phải JSON, ném lỗi chung
                    throw new Error(`Lỗi ${response.status}: ${response.statusText}`);
                }
            }
            // Chỉ parse JSON nếu phản hồi OK và đúng kiểu
            if (contentType && contentType.indexOf("application/json") !== -1) {
                return response.json();
            } else {
                throw new Error("Phản hồi không phải là JSON hợp lệ.");
            }
        })
        .then(data => {
            // Xử lý dữ liệu JSON thành công
            if (data.success) {
                const cartCountSpan = document.getElementById('cart-count');
                if (cartCountSpan) {
                    cartCountSpan.innerText = data.cartCount;
                    cartCountSpan.classList.toggle('hidden', data.cartCount <= 0); // Ẩn/hiện dựa trên số lượng
                }
                showNotification(data.message, 'success');
            } else {
                // Hiển thị lỗi từ server (ví dụ: hết hàng)
                showNotification(data.message || 'Có lỗi xảy ra!', 'error');
            }
        })
        .catch(error => {
            // Xử lý lỗi mạng hoặc lỗi parse JSON
            console.error('Lỗi khi thêm vào giỏ:', error);
            showNotification(error.message || 'Không thể kết nối đến máy chủ!', 'error');
        })
        .finally(() => {
            // Luôn khôi phục lại nút bấm
            if (button) {
                button.disabled = false;
                button.innerHTML = originalHtml;
            }
        });
}

/**
 * Wrapper function specifically for the Product Detail page button.
 * Reads quantity from the input before calling handleAddToCart.
 * @param {Event} event - The click event.
 * @param {number} productId - The ID of the product.
 */
function handleAddToCartFromDetail(event, productId) {
    const quantityInput = document.getElementById('quantity-input'); // Đảm bảo ID này đúng
    let quantity = 1;
    if (quantityInput) {
        quantity = parseInt(quantityInput.value);
        if (isNaN(quantity) || quantity < 1) {
            quantity = 1;
            quantityInput.value = 1; // Correct invalid input
        }
        // Optional: Check against a max value or stock if available
    }
    // Call the main AJAX function with the determined quantity
    handleAddToCart(event, productId, quantity);
}


/**
 * Displays a toast-style notification.
 * (Giữ nguyên hàm này)
 */
function showNotification(message, type = 'success') {
    // ... code showNotification đã đúng ...
}

// --- Các hàm JavaScript khác ---
// Giữ lại các hàm khác bạn cần (debounce, mobile menu toggle, ...)
// Ví dụ: Mobile Menu Toggle
document.addEventListener('DOMContentLoaded', function () {
    const mobileMenuButton = document.getElementById('mobile-menu-button');
    const mobileMenu = document.getElementById('mobile-menu');
    if (mobileMenuButton && mobileMenu) {
        mobileMenuButton.addEventListener('click', () => {
            mobileMenu.classList.toggle('hidden');
        });
        mobileMenu.addEventListener('click', (e) => {
            if (e.target.tagName === 'A') {
                mobileMenu.classList.add('hidden');
            }
        });
    }
    // Thêm các event listener khác nếu cần (ví dụ: cho bộ lọc AJAX)
});
