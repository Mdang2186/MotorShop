document.addEventListener('DOMContentLoaded', function () {
    //
    // Global AJAX Setup for Loading Indicators
    //
    const mainContent = document.querySelector('main');

    document.addEventListener('ajax:before', () => mainContent.style.opacity = '0.5');
    document.addEventListener('ajax:complete', () => mainContent.style.opacity = '1');


    //
    // Product Page: AJAX Filtering, Sorting, and Pagination
    //
    const filterForm = document.getElementById('filterForm');
    if (filterForm) {
        // Submit form via AJAX whenever a filter or search term changes
        filterForm.addEventListener('change', (e) => handleFilterSubmit(e.target.form));
        filterForm.addEventListener('keyup', debounce((e) => handleFilterSubmit(e.target.form), 300));

        // Handle pagination and sorting links via AJAX
        document.getElementById('productsContainer').addEventListener('click', function (e) {
            if (e.target.closest('.pagination-link') || e.target.closest('.sort-link')) {
                e.preventDefault();
                const url = e.target.closest('a').href;
                fetchProducts(url);
            }
        });
    }

    //
    // Product Details & Cart: AJAX operations
    //
    document.body.addEventListener('click', function (e) {
        // Handle "View Product" button clicks
        const viewBtn = e.target.closest('.view-product-btn');
        if (viewBtn) {
            e.preventDefault();
            const url = viewBtn.dataset.url;
            fetchAndShowModal(url, 'productModal');
        }

        // Handle "Add to Cart" button clicks
        const addBtn = e.target.closest('.add-to-cart-btn');
        if (addBtn) {
            e.preventDefault();
            const url = addBtn.dataset.url;
            const productId = addBtn.dataset.productId;
            addToCart(url, productId);
        }
    });
});

// Helper function to dispatch custom AJAX events
function dispatchAjaxEvent(eventName) {
    const event = new CustomEvent(eventName);
    document.dispatchEvent(event);
}

// Debounce function to limit how often a function can run
function debounce(func, delay = 250) {
    let timeout;
    return (...args) => {
        clearTimeout(timeout);
        timeout = setTimeout(() => {
            func.apply(this, args);
        }, delay);
    };
}

// Function to handle AJAX form submission for product filters
function handleFilterSubmit(form) {
    const formData = new FormData(form);
    const params = new URLSearchParams(formData);
    const url = `${form.action}?${params.toString()}`;
    fetchProducts(url);
}

// Function to fetch and update the product grid
function fetchProducts(url) {
    dispatchAjaxEvent('ajax:before');
    fetch(url)
        .then(response => response.text())
        .then(html => {
            document.getElementById('productsContainer').innerHTML = html;
            // Update URL in browser for bookmarking
            window.history.pushState({}, '', url);
        })
        .catch(error => console.error('Error fetching products:', error))
        .finally(() => dispatchAjaxEvent('ajax:complete'));
}

// Function to fetch content and display it in a modal
function fetchAndShowModal(url, modalId) {
    dispatchAjaxEvent('ajax:before');
    fetch(url)
        .then(response => response.text())
        .then(html => {
            const modalContent = document.getElementById(`${modalId}Content`);
            const modal = document.getElementById(modalId);
            modalContent.innerHTML = html;
            modal.classList.remove('hidden');
        })
        .catch(error => console.error('Error fetching modal content:', error))
        .finally(() => dispatchAjaxEvent('ajax:complete'));
}

// Function to close any open modal
function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.add('hidden');
        const modalContent = document.getElementById(`${modalId}Content`);
        if (modalContent) modalContent.innerHTML = ''; // Clear content
    }
}

// Function to add a product to the cart via AJAX
function addToCart(url, productId) {
    dispatchAjaxEvent('ajax:before');
    fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            // Include Anti-Forgery Token if needed for security
        },
        body: JSON.stringify({ productId: productId })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showNotification(data.message || 'Thêm vào giỏ thành công!');
                // Update cart count in the header
                const cartCount = document.getElementById('cartCount');
                if (cartCount) {
                    cartCount.textContent = data.cartCount;
                    cartCount.classList.remove('hidden');
                }
            } else {
                showNotification(data.message || 'Có lỗi xảy ra!', 'error');
            }
        })
        .catch(error => {
            console.error('Error adding to cart:', error);
            showNotification('Không thể thêm vào giỏ hàng!', 'error');
        })
        .finally(() => dispatchAjaxEvent('ajax:complete'));
}


// Function to show toast-style notifications
function showNotification(message, type = 'success') {
    const notification = document.getElementById('notification');
    if (!notification) return;

    const notificationText = document.getElementById('notificationText');
    notificationText.textContent = message;

    const icon = notification.querySelector('i');
    if (type === 'error') {
        notification.className = 'notification bg-red-500 text-white px-4 py-3 rounded-lg shadow-lg flex items-center';
        icon.className = 'fas fa-exclamation-circle mr-2';
    } else {
        notification.className = 'notification bg-green-500 text-white px-4 py-3 rounded-lg shadow-lg flex items-center';
        icon.className = 'fas fa-check-circle mr-2';
    }

    notification.classList.add('show');

    setTimeout(() => {
        notification.classList.remove('show');
    }, 3000);
}