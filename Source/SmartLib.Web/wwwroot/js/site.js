/**
 * SMARTLIB — Public Homepage JS
 * File: wwwroot/assets/js/home.js
 */
(function () {
    'use strict';

    /* ══════════════════════════════════
       1. ANIMATED COUNTER (hero stats)
       ══════════════════════════════════ */
    function animateCount(el, target, duration) {
        duration = duration || 1300;
        var start = null;
        function step(ts) {
            if (!start) start = ts;
            var p = Math.min((ts - start) / duration, 1);
            var ease = 1 - Math.pow(1 - p, 3);
            el.textContent = Math.round(target * ease).toLocaleString('vi-VN');
            if (p < 1) requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    }

    function initCounters() {
        var nums = document.querySelectorAll('.sl-stat-card__num[data-target]');
        if (!nums.length) return;
        var hero = document.getElementById('slHero');
        if (!hero) return;

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    nums.forEach(function (el, i) {
                        var target = parseInt(el.getAttribute('data-target'), 10) || 0;
                        setTimeout(function () { animateCount(el, target, 1400); }, i * 150);
                    });
                    observer.disconnect();
                }
            });
        }, { threshold: 0.3 });

        observer.observe(hero);
    }


    /* ══════════════════════════════════
       2. LIVE SEARCH DROPDOWN
          Gọi: GET /Home/SearchSuggest?q=...
          Trả về JSON: [{maSach, tenSach, tenTacGia, tenTheLoai, anhBia}]
       ══════════════════════════════════ */
    function initLiveSearch() {
        var input = document.getElementById('slSearchInput');
        var drop = document.getElementById('slSearchDrop');
        var wrap = document.getElementById('slSearchWrap');
        if (!input || !drop) return;

        var timer;

        input.addEventListener('input', function () {
            clearTimeout(timer);
            var q = this.value.trim();
            if (q.length < 2) { hideDrop(); return; }
            timer = setTimeout(function () { fetchSuggestions(q); }, 260);
        });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { hideDrop(); }
            if (e.key === 'Escape') { hideDrop(); }
        });

        document.addEventListener('click', function (e) {
            if (wrap && !wrap.contains(e.target)) hideDrop();
        });

        function hideDrop() { drop.style.display = 'none'; }

        function esc(s) {
            return (s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
        }

        function hl(text, q) {
            var re = new RegExp('(' + q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')', 'gi');
            return esc(text).replace(re, '<mark style="background:#fef08a;padding:0 1px;border-radius:2px;">$1</mark>');
        }

        function fetchSuggestions(q) {
            fetch('/Home/SearchSuggest?q=' + encodeURIComponent(q), {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (data) { renderDrop(data, q); })
                .catch(function () { hideDrop(); });
        }

        function renderDrop(items, q) {
            if (!items || !items.length) { hideDrop(); return; }
            var html = items.slice(0, 6).map(function (s) {
                var thumb = s.anhBia
                    ? '<img src="/uploads/books/' + esc(s.anhBia) + '" alt="">'
                    : '<i class="fa fa-book"></i>';
                var meta = [s.tenTacGia, s.tenTheLoai].filter(Boolean).map(esc).join(' · ');
                return '<a href="/Books/Detail/' + esc(s.maSach) + '" class="sl-sd-item">'
                    + '<div class="sl-sd-thumb">' + thumb + '</div>'
                    + '<div><div class="sl-sd-title">' + hl(s.tenSach, q) + '</div>'
                    + '<div class="sl-sd-meta">' + meta + '</div></div>'
                    + '</a>';
            }).join('');
            drop.innerHTML = html;
            drop.style.display = 'block';
        }
    }


    /* ══════════════════════════════════
       3. QUICK NAV — filter sách theo thể loại
          QuickNav buttons dùng data-filter="TenTheLoai" (hoặc "all")
          Book cards dùng data-genre="TenTheLoai"
       ══════════════════════════════════ */
    function initQuickNav() {
        var btns = document.querySelectorAll('.sl-qn');
        var cards = document.querySelectorAll('.sl-book-card[data-genre]');
        if (!btns.length) return;

        btns.forEach(function (btn) {
            btn.addEventListener('click', function () {
                btns.forEach(function (b) { b.classList.remove('active'); });
                btn.classList.add('active');

                var filter = btn.getAttribute('data-filter');
                var visible = 0;

                cards.forEach(function (card) {
                    var genre = (card.getAttribute('data-genre') || '').trim();
                    // So sánh TenTheLoai của card với data-filter của button
                    // Button "Tất cả" dùng filter="all"
                    var show = (filter === 'all') || (genre === filter);
                    card.classList.toggle('sl-hidden', !show);
                    if (show) visible++;
                });

                // Nếu không khớp thẻ nào thì hiện hết
                if (visible === 0) {
                    cards.forEach(function (c) { c.classList.remove('sl-hidden'); });
                }

                // Cuộn nhẹ lên đầu grid sách
                var grid = document.getElementById('slBookGrid');
                if (grid) {
                    var offset = 120; // bù cho sticky nav
                    var top = grid.getBoundingClientRect().top + window.scrollY - offset;
                    window.scrollTo({ top: top, behavior: 'smooth' });
                }
            });
        });
    }


    /* ══════════════════════════════════
       4. STICKY QUICKNAV — bù chiều cao topbar
       ══════════════════════════════════ */
    function initStickyNav() {
        var qn = document.getElementById('slQuicknav');
        if (!qn) return;

        function updateOffset() {
            var topNav = document.querySelector('.top-navbar');
            qn.style.top = (topNav ? topNav.offsetHeight : 0) + 'px';
        }
        updateOffset();
        window.addEventListener('resize', updateOffset);
    }


    /* ══════════════════════════════════
       5. FADE-IN SECTIONS khi scroll vào
       ══════════════════════════════════ */
    function initFadeIn() {
        var els = document.querySelectorAll('.sl-anim-up');
        if (!els.length) return;

        // Pause tất cả trước
        els.forEach(function (el) { el.style.animationPlayState = 'paused'; });

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.style.animationPlayState = 'running';
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.08 });

        els.forEach(function (el) { observer.observe(el); });
    }


    /* ══════════════════════════════════
       BOOT
       ══════════════════════════════════ */
    document.addEventListener('DOMContentLoaded', function () {
        initCounters();
        initLiveSearch();
        initQuickNav();
        initStickyNav();
        initFadeIn();
    });

}());