let eventoDiferido = null;
let referenciaDotNet = null;

window.addEventListener('beforeinstallprompt', (evento) => {
    evento.preventDefault();
    eventoDiferido = evento;
    if (referenciaDotNet) {
        referenciaDotNet.invokeMethodAsync('MostrarBotonInstalar');
    }
});

window.addEventListener('appinstalled', () => {
    eventoDiferido = null;
    if (referenciaDotNet) {
        referenciaDotNet.invokeMethodAsync('OcultarBotonInstalar');
    }
});

window.otiumInstalador = {
    registrar: function (referencia) {
        referenciaDotNet = referencia;
        // Detecta en qué modo está el navegador de forma directa:
        // 'instalado' = ya funciona como app (display-mode standalone).
        // 'prompt'   = el navegador permite instalar con prompt nativo.
        // 'ios'      = iOS: hay que guiar con instrucciones.
        // 'otro'     = navegador web sin prompt nativo: instrucciones genéricas.
        if (window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true) {
            return 'instalado';
        }
        const esIos = /iphone|ipad|ipod/.test(window.navigator.userAgent.toLowerCase());
        if (esIos) return 'ios';
        return eventoDiferido ? 'prompt' : 'otro';
    },
    // Escucha cambios de display-mode por si la app se instala a mitad de sesión.
    observarDisplayMode: function (referencia) {
        referenciaDotNet = referencia;
        window.matchMedia('(display-mode: standalone)').addEventListener('change', (e) => {
            if (e.matches) referencia.invokeMethodAsync('OcultarBotonInstalar');
        });
    },
    esIOS: function () {
        return /iphone|ipad|ipod/.test(window.navigator.userAgent.toLowerCase());
    },
    mostrarPrompt: async function () {
        if (!eventoDiferido) return 'sin-evento';
        eventoDiferido.prompt();
        const resultado = await eventoDiferido.userChoice;
        eventoDiferido = null;
        return resultado.outcome;
    }
};
