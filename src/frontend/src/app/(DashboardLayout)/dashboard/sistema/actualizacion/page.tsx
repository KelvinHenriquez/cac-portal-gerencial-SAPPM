"use client";
import { Fragment, useCallback, useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Chip,
  Collapse,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid2 as Grid,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { IconChevronDown, IconChevronRight, IconRefresh } from "@tabler/icons-react";
import { sistemaService } from "@/services/sistema/sistemaService";
import { useGlobalLoading } from "@/store/LoadingContext";
import EquiposSinClasificar from "@/components/sistema/EquiposSinClasificar";
import { AlertaDto, ApiError, CorridaDto, EquipoSinClasificarDto, EstadoActualizacionDto, EstadoCorrida } from "@/types";

const fmtFechaHora = new Intl.DateTimeFormat("es-DO", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
});
const fechaHora = (v: string) => fmtFechaHora.format(new Date(v));

const NOMBRE_PASO: Record<string, string> = {
  SAP_EQUIPOS: "Equipos de SAP",
  SAP_ORDENES: "Órdenes de SAP",
  SAP_OPERACIONES: "Operaciones (actividades) de SAP",
  SAP_AVISOS: "Avisos de SAP",
  SAP_HISTORIAL: "Historial de estados de SAP",
  CALC_SIN_CLASIFICAR: "Equipos sin clasificar",
  CALC_DISPONIBILIDAD: "Cálculo de disponibilidad",
  CALC_PERMANENCIA: "Cálculo de permanencia",
  CALC_AVANCE: "Cálculo de avance",
};

const ESTADO_CORRIDA: Record<EstadoCorrida, { etiqueta: string; color: "success" | "error" | "info" }> = {
  OK: { etiqueta: "Exitosa", color: "success" },
  ERROR: { etiqueta: "Falló", color: "error" },
  EN_CURSO: { etiqueta: "En curso", color: "info" },
};

function duracion(c: { inicio: string; fin: string | null }) {
  if (!c.fin) return "—";
  const s = Math.max(0, Math.round((new Date(c.fin).getTime() - new Date(c.inicio).getTime()) / 1000));
  return s < 60 ? `${s} s` : `${Math.floor(s / 60)} min ${s % 60} s`;
}

function origen(o: string | null) {
  if (!o) return "—";
  const [quien, modo] = o.split(":");
  const q = quien === "MANUAL" ? "Manual" : quien === "PROGRAMADA" ? "Programada" : quien === "RECARGA" ? "Recarga desde fecha" : quien;
  return modo === "LOCAL" ? `${q} · SAP simulado` : q;
}

function Dato({ titulo, valor, detalle }: { titulo: string; valor: React.ReactNode; detalle?: React.ReactNode }) {
  return (
    <Paper variant="outlined" sx={{ p: 2, height: "100%" }}>
      <Typography variant="caption" color="text.secondary" fontWeight={700} textTransform="uppercase">
        {titulo}
      </Typography>
      <Box sx={{ mt: 0.5, fontSize: "1.05rem", fontWeight: 600 }}>{valor}</Box>
      {detalle && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
          {detalle}
        </Typography>
      )}
    </Paper>
  );
}

