// Service worker de Otium.
// Otium necesita conexion a internet: no guarda paginas en cache.
// Solo guarda una pagina de aviso para mostrarla si se abre sin conexion.
const VERSION = 'otium-v1';
const PAGINA_SIN_CONEXION = '/sin-conexion.html';

self.addEventListener('install', (evento) => {
    evento.waitUntil(
        caches.open(VERSION).then((cache) => cache.add(PAGINA_SIN_CONEXION))
    );
    self.skipWaiting();
});

self.addEventListener('activate', (evento) => {
    evento.waitUntil(
        caches.keys()
            .then((nombres) => Promise.all(
                nombres.filter((n) => n !== VERSION).map((n) => caches.delete(n))
            ))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', (evento) => {
    // Solo interviene cuando se abre o recarga una pagina.
    if (evento.request.mode !== 'navigate') {
        return;
    }
    evento.respondWith(
        fetch(evento.request).catch(() => caches.match(PAGINA_SIN_CONEXION))
    );
});
