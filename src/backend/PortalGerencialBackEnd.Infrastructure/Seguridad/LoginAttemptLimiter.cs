using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PortalGerencialBackEnd.Core.Settings;

namespace PortalGerencialBackEnd.Infrastructure.Seguridad
{
    /// <summary>
    /// Cuenta intentos fallidos por usuario en memoria (el portal corre en una sola instancia). Al
    /// llegar a LoginSettings.MaxIntentosFallidos dentro de la ventana, el usuario queda frenado en el
    /// portal durante BloqueoMinutos, sin que sus intentos lleguen a Oracle ni sumen a su contador.
    /// </summary>
    public class LoginAttemptLimiter
    {
        private sealed class Estado
        {
            public int Fallidos;
            public DateTimeOffset InicioVentana;
            public DateTimeOffset? BloqueadoHasta;
        }

        private readonly IMemoryCache _cache;
        private readonly LoginSettings _settings;
        private readonly Func<DateTimeOffset> _ahora;
        private readonly object _lock = new();

        public LoginAttemptLimiter(IMemoryCache cache, IOptions<LoginSettings> settings)
            : this(cache, settings.Value, () => DateTimeOffset.UtcNow) { }

        public LoginAttemptLimiter(IMemoryCache cache, LoginSettings settings, Func<DateTimeOffset> ahora)
        {
            _cache = cache;
            _settings = settings;
            _ahora = ahora;
        }

        private static string Clave(string usuario) => $"login-fallidos:{usuario}";

        /// <summary>Hasta cuándo está frenado el usuario, o null si puede intentar.</summary>
        public DateTimeOffset? BloqueadoHasta(string usuario)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(Clave(usuario), out Estado? estado) && estado!.BloqueadoHasta > _ahora())
                    return estado.BloqueadoHasta;
                return null;
            }
        }

        /// <summary>Registra un fallo. Devuelve los intentos que le quedan antes del bloqueo (0 = bloqueado).</summary>
        public int RegistrarFallo(string usuario)
        {
            lock (_lock)
            {
                var ahora = _ahora();
                if (!_cache.TryGetValue(Clave(usuario), out Estado? estado)
                    || estado!.InicioVentana.AddMinutes(_settings.VentanaMinutos) < ahora
                    || estado.BloqueadoHasta <= ahora)
                {
                    estado = new Estado { InicioVentana = ahora };
                }

                estado.Fallidos++;
                if (estado.Fallidos >= _settings.MaxIntentosFallidos)
                    estado.BloqueadoHasta = ahora.AddMinutes(_settings.BloqueoMinutos);

                var expira = TimeSpan.FromMinutes(Math.Max(_settings.VentanaMinutos, _settings.BloqueoMinutos));
                _cache.Set(Clave(usuario), estado, expira);

                return Math.Max(0, _settings.MaxIntentosFallidos - estado.Fallidos);
            }
        }

        public void RegistrarExito(string usuario)
        {
            lock (_lock)
            {
                _cache.Remove(Clave(usuario));
            }
        }
    }
}
