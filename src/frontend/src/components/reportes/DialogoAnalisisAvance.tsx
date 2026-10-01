"use client";
import { useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid2 as Grid,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  useMediaQuery,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { avanceService } from "@/services/avance/avanceService";
import { AnalisisAvanceDto, AnalisisFilaDto, ApiError } from "@/types";
import { fechaLarga } from "@/utils/formato";
import { num } from "./comunes";

const filaTotal = { "& td": { fontWeight: 700, bgcolor: "#FFF9DB" } };

function Cifras({ f }: { f: AnalisisFilaDto }) {
  return (
    <>
      <TableCell align="right">{f.actTotal}</TableCell>
      <TableCell align="right">{f.actTerminadas}</TableCell>
      <TableCell align="right">{num(f.diasActProg)}</TableCell>
      <TableCell align="right">{num(f.diasActTerm)}</TableCell>
      <TableCell align="right" sx={{ fontWeight: 700 }}>
        {f.avancePct}
      </TableCell>
    </>
  );
}

const encabezadoCifras = (
  <>
    <TableCell align="right">Act. total</TableCell>
    <TableCell align="right">Terminadas</TableCell>
    <TableCell align="right">Días act. prog.</TableCell>
    <TableCell align="right">Días act. term.</TableCell>
    <TableCell align="right">Avance %</TableCell>
  </>
);

/** "Análisis de Avance" de una familia: por equipo (orden) y por sistema, como en Oracle. */
export default function DialogoAnalisisAvance({
  reporteId,
  familiaCodigo,
  onClose,
}: {
  reporteId: number;
  familiaCodigo: string | null;
  onClose: () => void;
}) {
  const theme = useTheme();
  const pantallaChica = useMediaQuery(theme.breakpoints.down("md"));
  const [datos, setDatos] = useState<AnalisisAvanceDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!familiaCodigo) return;
    setDatos(null);
    setError(null);
    avanceService
      .getAnalisis(reporteId, familiaCodigo)
      .then(setDatos)
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudo cargar el análisis."));
  }, [reporteId, familiaCodigo]);

  return (
    <Dialog open={!!familiaCodigo} onClose={onClose} fullWidth maxWidth="xl" fullScreen={pantallaChica}>
      <DialogTitle>
        Análisis de avance {datos ? `— ${datos.familiaNombre}` : ""}
        <Typography variant="body2" color="text.secondary">
          Por actividades: Avance % = días de actividades terminadas / días programados.
        </Typography>
      </DialogTitle>
      <DialogContent dividers>
        {error && <Alert severity="error">{error}</Alert>}
        {!datos && !error && (
          <Box sx={{ display: "flex", justifyContent: "center", py: 6 }}>
            <CircularProgress />
          </Box>
        )}
        {datos && (
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, lg: 8 }}>
              <Typography variant="subtitle2" fontWeight={700} sx={{ mb: 1 }}>
                Avance de órdenes de trabajo por equipos
              </Typography>
              <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 520 }}>
                <Table size="small" stickyHeader>
                  <TableHead>
                    <TableRow>
                      <TableCell>Equipo</TableCell>
                      <TableCell>Orden</TableCell>
                      <TableCell>Estado O.T.</TableCell>
                      <TableCell>Fecha real I.</TableCell>
                      <TableCell>Fecha real F.</TableCell>
                      {encabezadoCifras}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    <TableRow sx={filaTotal}>
                      <TableCell>{datos.total.nombre}</TableCell>
                      <TableCell>—</TableCell>
                      <TableCell>—</TableCell>
                      <TableCell>—</TableCell>
                      <TableCell>—</TableCell>
                      <Cifras f={datos.total} />
                    </TableRow>
                    {datos.equipos.map((e) => (
                      <TableRow key={e.ordenSapId} hover>
                        <TableCell sx={{ whiteSpace: "nowrap" }}>
                          ({e.ficha}) {e.nombre}
                        </TableCell>
                        <TableCell sx={{ fontVariantNumeric: "tabular-nums" }}>{e.ordenSapId}</TableCell>
                        <TableCell>{e.estado}</TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap" }}>{e.fechaRealInicio ? fechaLarga(e.fechaRealInicio) : ""}</TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap" }}>{e.fechaRealFin ? fechaLarga(e.fechaRealFin) : ""}</TableCell>
                        <Cifras f={e} />
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Grid>
            <Grid size={{ xs: 12, lg: 4 }}>
              <Typography variant="subtitle2" fontWeight={700} sx={{ mb: 1 }}>
                Avance de órdenes de trabajo por sistemas
              </Typography>
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Sistema</TableCell>
                      {encabezadoCifras}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {datos.sistemas.map((s) => (
                      <TableRow key={s.nombre} hover>
                        <TableCell>{s.nombre}</TableCell>
                        <Cifras f={s} />
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Grid>
          </Grid>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Regresar</Button>
      </DialogActions>
    </Dialog>
  );
}
