(function () {
    function temaGuardado() {
        try { return localStorage.getItem('otium-tema'); } catch (e) { return null; }
    }
    function aplicar(tema) {
        document.documentElement.setAttribute('data-theme', tema);
    }
    function aplicarGuardadoOPreferido() {
        var guardado = temaGuardado();
        var preferido = guardado || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
        aplicar(preferido);
    }
    aplicarGuardadoOPreferido();

    window.otiumTema = {
        obtener: function () {
            return document.documentElement.getAttribute('data-theme') || 'light';
        },
        alternar: function () {
            var actual = document.documentElement.getAttribute('data-theme') || 'light';
            var nuevo = actual === 'dark' ? 'light' : 'dark';
            aplicar(nuevo);
            try { localStorage.setItem('otium-tema', nuevo); } catch (e) { }
            return nuevo;
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        if (window.Blazor && window.Blazor.addEventListener) {
            window.Blazor.addEventListener('enhancedload', aplicarGuardadoOPreferido);
        }
    });
})();
