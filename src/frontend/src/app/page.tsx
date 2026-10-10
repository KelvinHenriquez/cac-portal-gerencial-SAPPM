"use client";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import Box from "@mui/material/Box";
import { TarjetaCargando } from "@/store/LoadingContext";
import { useAuth } from "@/store/AuthContext";

export default function Home() {
  const { usuario, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (loading) return;
    router.replace(usuario ? "/dashboard" : "/auth/login");
  }, [usuario, loading, router]);

  return (
    <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", height: "100vh", bgcolor: "background.default" }}>
      <TarjetaCargando mensaje="Cargando..." />
    </Box>
  );
}
