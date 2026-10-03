"use client";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  Alert,
  Box,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from "@mui/material";
import { estructuraService } from "@/services/estructura/estructuraService";
import { disponibilidadService } from "@/services/disponibilidad/disponibilidadService";
import { useGlobalLoading } from "@/store/LoadingContext";
import { ApiError, CeldaSemanaDto, FilaDisponibilidadDto, ReporteDisponibilidadDto, ReporteResumenDto, SemanaColumnaDto } from "@/types";
import { fechaCorta, fechaHora, porcentaje } from "@/utils/formato";
import { celdaFija, COLOR_TEXTO, FONDO_SEMANA_ACTUAL, num, Pct } from "@/components/reportes/comunes";
import DialogoDisponibilidadSemanal, { SeleccionSemana } from "@/components/reportes/DialogoDisponibilidadSemanal";
import DialogoEquiposAsignacion from "@/components/reportes/DialogoEquiposAsignacion";

const CLAVE_PREFERENCIAS = "portalgerencial_disponibilidad";

function leerReporteGuardado(): number | null {
  try {
    return Number(localStorage.getItem(CLAVE_PREFERENCIAS)) || null;
  } catch {
    return null;
  }
}

function guardarReporte(id: number) {
  try {
    localStorage.setItem(CLAVE_PREFERENCIAS, String(id));
  } catch {
    /* sin almacenamiento: no pasa nada */
  }
}

function Leyenda({ rojo, naranja }: { rojo: number; naranja: number }) {
  const items = [
    { color: COLOR_TEXTO.ROJO, texto: `Menor a ${rojo}` },
    { color: COLOR_TEXTO.NARANJA, texto: `Mayor o igual a ${rojo} y menor a ${naranja}` },
    { color: "#212121", texto: `Mayor o igual a ${naranja}` },
  ];
  return (
    <Stack direction={{ xs: "column", sm: "row" }} spacing={{ xs: 0.5, sm: 2 }}>
      {items.map((i) => (
        <Stack key={i.texto} direction="row" spacing={0.75} alignItems="center">
          <Box sx={{ width: 12, height: 12, bgcolor: i.color, borderRadius: "2px", flexShrink: 0 }} />
          <Typography variant="caption">{i.texto}</Typography>
        </Stack>
      ))}
    </Stack>
  );
}

