"use client";
import Link from "next/link";
import { Box, Card, CardActionArea, CardContent, Chip, Grid2 as Grid, Stack, Typography } from "@mui/material";
import { IconTractor, IconProgressCheck, IconBuildingFactory2, IconClockHour4, IconPresentation } from "@tabler/icons-react";
import { useAuth, usePermisos } from "@/store/AuthContext";

interface Tablero {
  titulo: string;
  descripcion: string;
  href?: string;
  icon: typeof IconTractor;
}

interface Area {
  /** Código del área (CDS / IND): solo se muestra a quien la ve. */
  codigo: string;
  nombre: string;
  tableros: Tablero[];
}

// Portada del portal: un bloque por área de negocio. El área industrial queda anunciada para que
// el portal se lea desde el primer día como el lugar de todos los reportes gerenciales.
const AREAS: Area[] = [
  {
    codigo: "CDS",
    nombre: "Centro de Servicios (Taller)",
    tableros: [
      {
        titulo: "Disponibilidad de Equipos",
        descripcion: "Equipos disponibles vs. requeridos por asignación, hora a hora, con semáforo de umbrales.",
        href: "/dashboard/taller/disponibilidad",
        icon: IconTractor,
      },
      {
        titulo: "Avance de Órdenes de Trabajo",
        descripcion: "Programado vs. ejecutado de los programas de reparación, semana a semana, con pendientes.",
        href: "/dashboard/taller/avance",
        icon: IconProgressCheck,
      },
      {
        titulo: "Resumen de avance de reparación",
        descripcion: "La lámina semanal por familia: programa vs. ejecución, acumulado, maquinaria entregada y notas del taller.",
        href: "/dashboard/taller/resumen-avance",
        icon: IconPresentation,
      },
      {
        titulo: "Tiempo de Permanencia",
        descripcion: "Días desde el aviso hasta la orden correctiva finalizada, por familia y equipo, con el tiempo de cada paso.",
        href: "/dashboard/taller/permanencia?area=CDS",
        icon: IconClockHour4,
      },
    ],
  },
  {
    codigo: "IND",
    nombre: "Área Industrial",
    tableros: [
      {
        titulo: "Tiempo de Permanencia",
        descripcion: "Días desde el aviso hasta la orden correctiva (ZI01) finalizada, por familia y equipo.",
        href: "/dashboard/taller/permanencia?area=IND",
        icon: IconClockHour4,
      },
      {
        titulo: "Próximamente",
        descripcion: "Avance de órdenes industriales, reporte por ubicaciones técnicas y otros tableros (segunda fase).",
        icon: IconBuildingFactory2,
      },
    ],
  },
];

function TarjetaTablero({ tablero }: { tablero: Tablero }) {
  const Icon = tablero.icon;
  const contenido = (
    <CardContent sx={{ display: "flex", gap: 2, alignItems: "flex-start" }}>
      <Box
        sx={{
          width: 48,
          height: 48,
          flexShrink: 0,
          borderRadius: "10px",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          bgcolor: tablero.href ? "primary.light" : "grey.100",
          color: tablero.href ? "primary.main" : "text.secondary",
        }}
      >
        <Icon size={26} stroke={1.5} />
      </Box>
      <Box>
        <Typography variant="subtitle1" fontWeight={600}>
          {tablero.titulo}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {tablero.descripcion}
        </Typography>
      </Box>
    </CardContent>
  );

  return (
    <Card variant="outlined" sx={{ height: "100%", opacity: tablero.href ? 1 : 0.7 }}>
      {tablero.href ? (
        <CardActionArea component={Link} href={tablero.href} sx={{ height: "100%" }}>
          {contenido}
        </CardActionArea>
      ) : (
        contenido
      )}
    </Card>
  );
}

export default function InicioPage() {
  const { usuario } = useAuth();
  const permisos = usePermisos();
  const areas = AREAS.filter((a) => permisos.veArea(a.codigo));

  return (
    <Stack spacing={4}>
      <Box>
        <Typography variant="h5" fontWeight={700}>
          Bienvenido, {usuario?.usuario}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Reportes y tableros gerenciales a partir de la información de SAP PM.
        </Typography>
      </Box>

      {areas.map((area) => (
        <Box key={area.nombre}>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1.5 }}>
            <Typography variant="h6" fontWeight={600}>
              {area.nombre}
            </Typography>
            {area.tableros.every((t) => !t.href) && <Chip label="Fase futura" size="small" />}
          </Stack>
          <Grid container spacing={2}>
            {area.tableros.map((t) => (
              <Grid key={t.titulo} size={{ xs: 12, md: 6 }}>
                <TarjetaTablero tablero={t} />
              </Grid>
            ))}
          </Grid>
        </Box>
      ))}
    </Stack>
  );
}
