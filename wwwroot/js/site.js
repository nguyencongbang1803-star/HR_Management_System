// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

function renderPasswordToggleIcon(button, isVisible) {
    button.innerHTML = isVisible
        ? '<svg class="password-toggle-icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M3 3l18 18"></path><path d="M10.6 10.6a2 2 0 0 0 2.8 2.8"></path><path d="M9.9 5.2A10.7 10.7 0 0 1 12 5c6.5 0 10 7 10 7a17 17 0 0 1-2.1 3.2"></path><path d="M6.2 6.2C3.5 8 2 12 2 12s3.5 7 10 7a10.8 10.8 0 0 0 5.8-1.7"></path></svg>'
        : '<svg class="password-toggle-icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"></path><circle cx="12" cy="12" r="3"></circle></svg>';
}

// Hiện/ẩn giá trị ngay trên trình duyệt; không gửi hay lưu mật khẩu.
document.addEventListener("click", function (event) {
    const button = event.target.closest("[data-password-toggle]");
    if (!button) return;

    const input = document.querySelector(button.dataset.passwordToggle);
    if (!input || (input.type !== "password" && input.type !== "text")) return;

    const isVisible = input.type === "text";
    input.type = isVisible ? "password" : "text";
    renderPasswordToggleIcon(button, !isVisible);
    button.setAttribute("aria-pressed", String(!isVisible));
    button.setAttribute("aria-label", isVisible ? "Hiện mật khẩu" : "Ẩn mật khẩu");
});

document.querySelectorAll("[data-password-toggle]").forEach(function (button) {
    const input = document.querySelector(button.dataset.passwordToggle);
    const isVisible = input?.type === "text";
    renderPasswordToggleIcon(button, isVisible);
    button.setAttribute("aria-pressed", String(isVisible));
});

// Tự động gửi biểu mẫu lọc theo thao tác nhập/chọn của người dùng.
(function () {
    document.querySelectorAll("form[data-auto-submit-search]").forEach(function (form) {
        let timer;
        let isComposing = false;

        function submitNow() {
            window.clearTimeout(timer);
            if (form.checkValidity()) form.requestSubmit();
        }

        function submitAfterTyping() {
            window.clearTimeout(timer);
            timer = window.setTimeout(submitNow, 400);
        }

        form.querySelectorAll("input, select").forEach(function (control) {
            if (!control.name || control.type === "hidden") return;

            if (control.matches('input[type="text"], input[type="search"], input:not([type])')) {
                control.addEventListener("compositionstart", function () {
                    isComposing = true;
                });
                control.addEventListener("compositionend", function () {
                    isComposing = false;
                    submitAfterTyping();
                });
                control.addEventListener("input", function () {
                    if (!isComposing) submitAfterTyping();
                });
                control.addEventListener("keydown", function (event) {
                    if (event.key === "Enter") {
                        event.preventDefault();
                        submitNow();
                    }
                });
            } else {
                control.addEventListener("change", submitNow);
            }
        });
    });
})();

// Thanh điều hướng: mở trên mobile, thu gọn trên desktop và ghi nhớ lựa chọn.
(function () {
    const sidebar = document.querySelector("#sidebar");
    if (!sidebar) return;

    const mobileToggle = document.querySelector("[data-sidebar-toggle]");
    const collapseToggle = document.querySelector("[data-sidebar-collapse]");
    const closeTargets = document.querySelectorAll("[data-sidebar-close]");

    function closeMobileSidebar() {
        document.body.classList.remove("sidebar-open");
    }

    mobileToggle?.addEventListener("click", function () {
        document.body.classList.toggle("sidebar-open");
    });

    closeTargets.forEach(function (target) {
        target.addEventListener("click", closeMobileSidebar);
    });

    sidebar.querySelectorAll("a").forEach(function (link) {
        link.addEventListener("click", closeMobileSidebar);
    });

    collapseToggle?.addEventListener("click", function () {
        const isCollapsed = document.body.classList.toggle("sidebar-collapsed");
        collapseToggle.setAttribute("aria-expanded", String(!isCollapsed));
        try {
            window.localStorage.setItem("tic-sidebar-collapsed", String(isCollapsed));
        } catch { /* Trình duyệt có thể chặn lưu trữ cục bộ. */ }
    });

    try {
        if (window.innerWidth > 900 && window.localStorage.getItem("tic-sidebar-collapsed") === "true") {
            document.body.classList.add("sidebar-collapsed");
            collapseToggle?.setAttribute("aria-expanded", "false");
        }
    } catch { /* Giữ trạng thái mở mặc định. */ }

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") closeMobileSidebar();
    });

    window.addEventListener("resize", function () {
        if (window.innerWidth > 900) closeMobileSidebar();
    });
})();

