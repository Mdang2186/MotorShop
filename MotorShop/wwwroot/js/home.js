// wwwroot/js/home.js
(() => {
    // ===== Debounce helper =====
    const debounce = (fn, wait = 250) => {
        let t;
        return (...args) => {
            clearTimeout(t);
            t = setTimeout(() => fn(...args), wait);
        };
    };

    // ===== Gợi ý tìm kiếm trên hero =====
    const input = document.getElementById("home-search");
    const suggestBox = document.getElementById("suggest-box");
    const suggestList = document.getElementById("suggest-list");

    if (!input || !suggestBox || !suggestList) {
        return; // không có phần tử => thoát
    }

    const hideSuggest = () => {
        suggestBox.classList.add("hidden");
        suggestList.innerHTML = "";
    };

    const showSuggest = (items) => {
        if (!items || items.length === 0) {
            hideSuggest();
            return;
        }
        suggestList.innerHTML = "";
        for (const it of items) {
            const li = document.createElement("button");
            li.type = "button";
            li.className =
                "flex items-center gap-3 w-full px-3 py-2 rounded-xl hover:bg-slate-100 text-left";
            li.innerHTML = `
                <div class="w-12 h-12 rounded-lg bg-slate-100 flex items-center justify-center overflow-hidden flex-shrink-0">
                    ${it.image
                    ? `<img src="${it.image}" class="w-full h-full object-cover" alt="${it.name}"/>`
                    : `<span class="text-sm font-semibold text-slate-500">Xe</span>`}
                </div>
                <div class="flex-1 min-w-0">
                    <div class="text-sm font-semibold text-slate-900 truncate">${it.name}</div>
                    <div class="text-xs text-slate-500 truncate">
                        ${it.brand ? it.brand + " · " : ""}${it.price?.toLocaleString("vi-VN")} ₫
                    </div>
                </div>
            `;
            li.addEventListener("click", () => {
                window.location.href = `/products/${it.id}`;
            });
            suggestList.appendChild(li);
        }

        suggestBox.classList.remove("hidden");
    };

    const fetchSuggest = debounce(async (term) => {
        term = term.trim();
        if (term.length < 2) {
            hideSuggest();
            return;
        }

        try {
            const url = `/products/suggest?term=${encodeURIComponent(term)}&take=8`;
            const res = await fetch(url, {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!res.ok) {
                hideSuggest();
                return;
            }

            const data = await res.json();
            if (!Array.isArray(data) || data.length === 0) {
                hideSuggest();
                return;
            }

            showSuggest(data);
        } catch (err) {
            console.error("Suggest error:", err);
            hideSuggest();
        }
    }, 250);

    input.addEventListener("input", (e) => {
        fetchSuggest(e.target.value || "");
    });

    input.addEventListener("focus", (e) => {
        if ((e.target.value || "").trim().length >= 2) {
            fetchSuggest(e.target.value);
        }
    });

    document.addEventListener("click", (e) => {
        if (!suggestBox.contains(e.target) && e.target !== input) {
            hideSuggest();
        }
    });

    // ===== Các nút CTA trên hero =====
    const btnViewBike = document.getElementById("btn-view-bike");
    const btnViewParts = document.getElementById("btn-view-parts");

    if (btnViewBike) {
        btnViewBike.addEventListener("click", () => {
            window.location.href = "/products";
        });
    }

    if (btnViewParts) {
        btnViewParts.addEventListener("click", () => {
            window.location.href = "/parts";
        });
    }
})();
