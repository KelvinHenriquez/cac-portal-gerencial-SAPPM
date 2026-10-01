"use client";
import Link from "next/link";
import { Box, Typography } from "@mui/material";
import { useSidebar } from "@/store/SidebarContext";

// CAC no tiene todavía un archivo de logo — placeholder tipográfico en el mismo lugar y peso
// visual que el <Image> de TPresto (Logo.tsx), fácil de reemplazar por una imagen real después.
export default function Logo() {
  const { isCollapsed, isHovering } = useSidebar();
  const hideText = isCollapsed && !isHovering;

  return (
    <Link href="/dashboard" style={{ display: "block", textDecoration: "none" }}>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1.25, height: 40 }}>
        <Box
          sx={{
            width: 40,
            height: 40,
            borderRadius: "10px",
            bgcolor: "primary.main",
            color: "primary.contrastText",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontWeight: 700,
            fontSize: "1.1rem",
            flexShrink: 0,
          }}
        >
          CAC
        </Box>
        {!hideText && (
          <Box sx={{ overflow: "hidden" }}>
            <Typography variant="h6" fontWeight={700} color="text.primary" noWrap>
              Portal Gerencial
            </Typography>
            <Typography variant="caption" color="text.secondary" noWrap>
              SAP PM · Consorcio Azucarero Central
            </Typography>
          </Box>
        )}
      </Box>
    </Link>
  );
}
