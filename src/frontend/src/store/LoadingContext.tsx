"use client";
import React, { createContext, useCallback, useContext, useMemo, useState } from "react";
import Backdrop from "@mui/material/Backdrop";
import CircularProgress from "@mui/material/CircularProgress";
import Typography from "@mui/material/Typography";
import Stack from "@mui/material/Stack";

interface LoadingContextType {
  showLoading: (message?: string) => void;
  hideLoading: () => void;
  isLoading: boolean;
}

const LoadingContext = createContext<LoadingContextType | undefined>(undefined);

export const LoadingProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState("Cargando...");

  const showLoading = useCallback((msg?: string) => {
    setMessage(msg || "Cargando...");
    setIsLoading(true);
  }, []);

  const hideLoading = useCallback(() => setIsLoading(false), []);

  const value = useMemo(() => ({ showLoading, hideLoading, isLoading }), [showLoading, hideLoading, isLoading]);

  return (
    <LoadingContext.Provider value={value}>
      {children}
      <Backdrop open={isLoading} sx={{ zIndex: (theme) => theme.zIndex.drawer + 10, color: "#fff" }}>
        <Stack alignItems="center" spacing={2}>
          <CircularProgress color="inherit" />
          <Typography variant="body1">{message}</Typography>
        </Stack>
      </Backdrop>
    </LoadingContext.Provider>
  );
};

export const useGlobalLoading = (): LoadingContextType => {
  const context = useContext(LoadingContext);
  if (!context) throw new Error("useGlobalLoading debe usarse dentro de LoadingProvider");
  return context;
};
