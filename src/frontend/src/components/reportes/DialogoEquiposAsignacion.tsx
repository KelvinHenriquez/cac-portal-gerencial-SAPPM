"use client";
import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Stack,
  Switch,
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
import { dias } from "./permanencia/comunesPermanencia";
import { disponibilidadService } from "@/services/disponibilidad/disponibilidadService";
import { ApiError, EquiposAsignacionDto, EstadoEquipo } from "@/types";
import { fechaHora } from "@/utils/formato";
import { ChipEstadoEquipo, etiquetaEstadoEquipo, TIPO_ORDEN } from "./comunes";

const ORDEN_ESTADOS: EstadoEquipo[] = ["FUERA_SERVICIO", "SIN_SAP", "DISPONIBLE", "INACTIVO"];

/** Equipos de una asignación y su estado en la última foto. */
export default function DialogoEquiposAsignacion({
  reporteId,
  asignacionId,
  periodo,
  onClose,
}: {
  reporteId: number;
  asignacionId: number | null;
  /** Período del MTTR/MTBF de cada equipo (el del reporte hasta hoy). */
  periodo: { desde: string; hasta: string } | null;
  onClose: () => void;
}) {
  const theme = useTheme();
  const pantallaChica = useMediaQuery(theme.breakpoints.down("md"));
  const [datos, setDatos] = useState<EquiposAsignacionDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [soloFuera, setSoloFuera] = useState(false);

  useEffect(() => {
    if (asignacionId == null) return;
    setDatos(null);
    setError(null);
    setSoloFuera(false);
    disponibilidadService
      .getEquipos(reporteId, asignacionId, periodo ?? undefined)
      .then(setDatos)
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudo cargar el detalle."));
  }, [reporteId, asignacionId, periodo]);

  const conteo = useMemo(() => {
    const c: Partial<Record<EstadoEquipo, number>> = {};
    datos?.equipos.forEach((e) => (c[e.estado] = (c[e.estado] ?? 0) + 1));
    return c;
  }, [datos]);

  const equipos = datos?.equipos.filter((e) => !soloFuera || e.estado === "FUERA_SERVICIO") ?? [];

  return (
    <Dialog open={asignacionId != null} onClose={onClose} fullWidth maxWidth="lg" fullScreen={pantallaChica}>
      <DialogTitle>
        {datos ? datos.asignacion : "Equipos"}
        {datos && (
          <Typography variant="body2" color="text.secondary">
            Estado de los equipos a las {fechaHora(datos.fecha)} · {datos.cantRequerida} requeridos
          </Typography>
        )}
      </DialogTitle>
      <DialogContent dividers>
        {error && <Alert severity="error">{error}</Alert>}
        {!datos && !error && (
          <Box sx={{ display: "flex", justifyContent: "center", py: 6 }}>
            <CircularProgress />
          </Box>
        )}
        {datos && (
          <Stack spacing={2}>
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap alignItems="center">
              {ORDEN_ESTADOS.filter((e) => conteo[e]).map((e) => (
                <Chip key={e} size="small" label={`${etiquetaEstadoEquipo(e)}: ${conteo[e]}`} />
              ))}
              <Box flexGrow={1} />
              <FormControlLabel
                control={<Switch checked={soloFuera} onChange={(e) => setSoloFuera(e.target.checked)} />}
                label="Solo fuera de servicio"
              />
            </Stack>
            {(conteo.SIN_SAP ?? 0) > 0 && (
              <Alert severity="warning" variant="outlined">
                {conteo.SIN_SAP} equipo(s) están en Oracle pero no se encontraron en SAP por su código legado: cuentan como
                disponibles porque no tienen órdenes.
              </Alert>
            )}
            <TableContainer>
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>Ficha</TableCell>
                    <TableCell>Equipo</TableCell>
                    <TableCell>Equipo SAP</TableCell>
                    <TableCell>Descripción</TableCell>
                    <TableCell>Estado</TableCell>
                    <TableCell align="right" title="Tiempo medio entre fallas">MTBF (d)</TableCell>
                    <TableCell align="right" title="Tiempo medio de restauración">MTTR (d)</TableCell>
                    <TableCell>Órdenes que lo detienen</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {equipos.map((e) => (
                    <TableRow key={e.ficha} hover>
                      <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>{e.ficha}</TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>{e.equipoCodigo ?? "—"}</TableCell>
                      <TableCell sx={{ fontVariantNumeric: "tabular-nums" }}>{e.equipoSapId ?? "—"}</TableCell>
                      <TableCell>{e.descripcion}</TableCell>
                      <TableCell>
                        <ChipEstadoEquipo estado={e.estado} />
                      </TableCell>
                      <TableCell align="right" sx={{ fontVariantNumeric: "tabular-nums" }} title={`${e.fallas} fallas en el período`}>
                        {dias(e.mtbfHoras)}
                      </TableCell>
                      <TableCell align="right" sx={{ fontVariantNumeric: "tabular-nums" }}>
                        {dias(e.mttrHoras)}
                      </TableCell>
                      <TableCell>
                        {e.ordenes.length === 0
                          ? "—"
                          : e.ordenes.map((o) => (
                              <Box key={o.ordenSapId} sx={{ fontSize: "0.8125rem" }}>
                                <b>{o.ordenSapId}</b> {TIPO_ORDEN[o.tipo] ?? o.tipo} · {o.estadoActual} · desde {fechaHora(o.desde)}
                                {o.observacion && (
                                  <Box component="span" sx={{ display: "block", color: "text.secondary" }}>
                                    {o.observacion}
                                  </Box>
                                )}
                              </Box>
                            ))}
                      </TableCell>
                    </TableRow>
                  ))}
                  {equipos.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={8} align="center" sx={{ color: "text.secondary", py: 4 }}>
                        Ningún equipo fuera de servicio.
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cerrar</Button>
      </DialogActions>
    </Dialog>
  );
}
