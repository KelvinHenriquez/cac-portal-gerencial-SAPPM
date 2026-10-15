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
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
} from "@mui/material";
import { estructuraService } from "@/services/estructura/estructuraService";
import { avanceService } from "@/services/avance/avanceService";
import { useGlobalLoading } from "@/store/LoadingContext";
import { ApiError, FilaAvanceDto, MedidaAvance, PeriodoDto, RefPlan, ReporteAvanceDto } from "@/types";
import { fechaCorta, fechaHora, fechaLarga } from "@/utils/formato";
import { celdaFija, FONDO_SEMANA_ACTUAL, num } from "@/components/reportes/comunes";
import CurvaAvance from "@/components/reportes/CurvaAvance";
import DialogoAvanceSemanal, { SeleccionSemanaAvance } from "@/components/reportes/DialogoAvanceSemanal";
import DialogoAnalisisAvance from "@/components/reportes/DialogoAnalisisAvance";

type Vista = "SEMANAL" | "ACUMULADO";

// Tooltips de las columnas, como en el reporte de Oracle.
const COLUMNAS: { clave: keyof FilaAvanceDto; titulo: string; ayuda: string }[] = [
  { clave: "tProg", titulo: "T. Prog.", ayuda: "Total programado: suma del plan de todas las semanas" },
  { clave: "progCierre", titulo: "Prog. Cierre", ayuda: "Programado acumulado hasta la semana anterior" },
  { clave: "totalEjec", titulo: "Total Ejec.", ayuda: "Ejecutado acumulado hasta hoy" },
  { clave: "progAct", titulo: "Prog. Act.", ayuda: "Programado para la semana actual" },
  { clave: "pendAnt", titulo: "Pend. Ant.", ayuda: "Pendiente de semanas anteriores: Prog. Cierre − ejecutado hasta la semana anterior" },
  { clave: "tPendR", titulo: "T. Pend. R.", ayuda: "Total pendiente real: Pend. Ant. + Prog. Act. − ejecutado esta semana" },
];