// Giữ biểu tượng thu gọn đồng bộ trên các panel tìm kiếm.
document.querySelectorAll('.panel-header[data-bs-toggle="collapse"]').forEach(function (header) {
    const target = document.querySelector(header.getAttribute("data-bs-target"));
    const icon = header.querySelector(".toggle-icon");
    if (!target || !icon) return;

    target.addEventListener("shown.bs.collapse", function () {
        icon.textContent = "▲";
    });
    target.addEventListener("hidden.bs.collapse", function () {
        icon.textContent = "▼";
    });
});

// Select nhân viên dùng chung: tìm theo mã/tên và hiển thị kết quả thành hai cột.
(function () {
    function normalize(value) {
        return (value || "")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase()
            .trim();
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function closeAll(except) {
        document.querySelectorAll(".employee-picker.is-open").forEach(function (picker) {
            if (picker !== except) picker.classList.remove("is-open");
        });
    }

    function enhance(select) {
        if (select.dataset.employeePickerReady === "true") return;
        select.dataset.employeePickerReady = "true";
        select.classList.add("employee-select-native");

        const picker = document.createElement("div");
        picker.className = "employee-picker";

        const trigger = document.createElement("button");
        trigger.type = "button";
        trigger.className = "employee-picker-trigger";
        trigger.setAttribute("aria-haspopup", "listbox");
        trigger.setAttribute("aria-expanded", "false");

        const dropdown = document.createElement("div");
        dropdown.className = "employee-picker-dropdown";

        const search = document.createElement("input");
        search.type = "search";
        search.className = "employee-picker-search";
        search.placeholder = "Tìm theo mã hoặc tên nhân viên";
        search.autocomplete = "off";

        const header = document.createElement("div");
        header.className = "employee-picker-header";
        header.innerHTML = "<span>Mã nhân viên</span><span>Tên nhân viên</span>";

        const list = document.createElement("div");
        list.className = "employee-picker-list";
        list.setAttribute("role", "listbox");

        dropdown.append(search, header, list);
        picker.append(trigger, dropdown);
        select.parentNode.insertBefore(picker, select);
        picker.appendChild(select);

        function optionData(option) {
            const rawText = option.textContent.trim();
            const parts = rawText.split(/\s+-\s+/, 2);
            return {
                code: option.dataset.employeeCode || (parts.length > 1 ? parts[0] : ""),
                name: option.dataset.employeeName || (parts.length > 1 ? parts[1] : rawText),
                note: option.dataset.employeeNote || ""
            };
        }

        function updateTrigger() {
            if (select.value) trigger.classList.remove("is-invalid");
            const option = select.options[select.selectedIndex];
            if (!option || !option.value) {
                trigger.innerHTML = `<span class="employee-picker-placeholder">${escapeHtml(option?.textContent.trim() || "Chọn nhân viên")}</span><span class="employee-picker-chevron">⌄</span>`;
                return;
            }
            const data = optionData(option);
            trigger.innerHTML = `<span class="employee-picker-code">${escapeHtml(data.code)}</span><span class="employee-picker-name">${escapeHtml(data.name)}</span><span class="employee-picker-chevron">⌄</span>`;
        }

        function renderList(query) {
            const keyword = normalize(query);
            list.replaceChildren();
            let resultCount = 0;

            Array.from(select.options).forEach(function (option) {
                const data = optionData(option);
                const searchable = normalize(`${data.code} ${data.name} ${option.textContent}`);
                if (keyword && !searchable.includes(keyword)) return;

                const item = document.createElement("button");
                item.type = "button";
                item.className = "employee-picker-option";
                item.setAttribute("role", "option");
                item.dataset.value = option.value;
                item.disabled = option.disabled;
                if (option.selected) item.classList.add("is-selected");

                if (!option.value) {
                    item.classList.add("is-placeholder");
                    item.innerHTML = `<span>${escapeHtml(option.textContent.trim())}</span>`;
                } else {
                    item.innerHTML = `<span class="employee-picker-code">${escapeHtml(data.code)}</span><span class="employee-picker-name">${escapeHtml(data.name)}${data.note ? `<small>${escapeHtml(data.note)}</small>` : ""}</span>`;
                }
                list.appendChild(item);
                resultCount++;
            });

            if (resultCount === 0) {
                const empty = document.createElement("div");
                empty.className = "employee-picker-empty";
                empty.textContent = "Không tìm thấy nhân viên phù hợp";
                list.appendChild(empty);
            }
        }

        trigger.addEventListener("click", function () {
            const willOpen = !picker.classList.contains("is-open");
            closeAll(picker);
            picker.classList.toggle("is-open", willOpen);
            trigger.setAttribute("aria-expanded", String(willOpen));
            if (willOpen) {
                search.value = "";
                renderList("");
                search.focus();
            }
        });

        search.addEventListener("input", function () {
            renderList(search.value);
        });

        search.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                picker.classList.remove("is-open");
                trigger.setAttribute("aria-expanded", "false");
                trigger.focus();
            }
        });

        list.addEventListener("click", function (event) {
            const item = event.target.closest(".employee-picker-option");
            if (!item || item.disabled) return;
            select.value = item.dataset.value;
            select.dispatchEvent(new Event("change", { bubbles: true }));
            picker.classList.remove("is-open");
            trigger.setAttribute("aria-expanded", "false");
            updateTrigger();
        });

        select.addEventListener("change", updateTrigger);
        select.addEventListener("invalid", function (event) {
            event.preventDefault();
            trigger.classList.add("is-invalid");
            trigger.focus();
        });
        trigger.addEventListener("click", function () {
            trigger.classList.remove("is-invalid");
        });

        updateTrigger();
    }

    function initialize(root) {
        if (root.matches?.("select.employee-select")) enhance(root);
        root.querySelectorAll?.("select.employee-select").forEach(enhance);
    }

    initialize(document);
    new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(function (node) {
                if (node.nodeType === Node.ELEMENT_NODE) initialize(node);
            });
        });
    }).observe(document.body, { childList: true, subtree: true });

    document.addEventListener("click", function (event) {
        if (!event.target.closest(".employee-picker")) closeAll();
    });
})();

