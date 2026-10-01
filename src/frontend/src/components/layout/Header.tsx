"use client";
import { AppBar, Toolbar, Typography, Box, IconButton, useMediaQuery } from "@mui/material";
import { useTheme, styled } from "@mui/material/styles";
import { IconMenu2 } from "@tabler/icons-react";
import { useSidebar, TOPBAR_HEIGHT } from "@/store/SidebarContext";
import IndicadorActualizacion from "./IndicadorActualizacion";

const AppBarStyled = styled(AppBar)(({ theme }) => ({
  boxShadow: "none",
  background: theme.palette.background.paper,
  borderBottom: `1px solid ${theme.palette.divider}`,
}));

// Mismo patrón que Header.tsx (vertical) de la plantilla de TPresto: AppBar "sticky" con el
// toggle del sidebar y el título a la izquierda, "Última actualización" a la derecha (sección 8.4).
export default function Header({ titulo }: { titulo: string }) {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up("lg"));
  const { toggleCollapsed, toggleMobile } = useSidebar();

  return (
    <AppBarStyled position="sticky" color="default" sx={{ minHeight: TOPBAR_HEIGHT }}>
      <Toolbar sx={{ minHeight: `${TOPBAR_HEIGHT}px !important`, gap: 1.5 }}>
        <IconButton color="inherit" onClick={isDesktop ? toggleCollapsed : toggleMobile} aria-label="Menú">
          <IconMenu2 size={20} />
        </IconButton>

        <Typography variant="h6" fontWeight={600} noWrap>
          {titulo}
        </Typography>

        <Box flexGrow={1} />

        <IndicadorActualizacion />
      </Toolbar>
    </AppBarStyled>
  );
}
