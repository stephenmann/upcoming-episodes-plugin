(function () {
    'use strict';

    var BADGE_CLASS = 'upcomingEpisodeBadge';
    var STYLE_ID = 'upcomingEpisodeBadgeStyle';
    var CACHE_MS = 60000;

    var cache = null;
    var cacheTime = 0;
    var pending = null;
    var scheduled = false;

    function injectStyle() {
        if (document.getElementById(STYLE_ID)) {
            return;
        }

        var style = document.createElement('style');
        style.id = STYLE_ID;
        style.textContent = '.' + BADGE_CLASS + '{color:#f5c518;font-weight:700;}';
        document.head.appendChild(style);
    }

    function loadMessages() {
        var client = window.ApiClient;
        if (!client || !client.accessToken || !client.accessToken()) {
            return Promise.resolve({});
        }

        if (cache && (Date.now() - cacheTime) < CACHE_MS) {
            return Promise.resolve(cache);
        }

        if (pending) {
            return pending;
        }

        pending = client.ajax({
            type: 'GET',
            url: client.getUrl('UpcomingEpisodes/Messages'),
            dataType: 'json'
        }).then(function (data) {
            cache = data || {};
            cacheTime = Date.now();
            pending = null;
            return cache;
        }, function () {
            pending = null;
            return {};
        });

        return pending;
    }

    function currentItemId() {
        var match = /[?&]id=([0-9a-f-]{32,36})/i.exec(window.location.hash || '');
        return match ? match[1].replace(/-/g, '').toLowerCase() : null;
    }

    function render() {
        var page = document.querySelector('.itemDetailPage:not(.hide)');
        if (!page) {
            return;
        }

        var container = page.querySelector('.itemMiscInfo-primary') || page.querySelector('.itemMiscInfo');
        if (!container) {
            return;
        }

        var itemId = currentItemId();
        var existing = container.querySelector('.' + BADGE_CLASS);

        if (!itemId) {
            if (existing) {
                existing.remove();
            }

            return;
        }

        loadMessages().then(function (messages) {
            var message = messages[itemId];
            var badge = container.querySelector('.' + BADGE_CLASS);

            if (!message) {
                if (badge) {
                    badge.remove();
                }

                return;
            }

            if (badge) {
                if (badge.textContent !== message) {
                    badge.textContent = message;
                }

                return;
            }

            injectStyle();

            badge = document.createElement('div');
            badge.className = 'mediaInfoItem ' + BADGE_CLASS;
            badge.textContent = message;

            var starRating = container.querySelector('.starRatingContainer');
            if (starRating) {
                starRating.insertAdjacentElement('afterend', badge);
            } else {
                container.appendChild(badge);
            }
        });
    }

    function schedule() {
        if (scheduled) {
            return;
        }

        scheduled = true;
        window.setTimeout(function () {
            scheduled = false;
            try {
                render();
            } catch (err) {
                console.error('[UpcomingEpisodes]', err);
            }
        }, 150);
    }

    window.addEventListener('hashchange', function () {
        cacheTime = 0;
        schedule();
    });

    new MutationObserver(schedule).observe(document.body, { childList: true, subtree: true });

    schedule();
})();
