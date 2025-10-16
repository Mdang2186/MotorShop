/**
 * Handles the "Add to Cart" button click event.
 * It sends an AJAX request to the server without reloading the page.
 * @param {Event} event - The click event.
 * @param {number} productId - The ID of the product to add.
 */
function handleAddToCart(event, productId) {
    // Prevents the parent link from navigating away
    event.preventDefault();
    event.stopPropagation();

    // Sends the product ID to the server's API endpoint
    fetch('/Cart/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            // For enhanced security, an anti-forgery token should be included here in a real application
        },
        body: JSON.stringify({ productId: productId })
    })
        .then(response => {
            if (!response.ok) {
                // Handle server errors (like 404, 500)
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                // If the server confirms success, update the cart icon's count
                const cartCountSpan = document.getElementById('cart-count');
                if (cartCountSpan) {
                    cartCountSpan.innerText = data.cartCount;
                    if (data.cartCount > 0) {
                        cartCountSpan.classList.remove('hidden');
                    } else {
                        cartCountSpan.classList.add('hidden');
                    }
                }
                // Show a success message to the user
                showNotification(data.message, 'success');
            } else {
                // If the server reports an error, show an error message
                showNotification(data.message || 'Có lỗi xảy ra!', 'error');
            }
        })
        .catch(error => {
            // Handle network errors or failed requests
            console.error('Error:', error);
            showNotification('Không thể thêm sản phẩm vào giỏ hàng!', 'error');
        });
}

/**
 * Displays a toast-style notification at the top-right of the screen.
 * @param {string} message - The message to display.
 * @param {string} type - The type of notification ('success' or 'error').
 */
function showNotification(message, type = 'success') {
    const notification = document.getElementById('notification');
    if (!notification) return;

    const notificationText = document.getElementById('notificationText');
    const icon = notification.querySelector('i');

    notificationText.textContent = message;

    // Change color and icon based on the notification type
    if (type === 'error') {
        notification.className = 'notification bg-red-500 text-white px-4 py-3 rounded-lg shadow-lg flex items-center';
        icon.className = 'fas fa-exclamation-circle mr-2';
    } else {
        notification.className = 'notification bg-green-500 text-white px-4 py-3 rounded-lg shadow-lg flex items-center';
        icon.className = 'fas fa-check-circle mr-2';
    }

    // Show the notification and hide it after 3 seconds
    notification.classList.add('show');
    setTimeout(() => {
        notification.classList.remove('show');
    }, 3000);
}