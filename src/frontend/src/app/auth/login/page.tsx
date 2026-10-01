"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { Box, Paper, TextField, Button, Typography, Alert, Stack } from "@mui/material";
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
        p: 2,
      }}
    >
      <Paper elevation={3} sx={{ p: 4, width: "100%", maxWidth: 400 }}>
        <Stack spacing={1} sx={{ mb: 3, textAlign: "center" }}>
          <Typography variant="h5" fontWeight={700}>
            Portal Gerencial SAP PM
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Consorcio Azucarero Central
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
