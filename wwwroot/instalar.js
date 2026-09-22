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
        if (eventoDiferido) {
            referencia.invokeMethodAsync('MostrarBotonInstalar');
        }
    },
    esIOS: function () {
        return /iphone|ipad|ipod/.test(window.navigator.userAgent.toLowerCase());
    },
    yaInstalada: function () {
        return window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    },
    mostrarPrompt: async function () {
        if (!eventoDiferido) return 'sin-evento';
        eventoDiferido.prompt();
        const resultado = await eventoDiferido.userChoice;
        eventoDiferido = null;
        return resultado.outcome;
    }
};
