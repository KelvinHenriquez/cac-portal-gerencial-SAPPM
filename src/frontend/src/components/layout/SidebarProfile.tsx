"use client";
import { Box, Avatar, Typography, IconButton, Tooltip } from "@mui/material";
import { IconPower } from "@tabler/icons-react";
import { useAuth } from "@/store/AuthContext";
import { RolUsuario } from "@/types";
import { useSidebar } from "@/store/SidebarContext";

// Misma tarjeta (avatar con iniciales + usuario + rol + botón de logout) que
// SidebarProfile/Profile.tsx de la plantilla de TPresto. Sin "cambiar contraseña": la clave es
// la de Oracle y la administra el DBA (sección 4.3).
const NOMBRE_ROL: Record<RolUsuario, string> = {
  ROL_PORTAL_ADMIN: "Administrador (TI)",
  ROL_PORTAL_GERENCIAL: "Gerencia",
  ROL_PORTAL_CDS: "Centro de Servicios",
  ROL_PORTAL_IND: "Industrial",
  ROL_PORTAL_PARAMETROS: "Parámetros",
};
const ORDEN: RolUsuario[] = ["ROL_PORTAL_ADMIN", "ROL_PORTAL_GERENCIAL", "ROL_PORTAL_CDS", "ROL_PORTAL_IND", "ROL_PORTAL_PARAMETROS"];

/** "Centro de Servicios · Parámetros"; el administrador ya lo incluye todo. */
function describirRoles(roles: RolUsuario[]) {
  if (roles.includes("ROL_PORTAL_ADMIN")) return NOMBRE_ROL.ROL_PORTAL_ADMIN;
  return ORDEN.filter((r) => roles.includes(r)).map((r) => NOMBRE_ROL[r]).join(" · ");
}

export default function SidebarProfile() {
  const { usuario, logout } = useAuth();
  const { isCollapsed, isHovering } = useSidebar();
  const hideText = isCollapsed && !isHovering;

  if (!usuario) return null;

  return (
    <Box
      sx={{
        display: "flex",
        alignItems: "center",
        justifyContent: hideText ? "center" : "flex-start",
        gap: hideText ? 0 : 1.5,
        m: 2,
        p: hideText ? 1 : 1.5,
        bgcolor: "primary.light",
        border: 1,
        borderColor: "primary.main",
        borderRadius: "10px",
      }}
    >
      <Avatar sx={{ width: 40, height: 40, bgcolor: "primary.main", color: "primary.contrastText" }}>
        {usuario.usuario.slice(0, 2)}
      </Avatar>

      {!hideText && (
        <>
          <Box sx={{ minWidth: 0, flex: 1, overflow: "hidden" }}>
            <Typography variant="subtitle2" fontWeight={600} color="text.primary" noWrap>
              {usuario.usuario}
            </Typography>
            <Typography variant="caption" color="text.secondary" noWrap>
              {describirRoles(usuario.roles)}
            </Typography>
          </Box>
          <Tooltip title="Cerrar sesión" placement="top">
            <IconButton color="primary" size="small" onClick={logout} aria-label="Cerrar sesión">
              <IconPower size={20} />
            </IconButton>
          </Tooltip>
        </>
      )}
    </Box>
  );
}