// Bộ chọn nhiều nhân viên cho các lịch phân công.
(function () {
    document.querySelectorAll(".employee-multi-picker").forEach(function (picker) {
        const search = picker.querySelector(".employee-multi-search");
        const department = picker.querySelector(".employee-multi-department");
        const position = picker.querySelector(".employee-multi-position");
        const options = Array.from(picker.querySelectorAll(".employee-multi-option"));
        const count = picker.querySelector("[data-employee-selected-count]");

        function updateCount() {
            count.textContent = options.filter(option => option.querySelector("input").checked).length;
        }

        function applyFilters() {
            const keyword = search.value.trim().toLocaleLowerCase("vi");
            const departmentId = department?.value || "";
            const positionId = position?.value || "";
            options.forEach(function (option) {
                const matchesKeyword = keyword.length === 0 || option.dataset.search.toLocaleLowerCase("vi").includes(keyword);
                const matchesDepartment = departmentId.length === 0 || option.dataset.department === departmentId;
                const matchesPosition = positionId.length === 0 || option.dataset.position === positionId;
                option.hidden = !(matchesKeyword && matchesDepartment && matchesPosition);
            });
        }

        search.addEventListener("input", applyFilters);
        department?.addEventListener("change", applyFilters);
        position?.addEventListener("change", applyFilters);
        options.forEach(option => option.querySelector("input").addEventListener("change", updateCount));
        updateCount();
    });
})();

