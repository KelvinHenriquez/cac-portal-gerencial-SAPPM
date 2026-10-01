import {
  IconHome,
  IconTractor,
  IconProgressCheck,
  IconRefresh,
} from "@tabler/icons-react";

export interface MenuItem {
  id: string;
  title: string;
  icon: typeof IconHome;
  href: string;
  /** Título de grupo que se pinta antes de este ítem (un área de negocio o "Sistema"). */
  subheader?: string;
  /** Solo se marca como seleccionado en la ruta exacta (para "Inicio", que es prefijo de todo). */
  exact?: boolean;
}

// Agrupado por área de negocio: hoy solo el Centro de Servicios (Taller). Cuando se agreguen
// tableros del área industrial, van como un grupo nuevo aquí y una tarjeta en Inicio.
export const MENU_ITEMS: MenuItem[] = [
  { id: "inicio", title: "Inicio", icon: IconHome, href: "/dashboard", exact: true },
  {
    id: "disponibilidad",
    title: "Disponibilidad de Equipos",
    icon: IconTractor,
    href: "/dashboard/taller/disponibilidad",
    subheader: "Centro de Servicios",
  },
  {
    id: "avance",
    title: "Avance de Órdenes de Trabajo",
    icon: IconProgressCheck,
    href: "/dashboard/taller/avance",
  },
  {
    id: "actualizacion",
    title: "Estado de actualización",
    icon: IconRefresh,
    href: "/dashboard/sistema/actualizacion",
    subheader: "Sistema",
  },
];
