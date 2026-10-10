"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { Box, Paper, TextField, Button, Typography, Alert, Stack } from "@mui/material";
import LogoCac from "@/components/layout/LogoCac";
import { useAuth } from "@/store/AuthContext";
import { useGlobalLoading } from "@/store/LoadingContext";
import { ApiError } from "@/types";

export default function LoginPage() {
  const { login } = useAuth();
  const { showLoading, hideLoading } = useGlobalLoading();
  const router = useRouter();

  const [usuario, setUsuario] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    showLoading("Validando credenciales...");
    try {
      await login(usuario, password);
      router.replace("/dashboard");
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError?.errorMessage || "No se pudo iniciar sesión.");
      setPassword("");
    } finally {
      hideLoading();
    }
  };

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        bgcolor: "background.default",
        backgroundImage: "linear-gradient(160deg, rgba(0,152,69,0.10) 0%, rgba(147,213,0,0.06) 45%, rgba(0,0,0,0) 75%)",
        p: 2,
      }}
    >
      <Paper elevation={3} sx={{ p: 4, width: "100%", maxWidth: 400, borderTop: 4, borderColor: "primary.main" }}>
        <Stack spacing={1.5} alignItems="center" sx={{ mb: 3, textAlign: "center" }}>
          <LogoCac variante="completo" alto={170} />
          <Typography variant="h5" fontWeight={700} sx={{ pt: 1 }}>
            Portal Gerencial SAP PM
          </Typography>
        </Stack>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit}>
          <Stack spacing={2}>
            <TextField
              label="Usuario"
              value={usuario}
              onChange={(e) => setUsuario(e.target.value)}
              required
              fullWidth
              autoFocus
              autoComplete="username"
            />
            <TextField
              label="Contraseña"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              fullWidth
              autoComplete="current-password"
            />
            <Button type="submit" variant="contained" size="large" fullWidth>
              Entrar
            </Button>
            <Typography variant="caption" color="text.secondary" textAlign="center">
              Use el mismo usuario y contraseña con que entra a Oracle.
            </Typography>
          </Stack>
        </Box>
      </Paper>
    </Box>
  );
}