// Đồng bộ icon mũi tên và hiệu ứng mở cho các select thông thường.
(function () {
    function enhance(select) {
        if (select.dataset.standardSelectReady === "true") return;
        select.dataset.standardSelectReady = "true";

        const wrapper = document.createElement("div");
        wrapper.className = "standard-select-wrap";
        if (select.style.width === "auto") {
            wrapper.classList.add("is-auto-width");
            select.style.width = "100%";
        }

        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(select);

        select.addEventListener("pointerdown", function () {
            document.querySelectorAll(".standard-select-wrap.is-open").forEach(function (item) {
                if (item !== wrapper) item.classList.remove("is-open");
            });
            wrapper.classList.add("is-open");
        });
        select.addEventListener("change", function () {
            wrapper.classList.remove("is-open");
        });
        select.addEventListener("blur", function () {
            wrapper.classList.remove("is-open");
        });
        select.addEventListener("keydown", function (event) {
            if (event.key === "Escape" || event.key === "Tab") {
                wrapper.classList.remove("is-open");
            } else if (event.key === "Enter" || event.key === " " || event.key === "ArrowDown") {
                wrapper.classList.add("is-open");
            }
        });
    }

    function initialize(root) {
        if (root.matches?.("select.form-select:not(.employee-select)")) enhance(root);
        root.querySelectorAll?.("select.form-select:not(.employee-select)").forEach(enhance);
    }

    initialize(document);
    new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(function (node) {
                if (node.nodeType === Node.ELEMENT_NODE) initialize(node);
            });
        });
    }).observe(document.body, { childList: true, subtree: true });
})();

// =========================================================
// UNIVERSAL TOAST NOTIFICATION SYSTEM
// =========================================================
window.showToast = function (message, type = 'success', duration = 4000, action = null) {
    if (!message) return;

    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container-root';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `app-toast app-toast-${type}`;

    let iconSvg = '';
    let titleText = 'Thông báo';

    if (type === 'success') {
        titleText = 'Thành công';
        iconSvg = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>';
    } else if (type === 'error' || type === 'danger') {
        titleText = 'Lỗi';
        iconSvg = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>';
    } else if (type === 'warning') {
        titleText = 'Cảnh báo';
        iconSvg = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>';
    } else {
        titleText = 'Thông tin';
        iconSvg = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>';
    }

    toast.innerHTML = `
        <div class="toast-icon"></div>
        <div class="toast-content">
            <div class="toast-title"></div>
            <div class="toast-message"></div>
            <a class="toast-action" hidden></a>
        </div>
        <button type="button" class="toast-close" aria-label="Đóng">&times;</button>
        <div class="toast-progress" style="animation-duration: ${duration}ms;"></div>
    `;

    toast.querySelector('.toast-icon').innerHTML = iconSvg;
    toast.querySelector('.toast-title').textContent = titleText;
    toast.querySelector('.toast-message').textContent = message;

    if (action && action.url) {
        const actionLink = toast.querySelector('.toast-action');
        actionLink.hidden = false;
        actionLink.href = action.url;
        actionLink.textContent = `${action.text || 'Xem chi tiết'} →`;
        toast.classList.add('is-actionable');
        toast.setAttribute('role', 'link');
        toast.tabIndex = 0;

        toast.addEventListener('click', function (event) {
            if (event.target.closest('.toast-close, .toast-action')) return;
            window.location.assign(action.url);
        });
        toast.addEventListener('keydown', function (event) {
            if (event.target.closest('.toast-close, .toast-action')) return;
            if (event.key !== 'Enter' && event.key !== ' ') return;
            event.preventDefault();
            window.location.assign(action.url);
        });
    }

    const closeBtn = toast.querySelector('.toast-close');
    let timer;

    function removeToast() {
        if (toast.classList.contains('is-leaving')) return;
        toast.classList.add('is-leaving');
        toast.addEventListener('animationend', function () {
            if (toast.parentNode) toast.parentNode.removeChild(toast);
        });
    }

    closeBtn.addEventListener('click', function (event) {
        event.stopPropagation();
        removeToast();
    });
    container.appendChild(toast);

    timer = setTimeout(removeToast, duration);

    toast.addEventListener('mouseenter', function () {
        clearTimeout(timer);
        const pb = toast.querySelector('.toast-progress');
        if (pb) pb.style.animationPlayState = 'paused';
    });

    toast.addEventListener('mouseleave', function () {
        const pb = toast.querySelector('.toast-progress');
        if (pb) pb.style.animationPlayState = 'running';
        timer = setTimeout(removeToast, 1800);
    });
};

