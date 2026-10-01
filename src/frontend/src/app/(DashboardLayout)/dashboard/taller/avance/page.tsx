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
import { avanceService } from "@/services/avance/avanceService";
import { useGlobalLoading } from "@/store/LoadingContext";
import { ApiError, FilaAvanceDto, ProgramaDto, ReporteAvanceDto } from "@/types";
import { fechaCorta, fechaLarga, porcentaje } from "@/utils/formato";
import CurvaAvance from "@/components/avance/CurvaAvance";
import { COLOR_ATRASO, nivelAtraso, TOLERANCIA_ATRASO } from "@/components/avance/diferencia";

const FORMULAS: Record<string, string> = {
  HH_ORDENES_CERRADAS: "horas planificadas de las órdenes ZC03 cerradas técnicamente / horas planificadas de todas las ZC03 de la familia",
  ORDENES_CERRADAS: "órdenes ZC03 cerradas técnicamente / total de órdenes ZC03 de la familia",
};

const celdaFija = {
  position: "sticky" as const,
  left: 0,
  zIndex: 1,
  bgcolor: "background.paper",
  minWidth: { xs: 130, sm: 190 },
  borderRight: 1,
  borderColor: "divider",
};

const separadorSemana = { borderLeft: 1, borderColor: "divider" };

function Diferencia({ valor }: { valor: number }) {
  return (
    <Box component="span" sx={{ color: COLOR_ATRASO[nivelAtraso(valor)], fontWeight: 700, fontVariantNumeric: "tabular-nums" }}>
      {valor > 0 ? "+" : ""}
      {valor.toFixed(1)}
    </Box>
  );
}

