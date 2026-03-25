const CACHE_NAME = 'todos-v1';

self.addEventListener('install', event => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(clients.claim());
});

self.addEventListener('push', event => {
    let data = { title: "📝 To Do's", body: 'You have to dos remaining.' };
    if (event.data) {
        try { data = JSON.parse(event.data.text()); } catch { }
    }
    event.waitUntil(
        self.registration.showNotification(data.title, {
            body: data.body,
            icon: '/icon-192.png',
            badge: '/icon-192.png',
            tag: 'daily-todo-reminder',
            renotify: true
        })
    );
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(clientList => {
            for (const client of clientList) {
                if (client.url.includes(self.location.origin) && 'focus' in client)
                    return client.focus();
            }
            return clients.openWindow('/');
        })
    );
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