// Luồng duyệt hai bước: chọn quyết định, nhập phản hồi trong Modal Dialog.
function closeApprovalModal(form, actions) {
    form.reset();
    form.hidden = true;
    if (actions) actions.hidden = false;
    document.body.style.overflow = '';
}

document.addEventListener('click', function (event) {
    const decisionButton = event.target.closest('[data-approval-decision]');
    if (decisionButton) {
        const workflow = decisionButton.closest('[data-approval-workflow]');
        const form = workflow?.querySelector('[data-approval-form]');
        const actions = workflow?.querySelector('[data-approval-actions]');
        const decisionInput = form?.querySelector('[data-approval-decision-input]');
        if (!form || !actions || !decisionInput) return;

        // Tự động bọc nội dung modal card nếu chưa có container
        if (!form.querySelector('.approval-response-card')) {
            const card = document.createElement('div');
            card.className = 'approval-response-card';
            const children = Array.from(form.children);
            children.forEach(function (child) {
                if (child.tagName !== 'INPUT' || child.type !== 'hidden') {
                    card.appendChild(child);
                }
            });
            form.appendChild(card);
        }

        const decision = decisionButton.dataset.approvalDecision;
        decisionInput.value = decision;
        const isApproved = decision === 'Approved';
        const title = form.querySelector('[data-approval-title]');
        if (title) title.textContent = isApproved ? 'Phản hồi khi duyệt' : 'Phản hồi khi từ chối';

        const badge = form.querySelector('[data-approval-badge]');
        if (badge) {
            badge.textContent = isApproved ? '✓' : '✕';
            badge.className = 'approval-modal-title-badge ' + (isApproved ? 'badge-approved' : 'badge-rejected');
        }

        const submitBtn = form.querySelector('[data-approval-submit-btn]');
        if (submitBtn) {
            submitBtn.textContent = isApproved ? 'Xác nhận duyệt' : 'Xác nhận từ chối';
            submitBtn.className = isApproved ? 'btn-green-solid' : 'btn-green-solid bg-danger border-danger';
        }

        form.querySelectorAll('[data-approved-only]').forEach(function (section) {
            section.hidden = !isApproved;
            section.querySelectorAll('input, select, textarea').forEach(function (input) {
                input.disabled = !isApproved;
            });
        });

        actions.hidden = true;
        form.hidden = false;
        document.body.style.overflow = 'hidden';
        setTimeout(function () {
            form.querySelector('textarea[name="response"], textarea[name="Response"]')?.focus();
        }, 50);
        return;
    }

    const cancelButton = event.target.closest('[data-approval-cancel]');
    if (cancelButton) {
        const workflow = cancelButton.closest('[data-approval-workflow]');
        const form = workflow?.querySelector('[data-approval-form]');
        const actions = workflow?.querySelector('[data-approval-actions]');
        if (!form || !actions) return;
        closeApprovalModal(form, actions);
        return;
    }

    // Click ra ngoài backdrop để đóng modal
    if (event.target.matches('[data-approval-form]')) {
        const form = event.target;
        const workflow = form.closest('[data-approval-workflow]');
        const actions = workflow?.querySelector('[data-approval-actions]');
        if (form && actions) {
            closeApprovalModal(form, actions);
        }
    }
});

document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        const openForm = document.querySelector('[data-approval-form]:not([hidden])');
        if (openForm) {
            const workflow = openForm.closest('[data-approval-workflow]');
            const actions = workflow?.querySelector('[data-approval-actions]');
            if (actions) {
                closeApprovalModal(openForm, actions);
            }
        }
    }
});

