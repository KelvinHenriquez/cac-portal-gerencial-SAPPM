"use client";
import {
  Box,
  Button,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { IndicadorPermanenciaDto } from "@/types";
import { BarraPasos, dias, PASOS, sumaPasos } from "./comunesPermanencia";

export type VistaPermanencia = "grafico" | "tabla";

/**
 * Familias (tipos de equipo) o equipos con su permanencia. En gráfico: una barra apilada por
 * fila con los pasos promedio; en tabla: todas las cifras. Tocar una fila baja al siguiente nivel.
 */
export default function ListaPermanencia({
  filas,
  vista,
  nivel,
  onElegir,
}: {
  filas: IndicadorPermanenciaDto[];
  vista: VistaPermanencia;
  nivel: "familia" | "equipo";
  onElegir: (fila: IndicadorPermanenciaDto) => void;
}) {
  if (filas.length === 0)
    return (
      <Typography color="text.secondary" sx={{ p: 3, textAlign: "center" }}>
        No hay órdenes correctivas terminadas en el período.
      </Typography>
    );

  const nombre = (f: IndicadorPermanenciaDto) => (nivel === "equipo" && f.clave ? `${f.clave} · ${f.nombre}` : f.nombre);

  if (vista === "grafico") {
    const escala = Math.max(...filas.map((f) => sumaPasos(f.pasos)), 1);
    return (
      <Stack spacing={1.25} sx={{ p: 2 }}>
        {filas.map((f) => (
          <Box
            key={f.codigo}
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", md: "minmax(180px, 26%) 1fr 190px" },
              gap: { xs: 0.5, md: 2 },
              alignItems: "center",
            }}
          >
            <Stack direction="row" alignItems="baseline" spacing={1} sx={{ minWidth: 0 }}>
              <Button
                variant="text"
                onClick={() => onElegir(f)}
                sx={{ justifyContent: "flex-start", textAlign: "left", p: 0, minWidth: 0, fontWeight: 600, lineHeight: 1.3 }}
              >
                {nombre(f)}
              </Button>
              <Typography
                component="span"
                variant="caption"
                color="text.secondary"
                title="MTBF: tiempo medio entre fallas"
                sx={{ whiteSpace: "nowrap", fontVariantNumeric: "tabular-nums" }}
              >
                MTBF {dias(f.mtbfHoras)} d
              </Typography>
            </Stack>
            {f.ordenes > 0 ? (
              <BarraPasos pasos={f.pasos} escala={escala} />
            ) : (
              <Typography variant="caption" color="text.secondary">
                Sin órdenes terminadas en el período
              </Typography>
            )}
            <Typography variant="body2" sx={{ fontVariantNumeric: "tabular-nums", whiteSpace: "nowrap" }}>
              <span title="MTTR: tiempo medio de restauración"><b>{dias(f.promedioHoras)} d</b></span>
              <Typography component="span" variant="caption" color="text.secondary">
                {" "}
                · {f.ordenes} órd.{f.ordenesEnCurso > 0 ? ` · ${f.ordenesEnCurso} en curso` : ""}
              </Typography>
            </Typography>
          </Box>
        ))}
      </Stack>
    );
  }

  return (
    <TableContainer>
      <Table size="small" stickyHeader>
        <TableHead>
          <TableRow>
            <TableCell>{nivel === "familia" ? "Tipo de equipo" : "Equipo"}</TableCell>
            <TableCell align="right">Órdenes</TableCell>
            {nivel === "familia" && <TableCell align="right">Equipos</TableCell>}
            <TableCell align="right" title="Tiempo medio de restauración: del aviso a la orden finalizada">MTTR (d)</TableCell>
            <TableCell align="right" title="Tiempo medio entre fallas">MTBF (d)</TableCell>
            <TableCell align="right">Fallas</TableCell>
            <TableCell align="right">Máximo (d)</TableCell>
            {PASOS.map((p) => (
              <TableCell key={p.clave} align="right" title={p.detalle}>
                {p.nombre} (d)
              </TableCell>
            ))}
            <TableCell align="right" title="Finalizada → cierre técnico (no suma a la permanencia)">
              Cierre adm. (d)
            </TableCell>
            <TableCell align="right">En curso</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {filas.map((f) => (
            <TableRow key={f.codigo} hover sx={{ cursor: "pointer" }} onClick={() => onElegir(f)}>
              <TableCell sx={{ fontWeight: 600 }}>{nombre(f)}</TableCell>
              <TableCell align="right">{f.ordenes}</TableCell>
              {nivel === "familia" && <TableCell align="right">{f.equipos}</TableCell>}
              <TableCell align="right" sx={{ fontWeight: 700 }}>
                {f.ordenes ? dias(f.promedioHoras) : "—"}
              </TableCell>
              <TableCell align="right">{dias(f.mtbfHoras)}</TableCell>
              <TableCell align="right">{f.fallas}</TableCell>
              <TableCell align="right">{f.ordenes ? dias(f.maximoHoras) : "—"}</TableCell>
              {PASOS.map((p) => (
                <TableCell key={p.clave} align="right">
                  {dias(f.pasos[p.clave])}
                </TableCell>
              ))}
              <TableCell align="right">{dias(f.pasos.cierreAdministrativo)}</TableCell>
              <TableCell align="right">
                {f.ordenesEnCurso > 0 ? `${f.ordenesEnCurso} (${dias(f.antiguedadEnCursoHoras)} d)` : "—"}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