export default function AvancePage() {
  const { showLoading, hideLoading } = useGlobalLoading();
  const [periodos, setPeriodos] = useState<PeriodoDto[]>([]);
  const [periodoId, setPeriodoId] = useState<number | "">("");
  const [reporteId, setReporteId] = useState<number | "">("");
  const [medida, setMedida] = useState<MedidaAvance>("ORDENES");
  const [refPlan, setRefPlan] = useState<RefPlan>("EJECUTADO");
  const [vista, setVista] = useState<Vista>("SEMANAL");
  const [reporte, setReporte] = useState<ReporteAvanceDto | null>(null);
  const [familiaGrafica, setFamiliaGrafica] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [semanaAbierta, setSemanaAbierta] = useState<SeleccionSemanaAvance | null>(null);
  const [analisisAbierto, setAnalisisAbierto] = useState<string | null>(null);
  const tablaRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    estructuraService
      .getPeriodos()
      .then((p) => {
        setPeriodos(p);
        const inicial = p.find((x) => x.reportes.length > 0);
        if (inicial) {
          setPeriodoId(inicial.periodoId);
          setReporteId(inicial.reportes[0].reporteId);
        }
      })
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudieron cargar los períodos."));
  }, []);

  const reportesDelPeriodo = periodos.find((p) => p.periodoId === periodoId)?.reportes ?? [];

  const cambiarPeriodo = (id: number) => {
    setPeriodoId(id);
    setReporteId(periodos.find((p) => p.periodoId === id)?.reportes[0]?.reporteId ?? "");
  };

  const cargar = useCallback(async () => {
    if (reporteId === "") return;
    setError(null);
    showLoading("Cargando avance...");
    try {
      const r = await avanceService.getReporte(reporteId, medida, refPlan);
      setReporte(r);
      setFamiliaGrafica((actual) =>
        r.familias.some((f) => f.familiaCodigo === actual) ? actual : ([...r.familias].sort((a, b) => b.tPendR - a.tPendR)[0]?.familiaCodigo ?? "")
      );
    } catch (e) {
      setError((e as ApiError)?.errorMessage || "No se pudo cargar el reporte.");
    } finally {
      hideLoading();
    }
  }, [reporteId, medida, refPlan, showLoading, hideLoading]);

  useEffect(() => {
    cargar();
  }, [cargar]);

  useEffect(() => {
    const contenedor = tablaRef.current;
    const actual = contenedor?.querySelector<HTMLElement>("[data-semana-actual]");
    if (contenedor && actual) contenedor.scrollLeft = Math.max(0, actual.offsetLeft - contenedor.clientWidth / 2);
  }, [reporte]);

  const familiaSeleccionada = reporte?.familias.find((f) => f.familiaCodigo === familiaGrafica);
  const hayDatos = reporte && reporte.familias.some((f) => f.semanas.some((c) => c.enPlan));

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: "column", lg: "row" }} spacing={2} alignItems={{ lg: "center" }} flexWrap="wrap" useFlexGap>
          <FormControl size="small" sx={{ minWidth: 160 }}>
            <InputLabel id="periodo">Período</InputLabel>
            <Select labelId="periodo" label="Período" value={periodoId} onChange={(e) => cambiarPeriodo(Number(e.target.value))}>
              {periodos.map((p) => (
                <MenuItem key={p.periodoId} value={p.periodoId}>
                  {p.periodoId}: {p.descripcion}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" sx={{ minWidth: 300 }}>
            <InputLabel id="reporte">Reporte</InputLabel>
            <Select labelId="reporte" label="Reporte" value={reporteId} onChange={(e) => setReporteId(Number(e.target.value))}>
              {reportesDelPeriodo.map((r) => (
                <MenuItem key={r.reporteId} value={r.reporteId}>
                  {r.descripcion}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <ToggleButtonGroup size="small" exclusive value={medida} onChange={(_, v) => v && setMedida(v)} aria-label="Medir el real por">
            <ToggleButton value="ORDENES">Por órdenes</ToggleButton>
            <ToggleButton value="ACTIVIDADES">Por actividades</ToggleButton>
          </ToggleButtonGroup>
          <FormControl size="small" sx={{ minWidth: 190 }} disabled={medida === "ORDENES"}>
            <InputLabel id="refplan">Ref. Plan.</InputLabel>
            <Select labelId="refplan" label="Ref. Plan." value={medida === "ORDENES" ? "EJECUTADO" : refPlan} onChange={(e) => setRefPlan(e.target.value as RefPlan)}>
              <MenuItem value="EJECUTADO">Real Ejecutado</MenuItem>
              <MenuItem value="PROGRAMADO">Real Programado</MenuItem>
              <MenuItem value="NO_PROGRAMADO">Real No Programado</MenuItem>
            </Select>
          </FormControl>
          <ToggleButtonGroup size="small" exclusive value={vista} onChange={(_, v) => v && setVista(v)} aria-label="Vista">
            <ToggleButton value="SEMANAL">Semanal</ToggleButton>
            <ToggleButton value="ACUMULADO">Acumulado</ToggleButton>
          </ToggleButtonGroup>
          <Box flexGrow={1} />
          {reporte?.fechaCorte && (
            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: "nowrap" }}>
              Ejecutado al {fechaHora(reporte.fechaCorte)}
            </Typography>
          )}
        </Stack>
      </Paper>

      {error && <Alert severity="error">{error}</Alert>}

      {reporte && !hayDatos && <Alert severity="info">Este reporte no tiene plan semanal cargado en Oracle.</Alert>}

      {reporte && hayDatos && (
        <Paper variant="outlined">
          <Box sx={{ px: 2, pt: 2, pb: 1 }}>
            <Typography variant="subtitle1" fontWeight={700}>
              {reporte.nombre}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Del {fechaLarga(reporte.fechaInicio)} al {fechaLarga(reporte.fechaFin)}. Semanas pasadas: Real %; semanas futuras:
              Prog. %. Toque una familia para su análisis por equipo y sistema, o una semana para su detalle.
            </Typography>
          </Box>
          <TableContainer ref={tablaRef} sx={{ maxHeight: "calc(100vh - 330px)" }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ ...celdaFija, zIndex: 3 }}>Asignación</TableCell>
                  <TableCell align="right">Cant. Eq.</TableCell>
                  <TableCell>Fecha Inicio</TableCell>
                  <TableCell>Fecha Fin</TableCell>
                  {COLUMNAS.map((c) => (
                    <TableCell key={c.clave} align="right" sx={{ whiteSpace: "nowrap" }}>
                      <Tooltip title={c.ayuda} arrow>
                        <span>{c.titulo}</span>
                      </Tooltip>
                    </TableCell>
                  ))}
                  {reporte.semanas.map((s) => (
                    <TableCell
                      key={s.noSemana}
                      align="right"
                      data-semana-actual={s.momento === "ACTUAL" ? "" : undefined}
                      sx={{ whiteSpace: "nowrap", px: 1, borderLeft: 1, borderColor: "divider", ...(s.momento === "ACTUAL" ? { bgcolor: FONDO_SEMANA_ACTUAL } : {}) }}
                    >
                      <Tooltip title={`Del ${fechaCorta(s.fechaInicio)} al ${fechaCorta(s.fechaFin)}`} arrow>
                        <span>
                          Sem #{s.noSemana}
                          <Typography variant="caption" display="block" color="text.secondary">
                            {s.momento === "FUTURA" ? "Prog. %" : "Real %"}
                          </Typography>
                        </span>
                      </Tooltip>
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {reporte.familias.map((f) => (
                  <TableRow key={f.familiaCodigo} hover selected={f.familiaCodigo === familiaGrafica}>
                    <TableCell sx={celdaFija}>
                      <Button
                        variant="text"
                        size="small"
                        onClick={() => setAnalisisAbierto(f.familiaCodigo)}
                        sx={{ p: 0, minWidth: 0, textAlign: "left", fontWeight: 600, justifyContent: "flex-start" }}
                      >
                        {f.familiaNombre}
                      </Button>
                    </TableCell>
                    <TableCell align="right">{f.cantEquipo}</TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>{fechaLarga(f.fechaEstInicio)}</TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>{fechaLarga(f.fechaEstFin)}</TableCell>
                    {COLUMNAS.map((c) => (
                      <TableCell key={c.clave} align="right" sx={{ fontVariantNumeric: "tabular-nums", fontWeight: c.clave === "totalEjec" ? 700 : undefined }}>
                        {c.clave === "totalEjec" ? (
                          <Tooltip
                            title={medida === "ORDENES" ? `${f.ordenesCerradas} de ${f.ordenesTotal} órdenes cerradas` : `${f.actTerminadas} de ${f.actTotal} actividades terminadas`}
                            arrow
                          >
                            <span>{num(f[c.clave] as number)}</span>
                          </Tooltip>
                        ) : (
                          num(f[c.clave] as number)
                        )}
                      </TableCell>
                    ))}
                    {reporte.semanas.map((s, i) => {
                      const c = f.semanas[i];
                      const futura = s.momento === "FUTURA";
                      const valor = !c.enPlan
                        ? null
                        : vista === "ACUMULADO"
                          ? futura ? c.progAcumPct : c.realAcumPct
                          : futura ? c.progPct : c.realPct;
                      return (
                        <TableCell
                          key={s.noSemana}
                          align="right"
                          sx={{
                            px: 1,
                            borderLeft: 1,
                            borderColor: "divider",
                            fontVariantNumeric: "tabular-nums",
                            color: futura ? "text.secondary" : undefined,
                            ...(s.momento === "ACTUAL" ? { bgcolor: FONDO_SEMANA_ACTUAL } : {}),
                          }}
                        >
                          {valor == null ? (
                            ""
                          ) : (
                            <Tooltip title={`Programado ${num(vista === "ACUMULADO" ? c.progAcumPct : c.progPct)}${futura ? "" : ` · real ${num(vista === "ACUMULADO" ? c.realAcumPct : c.realPct)}`} · clic para el detalle`} arrow>
                              <Box
                                component="button"
                                onClick={() => setSemanaAbierta({ familiaCodigo: f.familiaCodigo, noSemana: s.noSemana })}
                                sx={{ all: "unset", cursor: "pointer", "&:focus-visible": { outline: "2px solid", outlineColor: "primary.main" } }}
                              >
                                {num(valor)}
                              </Box>
                            </Tooltip>
                          )}
                        </TableCell>
                      );
                    })}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <Box sx={{ px: 2, py: 1.5, borderTop: 1, borderColor: "divider" }}>
            <Typography variant="caption" color="text.secondary" component="p">
              Programado = plan semanal de Oracle ({reporte.basePlan === "DIAS" ? "días" : "actividades"} de la semana / total de
              la familia). Real {medida === "ORDENES"
                ? "por órdenes = órdenes del programa cerradas cuyo trabajo terminó (fin real) en la semana / total de órdenes del programa"
                : "por actividades = actividades terminadas en la semana / total de actividades del programa, como Oracle. Una actividad está terminada cuando tiene horas reales confirmadas en SAP y la confirmación final, y cuenta en la semana de su última confirmación; las no notificables (sin clave de control) no cuentan. Ambas reglas se ven en Parámetros"}
              . La semana resaltada es la actual.
            </Typography>
          </Box>
        </Paper>
      )}

      {reporte && hayDatos && familiaSeleccionada && (
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ sm: "center" }} sx={{ mb: 2 }}>
            <Box flexGrow={1}>
              <Typography variant="subtitle1" fontWeight={700}>
                Curva de avance — {familiaSeleccionada.familiaNombre}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Programado a la fecha {num(familiaSeleccionada.progCierre + familiaSeleccionada.progAct)} · ejecutado{" "}
                {num(familiaSeleccionada.totalEjec)} · pendiente real {num(familiaSeleccionada.tPendR)}
              </Typography>
            </Box>
            <FormControl size="small" sx={{ minWidth: 220 }}>
              <InputLabel id="familia-grafica">Familia</InputLabel>
              <Select labelId="familia-grafica" label="Familia" value={familiaGrafica} onChange={(e) => setFamiliaGrafica(e.target.value)}>
                {reporte.familias.map((f) => (
                  <MenuItem key={f.familiaCodigo} value={f.familiaCodigo}>
                    {f.familiaNombre}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
          <CurvaAvance familia={familiaSeleccionada} semanas={reporte.semanas} />
        </Paper>
      )}

      {reporteId !== "" && (
        <>
          <DialogoAvanceSemanal reporteId={reporteId} seleccion={semanaAbierta} onClose={() => setSemanaAbierta(null)} />
          <DialogoAnalisisAvance reporteId={reporteId} familiaCodigo={analisisAbierto} onClose={() => setAnalisisAbierto(null)} />
        </>
      )}
    </Stack>
  );
}
