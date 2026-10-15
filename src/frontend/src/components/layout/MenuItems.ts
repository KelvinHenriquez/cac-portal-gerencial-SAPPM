import {
  IconHome,
  IconTractor,
  IconProgressCheck,
  IconRefresh,
  IconAdjustments,
  IconClockHour4,
  IconPresentation,
} from "@tabler/icons-react";

export interface MenuItem {
  id: string;
  title: string;
  icon: typeof IconHome;
  /** Ruta, con ?area=... cuando el mismo reporte se abre para un área. */
  href: string;
  /** Grupo del menú (un área de negocio o "Sistema"); el título se pinta al empezar cada grupo. */
  grupo?: string;
  /** Área que hay que ver para que aparezca ("CDS" o "IND"); sin área, lo ven todos. */
  area?: string;
  /** Solo se marca como seleccionado en la ruta exacta (para "Inicio", que es prefijo de todo). */
  exact?: boolean;
}

/** Ruta sin la consulta (?area=...). */
export const rutaDe = (href: string) => href.split("?")[0];

// Agrupado por área de negocio. Cada persona ve solo las áreas de sus roles de Oracle (la API
// también lo exige). El Avance industrial queda para la segunda fase (los equipos no tienen Ficha).
export const MENU_ITEMS: MenuItem[] = [
  { id: "inicio", title: "Inicio", icon: IconHome, href: "/dashboard", exact: true },
  {
    id: "disponibilidad",
    title: "Disponibilidad de Equipos",
    icon: IconTractor,
    href: "/dashboard/taller/disponibilidad",
    grupo: "Centro de Servicios",
    area: "CDS",
  },
  {
    id: "avance",
    title: "Avance de Órdenes de Trabajo",
    icon: IconProgressCheck,
    href: "/dashboard/taller/avance",
    grupo: "Centro de Servicios",
    area: "CDS",
  },
  {
    id: "resumen-avance",
    title: "Resumen de avance de reparación",
    icon: IconPresentation,
    href: "/dashboard/taller/resumen-avance",
    grupo: "Centro de Servicios",
    area: "CDS",
  },
  {
    id: "permanencia-cds",
    title: "Tiempo de Permanencia",
    icon: IconClockHour4,
    href: "/dashboard/taller/permanencia?area=CDS",
    grupo: "Centro de Servicios",
    area: "CDS",
  },
  {
    id: "permanencia-ind",
    title: "Tiempo de Permanencia",
    icon: IconClockHour4,
    href: "/dashboard/taller/permanencia?area=IND",
    grupo: "Industrial",
    area: "IND",
  },
  {
    id: "actualizacion",
    title: "Estado de actualización",
    icon: IconRefresh,
    href: "/dashboard/sistema/actualizacion",
    grupo: "Sistema",
  },
  {
    id: "parametros",
    title: "Parámetros",
    icon: IconAdjustments,
    href: "/dashboard/sistema/parametros",
    grupo: "Sistema",
  },
];

/** Área que exige cada ruta (null = ninguna). Permanencia exige al menos un área. */
export function areaRequerida(pathname: string): "CDS" | "ALGUNA" | null {
  if (
    pathname.startsWith("/dashboard/taller/disponibilidad") ||
    pathname.startsWith("/dashboard/taller/avance") ||
    pathname.startsWith("/dashboard/taller/resumen-avance")
  )
    return "CDS";
  if (pathname.startsWith("/dashboard/taller/permanencia")) return "ALGUNA";
  return null;
}
