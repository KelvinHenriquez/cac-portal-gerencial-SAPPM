"use client";
import { Box, Stack, Typography } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { FilaAvanceDto, SemanaAvanceDto } from "@/types";
import { fechaCorta, porcentaje } from "@/utils/formato";

const ANCHO = 760;
const ALTO = 280;
const M = { izq: 44, der: 16, arriba: 16, abajo: 36 };

/**
 * Curva en "S" de una familia: programado acumulado (punteado) vs. real ejecutado (sólido) por
 * semana, con una marca en la semana actual. SVG simple, sin librería de gráficos.
 */
export default function CurvaAvance({ familia, semanas }: { familia: FilaAvanceDto; semanas: SemanaAvanceDto[] }) {
  const theme = useTheme();
  const puntos = semanas
    .map((s, i) => ({ semana: s, celda: familia.semanas[i] }))
    .filter((p) => p.celda.enVentana);

  if (puntos.length === 0) return null;

  const anchoUtil = ANCHO - M.izq - M.der;
  const altoUtil = ALTO - M.arriba - M.abajo;
  const x = (i: number) => M.izq + (puntos.length === 1 ? anchoUtil / 2 : (i * anchoUtil) / (puntos.length - 1));
  const y = (pct: number) => M.arriba + altoUtil - (pct / 100) * altoUtil;

  const linea = (valores: (number | null)[]) =>
    valores
      .map((v, i) => (v == null ? null : `${x(i).toFixed(1)},${y(v).toFixed(1)}`))
      .filter(Boolean)
      .join(" ");

  const prog = puntos.map((p) => p.celda.progPct);
  const real = puntos.map((p) => p.celda.realPct);
  const iActual = puntos.findIndex((p) => p.semana.momento === "ACTUAL");
  // Etiquetas del eje X: cada ~4 semanas para que no se encimen.
  const paso = Math.max(1, Math.ceil(puntos.length / 7));
  const colorProg = theme.palette.grey[500];
  const colorReal = theme.palette.primary.main;

  return (
    <Box>
      <Stack direction="row" spacing={3} sx={{ mb: 1 }} flexWrap="wrap" useFlexGap>
        <Leyenda color={colorProg} punteado texto="Programado (acumulado)" />
        <Leyenda color={colorReal} texto="Real ejecutado" />
        {iActual >= 0 && <Leyenda color={theme.palette.warning.main} texto="Semana actual" vertical />}
      </Stack>
      <Box
        component="svg"
        viewBox={`0 0 ${ANCHO} ${ALTO}`}
        role="img"
        aria-label={`Curva de avance de ${familia.familiaNombre}: programado ${porcentaje(familia.totalProgramadoPct)}, ejecutado ${porcentaje(familia.totalEjecutadoPct)}`}
        sx={{ width: "100%", height: "auto", display: "block" }}
      >
        {[0, 25, 50, 75, 100].map((v) => (
          <g key={v}>
            <line x1={M.izq} x2={ANCHO - M.der} y1={y(v)} y2={y(v)} stroke={theme.palette.divider} />
            <text x={M.izq - 8} y={y(v) + 4} fontSize={11} textAnchor="end" fill={theme.palette.text.secondary}>
              {v}%
            </text>
          </g>
        ))}

        {puntos.map(
          (p, i) =>
            i % paso === 0 && (
              <text key={p.semana.lunes} x={x(i)} y={ALTO - 12} fontSize={11} textAnchor="middle" fill={theme.palette.text.secondary}>
                {fechaCorta(p.semana.lunes)}
              </text>
            )
        )}

        {iActual >= 0 && (
          <line
            x1={x(iActual)}
            x2={x(iActual)}
            y1={M.arriba}
            y2={M.arriba + altoUtil}
            stroke={theme.palette.warning.main}
            strokeWidth={1.5}
            strokeDasharray="4 3"
          />
        )}

        <polyline points={linea(prog)} fill="none" stroke={colorProg} strokeWidth={2} strokeDasharray="6 4" />
        <polyline points={linea(real)} fill="none" stroke={colorReal} strokeWidth={2.5} />

        {puntos.map((p, i) => (
          <g key={`pt-${p.semana.lunes}`}>
            {p.celda.realPct != null && (
              <circle cx={x(i)} cy={y(p.celda.realPct)} r={3.5} fill={colorReal}>
                <title>
                  Semana del {fechaCorta(p.semana.lunes)}: real {porcentaje(p.celda.realPct)}
                  {p.celda.progPct != null ? `, programado ${porcentaje(p.celda.progPct)}` : ""}
                </title>
              </circle>
            )}
          </g>
        ))}
      </Box>
    </Box>
  );
}

function Leyenda({ color, texto, punteado, vertical }: { color: string; texto: string; punteado?: boolean; vertical?: boolean }) {
  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <svg width={vertical ? 10 : 26} height={14} aria-hidden>
        {vertical ? (
          <line x1={5} x2={5} y1={0} y2={14} stroke={color} strokeWidth={2} strokeDasharray="4 3" />
        ) : (
          <line x1={0} x2={26} y1={7} y2={7} stroke={color} strokeWidth={2.5} strokeDasharray={punteado ? "6 4" : undefined} />
        )}
      </svg>
      <Typography variant="caption" color="text.secondary">
        {texto}
      </Typography>
    </Stack>
  );
}