export default function ActualizacionPage() {
  const { showLoading, hideLoading } = useGlobalLoading();
  const [estado, setEstado] = useState<EstadoActualizacionDto | null>(null);
  const [corridas, setCorridas] = useState<CorridaDto[]>([]);
  const [sinClasificar, setSinClasificar] = useState<EquipoSinClasificarDto[]>([]);
  const [alertas, setAlertas] = useState<AlertaDto[]>([]);
  const [abierta, setAbierta] = useState<string | null>(null);
  const [mensaje, setMensaje] = useState<{ tipo: "success" | "error" | "warning"; texto: string } | null>(null);

  const cargar = useCallback(async () => {
    const [e, c, s, a] = await Promise.all([
      sistemaService.getEstadoActualizacion(),
      sistemaService.getCorridas(20),
      sistemaService.getEquiposSinClasificar(),
      sistemaService.getAlertas(5),
    ]);
    setEstado(e);
    setCorridas(c);
    setSinClasificar(s);
    setAlertas(a);
  }, []);

  const [esAdmin, setEsAdmin] = useState(false);
  const [dialogoDesde, setDialogoDesde] = useState(false);
  const [fechaDesde, setFechaDesde] = useState("2026-06-01");

  useEffect(() => {
    sistemaService
      .getParametros()
      .then((p) => setEsAdmin(p.puedeEditar))
      .catch(() => setEsAdmin(false));
  }, []);

  useEffect(() => {
    cargar().catch((e: ApiError) => setMensaje({ tipo: "error", texto: e?.errorMessage || "No se pudo cargar el estado." }));
  }, [cargar]);

  const actualizarAhora = async (desde?: string) => {
    setMensaje(null);
    setDialogoDesde(false);
    showLoading(desde ? `Trayendo de SAP desde el ${desde.split("-").reverse().join("/")} y recalculando...` : "Actualizando datos desde SAP y recalculando...");
    try {
      const corrida = await sistemaService.ejecutarCorrida(desde);
      setMensaje(
        corrida.estado === "OK"
          ? { tipo: "success", texto: `Actualización completada en ${duracion(corrida)}.` }
          : { tipo: "error", texto: "La actualización falló. Los reportes siguen mostrando los datos anteriores; vea el detalle abajo." }
      );
      setAbierta(corrida.estado === "OK" ? null : corrida.corridaId);
      await cargar();
    } catch (e) {
      const err = e as ApiError;
      setMensaje({ tipo: err?.statusCode === 409 ? "warning" : "error", texto: err?.errorMessage || "No se pudo actualizar." });
    } finally {
      hideLoading();
    }
  };

  const ultimaAlerta = alertas[0];

  return (
    <Stack spacing={3}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ sm: "center" }}>
        <Box flexGrow={1}>
          <Typography variant="body2" color="text.secondary">
            Cada día se extraen de SAP los equipos, órdenes, estados y horas, y se recalculan los reportes. Si una
            actualización falla, los reportes siguen mostrando los últimos datos buenos.
          </Typography>
        </Box>
        {esAdmin && (
          <Button variant="outlined" onClick={() => setDialogoDesde(true)} disabled={!estado || estado.corridaEnCurso} sx={{ flexShrink: 0 }}>
            Actualizar desde…
          </Button>
        )}
        <Button
          variant="contained"
          startIcon={<IconRefresh size={18} />}
          onClick={() => actualizarAhora()}
          disabled={!estado || estado.corridaEnCurso}
          sx={{ flexShrink: 0 }}
        >
          Actualizar ahora
        </Button>
      </Stack>

      <Dialog open={dialogoDesde} onClose={() => setDialogoDesde(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Volver a traer de SAP desde una fecha</DialogTitle>
        <DialogContent dividers>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Normalmente cada actualización trae solo lo que cambió desde la anterior. Úselo para recargar un período (por ejemplo,
            después de una corrección en SAP). Mientras más atrás, más tarda.
          </Typography>
          <TextField
            type="date"
            label="Desde"
            size="small"
            fullWidth
            value={fechaDesde}
            onChange={(e) => setFechaDesde(e.target.value)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogoDesde(false)}>Cancelar</Button>
          <Button variant="contained" onClick={() => actualizarAhora(fechaDesde)} disabled={!fechaDesde}>
            Traer y recalcular
          </Button>
        </DialogActions>
      </Dialog>

      {mensaje && (
        <Alert severity={mensaje.tipo} onClose={() => setMensaje(null)}>
          {mensaje.texto}
        </Alert>
      )}

      {estado && (
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
            <Dato
              titulo="Última actualización"
              valor={
                <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                  <span>{estado.ultimaActualizacion ? fechaHora(estado.ultimaActualizacion) : "Nunca"}</span>
                  <Chip
                    size="small"
                    color={estado.desactualizado ? "warning" : "success"}
                    label={estado.desactualizado ? "Desactualizado" : "Al día"}
                  />
                </Stack>
              }
              detalle={
                estado.horasDesdeUltimaActualizacion != null
                  ? `Hace ${estado.horasDesdeUltimaActualizacion} h · se alerta a las ${estado.horasAlerta} h`
                  : `Se alerta a las ${estado.horasAlerta} h sin datos`
              }
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
            <Dato
              titulo="Próxima actualización"
              valor={estado.proximaCorrida ? fechaHora(estado.proximaCorrida) : "Automática apagada"}
              detalle={estado.jobHabilitado ? "Automática, todos los días" : "Job:Habilitado = false"}
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
            <Dato
              titulo="Origen de los datos de SAP"
              valor={estado.modo === "LOCAL" ? "SAP simulado" : estado.modo === "EXCEL" ? "Exports de SAP (Excel)" : "SAP PM (servicios web)"}
              detalle={
                estado.modo === "LOCAL"
                  ? "Datos de prueba generados localmente"
                  : estado.modo === "EXCEL"
                    ? "Archivos de la carpeta del Sprint 0"
                    : "Módulos Z_PM_MF_WS_*"
              }
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
            <Dato
              titulo="Última alerta"
              valor={ultimaAlerta ? fechaHora(ultimaAlerta.fecha) : "Ninguna"}
              detalle={
                ultimaAlerta
                  ? ultimaAlerta.enviada
                    ? `Enviada a ${ultimaAlerta.destinatarios}`
                    : `No se pudo enviar: ${ultimaAlerta.error}`
                  : "Se avisa por correo si los datos se atrasan"
              }
            />
          </Grid>
        </Grid>
      )}

      {estado?.ultimoErrorFecha && (
        <Alert severity="warning">
          La última actualización ({fechaHora(estado.ultimoErrorFecha)}) falló: {estado.ultimoErrorMensaje}
        </Alert>
      )}

      <Paper variant="outlined">
        <Box sx={{ px: 2, pt: 2, pb: 1 }}>
          <Typography variant="subtitle1" fontWeight={700}>
            Últimas actualizaciones
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Toque una fila para ver cada paso: cuántos registros trajo y, si falló, por qué.
          </Typography>
        </Box>
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell padding="checkbox" />
                <TableCell>Inicio</TableCell>
                <TableCell>Tipo</TableCell>
                <TableCell>Resultado</TableCell>
                <TableCell align="right">Duración</TableCell>
                <TableCell align="right">Órdenes de SAP</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {corridas.map((c) => {
                const estaAbierta = abierta === c.corridaId;
                const ordenes = c.pasos.find((p) => p.fuente === "SAP_ORDENES")?.registrosProcesados;
                return (
                  <Fragment key={c.corridaId}>
                    <TableRow hover sx={{ cursor: "pointer" }} onClick={() => setAbierta(estaAbierta ? null : c.corridaId)}>
                      <TableCell padding="checkbox">
                        <IconButton size="small" aria-label={estaAbierta ? "Ocultar pasos" : "Ver pasos"}>
                          {estaAbierta ? <IconChevronDown size={18} /> : <IconChevronRight size={18} />}
                        </IconButton>
                      </TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>{fechaHora(c.inicio)}</TableCell>
                      <TableCell>{origen(c.origen)}</TableCell>
                      <TableCell>
                        <Chip size="small" color={ESTADO_CORRIDA[c.estado].color} label={ESTADO_CORRIDA[c.estado].etiqueta} />
                      </TableCell>
                      <TableCell align="right">{duracion(c)}</TableCell>
                      <TableCell align="right">{ordenes ?? "—"}</TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell colSpan={6} sx={{ py: 0, borderBottom: estaAbierta ? undefined : "none" }}>
                        <Collapse in={estaAbierta} unmountOnExit>
                          <Table size="small" sx={{ my: 1 }}>
                            <TableHead>
                              <TableRow>
                                <TableCell>Paso</TableCell>
                                <TableCell>Resultado</TableCell>
                                <TableCell align="right">Registros</TableCell>
                                <TableCell>Cambios desde</TableCell>
                                <TableCell align="right">Duración</TableCell>
                              </TableRow>
                            </TableHead>
                            <TableBody>
                              {c.pasos.map((p) => (
                                <Fragment key={p.fuente}>
                                  <TableRow>
                                    <TableCell>{NOMBRE_PASO[p.fuente] ?? p.fuente}</TableCell>
                                    <TableCell>
                                      <Chip
                                        size="small"
                                        variant="outlined"
                                        color={ESTADO_CORRIDA[p.estado].color}
                                        label={ESTADO_CORRIDA[p.estado].etiqueta}
                                      />
                                    </TableCell>
                                    <TableCell align="right">{p.registrosProcesados ?? "—"}</TableCell>
                                    <TableCell>{p.fechaCorteDesde ? fechaHora(p.fechaCorteDesde).slice(0, 10) : "—"}</TableCell>
                                    <TableCell align="right">{duracion(p)}</TableCell>
                                  </TableRow>
                                  {p.errorMensaje && (
                                    <TableRow>
                                      <TableCell colSpan={5} sx={{ color: "error.main", fontFamily: "monospace", fontSize: "0.75rem" }}>
                                        {p.errorMensaje}
                                      </TableCell>
                                    </TableRow>
                                  )}
                                </Fragment>
                              ))}
                            </TableBody>
                          </Table>
                        </Collapse>
                      </TableCell>
                    </TableRow>
                  </Fragment>
                );
              })}
              {corridas.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 4, color: "text.secondary" }}>
                    Todavía no hay actualizaciones registradas.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>

      <EquiposSinClasificar equipos={sinClasificar} />
    </Stack>
  );
}
