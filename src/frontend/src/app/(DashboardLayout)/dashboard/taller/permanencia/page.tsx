"use client";
import { Suspense, useCallback, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  Alert,
  Box,
  Breadcrumbs,
  Grid2 as Grid,
  Link,
  MenuItem,
  Paper,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import { permanenciaService, PeriodoPermanencia } from "@/services/permanencia/permanenciaService";
import { useGlobalLoading } from "@/store/LoadingContext";
import { ApiError, AreaPermanenciaDto, FamiliaPermanenciaDto, IndicadorPermanenciaDto, ResumenPermanenciaDto } from "@/types";
import { fechaHora, fechaLarga } from "@/utils/formato";
import { BarraPasos, dias, duracion, LeyendaPasos, PASOS, sumaPasos, useColoresPasos } from "@/components/reportes/permanencia/comunesPermanencia";
import ListaPermanencia, { VistaPermanencia } from "@/components/reportes/permanencia/ListaPermanencia";
import DialogoEquipoPermanencia from "@/components/reportes/permanencia/DialogoEquipoPermanencia";
import { useSidebar } from "@/store/SidebarContext";

const CLAVE_PREFERENCIAS = "portalgerencial_permanencia";

function leerPreferencias(): { area?: string; vista?: VistaPermanencia } {
  try {
    return JSON.parse(localStorage.getItem(CLAVE_PREFERENCIAS) ?? "{}");
  } catch {
    return {};
  }
}

function guardarPreferencias(p: { area: string; vista: VistaPermanencia }) {
  try {
    localStorage.setItem(CLAVE_PREFERENCIAS, JSON.stringify(p));
  } catch {
    /* sin almacenamiento: no pasa nada */
  }
}

const iso = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

// Rangos rápidos (pedido del Taller, 14/10/2026): días hacia atrás desde hoy; 0 = todo lo que hay.
const RANGOS = [
  { dias: 7, nombre: "Últimos 7 días" },
  { dias: 28, nombre: "Últimos 28 días" },
  { dias: 90, nombre: "Últimos 90 días" },
  { dias: 365, nombre: "Últimos 365 días" },
  { dias: 0, nombre: "Todo" },
];
const DESDE_TODO = "2000-01-01";

function periodoDeRango(diasAtras: number): PeriodoPermanencia {
  const hasta = new Date();
  if (diasAtras === 0) return { desde: DESDE_TODO, hasta: iso(hasta) };
  const desde = new Date(hasta);
  desde.setDate(desde.getDate() - diasAtras + 1);
  return { desde: iso(desde), hasta: iso(hasta) };
}

const periodoPorDefecto = () => periodoDeRango(90);

/** El rango rápido que corresponde al período, o "" si las fechas se eligieron a mano. */
function rangoDe(p: PeriodoPermanencia): string {
  const r = RANGOS.find((x) => {
    const q = periodoDeRango(x.dias);
    return q.desde === p.desde && q.hasta === p.hasta;
  });
  return r ? String(r.dias) : "";
}

function Indicador({ titulo, valor, detalle }: { titulo: string; valor: string; detalle?: string }) {
  return (
    <Paper variant="outlined" sx={{ p: 2, height: "100%" }}>
      <Typography variant="body2" color="text.secondary">
        {titulo}
      </Typography>
      <Typography variant="h4" sx={{ fontWeight: 700, my: 0.5, fontVariantNumeric: "tabular-nums" }}>
        {valor}
      </Typography>
      {detalle && (
        <Typography variant="caption" color="text.secondary">
          {detalle}
        </Typography>
      )}
    </Paper>
  );
}

/** Desglose del promedio del nivel actual: barra apilada + el valor de cada paso. */
function Desglose({ indicador }: { indicador: IndicadorPermanenciaDto }) {
  const colores = useColoresPasos();
  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Typography variant="subtitle2" sx={{ mb: 1 }}>
        ¿Dónde se va el tiempo? Promedio por paso de las órdenes terminadas
      </Typography>
      <BarraPasos pasos={indicador.pasos} escala={sumaPasos(indicador.pasos)} alto={22} />
      <Stack direction="row" spacing={3} flexWrap="wrap" useFlexGap sx={{ mt: 1.5 }}>
        {colores.map((p) => (
          <Stack key={p.clave} direction="row" spacing={0.75} alignItems="center">
            <Box sx={{ width: 12, height: 12, borderRadius: "3px", bgcolor: p.color }} />
            <Typography variant="body2">
              {p.nombre}: <b>{duracion(indicador.pasos[p.clave])}</b>
            </Typography>
          </Stack>
        ))}
        <Typography variant="body2" color="text.secondary">
          Cierre administrativo (aparte): <b>{duracion(indicador.pasos.cierreAdministrativo)}</b>
        </Typography>
      </Stack>
    </Paper>
  );
}