export default function AvancePage() {
  const { showLoading, hideLoading } = useGlobalLoading();
  const [programas, setProgramas] = useState<ProgramaDto[]>([]);
  const [programa, setPrograma] = useState("");
  const [reporte, setReporte] = useState<ReporteAvanceDto | null>(null);
  const [familiaGrafica, setFamiliaGrafica] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const tablaRef = useRef<HTMLDivElement>(null);
  const graficaRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    estructuraService
      .getProgramas()
      .then((p) => {
        setProgramas(p);
        if (p[0]) setPrograma(p[0].anioPrograma);
      })
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudieron cargar los programas."));
  }, []);

  const cargar = useCallback(async () => {
    if (!programa) return;
    setError(null);
    showLoading("Cargando avance...");
    try {
      const r = await avanceService.getReporte(programa);
      setReporte(r);
      // Por defecto se grafica la familia más atrasada: es la que gerencia va a querer ver.
      const masAtrasada = [...r.familias].sort((a, b) => a.diferenciaPct - b.diferenciaPct)[0];
      setFamiliaGrafica(masAtrasada?.familiaCodigo ?? "");
    } catch (e) {
      setError((e as ApiError)?.errorMessage || "No se pudo cargar el reporte.");
    } finally {
      hideLoading();
    }
  }, [programa, showLoading, hideLoading]);

  useEffect(() => {
    cargar();
  }, [cargar]);

  // Con ~28 semanas la tabla es ancha: se abre desplazada para que la semana actual quede a la vista.
  useEffect(() => {
    const contenedor = tablaRef.current;
    const actual = contenedor?.querySelector<HTMLElement>("[data-semana-actual]");
    if (contenedor && actual) {
      contenedor.scrollLeft = Math.max(0, actual.offsetLeft - contenedor.clientWidth / 2);
    }
  }, [reporte]);

  const verCurva = (f: FilaAvanceDto) => {
    setFamiliaGrafica(f.familiaCodigo);
    graficaRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  const familiaSeleccionada = reporte?.familias.find((f) => f.familiaCodigo === familiaGrafica);
  const corte = reporte?.fechaCorte;

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
          <FormControl size="small" sx={{ minWidth: 280 }}>
            <InputLabel id="programa">Programa</InputLabel>
            <Select labelId="programa" label="Programa" value={programa} onChange={(e) => setPrograma(e.target.value)}>
              {programas.map((p) => (
                <MenuItem key={p.anioPrograma} value={p.anioPrograma}>
                  Programa de Reparación {p.anioPrograma}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          {reporte && (
            <Typography variant="body2" color="text.secondary">
              Del {fechaLarga(reporte.fechaInicio)} al {fechaLarga(reporte.fechaFin)}
              {corte ? ` · ejecutado al ${fechaLarga(corte)}` : ""}
            </Typography>
          )}
        </Stack>
      </Paper>

      {error && <Alert severity="error">{error}</Alert>}

      {reporte && !corte && (
        <Alert severity="info">Todavía no hay avance calculado para este programa. Se calcula en la próxima actualización de datos.</Alert>
      )}

      {reporte && corte && (
        <Paper variant="outlined">
          <Box sx={{ px: 2, pt: 2, pb: 1 }}>
            <Typography variant="subtitle1" fontWeight={700}>
              Avance por familia
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Semanas pasadas: programado y real lado a lado. Semanas futuras: solo programado. Toque una familia para ver su
              curva.
            </Typography>
          </Box>
          <TableContainer ref={tablaRef} sx={{ maxHeight: "calc(100vh - 300px)" }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell rowSpan={2} sx={{ ...celdaFija, zIndex: 3 }}>
                    Familia
                  </TableCell>
                  <TableCell rowSpan={2} align="right">
                    Cant. Eq.
                  </TableCell>
                  <TableCell rowSpan={2}>Inicio</TableCell>
                  <TableCell rowSpan={2}>Fin</TableCell>
                  <TableCell rowSpan={2} align="right">
                    Total Programado
                  </TableCell>
                  <TableCell rowSpan={2} align="right">
                    Total Ejecutado
                  </TableCell>
                  <TableCell rowSpan={2} align="right">
                    Diferencia
                  </TableCell>
                  {reporte.semanas.map((s) => (
                    <TableCell
                      key={s.lunes}
                      colSpan={s.momento === "FUTURA" ? 1 : 2}
                      align="center"
                      data-semana-actual={s.momento === "ACTUAL" ? "" : undefined}
                      sx={{
                        ...separadorSemana,
                        whiteSpace: "nowrap",
                        px: 0.75,
                        ...(s.momento === "ACTUAL" ? { bgcolor: "warning.light" } : {}),
                      }}
                    >
                      Sem {s.semanaIso}
                      <Typography variant="caption" display="block" color="text.secondary">
                        {fechaCorta(s.lunes)}
                      </Typography>
                    </TableCell>
                  ))}
                </TableRow>
                <TableRow>
                  {reporte.semanas.map((s) =>
                    s.momento === "FUTURA" ? (
                      <TableCell key={`${s.lunes}-p`} align="center" sx={{ ...separadorSemana, top: 57, px: 0.75 }}>
                        <Typography variant="caption" color="text.secondary">
                          Prog.
                        </Typography>
                      </TableCell>
                    ) : (
                      [
                        <TableCell key={`${s.lunes}-p`} align="center" sx={{ ...separadorSemana, top: 57, px: 0.75 }}>
                          <Typography variant="caption" color="text.secondary">
                            Prog.
                          </Typography>
                        </TableCell>,
                        <TableCell key={`${s.lunes}-r`} align="center" sx={{ top: 57, px: 0.75 }}>
                          <Typography variant="caption" fontWeight={700}>
                            Real
                          </Typography>
                        </TableCell>,
                      ]
                    )
                  )}
                </TableRow>
              </TableHead>
              <TableBody>
                {reporte.familias.map((f) => (
                  <TableRow key={f.familiaCodigo} hover selected={f.familiaCodigo === familiaGrafica}>
                    <TableCell sx={celdaFija}>
                      <Button
                        variant="text"
                        size="small"
                        onClick={() => verCurva(f)}
                        sx={{ p: 0, minWidth: 0, textAlign: "left", fontWeight: 600, justifyContent: "flex-start" }}
                      >
                        {f.familiaNombre}
                      </Button>
                    </TableCell>
                    <TableCell align="right">{f.cantEquipo}</TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>{fechaCorta(f.fechaEstInicio)}</TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>{fechaCorta(f.fechaEstFin)}</TableCell>
                    <TableCell align="right" sx={{ fontVariantNumeric: "tabular-nums" }}>
                      {porcentaje(f.totalProgramadoPct)}
                    </TableCell>
                    <TableCell align="right" sx={{ fontWeight: 700, fontVariantNumeric: "tabular-nums" }}>
                      <Tooltip title={`${f.ordenesCerradas} de ${f.totalOrdenes} órdenes cerradas técnicamente`} arrow>
                        <span>{porcentaje(f.totalEjecutadoPct)}</span>
                      </Tooltip>
                    </TableCell>
                    <TableCell align="right">
                      <Diferencia valor={f.diferenciaPct} />
                    </TableCell>
                    {reporte.semanas.map((s, i) => {
                      const c = f.semanas[i];
                      const vacio = (
                        <Box component="span" sx={{ color: "text.disabled" }}>
                          ·
                        </Box>
                      );
                      const prog = (
                        <TableCell
                          key={`${s.lunes}-p`}
                          align="center"
                          sx={{ ...separadorSemana, px: 0.75, color: "text.secondary", fontVariantNumeric: "tabular-nums" }}
                        >
                          {c.enVentana && c.progPct != null ? c.progPct.toFixed(1) : vacio}
                        </TableCell>
                      );
                      if (s.momento === "FUTURA") return prog;
                      const atraso = c.realPct != null && c.progPct != null ? nivelAtraso(c.realPct - c.progPct) : null;
                      return [
                        prog,
                        <TableCell
                          key={`${s.lunes}-r`}
                          align="center"
                          sx={{
                            px: 0.75,
                            fontWeight: 700,
                            fontVariantNumeric: "tabular-nums",
                            color: atraso ? COLOR_ATRASO[atraso] : undefined,
                          }}
                        >
                          {c.enVentana && c.realPct != null ? c.realPct.toFixed(1) : vacio}
                        </TableCell>,
                      ];
                    })}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <Box sx={{ px: 2, py: 1.5, borderTop: 1, borderColor: "divider" }}>
            <Typography variant="caption" color="text.secondary" component="p">
              Programado = curva planeada de Oracle, acumulada semana a semana. Real (provisional, hasta confirmar la fórmula de
              &quot;Total Ejec. %&quot;) = {FORMULAS[reporte.formulaReal] ?? reporte.formulaReal}. Diferencia en puntos: verde
              al día o adelantado, naranja hasta {TOLERANCIA_ATRASO} puntos de atraso, rojo más de {TOLERANCIA_ATRASO}.
            </Typography>
          </Box>
        </Paper>
      )}

      {reporte && corte && familiaSeleccionada && (
        <Paper variant="outlined" sx={{ p: 2 }} ref={graficaRef}>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ sm: "center" }} sx={{ mb: 2 }}>
            <Box flexGrow={1}>
              <Typography variant="subtitle1" fontWeight={700}>
                Curva de avance — {familiaSeleccionada.familiaNombre}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Programado {porcentaje(familiaSeleccionada.totalProgramadoPct)} · ejecutado{" "}
                {porcentaje(familiaSeleccionada.totalEjecutadoPct)} · diferencia{" "}
                <Diferencia valor={familiaSeleccionada.diferenciaPct} /> puntos
              </Typography>
            </Box>
            <FormControl size="small" sx={{ minWidth: 220 }}>
              <InputLabel id="familia-grafica">Familia</InputLabel>
              <Select
                labelId="familia-grafica"
                label="Familia"
                value={familiaGrafica}
                onChange={(e) => setFamiliaGrafica(e.target.value)}
              >
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
    </Stack>
  );
}
