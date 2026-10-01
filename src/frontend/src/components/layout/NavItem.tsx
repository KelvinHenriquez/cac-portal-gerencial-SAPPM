"use client";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { Box, ListItemButton, ListItemIcon, ListItemText, Tooltip } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { MenuItem } from "./MenuItems";

// Mismo estilo (pill redondeada, color de fondo al seleccionar, ícono 24/stroke 1.5) que
// NavItem.tsx de la plantilla de TPresto.
export default function NavItem({
  item,
  hideText,
  onClick,
}: {
  item: MenuItem;
  hideText: boolean;
  onClick?: () => void;
}) {
  const pathname = usePathname();
  const theme = useTheme();
  const Icon = item.icon;
  const isSelected = item.exact
    ? pathname === item.href
    : pathname === item.href || pathname?.startsWith(item.href + "/");

  const content = (
    <ListItemButton
      component={Link}
      href={item.href}
      onClick={onClick}
      selected={isSelected}
      sx={{
        mb: 0.5,
        py: 1.5,
        px: hideText ? 0 : 2,
        borderRadius: "8px",
        display: "flex",
        justifyContent: hideText ? "center" : "flex-start",
        color: isSelected ? theme.palette.primary.contrastText : theme.palette.text.secondary,
        backgroundColor: isSelected ? theme.palette.primary.main : "transparent",
        "&:hover": {
          backgroundColor: isSelected ? theme.palette.primary.dark : theme.palette.primary.light,
          color: isSelected ? theme.palette.primary.contrastText : theme.palette.primary.main,
        },
        // MUI aplica su propio fondo translúcido a .Mui-selected con más especificidad que el
        // backgroundColor de arriba; sin esto el texto blanco queda sobre un fondo casi blanco.
        "&.Mui-selected": {
          backgroundColor: theme.palette.primary.main,
          color: theme.palette.primary.contrastText,
        },
        "&.Mui-selected:hover": {
          backgroundColor: theme.palette.primary.dark,
          color: theme.palette.primary.contrastText,
        },
      }}
    >
      <ListItemIcon
        sx={{
          minWidth: hideText ? "auto" : 36,
          mr: hideText ? 0 : 1.5,
          color: "inherit",
          display: "flex",
          justifyContent: "center",
        }}
      >
        <Icon size={22} stroke={1.5} />
      </ListItemIcon>
      {!hideText && (
        <ListItemText
          primary={item.title}
          primaryTypographyProps={{ fontSize: "0.875rem", fontWeight: isSelected ? 600 : 500 }}
        />
      )}
    </ListItemButton>
  );

  return hideText ? (
    <Tooltip title={item.title} placement="right">
      <Box>{content}</Box>
    </Tooltip>
  ) : (
    content
  );
}
