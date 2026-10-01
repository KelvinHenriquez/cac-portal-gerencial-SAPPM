"use client";
import Link from "next/link";
import { Box, Card, CardActionArea, CardContent, Chip, Grid2 as Grid, Stack, Typography } from "@mui/material";
import { IconTractor, IconProgressCheck, IconBuildingFactory2 } from "@tabler/icons-react";
import { useAuth } from "@/store/AuthContext";

interface Tablero {
  titulo: string;
  descripcion: string;
  href?: string;
  icon: typeof IconTractor;
}

interface Area {
  nombre: string;
  tableros: Tablero[];
}

// Portada del portal: un bloque por área de negocio. El área industrial queda anunciada para que
// el portal se lea desde el primer día como el lugar de todos los reportes gerenciales.
const AREAS: Area[] = [
  {
    nombre: "Centro de Servicios (Taller)",
    tableros: [
      {
        titulo: "Disponibilidad de Maquinaria",
        descripcion: "Equipos disponibles vs. requeridos por familia, con semáforo de umbrales.",
        href: "/dashboard/taller/disponibilidad",
        icon: IconTractor,
      },
      {
        titulo: "Avance de Reparación",
        descripcion: "Avance planificado vs. real del programa de reparación, semana a semana.",
        href: "/dashboard/taller/avance",
        icon: IconProgressCheck,
      },
    ],
  },
  {
    nombre: "Área Industrial",
    tableros: [
      {
        titulo: "Próximamente",
        descripcion: "Los tableros del área industrial se agregarán en una fase posterior.",
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

      {AREAS.map((area) => (
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
