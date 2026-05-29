// Minimal service worker — no aggressive caching that breaks on Netlify
self.addEventListener('install', event => {
    console.log('Service worker: Install');
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys.map(key => caches.delete(key)))
        )
    );
});

self.addEventListener('fetch', event => {
    // Only handle GET requests
    if (event.request.method !== 'GET') return;

    // Do not intercept API calls to Google
    const url = event.request.url;
    if (url.includes('googleapis.com') ||
        url.includes('accounts.google.com') ||
        url.includes('google.com')) {
        return;
    }

    event.respondWith(
        fetch(event.request).catch(() => {
            // If fetch fails, try cache
            return caches.match(event.request);
        })
    );
});