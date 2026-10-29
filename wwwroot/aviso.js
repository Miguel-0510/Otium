window.otiumAviso = {
    CLAVE: 'otium:avisoBienvenidaOculto',
    estaOculto: function () {
        try {
            return window.localStorage.getItem(window.otiumAviso.CLAVE) === '1';
        } catch {
            return false;
        }
    },
    ocultarSiempre: function () {
        try {
            window.localStorage.setItem(window.otiumAviso.CLAVE, '1');
        } catch {
            // Sin acceso a localStorage (navegación privada), el aviso simplemente se muestra de nuevo.
        }
    },
    bloquearFondo: function () {
        document.body.classList.add('aviso-fondo-bloqueado');
    },
    liberarFondo: function () {
        document.body.classList.remove('aviso-fondo-bloqueado');
    }
};
