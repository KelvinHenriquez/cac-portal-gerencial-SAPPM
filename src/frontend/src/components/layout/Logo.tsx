"use client";
import Link from "next/link";
import { Box, Typography } from "@mui/material";
import { useSidebar } from "@/store/SidebarContext";
import LogoCac from "./LogoCac";

// Logo institucional en el menú: triángulo + "CAC" con el nombre del portal al lado; con el menú
// contraído, solo el triángulo. El nombre de la empresa va como texto (en el logo completo es
// demasiado fino para leerse a este tamaño).
export default function Logo() {
  const { isCollapsed, isHovering } = useSidebar();
  const hideText = isCollapsed && !isHovering;

  return (
    <Link href="/dashboard" style={{ display: "block", textDecoration: "none" }}>
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          justifyContent: hideText ? "center" : "flex-start",
          gap: 1.5,
          height: 56,
        }}
      >
        {hideText ? <LogoCac variante="icono" alto={44} /> : <LogoCac variante="marca" alto={56} />}
        {!hideText && (
          <Box sx={{ overflow: "hidden", borderLeft: 1, borderColor: "divider", pl: 1.5 }}>
            <Typography variant="h6" fontWeight={700} color="text.primary" noWrap sx={{ lineHeight: 1.2 }}>
              Portal Gerencial
            </Typography>
            <Typography variant="caption" color="text.secondary" component="div" sx={{ lineHeight: 1.3 }}>
              Consorcio Azucarero
              <br />
              Central · SAP PM
            </Typography>
          </Box>
        )}
      </Box>
    </Link>
  );
}
