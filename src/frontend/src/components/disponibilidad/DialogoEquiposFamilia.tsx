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
import { disponibilidadService } from "@/services/disponibilidad/disponibilidadService";
import { ApiError, EquiposFamiliaDetalleDto, EstadoEquipo } from "@/types";
import { fechaLarga } from "@/utils/formato";
import { ChipEstadoEquipo, etiquetaEstadoEquipo } from "./semaforo";
import ListaOrdenes from "./ListaOrdenes";

const ORDEN_ESTADOS: EstadoEquipo[] = ["FUERA_SERVICIO", "SIN_SAP", "DISPONIBLE", "INACTIVO"];

/** Drill-down por familia (sección 8.2): equipos individuales y su estado a la fecha de corte. */
export default function DialogoEquiposFamilia({
  tipoReporte,
  familiaCodigo,
  onClose,
}: {
  tipoReporte: string;
  familiaCodigo: string | null;
  onClose: () => void;
}) {
  const theme = useTheme();
  const pantallaChica = useMediaQuery(theme.breakpoints.down("md"));
  const [datos, setDatos] = useState<EquiposFamiliaDetalleDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [soloFuera, setSoloFuera] = useState(false);

  useEffect(() => {
    if (!familiaCodigo) return;
    setDatos(null);
    setError(null);
    setSoloFuera(false);
    disponibilidadService
      .getEquiposFamilia(tipoReporte, familiaCodigo)
      .then(setDatos)
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudo cargar el detalle."));
  }, [tipoReporte, familiaCodigo]);

  const conteo = useMemo(() => {
    const c: Partial<Record<EstadoEquipo, number>> = {};
    datos?.equipos.forEach((e) => (c[e.estado] = (c[e.estado] ?? 0) + 1));
    return c;
  }, [datos]);

  const equipos = datos?.equipos.filter((e) => !soloFuera || e.estado === "FUERA_SERVICIO") ?? [];

  return (
    <Dialog open={!!familiaCodigo} onClose={onClose} fullWidth maxWidth="lg" fullScreen={pantallaChica}>
      <DialogTitle>
        {datos ? datos.familiaNombre : "Equipos"}
        {datos && (
          <Typography variant="body2" color="text.secondary">
            Estado de los equipos al {fechaLarga(datos.fecha)}
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
            <Stack direction={{ xs: "column", sm: "row" }} spacing={2} justifyContent="space-between">
              <Box>
                <Typography variant="caption" color="text.secondary" fontWeight={700} textTransform="uppercase">
                  Requeridos por asignación
                </Typography>
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: 0.5 }}>
                  {datos.requeridos.map((r) => (
                    <Chip
                      key={r.asignacion}
                      size="small"
                      variant="outlined"
                      label={`${r.asignacion}: ${r.cantRequerida} requeridos (${r.cantAsignada} asignados)`}
                    />
                  ))}
                </Stack>
              </Box>
              <Box>
                <Typography variant="caption" color="text.secondary" fontWeight={700} textTransform="uppercase">
                  Equipos
                </Typography>
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: 0.5 }}>
                  {ORDEN_ESTADOS.filter((e) => conteo[e]).map((e) => (
                    <Chip key={e} size="small" label={`${etiquetaEstadoEquipo(e)}: ${conteo[e]}`} />
                  ))}
                </Stack>
              </Box>
            </Stack>

            {(conteo.SIN_SAP ?? 0) > 0 && (
              <Alert severity="warning" variant="outlined">
                {conteo.SIN_SAP} equipo(s) existen en Oracle pero no se encontraron en SAP por su código legado: se cuentan como
                disponibles porque no tienen órdenes.
              </Alert>
            )}

            <FormControlLabel
              control={<Switch checked={soloFuera} onChange={(e) => setSoloFuera(e.target.checked)} />}
              label="Solo fuera de servicio"
            />

            <TableContainer>
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>Ficha</TableCell>
                    <TableCell>Equipo SAP</TableCell>
                    <TableCell>Descripción</TableCell>
                    <TableCell>Estado</TableCell>
                    <TableCell>Órdenes que lo detienen</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {equipos.map((e) => (
                    <TableRow key={e.codigoLegado} hover>
                      <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>{e.codigoLegado}</TableCell>
                      <TableCell sx={{ fontVariantNumeric: "tabular-nums" }}>{e.equipoSapId ?? "—"}</TableCell>
                      <TableCell>{e.descripcion}</TableCell>
                      <TableCell>
                        <ChipEstadoEquipo estado={e.estado} />
                      </TableCell>
                      <TableCell>
                        <ListaOrdenes ordenes={e.ordenes} />
                      </TableCell>
                    </TableRow>
                  ))}
                  {equipos.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={5} align="center" sx={{ color: "text.secondary", py: 4 }}>
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