export default function DisponibilidadPage() {
  const { showLoading, hideLoading } = useGlobalLoading();
  const [reportes, setReportes] = useState<ReporteResumenDto[]>([]);
  const [reporteId, setReporteId] = useState<number | "">("");
  const [reporte, setReporte] = useState<ReporteDisponibilidadDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [semanaAbierta, setSemanaAbierta] = useState<SeleccionSemana | null>(null);
  const [asignacionAbierta, setAsignacionAbierta] = useState<number | null>(null);
  const tablaRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    estructuraService
      .getReportesDisponibilidad()
      .then((r) => {
        setReportes(r);
        const guardado = leerReporteGuardado();
        const inicial = r.find((x) => x.reporteId === guardado) ?? r[0];
        if (inicial) setReporteId(inicial.reporteId);
      })
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudieron cargar los reportes."));
  }, []);

  const cargar = useCallback(async () => {
    if (reporteId === "") return;
    setError(null);
    showLoading("Cargando disponibilidad...");
    try {
      setReporte(await disponibilidadService.getReporte(reporteId));
      guardarReporte(reporteId);
    } catch (e) {
      setError((e as ApiError)?.errorMessage || "No se pudo cargar el reporte.");
    } finally {
      hideLoading();
    }
  }, [reporteId, showLoading, hideLoading]);

  useEffect(() => {
    cargar();
  }, [cargar]);

  // El calendario del reporte puede tener ~30 semanas: se abre en la semana actual.
  useEffect(() => {
    const contenedor = tablaRef.current;
    const actual = contenedor?.querySelector<HTMLElement>("[data-semana-actual]");
    if (contenedor && actual) contenedor.scrollLeft = Math.max(0, actual.offsetLeft - contenedor.clientWidth / 2);
  }, [reporte]);

  const celda = (fila: FilaDisponibilidadDto, c: CeldaSemanaDto, s: SemanaColumnaDto, esTodas: boolean) => {
    const contenido = <Pct valor={c.dispRealPct} color={c.semaforo} negrita={esTodas || fila.negrita} />;
    if (c.dispRealPct == null) return contenido;
    const detalle =
      `Sem. ${s.noSemana} (${fechaCorta(s.fechaInicio)} al ${fechaCorta(s.fechaFin)}): disponibilidad ${porcentaje(c.dispRealPct)}` +
      (c.potencialPct != null ? `, potencial ${porcentaje(c.potencialPct)}` : "") +
      (c.dispRealMinPct != null ? `, peor día ${porcentaje(c.dispRealMinPct)}` : "") +
      (c.diasCalculados < 7 ? ` · ${c.diasCalculados} de 7 días` : "") +
      (esTodas ? "" : " · clic para el detalle");
    return (
      <Tooltip title={detalle} arrow>
        {esTodas ? (
          <span>{contenido}</span>
        ) : (
          <Box
            component="button"
            onClick={() => setSemanaAbierta({ asignacionId: fila.asignacionId, noSemana: c.noSemana })}
            sx={{ all: "unset", cursor: "pointer", "&:focus-visible": { outline: "2px solid", outlineColor: "primary.main" } }}
            aria-label={detalle}
          >
            {contenido}
          </Box>
        )}
      </Tooltip>
    );
  };

  const fila = (f: FilaDisponibilidadDto, esTodas = false) => (
    <TableRow
      key={esTodas ? "todas" : f.asignacionId}
      hover={!esTodas}
      sx={{
        "& td": { fontWeight: esTodas || f.negrita || f.grupo ? 700 : undefined },
        ...(f.grupo ? { "& td": { fontWeight: 700, bgcolor: "grey.100" } } : {}),
        ...(esTodas ? { "& td": { fontWeight: 700, bgcolor: "#FFF9DB", borderBottom: 2, borderColor: "divider" } } : {}),
      }}
    >
      <TableCell sx={{ ...celdaFija, ...(esTodas ? { bgcolor: "#FFF9DB" } : {}) }}>
        {esTodas ? (
          f.asignacion
        ) : (
          <Button
            variant="text"
            size="small"
            onClick={() => setAsignacionAbierta(f.asignacionId)}
            sx={{ p: 0, minWidth: 0, textAlign: "left", fontWeight: f.negrita || f.grupo ? 700 : 600, justifyContent: "flex-start" }}
          >
            {f.asignacion}
          </Button>
        )}
        {f.enGrupo && (
          <Typography variant="caption" color="text.secondary" display="block" sx={{ fontWeight: 400 }}>
            Incluido en {f.enGrupo}
          </Typography>
        )}
      </TableCell>
      <TableCell align="right">{f.cantEquipo}</TableCell>
      <TableCell align="right">{f.eqRequerido}</TableCell>
      <TableCell align="right">{f.eqDisponible}</TableCell>
      <TableCell align="right">
        <Pct valor={f.dispRealPct} color={f.semaforo} negrita />
      </TableCell>
      <TableCell align="right" sx={{ fontVariantNumeric: "tabular-nums" }}>
        {num(f.potencialPct)}
      </TableCell>
      {reporte!.semanas.map((s, i) => (
        <TableCell
          key={s.noSemana}
          align="right"
          sx={{ px: 1, borderLeft: 1, borderColor: "divider", bgcolor: s.momento === "ACTUAL" ? FONDO_SEMANA_ACTUAL : undefined }}
        >
          {celda(f, f.semanas[i], s, esTodas)}
        </TableCell>
      ))}
    </TableRow>
  );

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
          <FormControl size="small" sx={{ minWidth: 300 }}>
            <InputLabel id="reporte">Reporte</InputLabel>
            <Select labelId="reporte" label="Reporte" value={reporteId} onChange={(e) => setReporteId(Number(e.target.value))}>
              {reportes.map((r) => (
                <MenuItem key={r.reporteId} value={r.reporteId}>
                  {r.descripcion}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          {reporte && <Leyenda rojo={reporte.umbralRojo} naranja={reporte.umbralNaranja} />}
          <Box flexGrow={1} />
          {reporte?.fechaCorte && (
            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: "nowrap" }}>
              Foto de las {fechaHora(reporte.fechaCorte)}
            </Typography>
          )}
        </Stack>
      </Paper>

      {error && <Alert severity="error">{error}</Alert>}

      {reporte && !reporte.fechaCorte && (
        <Alert severity="info">Todavía no hay disponibilidad calculada. Se calcula en la próxima actualización de datos.</Alert>
      )}

      {reporte?.fechaCorte && (
        <Paper variant="outlined">
          <TableContainer ref={tablaRef} sx={{ maxHeight: "calc(100vh - 260px)" }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ ...celdaFija, zIndex: 3 }}>Asignación</TableCell>
                  <TableCell align="right">Cant. Equipo</TableCell>
                  <TableCell align="right">Eq. Requerido</TableCell>
                  <TableCell align="right">Eq. Disponible</TableCell>
                  <TableCell align="right">Disponibilidad %</TableCell>
                  <TableCell align="right">Potencial %</TableCell>
                  {reporte.semanas.map((s) => (
                    <TableCell
                      key={s.noSemana}
                      align="center"
                      data-semana-actual={s.momento === "ACTUAL" ? "" : undefined}
                      sx={{
                        whiteSpace: "nowrap",
                        px: 1,
                        borderLeft: 1,
                        borderColor: "divider",
                        ...(s.momento === "ACTUAL" ? { bgcolor: FONDO_SEMANA_ACTUAL } : {}),
                      }}
                    >
                      <Tooltip title={`Del ${fechaCorta(s.fechaInicio)} al ${fechaCorta(s.fechaFin)}`} arrow>
                        <span>
                          Sem. {s.noSemana}
                          <Typography variant="caption" display="block" color="text.secondary">
                            {fechaCorta(s.fechaInicio)}
                          </Typography>
                        </span>
                      </Tooltip>
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {reporte.todas && fila(reporte.todas, true)}
                {reporte.filas.map((f) => fila(f))}
              </TableBody>
            </Table>
          </TableContainer>
          <Box sx={{ px: 2, py: 1.5, borderTop: 1, borderColor: "divider" }}>
            <Typography variant="caption" color="text.secondary" component="p">
              Se mide cada hora configurada en el reporte: un equipo está fuera de servicio desde que se crea su orden hasta su
              cierre técnico (las correctivas, según el aviso de parada). Disponibilidad % = disponibles / requeridos, máximo
              100. Potencial % = disponibles / requeridos, sin tope. Cada semana es el promedio de sus días. &quot;Todas&quot;
              = total disponible / total requerido, sin que lo que sobra en una asignación compense lo que falta en otra. Una
              fila cuyos equipos ya están todos en un grupo se muestra, pero en &quot;Todas&quot; solo cuenta el grupo.
            </Typography>
          </Box>
        </Paper>
      )}

      {reporteId !== "" && (
        <>
          <DialogoDisponibilidadSemanal reporteId={reporteId} seleccion={semanaAbierta} onClose={() => setSemanaAbierta(null)} />
          <DialogoEquiposAsignacion reporteId={reporteId} asignacionId={asignacionAbierta} onClose={() => setAsignacionAbierta(null)} />
        </>
      )}
    </Stack>
  );
}
