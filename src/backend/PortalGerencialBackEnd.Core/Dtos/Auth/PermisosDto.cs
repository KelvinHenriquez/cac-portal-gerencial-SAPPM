using PortalGerencialBackEnd.Core.Constants;

namespace PortalGerencialBackEnd.Core.Dtos.Auth
{
    /// <summary>
    /// Qué puede hacer el usuario según sus roles de Oracle. La API lo aplica en cada consulta; el
    /// frontend lo usa para mostrar solo el menú, las tarjetas y los botones que corresponden.
    /// </summary>
    public class PermisosDto
    {
        /// <summary>Áreas que ve: CDS y/o IND.</summary>
        public List<string> Areas { get; set; } = new();

        /// <summary>Edita los parámetros de negocio (Disponibilidad, Avance, Permanencia).</summary>
        public bool EditarParametros { get; set; }

        /// <summary>Edita también los técnicos (SAP y horario de la actualización): solo TI.</summary>
        public bool EditarParametrosTecnicos { get; set; }

        /// <summary>"Actualizar ahora" y "Actualizar desde": solo TI.</summary>
        public bool Actualizar { get; set; }

        public bool VeArea(string area) => Areas.Contains(area, StringComparer.OrdinalIgnoreCase);

        public static PermisosDto De(IEnumerable<string> roles)
        {
            var r = roles.Select(x => x.ToUpperInvariant()).ToHashSet();
            var admin = r.Contains(Roles.Admin);
            var areas = new List<string>();
            if (admin || r.Contains(Roles.Gerencial) || r.Contains(Roles.CentroServicios))
                areas.Add(Constants.Areas.CentroServicios);
            if (admin || r.Contains(Roles.Gerencial) || r.Contains(Roles.Industrial))
                areas.Add(Constants.Areas.Industrial);
            return new PermisosDto
            {
                Areas = areas,
                EditarParametros = admin || r.Contains(Roles.Parametros),
                EditarParametrosTecnicos = admin,
                Actualizar = admin
            };
        }
    }
}
