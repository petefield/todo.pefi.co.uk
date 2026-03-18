const CACHE_NAME = 'todos-v1';

self.addEventListener('install', event => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(clients.claim());
});

self.addEventListener('fetch', event => {
    // Network-first strategy: Blazor Server needs a live connection,
    // but we cache static assets as a fallback for faster loads
    if (event.request.method !== 'GET') return;

    const url = new URL(event.request.url);
    const isStaticAsset = url.pathname.match(/\.(css|js|png|jpg|svg|woff2?|ico)$/);

    if (isStaticAsset) {
        event.respondWith(
            caches.open(CACHE_NAME).then(cache =>
                fetch(event.request)
                    .then(response => {
                        cache.put(event.request, response.clone());
                        return response;
                    })
                    .catch(() => cache.match(event.request))
            )
        );
    }
});
