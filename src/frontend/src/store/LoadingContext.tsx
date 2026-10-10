"use client";
import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { usePathname } from "next/navigation";
import { Backdrop, LinearProgress, Paper, Typography } from "@mui/material";
import { keyframes } from "@mui/material/styles";
import LogoCac from "@/components/layout/LogoCac";

interface LoadingContextType {
  showLoading: (message?: string) => void;
  hideLoading: () => void;
  isLoading: boolean;
}

const LoadingContext = createContext<LoadingContextType | undefined>(undefined);

const latido = keyframes`
  0%, 100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(0.93); opacity: 0.75; }
`;

/** Tarjeta de "cargando" con el logo de CAC: la misma en el overlay y al validar la sesión. */
export function TarjetaCargando({ mensaje }: { mensaje: string }) {
  return (
    <Paper
      elevation={8}
      role="status"
      aria-live="polite"
      sx={{
        px: 5,
        py: 4,
        width: 300,
        maxWidth: "calc(100vw - 32px)",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 2.5,
        borderRadius: 3,
        borderTop: 4,
        borderColor: "primary.main",
      }}
    >
      <LogoCac
        variante="completo"
        alto={120}
        sx={{
          animation: `${latido} 1.6s ease-in-out infinite`,
          "@media (prefers-reduced-motion: reduce)": { animation: "none" },
        }}
      />
      <LinearProgress sx={{ width: "100%", height: 5, borderRadius: 3 }} />
      <Typography variant="body1" fontWeight={600} color="text.primary" textAlign="center">
        {mensaje}
      </Typography>
    </Paper>
  );
}

/** ¿El clic es en un enlace interno a otra pantalla del portal (menú, tarjetas del Inicio...)? */
function destinoInterno(e: MouseEvent): boolean {
  if (e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return false;
  const a = (e.target as Element | null)?.closest?.("a[href]") as HTMLAnchorElement | null;
  if (!a || (a.target && a.target !== "_self") || a.hasAttribute("download")) return false;
  const url = new URL(a.href, window.location.href);
  return url.origin === window.location.origin && url.pathname !== window.location.pathname;
}

export const LoadingProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState("Cargando...");
  // Cambio de pantalla en curso: desde el clic hasta que la ruta nueva se monta. Va aparte de
  // isLoading porque la página nueva pide su propio "cargando" al montarse (sus efectos corren
  // antes que los de este proveedor).
  const [navegando, setNavegando] = useState(false);
  const pathname = usePathname();

  const showLoading = useCallback((msg?: string) => {
    setMessage(msg || "Cargando...");
    setIsLoading(true);
  }, []);

  const hideLoading = useCallback(() => setIsLoading(false), []);

  useEffect(() => {
    const alHacerClic = (e: MouseEvent) => {
      if (destinoInterno(e)) setNavegando(true);
    };
    // En captura: el <Link> de Next cancela el clic (preventDefault) antes de que llegue a document.
    document.addEventListener("click", alHacerClic, true);
    return () => document.removeEventListener("click", alHacerClic, true);
  }, []);

  // La ruta nueva ya está montada. Se espera un instante: la página suele pedir su propio
  // "cargando" al traer sus datos y así la tarjeta no parpadea entre una cosa y la otra.
  useEffect(() => {
    const t = setTimeout(() => setNavegando(false), 500);
    return () => clearTimeout(t);
  }, [pathname]);

  // Si la navegación no llega a completarse (error, sesión vencida), no dejar la pantalla tapada.
  useEffect(() => {
    if (!navegando) return;
    const t = setTimeout(() => setNavegando(false), 15000);
    return () => clearTimeout(t);
  }, [navegando]);

  const value = useMemo(() => ({ showLoading, hideLoading, isLoading }), [showLoading, hideLoading, isLoading]);
  const abierto = isLoading || navegando;

  return (
    <LoadingContext.Provider value={value}>
      {children}
      <Backdrop
        open={abierto}
        sx={{
          zIndex: (theme) => theme.zIndex.drawer + 10,
          bgcolor: "rgba(17, 32, 23, 0.45)",
          backdropFilter: "blur(2px)",
        }}
      >
        {abierto && <TarjetaCargando mensaje={isLoading ? message : "Cargando..."} />}
      </Backdrop>
    </LoadingContext.Provider>
  );
};

export const useGlobalLoading = (): LoadingContextType => {
  const context = useContext(LoadingContext);
  if (!context) throw new Error("useGlobalLoading debe usarse dentro de LoadingProvider");
  return context;
};