function Permanencia() {
  const { showLoading, hideLoading } = useGlobalLoading();
  const [areas, setAreas] = useState<AreaPermanenciaDto[]>([]);
  const [area, setArea] = useState<string>("");
  const [vista, setVista] = useState<VistaPermanencia>("grafico");
  const [periodo, setPeriodo] = useState<PeriodoPermanencia>(periodoPorDefecto);
  const [resumen, setResumen] = useState<ResumenPermanenciaDto | null>(null);
  const [familia, setFamilia] = useState<FamiliaPermanenciaDto | null>(null);
  const [equipo, setEquipo] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();
  const { setAreaActiva } = useSidebar();
  // ?area= viene del menú o del Inicio (Centro de Servicios / Industrial); la API solo devuelve
  // las áreas que el usuario ve.
  const pedida = useSearchParams().get("area")?.toUpperCase() ?? null;

  useEffect(() => {
    const pref = leerPreferencias();
    if (pref.vista) setVista(pref.vista);
    permanenciaService
      .getAreas()
      .then(setAreas)
      .catch((e: ApiError) => setError(e?.errorMessage || "No se pudieron cargar las áreas."));
  }, []);

  // El área abierta sigue a la URL; si la pedida no está permitida, la última usada o la primera.
  useEffect(() => {
    if (areas.length === 0) return;
    const elegida =
      areas.find((x) => x.codigo === pedida)?.codigo ??
      areas.find((x) => x.codigo === leerPreferencias().area)?.codigo ??
      areas[0].codigo;
    setArea(elegida);
    if (elegida !== pedida) router.replace(`/dashboard/taller/permanencia?area=${elegida}`);
  }, [areas, pedida, router]);

  const cargar = useCallback(async () => {
    if (!area || !periodo.desde || !periodo.hasta) return;
    setError(null);
    setFamilia(null);
    showLoading("Calculando permanencia...");
    try {
      setResumen(await permanenciaService.getResumen(area, periodo));
    } catch (e) {
      setError((e as ApiError)?.errorMessage || "No se pudo cargar el reporte.");
    } finally {
      hideLoading();
    }
  }, [area, periodo, showLoading, hideLoading]);

  useEffect(() => {
    cargar();
  }, [cargar]);

  useEffect(() => {
    if (area) guardarPreferencias({ area, vista });
  }, [area, vista]);

  // El menú marca "Tiempo de Permanencia" del área abierta.
  useEffect(() => {
    setAreaActiva(area || null);
    return () => setAreaActiva(null);
  }, [area, setAreaActiva]);

  const abrirFamilia = async (f: IndicadorPermanenciaDto) => {
    showLoading("Cargando equipos...");
    try {
      setFamilia(await permanenciaService.getFamilia(area, f.codigo, periodo));
    } catch (e) {
      setError((e as ApiError)?.errorMessage || "No se pudo cargar la familia.");
    } finally {
      hideLoading();
    }
  };

  const nivel = familia ? familia.familia : resumen?.total;
  const filas = useMemo(() => (familia ? familia.equipos : resumen?.familias ?? []), [familia, resumen]);

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ p: 2 }}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }} flexWrap="wrap" useFlexGap>
          <ToggleButtonGroup size="small" exclusive value={area} onChange={(_, v) => v && router.replace(`/dashboard/taller/permanencia?area=${v}`)} aria-label="Área">
            {areas.map((a) => (
              <ToggleButton key={a.codigo} value={a.codigo} sx={{ px: 2 }}>
                {a.nombre}
              </ToggleButton>
            ))}
          </ToggleButtonGroup>
          <Stack direction="row" spacing={1}>
            <TextField
              select
              size="small"
              label="Rango"
              value={rangoDe(periodo)}
              onChange={(e) => e.target.value !== "" && setPeriodo(periodoDeRango(Number(e.target.value)))}
              sx={{ minWidth: 170 }}
              slotProps={{ select: { displayEmpty: true, renderValue: (v) => RANGOS.find((r) => String(r.dias) === v)?.nombre ?? "Personalizado" } }}
            >
              {RANGOS.map((r) => (
                <MenuItem key={r.dias} value={String(r.dias)}>
                  {r.nombre}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              size="small"
              type="date"
              label="Finalizadas desde"
              value={periodo.desde}
              onChange={(e) => e.target.value && setPeriodo((p) => ({ ...p, desde: e.target.value }))}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              size="small"
              type="date"
              label="hasta"
              value={periodo.hasta}
              onChange={(e) => e.target.value && setPeriodo((p) => ({ ...p, hasta: e.target.value }))}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Stack>
          <ToggleButtonGroup size="small" exclusive value={vista} onChange={(_, v) => v && setVista(v)} aria-label="Vista">
            <ToggleButton value="grafico">Gráfico</ToggleButton>
            <ToggleButton value="tabla">Tabla</ToggleButton>
          </ToggleButtonGroup>
          <Box flexGrow={1} />
          {resumen?.fechaCalculo && (
            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: "nowrap" }}>
              Calculado el {fechaHora(resumen.fechaCalculo)}
            </Typography>
          )}
        </Stack>
      </Paper>

      {error && <Alert severity="error">{error}</Alert>}

      {resumen && nivel && (
        <>
          <Breadcrumbs>
            {familia ? (
              <Link component="button" underline="hover" onClick={() => setFamilia(null)}>
                {resumen.areaNombre}
              </Link>
            ) : (
              <Typography color="text.primary">{resumen.areaNombre}</Typography>
            )}
            {familia && <Typography color="text.primary">{familia.familia.nombre}</Typography>}
          </Breadcrumbs>

          <Grid container spacing={2}>
            <Grid size={{ xs: 6, md: 3 }}>
              <Indicador
                titulo="MTTR · tiempo medio de restauración"
                valor={nivel.ordenes ? `${dias(nivel.promedioHoras)} días` : "—"}
                detalle={nivel.ordenes ? `Del aviso a la orden finalizada · máximo ${dias(nivel.maximoHoras)} días` : "Del aviso a la orden finalizada"}
              />
            </Grid>
            <Grid size={{ xs: 6, md: 3 }}>
              <Indicador
                titulo="MTBF · tiempo medio entre fallas"
                valor={nivel.mtbfHoras != null ? `${dias(nivel.mtbfHoras)} días` : "—"}
                detalle={`${nivel.fallas} fallas en el período · ${nivel.equiposPoblacion} equipos`}
              />
            </Grid>
            <Grid size={{ xs: 6, md: 3 }}>
              <Indicador
                titulo="Órdenes terminadas"
                valor={String(nivel.ordenes)}
                detalle={`${nivel.equipos} equipos · ${fechaLarga(resumen.desde)} al ${fechaLarga(resumen.hasta)}`}
              />
            </Grid>
            <Grid size={{ xs: 6, md: 3 }}>
              <Indicador
                titulo="En curso"
                valor={String(nivel.ordenesEnCurso)}
                detalle={nivel.ordenesEnCurso ? `Antigüedad promedio ${dias(nivel.antiguedadEnCursoHoras)} días` : "Sin órdenes abiertas"}
              />
            </Grid>
          </Grid>

          {nivel.ordenes > 0 && <Desglose indicador={nivel} />}

          <Paper variant="outlined">
            <Stack
              direction={{ xs: "column", md: "row" }}
              spacing={1}
              justifyContent="space-between"
              alignItems={{ md: "center" }}
              sx={{ px: 2, pt: 2 }}
            >
              <Typography variant="subtitle1" fontWeight={700}>
                {familia ? `Equipos de ${familia.familia.nombre}` : "Familias (tipo de equipo en SAP)"}
              </Typography>
              {vista === "grafico" && <LeyendaPasos />}
            </Stack>
            <ListaPermanencia
              filas={filas}
              vista={vista}
              nivel={familia ? "equipo" : "familia"}
              onElegir={(f) => (familia ? setEquipo(f.codigo) : abrirFamilia(f))}
            />
            <Box sx={{ px: 2, py: 1.5, borderTop: 1, borderColor: "divider" }}>
              <Typography variant="caption" color="text.secondary" component="p">
                Solo órdenes correctivas. Permanencia = desde que se abre el aviso (o se crea la orden, si no tiene aviso) hasta que
                la orden queda finalizada (estado de usuario FINL; si no, su fin real). Pasos:{" "}
                {PASOS.map((p) => `${p.nombre.toLowerCase()} (${p.detalle.toLowerCase()})`).join(", ")}. El cierre técnico se
                muestra aparte porque a veces se hace días después. MTTR = promedio de las órdenes finalizadas en el período; las
                abiertas se cuentan &quot;en curso&quot; con su antigüedad. MTBF = (equipos × días del período − días fuera de servicio
                por correctivas) / correctivas que empezaron en el período; cuenta también los equipos activos que no fallaron, y el
                período empieza como muy temprano cuando SAP salió en vivo. Días de calendario.
                {!resumen.esperaRepuestosConfigurada &&
                  " La espera de repuestos todavía no se distingue: queda dentro de la ejecución hasta que exista un estado para ella en SAP."}
              </Typography>
            </Box>
          </Paper>
        </>
      )}

      <DialogoEquipoPermanencia
        equipoSapId={equipo}
        periodo={periodo}
        esperaConfigurada={resumen?.esperaRepuestosConfigurada ?? false}
        onClose={() => setEquipo(null)}
      />
    </Stack>
  );
}

export default function PermanenciaPage() {
  // useSearchParams necesita un Suspense para la compilación estática de Next.
  return (
    <Suspense fallback={null}>
      <Permanencia />
    </Suspense>
  );
}
