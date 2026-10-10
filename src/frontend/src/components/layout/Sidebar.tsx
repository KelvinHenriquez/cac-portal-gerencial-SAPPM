"use client";
import { Box, Drawer, List, useMediaQuery } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import Logo from "./Logo";
import NavItem from "./NavItem";
import NavGroup from "./NavGroup";
import SidebarProfile from "./SidebarProfile";
import { MENU_ITEMS } from "./MenuItems";
import { usePermisos } from "@/store/AuthContext";
import { useSidebar, SIDEBAR_WIDTH, MINI_SIDEBAR_WIDTH } from "@/store/SidebarContext";

// Mismo patrón que Sidebar.tsx de la plantilla de TPresto: drawer permanente y colapsable en
// escritorio, drawer temporal en móvil — sin el store de Redux, con SidebarContext en su lugar.
export default function Sidebar() {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up("lg"));
  const { isCollapsed, isHovering, isMobileOpen, setHovering, closeMobile } = useSidebar();
  const permisos = usePermisos();
  const visibles = MENU_ITEMS.filter((item) => !item.area || permisos.veArea(item.area));

  const hideText = isDesktop && isCollapsed && !isHovering;
  const width = isDesktop && isCollapsed && !isHovering ? MINI_SIDEBAR_WIDTH : SIDEBAR_WIDTH;

  const content = (
    <Box sx={{ height: "100%", display: "flex", flexDirection: "column" }}>
      <Box sx={{ px: hideText ? 1.5 : 3, py: 3 }}>
        <Logo />
      </Box>
      <Box sx={{ flex: 1, overflowY: "auto", overflowX: "hidden", px: 2 }}>
        <List sx={{ pt: 0 }}>
          {visibles.map((item, i) => (
            <Box key={item.id}>
              {item.grupo && item.grupo !== visibles[i - 1]?.grupo && <NavGroup label={item.grupo} hideText={hideText} />}
              <NavItem item={item} hideText={hideText} onClick={isDesktop ? undefined : closeMobile} />
            </Box>
          ))}
        </List>
      </Box>
      <SidebarProfile />
    </Box>
  );

  if (isDesktop) {
    return (
      <Box sx={{ width, flexShrink: 0, transition: "width 0.2s" }}>
        <Drawer
          anchor="left"
          open
          variant="permanent"
          onMouseEnter={() => isCollapsed && setHovering(true)}
          onMouseLeave={() => setHovering(false)}
          PaperProps={{
            sx: {
              width,
              boxSizing: "border-box",
              border: "none",
              boxShadow: "0 4px 6px -1px rgba(0,0,0,0.08), 0 2px 4px -1px rgba(0,0,0,0.05)",
              transition: "width 0.2s",
              overflowX: "hidden",
            },
          }}
        >
          {content}
        </Drawer>
      </Box>
    );
  }

  return (
    <Drawer
      anchor="left"
      open={isMobileOpen}
      onClose={closeMobile}
      variant="temporary"
      PaperProps={{ sx: { width: SIDEBAR_WIDTH } }}
    >
      {content}
    </Drawer>
  );
}
