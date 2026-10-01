"use client";
import { useCallback, useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Chip,
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
import { ApiError, CeldaSemanaDto, FilaDisponibilidadDto, ReporteDisponibilidadDto, TipoReporteDto } from "@/types";
import { fechaCorta, fechaLarga, porcentaje } from "@/utils/formato";
import { CeldaPorcentaje, COLORES_SEMAFORO } from "@/components/disponibilidad/semaforo";
import DialogoEquiposFamilia from "@/components/disponibilidad/DialogoEquiposFamilia";
import DialogoDetalleSemana, { SemanaSeleccionada } from "@/components/disponibilidad/DialogoDetalleSemana";

const OPCIONES_SEMANAS = [4, 8, 13, 26];
const CLAVE_PREFERENCIAS = "portalgerencial_disponibilidad";

// Preferencias del usuario en este navegador (tipo de reporte y semanas). Si el almacenamiento no
// está disponible, la pantalla funciona igual con los valores por defecto.
function leerPreferencias(): { tipoReporte?: string; semanas?: number } {
  try {
    return JSON.parse(localStorage.getItem(CLAVE_PREFERENCIAS) || "{}");
  } catch {
    return {};
  }
}

function guardarPreferencias(p: { tipoReporte: string; semanas: number }) {
  try {
    localStorage.setItem(CLAVE_PREFERENCIAS, JSON.stringify(p));
  } catch {
    /* sin almacenamiento: no pasa nada */
  }
}

// Primera columna fija al hacer scroll horizontal (muchas semanas o pantalla chica).
const celdaFija = {
  position: "sticky" as const,
  left: 0,
  zIndex: 1,
  bgcolor: "background.paper",
  minWidth: { xs: 130, sm: 190 },
  borderRight: 1,
  borderColor: "divider",
};

export default function DisponibilidadPage() {
  const { showLoading, hideLoading } = useGlobalLoading();
  const [tipos, setTipos] = useState<TipoReporteDto[]>([]);
  const [tipoReporte, setTipoReporte] = useState<string>("");
  const [semanas, setSemanas] = useState<number>(8);
  const [reporte, setReporte] = useState<ReporteDisponibilidadDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [familiaAbierta, setFamiliaAbierta] = useState<string | null>(null);
  const [semanaAbierta, setSemanaAbierta] = useState<SemanaSeleccionada | null>(null);

  useEffect(() => {
    const pref = leerPreferencias();
    if (pref.semanas && OPCIONES_SEMANAS.includes(pref.semanas)) setSemanas(pref.semanas);
    estructuraService
      .getTiposReporte()
      .then((t) => {
        setTipos(t);
        const inicial = t.find((x) => x.tipoReporte === pref.tipoReporte) ?? t[0];
        if (inicial) setTipoReporte(inicial.tipoReporte);
      })
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudieron cargar los tipos de reporte."));
  }, []);

  const cargar = useCallback(async () => {
    if (!tipoReporte) return;
    setError(null);
    showLoading("Cargando disponibilidad...");
    try {
      setReporte(await disponibilidadService.getReporte(tipoReporte, semanas));
      guardarPreferencias({ tipoReporte, semanas });
    } catch (e) {
      setError((e as ApiError)?.errorMessage || "No se pudo cargar el reporte.");
    } finally {
      hideLoading();
    }
  }, [tipoReporte, semanas, showLoading, hideLoading]);

  useEffect(() => {
    cargar();
  }, [cargar]);

  const celdaSemana = (fila: FilaDisponibilidadDto, celda: CeldaSemanaDto, esPromedio: boolean) => {
    const columna = reporte!.semanas.find((s) => s.anioIso === celda.anioIso && s.semanaIso === celda.semanaIso)!;
    const contenido = <CeldaPorcentaje valor={celda.dispRealPct} color={celda.semaforo} negrita={esPromedio} />;
    if (celda.dispRealPct == null) return contenido;

    const detalle =
      `Semana ${celda.semanaIso} (${fechaCorta(columna.fechaInicio)} al ${fechaCorta(columna.fechaFin)}): ` +
      `promedio ${porcentaje(celda.dispRealPct)}` +
      (celda.dispRealMinPct != null ? `, peor día ${porcentaje(celda.dispRealMinPct)}` : "") +
      (celda.diasCalculados < 7 ? ` · ${celda.diasCalculados} de 7 días` : "") +
      (esPromedio ? "" : " · clic para ver el detalle diario");

    return (
      <Tooltip title={detalle} arrow>
        {esPromedio ? (
          <span>{contenido}</span>
        ) : (
          <Box
            component="button"
            onClick={() => setSemanaAbierta({ familiaCodigo: fila.familiaCodigo, anioIso: celda.anioIso, semanaIso: celda.semanaIso })}
            sx={{ all: "unset", cursor: "pointer", borderRadius: "6px", "&:focus-visible": { outline: "2px solid", outlineColor: "primary.main" } }}
            aria-label={detalle}
          >
            {contenido}
          </Box>
        )}
      </Tooltip>
    );
  };

  const fila = (f: FilaDisponibilidadDto, esPromedio = false) => (
    <TableRow
      key={f.familiaCodigo}
      hover={!esPromedio}
      sx={esPromedio ? { "& td": { fontWeight: 700, bgcolor: "grey.100", borderTop: 2, borderColor: "divider" } } : undefined}
    >
      <TableCell sx={{ ...celdaFija, ...(esPromedio ? { bgcolor: "grey.100" } : {}) }}>
        {esPromedio ? (
          f.familiaNombre
        ) : (
          <Button
            variant="text"
            size="small"
            onClick={() => setFamiliaAbierta(f.familiaCodigo)}
            sx={{ p: 0, minWidth: 0, textAlign: "left", fontWeight: 600, justifyContent: "flex-start" }}
          >
            {f.familiaNombre}
          </Button>
        )}
      </TableCell>
      <TableCell align="right">{f.cantEquipo}</TableCell>
      <TableCell align="right">{f.eqRequerido}</TableCell>
      <TableCell align="right">{f.eqDisponible}</TableCell>
      <TableCell align="center">
        <CeldaPorcentaje valor={f.dispRealPct} color={f.semaforo} negrita />
      </TableCell>
      <TableCell align="right" sx={{ color: "text.secondary", fontVariantNumeric: "tabular-nums" }}>
        {porcentaje(f.dispPotencialPct)}
      </TableCell>
      {f.semanas.map((c) => (
        <TableCell key={`${c.anioIso}-${c.semanaIso}`} align="center" sx={{ px: 0.75 }}>
          {celdaSemana(f, c, esPromedio)}
        </TableCell>
      ))}
    </TableRow>
  );

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
          <FormControl size="small" sx={{ minWidth: 260 }}>
            <InputLabel id="tipo-reporte">Tipo de reporte</InputLabel>
            <Select
              labelId="tipo-reporte"
              label="Tipo de reporte"
              value={tipoReporte}
              onChange={(e) => setTipoReporte(e.target.value)}
            >
              {tipos.map((t) => (
                <MenuItem key={t.tipoReporte} value={t.tipoReporte}>
                  {t.descripcion}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" sx={{ minWidth: 140 }}>
            <InputLabel id="semanas">Semanas</InputLabel>
            <Select labelId="semanas" label="Semanas" value={semanas} onChange={(e) => setSemanas(Number(e.target.value))}>
              {OPCIONES_SEMANAS.map((n) => (
                <MenuItem key={n} value={n}>
                  Últimas {n}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <Box flexGrow={1} />
          {reporte && (
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap alignItems="center">
              <Typography variant="caption" color="text.secondary">
                Semáforo:
              </Typography>
              {[
                { color: "ROJO" as const, texto: `menos de ${reporte.umbralRojo}%` },
                { color: "NARANJA" as const, texto: `${reporte.umbralRojo}% a ${reporte.umbralNaranja}%` },
                { color: "VERDE" as const, texto: `${reporte.umbralNaranja}% o más` },
              ].map((l) => (
                <Chip
                  key={l.color}
                  size="small"
                  label={l.texto}
                  sx={{
                    bgcolor: COLORES_SEMAFORO[l.color].fondo,
                    color: COLORES_SEMAFORO[l.color].texto,
                    border: `1px solid ${COLORES_SEMAFORO[l.color].borde}`,
                    fontWeight: 600,
                  }}
                />
              ))}
            </Stack>
          )}
        </Stack>
      </Paper>

      {error && <Alert severity="error">{error}</Alert>}

      {reporte && !reporte.fechaCorte && (
        <Alert severity="info">
          Todavía no hay disponibilidad calculada para {reporte.descripcion}. Se calcula en la próxima actualización de
          datos.
        </Alert>
      )}

      {reporte?.fechaCorte && (
        <Paper variant="outlined">
          <Box sx={{ px: 2, pt: 2, pb: 1 }}>
            <Typography variant="subtitle1" fontWeight={700}>
              {reporte.descripcion}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Valores actuales al {fechaLarga(reporte.fechaCorte)}. Toque una familia para ver sus equipos, o una semana
              para ver el detalle por día.
            </Typography>
          </Box>
          <TableContainer sx={{ maxHeight: "calc(100vh - 290px)" }}>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ ...celdaFija, zIndex: 3 }}>Familia</TableCell>
                  <TableCell align="right">Cant. Equipo</TableCell>
                  <TableCell align="right">Eq. Requerido</TableCell>
                  <TableCell align="right">Eq. Disponible</TableCell>
                  <TableCell align="center">Disponibilidad %</TableCell>
                  <TableCell align="right">Potencial %</TableCell>
                  {reporte.semanas.map((s) => (
                    <TableCell key={`${s.anioIso}-${s.semanaIso}`} align="center" sx={{ whiteSpace: "nowrap", px: 0.75 }}>
                      Sem {s.semanaIso}
                      <Typography variant="caption" display="block" color="text.secondary">
                        {fechaCorta(s.fechaInicio)}
                      </Typography>
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {reporte.familias.map((f) => fila(f))}
                {reporte.promedio && fila(reporte.promedio, true)}
              </TableBody>
            </Table>
          </TableContainer>
          <Box sx={{ px: 2, py: 1.5, borderTop: 1, borderColor: "divider" }}>
            <Typography variant="caption" color="text.secondary" component="p">
              Disponibilidad % = equipos disponibles / equipos requeridos (máximo 100%). Potencial % = equipos disponibles /
              total de equipos de la familia. Cada semana muestra el promedio de sus días; la fila &quot;Todas&quot; promedia
              las familias.
            </Typography>
          </Box>
        </Paper>
      )}

      <DialogoEquiposFamilia tipoReporte={tipoReporte} familiaCodigo={familiaAbierta} onClose={() => setFamiliaAbierta(null)} />
      <DialogoDetalleSemana tipoReporte={tipoReporte} seleccion={semanaAbierta} onClose={() => setSemanaAbierta(null)} />
    </Stack>
  );
}
