"use client";
import { Fragment, useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Collapse,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
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
import { IconChevronDown, IconChevronRight } from "@tabler/icons-react";
import { disponibilidadService } from "@/services/disponibilidad/disponibilidadService";
import { ApiError, DetalleSemanaDto } from "@/types";
import { fechaConDia, fechaCorta } from "@/utils/formato";
import { CeldaPorcentaje } from "./semaforo";
import ListaOrdenes from "./ListaOrdenes";

export interface SemanaSeleccionada {
  familiaCodigo: string;
  anioIso: number;
  semanaIso: number;
}

/** Drill-down por semana (sección 8.2): detalle diario de una familia con las órdenes de cada día. */
export default function DialogoDetalleSemana({
  tipoReporte,
  seleccion,
  onClose,
}: {
  tipoReporte: string;
  seleccion: SemanaSeleccionada | null;
  onClose: () => void;
}) {
  const theme = useTheme();
  const pantallaChica = useMediaQuery(theme.breakpoints.down("md"));
  const [datos, setDatos] = useState<DetalleSemanaDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [abierto, setAbierto] = useState<string | null>(null);

  useEffect(() => {
    if (!seleccion) return;
    setDatos(null);
    setError(null);
    setAbierto(null);
    disponibilidadService
      .getDetalleSemana(tipoReporte, seleccion.familiaCodigo, seleccion.anioIso, seleccion.semanaIso)
      .then(setDatos)
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudo cargar el detalle."));
  }, [tipoReporte, seleccion]);

  return (
    <Dialog open={!!seleccion} onClose={onClose} fullWidth maxWidth="md" fullScreen={pantallaChica}>
      <DialogTitle>
        {datos ? `${datos.familiaNombre} — Semana ${datos.semanaIso}` : "Detalle de la semana"}
        {datos && (
          <Typography variant="body2" color="text.secondary">
            Del {fechaCorta(datos.fechaInicio)} al {fechaCorta(datos.fechaFin)}. Toque un día para ver las órdenes.
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
        {datos && datos.dias.length === 0 && <Alert severity="info">No hay días calculados en esta semana.</Alert>}
        {datos && datos.dias.length > 0 && (
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell padding="checkbox" />
                  <TableCell>Día</TableCell>
                  <TableCell align="right">Fuera de servicio</TableCell>
                  <TableCell align="right">Disponibles</TableCell>
                  <TableCell align="right">Requeridos</TableCell>
                  <TableCell align="center">Disponibilidad</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {datos.dias.map((d) => {
                  const estaAbierto = abierto === d.fecha;
                  return (
                    <Fragment key={d.fecha}>
                      <TableRow
                        hover
                        sx={{ cursor: "pointer", "& > td": { borderBottom: estaAbierto ? "none" : undefined } }}
                        onClick={() => setAbierto(estaAbierto ? null : d.fecha)}
                      >
                        <TableCell padding="checkbox">
                          <IconButton size="small" aria-label={estaAbierto ? "Ocultar órdenes" : "Ver órdenes"}>
                            {estaAbierto ? <IconChevronDown size={18} /> : <IconChevronRight size={18} />}
                          </IconButton>
                        </TableCell>
                        <TableCell sx={{ textTransform: "capitalize", whiteSpace: "nowrap" }}>{fechaConDia(d.fecha)}</TableCell>
                        <TableCell align="right">{d.eqFueraServicio}</TableCell>
                        <TableCell align="right">{d.eqDisponible}</TableCell>
                        <TableCell align="right">{d.eqRequerido}</TableCell>
                        <TableCell align="center">
                          <CeldaPorcentaje valor={d.dispRealPct} color={d.semaforo} />
                        </TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell colSpan={6} sx={{ py: 0, borderBottom: estaAbierto ? undefined : "none" }}>
                          <Collapse in={estaAbierto} unmountOnExit>
                            <Box sx={{ py: 1.5, pl: { xs: 1, sm: 7 } }}>
                              <Typography variant="caption" color="text.secondary" fontWeight={700} textTransform="uppercase">
                                Órdenes que tenían equipos fuera de servicio ({d.ordenes.length})
                              </Typography>
                              <Box sx={{ mt: 1 }}>
                                <ListaOrdenes ordenes={d.ordenes} mostrarEquipo />
                              </Box>
                            </Box>
                          </Collapse>
                        </TableCell>
                      </TableRow>
                    </Fragment>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cerrar</Button>
      </DialogActions>
    </Dialog>
  );
}