// Xem trước minh chứng ngay tại trang duyệt, không buộc tải tệp xuống.
document.addEventListener('click', function (event) {
    const trigger = event.target.closest('[data-file-preview]');
    if (!trigger) return;

    const modalElement = document.getElementById('filePreviewModal');
    if (!modalElement || typeof bootstrap === 'undefined') return;

    const previewUrl = trigger.dataset.previewUrl;
    const downloadUrl = trigger.dataset.downloadUrl;
    const fileName = trigger.dataset.fileName || 'Tệp minh chứng';
    const extension = fileName.includes('.') ? fileName.split('.').pop().toLowerCase() : '';
    const contentType = extension === 'pdf' ? 'application/pdf' :
        (extension === 'png' ? 'image/png' : (extension === 'jpg' || extension === 'jpeg' ? 'image/jpeg' : ''));
    const title = modalElement.querySelector('[data-file-preview-title]');
    const typeLabel = modalElement.querySelector('[data-file-preview-type]');
    const image = modalElement.querySelector('[data-file-preview-image]');
    const frame = modalElement.querySelector('[data-file-preview-frame]');
    const unsupported = modalElement.querySelector('[data-file-preview-unsupported]');
    const download = modalElement.querySelector('[data-file-preview-download]');

    title.textContent = fileName;
    typeLabel.textContent = contentType || 'Không xác định định dạng';
    download.href = downloadUrl || previewUrl || '#';
    image.hidden = true;
    image.removeAttribute('src');
    frame.hidden = true;
    frame.removeAttribute('src');
    unsupported.hidden = true;

    if (contentType.startsWith('image/')) {
        image.src = previewUrl;
        image.alt = fileName;
        image.hidden = false;
    } else if (contentType === 'application/pdf') {
        frame.src = previewUrl;
        frame.hidden = false;
    } else {
        unsupported.hidden = false;
    }

    bootstrap.Modal.getOrCreateInstance(modalElement).show();
});

document.getElementById('filePreviewModal')?.addEventListener('hidden.bs.modal', function () {
    const image = this.querySelector('[data-file-preview-image]');
    const frame = this.querySelector('[data-file-preview-frame]');
    image?.removeAttribute('src');
    frame?.removeAttribute('src');
});

function updateAllowanceForm(form) {
    const scope = form.querySelector('[data-allowance-scope]')?.value;
    form.querySelectorAll('[data-scope-target]').forEach(function (section) {
        const visible = section.dataset.scopeTarget === scope;
        section.hidden = !visible;
        section.querySelectorAll('input, select').forEach(function (input) { input.disabled = !visible; });
    });
    const fixed = form.querySelector('[data-allowance-duration]')?.value === 'FixedMonths';
    const fixedSection = form.querySelector('[data-fixed-months]');
    if (fixedSection) {
        fixedSection.hidden = !fixed;
        fixedSection.querySelectorAll('input').forEach(function (input) { input.disabled = !fixed; });
    }
}

document.querySelectorAll('[data-allowance-form]').forEach(updateAllowanceForm);
document.addEventListener('change', function (event) {
    if (!event.target.matches('[data-allowance-scope], [data-allowance-duration]')) return;
    const form = event.target.closest('[data-allowance-form]');
    if (form) updateAllowanceForm(form);
});

function updateAudienceForm(form) {
    const scope = form.querySelector('[data-audience-scope]')?.value;
    form.querySelectorAll('[data-audience-target]').forEach(function (section) {
        const visible = section.dataset.audienceTarget === scope;
        section.hidden = !visible;
        section.querySelectorAll('input, select').forEach(function (input) {
            input.disabled = !visible;
            input.required = visible;
        });
    });
}

document.querySelectorAll('[data-audience-form]').forEach(updateAudienceForm);
document.addEventListener('change', function (event) {
    if (!event.target.matches('[data-audience-scope]')) return;
    const form = event.target.closest('[data-audience-form]');
    if (form) updateAudienceForm(form);
});

function updateContractEndDate(typeSelect) {
    const form = typeSelect.closest('form');
    const section = form?.querySelector('[data-contract-end]');
    const input = form?.querySelector('[data-contract-end-input]');
    if (!section || !input) return;
    const indefiniteType = 'Hợp đồng lao động không xác định thời hạn';
    const isIndefinite = typeSelect.value === indefiniteType;
    section.hidden = isIndefinite;
    input.disabled = isIndefinite;
    input.required = !isIndefinite;
    if (isIndefinite) input.value = '';
}

document.querySelectorAll('[data-contract-type]').forEach(updateContractEndDate);
document.addEventListener('change', function (event) {
    if (event.target.matches('[data-contract-type]')) updateContractEndDate(event.target);
});
