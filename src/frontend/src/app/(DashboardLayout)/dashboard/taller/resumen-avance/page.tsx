"use client";
import { Fragment, useCallback, useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  GlobalStyles,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import { IconPrinter, IconSettings } from "@tabler/icons-react";
import { resumenAvanceService } from "@/services/resumenAvance/resumenAvanceService";
import { useGlobalLoading } from "@/store/LoadingContext";
import { ApiError, FilaResumenDto, ResumenAvanceDto } from "@/types";
import { fechaCorta, fechaHora } from "@/utils/formato";
import LogoCac from "@/components/layout/LogoCac";
import NotaResumen from "@/components/reportes/resumen/NotaResumen";
import DialogoConfigResumen from "@/components/reportes/resumen/DialogoConfigResumen";

const AREA = "CDS";

// Colores de la lámina del Taller: claro para las familias, fuerte para la fila General.
const COLORES = [
  { claro: "#E2F0D9", fuerte: "#A9D18E" }, // agrícola
  { claro: "#DEEBF7", fuerte: "#5FB4E5" }, // automotriz
  { claro: "#FBE5D6", fuerte: "#F4B183" }, // ferroviaria
  { claro: "#EDEDED", fuerte: "#BFBFBF" },
];

const pct = (v: number | null | undefined) =>
  v == null ? "" : `${v.toLocaleString("es-DO", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}%`;

const borde = "1px solid #7f7f7f";
const celda = { border: borde, py: 0.5, px: 1, fontVariantNumeric: "tabular-nums" as const, fontSize: "0.85rem" };
const hueco = { border: "none", width: 16, p: 0 };

export default function ResumenAvancePage() {
  const { showLoading, hideLoading } = useGlobalLoading();
  const [datos, setDatos] = useState<ResumenAvanceDto | null>(null);
  const [semanaFin, setSemanaFin] = useState<string | null>(null);
  const [medida, setMedida] = useState<"ORDENES" | "ACTIVIDADES">("ORDENES");
  const [error, setError] = useState<string | null>(null);
  const [configAbierta, setConfigAbierta] = useState(false);

  const cargar = useCallback(async () => {
    setError(null);
    showLoading("Armando el resumen de la semana...");
    try {
      setDatos(await resumenAvanceService.getResumen(AREA, semanaFin, medida));
    } catch (e) {
      setError((e as ApiError)?.errorMessage || "No se pudo cargar el resumen.");
    } finally {
      hideLoading();
    }
  }, [semanaFin, medida, showLoading, hideLoading]);

  useEffect(() => {
    cargar();
  }, [cargar]);

  const guardarNota = async (f: FilaResumenDto, texto: string) => {
    if (!datos) return;
    await resumenAvanceService.guardarNota(AREA, {
      reporteId: f.reporteId,
      familiaCodigo: f.familiaCodigo,
      semanaFin: datos.semanaFin.slice(0, 10),
      texto,
    });
    await cargar();
  };

  const n = datos?.noSemana;

  const fila = (f: FilaResumenDto, color: { claro: string; fuerte: string }, general: boolean) => {
    const fondo = general ? color.fuerte : color.claro;
    const negrita = general ? 700 : 400;
    return (
      <TableRow key={general ? `g-${f.nombre}` : `${f.reporteId}-${f.familiaCodigo}`}>
        <TableCell sx={{ ...celda, bgcolor: fondo, fontWeight: 700, minWidth: 200 }}>{f.nombre}</TableCell>
        <TableCell align="center" sx={{ ...celda, bgcolor: general ? fondo : undefined, fontWeight: general ? 700 : 400 }}>
          {f.cantidad}
        </TableCell>
        <TableCell align="center" sx={{ ...celda, fontWeight: 700 }}>
          {pct(f.progSemanaPct)}
        </TableCell>
        <TableCell align="center" sx={celda}>
          {pct(f.ejecSemanaPct)}
        </TableCell>
        <TableCell sx={hueco} />
        <TableCell align="center" sx={{ ...celda, fontWeight: 700 }}>
          {pct(f.progAcumPct)}
        </TableCell>
        <TableCell align="center" sx={{ ...celda, fontWeight: negrita }}>
          {pct(f.ejecAcumPct)}
        </TableCell>
        <TableCell align="center" sx={{ ...celda, fontWeight: 700, color: f.diferenciaPct < 0 ? "error.main" : "success.dark" }}>
          {pct(f.diferenciaPct)}
        </TableCell>
        <TableCell sx={hueco} />
        <TableCell align="center" sx={{ ...celda, fontWeight: 700 }}>
          {f.programadosEntregar}
        </TableCell>
        <TableCell align="center" sx={{ ...celda, fontWeight: negrita }}>
          {f.entregados}
        </TableCell>
        <TableCell sx={{ border: "none", pl: 1.5, py: 0.25, minWidth: 260 }}>
          {!general && (
            <NotaResumen
              key={`${datos?.semanaFin}-${f.nota}`}
              nota={f.nota}
              por={f.notaPor}
              en={f.notaEn}
              puedeEditar={datos?.puedeEditar ?? false}
              onGuardar={(t) => guardarNota(f, t)}
            />
          )}
        </TableCell>
      </TableRow>
    );
  };

  return (
    <Stack spacing={2}>
      {/* Impresión: solo la lámina, sin menú, encabezado ni controles. */}
      <GlobalStyles
        styles={{
          "@media print": {
            "@page": { size: "landscape", margin: "10mm" },
            ".MuiDrawer-root, .MuiAppBar-root, .no-imprimir, .no-imprimir-borde": { display: "none !important" },
            "body, main": { background: "#fff !important" },
            ".lamina": { boxShadow: "none !important", border: "none !important" },
          },
        }}
      />

      <Paper variant="outlined" sx={{ p: 2 }} className="no-imprimir">
        <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }} flexWrap="wrap" useFlexGap>
          <TextField
            select
            size="small"
            label="Semana"
            value={datos?.semanaFin?.slice(0, 10) ?? ""}
            onChange={(e) => setSemanaFin(e.target.value)}
            sx={{ minWidth: 260 }}
          >
            {(datos?.semanas ?? []).map((s) => (
              <MenuItem key={s.noSemana} value={s.fechaFin.slice(0, 10)}>
                Semana {s.noSemana} · {fechaCorta(s.fechaInicio)} al {fechaCorta(s.fechaFin)}
                {s.momento === "ACTUAL" ? " (actual)" : ""}
              </MenuItem>
            ))}
          </TextField>
          <ToggleButtonGroup size="small" exclusive value={medida} onChange={(_, v) => v && setMedida(v)} aria-label="Ejecución por">
            <ToggleButton value="ORDENES">Por órdenes</ToggleButton>
            <ToggleButton value="ACTIVIDADES">Por actividades</ToggleButton>
          </ToggleButtonGroup>
          <Box flexGrow={1} />
          {datos?.puedeEditar && (
            <Button variant="outlined" startIcon={<IconSettings size={18} />} onClick={() => setConfigAbierta(true)}>
              Configurar filas
            </Button>
          )}
          <Button variant="contained" startIcon={<IconPrinter size={18} />} onClick={() => window.print()} disabled={!datos}>
            Imprimir / PDF
          </Button>
        </Stack>
      </Paper>

      {error && <Alert severity="error">{error}</Alert>}

      {datos && datos.grupos.length > 0 && (
        <Paper variant="outlined" className="lamina" sx={{ p: { xs: 2, md: 3 }, borderLeft: "14px solid #70AD47", overflowX: "auto" }}>
          <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 2 }}>
            <Box>
              <Typography variant="h3" fontWeight={800} sx={{ fontSize: { xs: "1.6rem", md: "2.4rem" }, lineHeight: 1.25, mb: 0.75 }}>
                Avance Reparación Semana {n}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {fechaCorta(datos.semanaInicio)} al {fechaCorta(datos.semanaFin)}
                {datos.periodo ? ` · Período ${datos.periodo}` : ""} · Ejecución {datos.medida === "ORDENES" ? "por órdenes" : "por actividades"}
                {datos.fechaCorte ? ` · Datos al ${fechaHora(datos.fechaCorte)}` : ""}
              </Typography>
            </Box>
            <LogoCac variante="marca" alto={100} />
          </Stack>

          <TableContainer>
            <Table size="small" sx={{ borderCollapse: "collapse", width: "auto" }}>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ border: "none" }} colSpan={4} />
                  <TableCell sx={hueco} />
                  <TableCell colSpan={3} align="center" sx={{ border: "none", fontWeight: 800, fontSize: "1rem" }}>
                    Acumulado (Semana)
                  </TableCell>
                  <TableCell sx={hueco} />
                  <TableCell colSpan={2} align="center" sx={{ border: "none", fontWeight: 800, fontSize: "1rem" }}>
                    Maquinaria entregada
                  </TableCell>
                  <TableCell sx={{ border: "none" }} />
                </TableRow>
                <TableRow sx={{ "& th": { ...celda, fontWeight: 700, textAlign: "center", lineHeight: 1.2, fontSize: "0.8rem" } }}>
                  <TableCell sx={{ border: "none !important" }} />
                  <TableCell sx={{ bgcolor: "#C5E0B4" }}>Cantidad maquinaria</TableCell>
                  <TableCell>Programa semana {n} (%)</TableCell>
                  <TableCell>Ejecución semana {n} (%)</TableCell>
                  <TableCell sx={hueco} />
                  <TableCell>Programa</TableCell>
                  <TableCell>Ejecución</TableCell>
                  <TableCell>Diferencia</TableCell>
                  <TableCell sx={hueco} />
                  <TableCell>Equipos programados a ser entregados a la fecha</TableCell>
                  <TableCell>Equipos entregados a la fecha</TableCell>
                  <TableCell sx={{ border: "none !important", textAlign: "left !important" }}>Notas</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {datos.grupos.map((g, i) => {
                  const color = COLORES[Math.min(i, COLORES.length - 1)];
                  return (
                    <Fragment key={g.nombre}>
                      {i > 0 && (
                        <TableRow>
                          <TableCell colSpan={12} sx={{ border: "none", height: 14, p: 0 }} />
                        </TableRow>
                      )}
                      {g.filas.map((f) => fila(f, color, false))}
                      {fila(g.general, color, true)}
                    </Fragment>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>

          <Typography variant="caption" color="text.secondary" component="p" sx={{ mt: 2, maxWidth: 1100 }}>
            Cada fila es una familia de un programa de reparación de Oracle. Programa = plan semanal de Oracle; Ejecución = órdenes de
            reparación de SAP {datos.medida === "ORDENES" ? "terminadas (fin real)" : "por actividades confirmadas"}, como el reporte de
            Avance. Acumulado = desde el inicio del programa hasta esta semana. Programados a la fecha = equipos cuya última actividad
            planificada en su OT de Oracle cae hasta esta semana; entregados = equipos con sus órdenes de reparación de SAP del programa
            terminadas hasta el fin de la semana. La fila General suma las cantidades y pondera los porcentajes por cantidad de máquinas.
            {!datos.configuracionGuardada && " Filas según la configuración sugerida."}
          </Typography>
        </Paper>
      )}

      {datos && datos.grupos.length === 0 && <Alert severity="info">No hay programas de reparación para armar el resumen.</Alert>}

      <DialogoConfigResumen area={AREA} abierto={configAbierta} onClose={() => setConfigAbierta(false)} onGuardado={cargar} />
    </Stack>
  );
}
