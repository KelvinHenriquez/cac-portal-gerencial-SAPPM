"use client";
import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Alert, Box } from "@mui/material";
import { TarjetaCargando } from "@/store/LoadingContext";
import { useAuth, usePermisos } from "@/store/AuthContext";
import { areaRequerida } from "@/components/layout/MenuItems";
import { SidebarProvider } from "@/store/SidebarContext";
import Sidebar from "@/components/layout/Sidebar";
import Header from "@/components/layout/Header";
import { usePageTitle } from "@/components/layout/usePageTitle";

function DashboardShell({ children }: { children: React.ReactNode }) {
  const titulo = usePageTitle();
  const pathname = usePathname() || "";
  const permisos = usePermisos();
  // La API también lo exige; aquí solo se evita mostrar una pantalla vacía con errores.
  const requerida = areaRequerida(pathname);
  const permitido = requerida === null || (requerida === "ALGUNA" ? permisos.areas.length > 0 : permisos.veArea(requerida));

  return (
    <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "background.default" }}>
      <Sidebar />
      <Box sx={{ flex: 1, minWidth: 0, display: "flex", flexDirection: "column" }}>
        <Header titulo={titulo} />
        <Box sx={{ p: { xs: 2, md: 3 }, flex: 1 }}>
          {permitido ? children : <Alert severity="warning">Su usuario no tiene acceso a este reporte. Si lo necesita, pídale a TI el rol del área.</Alert>}
        </Box>
      </Box>
    </Box>
  );
}

// Un único rol (sección 7): basta con estar autenticado para ver todo el portal.
export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const { usuario, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!loading && !usuario) {
      router.replace("/auth/login");
    }
  }, [usuario, loading, router]);

  if (loading || !usuario) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", height: "100vh", bgcolor: "background.default" }}>
        <TarjetaCargando mensaje="Verificando sesión..." />
      </Box>
    );
  }

  return (
    <SidebarProvider>
      <DashboardShell>{children}</DashboardShell>
    </SidebarProvider>
  );
}
