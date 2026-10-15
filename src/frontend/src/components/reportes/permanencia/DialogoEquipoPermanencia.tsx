"use client";
import { Fragment, useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Collapse,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
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
import { permanenciaService, PeriodoPermanencia } from "@/services/permanencia/permanenciaService";
import { ApiError, EquipoPermanenciaDto, OrdenPermanenciaDto } from "@/types";
import { fechaHora } from "@/utils/formato";
import { BarraPasos, dias, duracion, LeyendaPasos } from "./comunesPermanencia";
import AvisoParada from "../AvisoParada";

/** Los hitos de una orden, en orden, con el tiempo que tomó llegar a cada uno. */
function PasosOrden({ orden, esperaConfigurada }: { orden: OrdenPermanenciaDto; esperaConfigurada: boolean }) {
  const hitos: { nombre: string; fecha: string | null; horas: number | null | undefined; nota?: string }[] = [
    { nombre: "Aviso abierto", fecha: orden.fechaAviso, horas: undefined, nota: orden.avisoSapId ? `Aviso ${orden.avisoSapId}` : "Sin aviso: empieza con la orden" },
    { nombre: "Orden creada", fecha: orden.fechaCreada, horas: orden.pasos.respuesta },
    { nombre: "Liberada", fecha: orden.fechaLiberada, horas: orden.pasos.liberacion },
    { nombre: "En proceso", fecha: orden.fechaEnProceso, horas: orden.pasos.esperaInicio },
    { nombre: "Finalizada", fecha: orden.fechaFinalizada, horas: orden.pasos.ejecucion, nota: orden.terminada ? undefined : "Todavía en curso" },
    {
      nombre: "Cierre técnico",
      fecha: orden.fechaCierreTecnico,
      horas: orden.pasos.cierreAdministrativo,
      nota: "Cierre administrativo: no suma a la permanencia",
    },
  ];
  return (
    <Box sx={{ py: 1.5, px: { xs: 0, md: 2 } }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Paso</TableCell>
            <TableCell>Fecha y hora</TableCell>
            <TableCell align="right">Tiempo desde el paso anterior</TableCell>
            <TableCell>Nota</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {hitos.map((h) => (
            <TableRow key={h.nombre}>
              <TableCell sx={{ fontWeight: 600, whiteSpace: "nowrap" }}>{h.nombre}</TableCell>
              <TableCell sx={{ whiteSpace: "nowrap" }}>{h.fecha ? fechaHora(h.fecha) : "—"}</TableCell>
              <TableCell align="right" sx={{ fontVariantNumeric: "tabular-nums" }}>
                {h.horas === undefined ? "" : duracion(h.horas)}
              </TableCell>
              <TableCell sx={{ color: "text.secondary" }}>{h.nota ?? (h.fecha ? "" : "No pasó por este estado")}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      {esperaConfigurada && (orden.pasos.esperaRepuestos ?? 0) > 0 && (
        <Typography variant="body2" sx={{ mt: 1 }}>
          De ese tiempo, <b>{duracion(orden.pasos.esperaRepuestos)}</b> en espera de repuestos.
        </Typography>
      )}
    </Box>
  );
}

/** Equipo: permanencia promedio y cada orden correctiva (la más reciente primero) con sus pasos. */
export default function DialogoEquipoPermanencia({
  equipoSapId,
  periodo,
  esperaConfigurada,
  onClose,
}: {
  equipoSapId: string | null;
  periodo: PeriodoPermanencia;
  esperaConfigurada: boolean;
  onClose: () => void;
}) {
  const theme = useTheme();
  const pantallaChica = useMediaQuery(theme.breakpoints.down("md"));
  const [datos, setDatos] = useState<EquipoPermanenciaDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [abierta, setAbierta] = useState<string | null>(null);

  useEffect(() => {
    if (!equipoSapId) return;
    setDatos(null);
    setError(null);
    setAbierta(null);
    permanenciaService
      .getEquipo(equipoSapId, periodo)
      .then(setDatos)
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudo cargar el equipo."));
  }, [equipoSapId, periodo]);

  const escala = datos ? Math.max(...datos.ordenes.map((o) => o.horasPermanencia), 1) : 1;
  const e = datos?.equipo;

  return (
    <Dialog open={equipoSapId != null} onClose={onClose} fullWidth maxWidth="lg" fullScreen={pantallaChica}>
      <DialogTitle>
        {e ? `${e.clave ?? e.codigo} · ${e.nombre}` : "Equipo"}
        {datos && (
          <Typography variant="body2" color="text.secondary">
            {datos.tipoEquipoDesc} · código SAP {e?.codigo}
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
        {datos && e && (
          <Stack spacing={2}>
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              <Chip label={`MTTR ${dias(e.promedioHoras)} d`} color="primary" title="Tiempo medio de restauración" />
              <Chip label={`MTBF ${dias(e.mtbfHoras)} d`} variant="outlined" title="Tiempo medio entre fallas" />
              <Chip label={`${e.fallas} fallas en el período`} variant="outlined" />
              <Chip label={`${e.ordenes} terminadas en el período`} variant="outlined" />
              {e.ordenesEnCurso > 0 && <Chip label={`${e.ordenesEnCurso} en curso`} color="warning" variant="outlined" />}
            </Stack>
            <LeyendaPasos />
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell />
                    <TableCell>Orden</TableCell>
                    <TableCell>Inicio (aviso u orden)</TableCell>
                    <TableCell>Finalizada</TableCell>
                    <TableCell align="right">Permanencia</TableCell>
                    <TableCell sx={{ width: "32%", display: { xs: "none", md: "table-cell" } }}>Pasos</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {datos.ordenes.map((o) => (
                    <Fragment key={o.ordenSapId}>
                      <TableRow hover sx={{ cursor: "pointer" }} onClick={() => setAbierta(abierta === o.ordenSapId ? null : o.ordenSapId)}>
                        <TableCell padding="checkbox">
                          <IconButton size="small" aria-label="Ver los pasos de la orden">
                            {abierta === o.ordenSapId ? <IconChevronDown size={18} /> : <IconChevronRight size={18} />}
                          </IconButton>
                        </TableCell>
                        <TableCell>
                          <b>{o.ordenSapId}</b> <Typography component="span" variant="caption" color="text.secondary">{o.claseOrden}</Typography>
                          <Typography variant="caption" color="text.secondary" display="block">
                            {o.descripcion}
                          </Typography>
                          <AvisoParada parada={o.avisoParada} />
                        </TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap" }}>{fechaHora(o.inicio)}</TableCell>
                        <TableCell sx={{ whiteSpace: "nowrap" }}>
                          {o.fechaFinalizada ? fechaHora(o.fechaFinalizada) : <Chip size="small" color="warning" label="En curso" />}
                        </TableCell>
                        <TableCell align="right" sx={{ fontWeight: 700, whiteSpace: "nowrap" }}>
                          {duracion(o.horasPermanencia)}
                        </TableCell>
                        <TableCell sx={{ display: { xs: "none", md: "table-cell" } }}>
                          <BarraPasos pasos={o.pasos} escala={escala} alto={10} />
                        </TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell colSpan={6} sx={{ p: 0, borderBottom: abierta === o.ordenSapId ? undefined : "none" }}>
                          <Collapse in={abierta === o.ordenSapId} unmountOnExit>
                            {o.avisoParada != null && (
                              <Box sx={{ px: 2, pt: 1.5 }}>
                                <AvisoParada parada={o.avisoParada} inicio={o.inicioAveria} fin={o.finAveria} conFechas />
                              </Box>
                            )}
                            <PasosOrden orden={o} esperaConfigurada={esperaConfigurada} />
                          </Collapse>
                        </TableCell>
                      </TableRow>
                    </Fragment>
                  ))}
                  {datos.ordenes.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={6} align="center" sx={{ color: "text.secondary", py: 4 }}>
                        Sin órdenes correctivas en el período.
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
