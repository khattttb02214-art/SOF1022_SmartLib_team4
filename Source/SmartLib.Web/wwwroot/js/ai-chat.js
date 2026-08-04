// ============================================================
// AI Chat Widget — Trợ lý AI thư viện SmartLib
// Gọi API /AI/Ask, /AI/LichSu, /AI/XoaLichSu (xem Controllers/AIController.cs).
// Toàn bộ nội dung tin nhắn được render bằng DOM text node (KHÔNG dùng innerHTML với nội
// dung động) để tránh rủi ro XSS, kể cả khi tên sách/nội dung có ký tự đặc biệt.
// ============================================================
(function () {
    'use strict';

    const toggleBtn = document.getElementById('aiChatToggleBtn');
    const panel = document.getElementById('aiChatPanel');
    const closeBtn = document.getElementById('aiChatCloseBtn');
    const clearBtn = document.getElementById('aiChatClearBtn');
    const messagesEl = document.getElementById('aiChatMessages');
    const suggestionsEl = document.getElementById('aiChatSuggestions');
    const form = document.getElementById('aiChatForm');
    const input = document.getElementById('aiChatInput');
    const sendBtn = document.getElementById('aiChatSendBtn');

    // Widget chỉ được render khi người dùng đã đăng nhập (xem _Layout.cshtml) — nếu không có
    // trên trang thì bỏ qua toàn bộ, tránh lỗi null reference.
    if (!toggleBtn || !panel || !form) return;

    let daTaiLichSu = false;
    let dangGui = false;

    function layToken() {
        return document.querySelector('#antiForgeryForm [name=__RequestVerificationToken]')?.value || '';
    }

    // Render 1 đoạn text an toàn: tách theo dòng, mỗi dòng là 1 text node, giữa các dòng
    // chèn thẻ <br> — không bao giờ gán thẳng chuỗi động vào innerHTML.
    function renderTextAnToan(container, text) {
        const cacDong = String(text ?? '').split('\n');
        cacDong.forEach((dong, idx) => {
            container.appendChild(document.createTextNode(dong));
            if (idx < cacDong.length - 1) container.appendChild(document.createElement('br'));
        });
    }

    function cuonXuongCuoi() {
        messagesEl.scrollTop = messagesEl.scrollHeight;
    }

    function anGoiY() {
        if (suggestionsEl) suggestionsEl.style.display = 'none';
    }

    function themTinNhan(role, noiDung) {
        const wrap = document.createElement('div');
        wrap.className = 'ai-chat-msg ai-chat-msg--' + (role === 'user' ? 'user' : 'ai');

        if (role !== 'user') {
            const avatar = document.createElement('div');
            avatar.className = 'ai-chat-msg-avatar';
            avatar.innerHTML = '<i class="fa fa-robot"></i>';
            wrap.appendChild(avatar);
        }

        const bubble = document.createElement('div');
        bubble.className = 'ai-chat-bubble';
        renderTextAnToan(bubble, noiDung);
        wrap.appendChild(bubble);

        messagesEl.appendChild(wrap);
        cuonXuongCuoi();
        return wrap;
    }

    function hienDangGoi() {
        const wrap = document.createElement('div');
        wrap.className = 'ai-chat-msg ai-chat-msg--ai';
        wrap.id = 'aiChatLoadingBubble';

        const avatar = document.createElement('div');
        avatar.className = 'ai-chat-msg-avatar';
        avatar.innerHTML = '<i class="fa fa-robot"></i>';
        wrap.appendChild(avatar);

        const bubble = document.createElement('div');
        bubble.className = 'ai-chat-bubble ai-chat-typing';
        bubble.innerHTML = '<span></span><span></span><span></span>';
        wrap.appendChild(bubble);

        messagesEl.appendChild(wrap);
        cuonXuongCuoi();
    }

    function anDangGoi() {
        document.getElementById('aiChatLoadingBubble')?.remove();
    }

    async function taiLichSu() {
        try {
            const res = await fetch('/AI/LichSu');
            if (!res.ok) return;
            const data = await res.json();
            const tinNhan = (data && data.tinNhan) || [];

            if (tinNhan.length > 0) {
                anGoiY();
                tinNhan.forEach(function (tn) {
                    themTinNhan(tn.role === 'user' ? 'user' : 'ai', tn.noiDung);
                });
            } else {
                themTinNhan('ai', 'Chào bạn! 📚 Mình là trợ lý AI của thư viện SmartLib. Bạn có thể hỏi mình về sách đang mượn, hạn trả, đặt trước, hoặc quy định thư viện nhé!');
            }
        } catch (e) {
            console.error('Không tải được lịch sử chat:', e);
        }
    }

    async function guiCauHoi(cauHoi) {
        cauHoi = (cauHoi || '').trim();
        if (dangGui || !cauHoi) return;

        dangGui = true;
        sendBtn.disabled = true;
        anGoiY();

        themTinNhan('user', cauHoi);
        hienDangGoi();

        try {
            const res = await fetch('/AI/Ask', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': layToken() },
                body: JSON.stringify({ message: cauHoi })
            });
            const data = await res.json().catch(function () { return null; });
            anDangGoi();

            if (res.ok && data && data.success) {
                themTinNhan('ai', data.reply);
            } else {
                themTinNhan('ai', (data && data.message) || 'Xin lỗi, có lỗi xảy ra. Bạn thử lại nhé.');
            }
        } catch (e) {
            anDangGoi();
            themTinNhan('ai', 'Không thể kết nối tới máy chủ. Bạn kiểm tra mạng rồi thử lại nhé.');
        } finally {
            dangGui = false;
            sendBtn.disabled = false;
            input.focus();
        }
    }

    toggleBtn.addEventListener('click', function () {
        const dangMo = panel.style.display !== 'none';
        panel.style.display = dangMo ? 'none' : 'flex';

        if (!dangMo) {
            input.focus();
            if (!daTaiLichSu) {
                daTaiLichSu = true;
                taiLichSu();
            }
        }
    });

    closeBtn && closeBtn.addEventListener('click', function () {
        panel.style.display = 'none';
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && panel.style.display !== 'none') panel.style.display = 'none';
    });

    clearBtn && clearBtn.addEventListener('click', async function () {
        if (!confirm('Xóa toàn bộ hội thoại hiện tại?')) return;

        try {
            await fetch('/AI/XoaLichSu', {
                method: 'POST',
                headers: { 'RequestVerificationToken': layToken() }
            });
        } catch (e) {
            // Bỏ qua lỗi mạng khi xóa — vẫn xóa giao diện phía client để người dùng thấy phản hồi ngay.
        }

        messagesEl.innerHTML = '';
        if (suggestionsEl) suggestionsEl.style.display = '';
        daTaiLichSu = true;
        themTinNhan('ai', 'Đã xóa hội thoại. Bạn muốn hỏi gì tiếp theo nào? 😊');
    });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        const cauHoi = input.value;
        input.value = '';
        guiCauHoi(cauHoi);
    });

    if (suggestionsEl) {
        suggestionsEl.querySelectorAll('.ai-chat-suggestion-chip').forEach(function (chip) {
            chip.addEventListener('click', function () { guiCauHoi(chip.textContent); });
        });
    }
})();
