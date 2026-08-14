(function () {
    'use strict';

    var BADGE_CLASS = 'upcomingEpisodeBadge';
    var STYLE_ID = 'upcomingEpisodeBadgeStyle';
    var CACHE_MS = 300000;
    var FAILURE_BACKOFF_MS = 60000;

    // Work only happens after a navigation, a handful of times, with growing gaps.
    // Nothing observes or polls the document while the client is idle or scrolling,
    // which keeps low powered clients (webOS, Android) responsive.
    var ATTEMPT_DELAYS = [0, 250, 750, 1500, 3000];

    var cache = null;
    var cacheTime = 0;
    var pending = null;
    var failureTime = 0;
    var timer = null;
    var attempt = 0;
    var pass = 0;

    function isSupported() {
        return typeof window.Promise === 'function' &&
            typeof window.setTimeout === 'function' &&
            !!document.addEventListener &&
            !!document.querySelector;
    }

    function log(err) {
        if (window.console && window.console.error) {
            window.console.error('[UpcomingEpisodes]', err);
        }
    }

    function injectStyle() {
        if (document.getElementById(STYLE_ID)) {
            return;
        }

        var parent = document.head || document.body;
        if (!parent) {
            return;
        }

        var style = document.createElement('style');
        style.id = STYLE_ID;
        style.textContent = '.' + BADGE_CLASS + '{color:#f5c518;font-weight:700;}';
        parent.appendChild(style);
    }

    function removeNode(node) {
        if (node && node.parentNode) {
            node.parentNode.removeChild(node);
        }
    }

    function currentItemId() {
        var location = window.location;
        var match = /[?&]id=([0-9a-f-]{32,36})/i.exec((location.hash || '') + (location.search || ''));
        return match ? match[1].replace(/-/g, '').toLowerCase() : null;
    }

    function loadMessages() {
        var client = window.ApiClient;
        if (!client ||
            typeof client.ajax !== 'function' ||
            typeof client.getUrl !== 'function' ||
            typeof client.accessToken !== 'function' ||
            !client.accessToken()) {
            return Promise.resolve(null);
        }

        var now = Date.now();

        if (cache && (now - cacheTime) < CACHE_MS) {
            return Promise.resolve(cache);
        }

        if (pending) {
            return pending;
        }

        // A client that cannot reach the endpoint must not retry on every navigation.
        if (failureTime && (now - failureTime) < FAILURE_BACKOFF_MS) {
            return Promise.resolve(cache);
        }

        var request;
        try {
            request = client.ajax({
                type: 'GET',
                url: client.getUrl('UpcomingEpisodes/Messages'),
                dataType: 'json'
            });
        } catch (err) {
            failureTime = now;
            log(err);
            return Promise.resolve(cache);
        }

        pending = Promise.resolve(request).then(function (data) {
            cache = data || {};
            cacheTime = Date.now();
            failureTime = 0;
            pending = null;
            return cache;
        }, function () {
            failureTime = Date.now();
            pending = null;
            return cache;
        });

        return pending;
    }

    function apply(container, message) {
        var badge = container.querySelector('.' + BADGE_CLASS);

        if (!message) {
            removeNode(badge);
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
        if (starRating && starRating.parentNode) {
            starRating.parentNode.insertBefore(badge, starRating.nextSibling);
        } else {
            container.appendChild(badge);
        }
    }

    function render() {
        var page = document.querySelector('.itemDetailPage:not(.hide)');
        if (!page) {
            return;
        }

        var itemId = currentItemId();
        if (!itemId) {
            return;
        }

        var container = page.querySelector('.itemMiscInfo-primary') || page.querySelector('.itemMiscInfo');
        if (!container) {
            return;
        }

        var token = pass;
        loadMessages().then(function (messages) {
            // Skip a response that arrived after the user moved on, and leave the
            // page alone while no messages are known.
            if (!messages || token !== pass || !container.parentNode) {
                return;
            }

            try {
                apply(container, messages[itemId]);
            } catch (err) {
                log(err);
            }
        });
    }

    function queue() {
        if (attempt >= ATTEMPT_DELAYS.length) {
            return;
        }

        timer = window.setTimeout(run, ATTEMPT_DELAYS[attempt++]);
    }

    function run() {
        timer = null;

        try {
            render();
        } catch (err) {
            log(err);
        }

        queue();
    }

    // Called once per navigation: the detail page is built asynchronously, so the
    // attempts above cover both the initial render and any later rebuild of the header.
    function restart() {
        pass++;
        attempt = 0;

        if (timer) {
            window.clearTimeout(timer);
            timer = null;
        }

        queue();
    }

    function start() {
        try {
            window.addEventListener('hashchange', restart);
            window.addEventListener('popstate', restart);
            document.addEventListener('viewshow', restart);
            restart();
        } catch (err) {
            log(err);
        }
    }

    try {
        if (isSupported()) {
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', start);
            } else {
                window.setTimeout(start, 0);
            }
        }
    } catch (err) {
        log(err);
    }
})();
