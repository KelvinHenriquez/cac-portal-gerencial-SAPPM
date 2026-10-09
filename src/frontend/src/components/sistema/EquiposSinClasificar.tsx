"use client";
import { useMemo, useState } from "react";
import {
  Box,
  Chip,
  InputAdornment,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from "@mui/material";
import { IconSearch } from "@tabler/icons-react";
import { EquipoSinClasificarDto } from "@/types";

const MOTIVO: Record<EquipoSinClasificarDto["motivo"], string> = {
  SIN_CODIGO_LEGADO: "Sin Ficha en SAP",
  CODIGO_NO_EXISTE_EN_ORACLE: "Ficha que no está en Oracle",
};

type Filtro = "conOrdenes" | "todos";

/**
 * Equipos de SAP sin familia de Oracle. En producción son ~1.400 (casi todos de fábrica, que nunca
 * tendrán Ficha): por defecto se muestran solo los que tienen órdenes, con búsqueda y páginas.
 */
export default function EquiposSinClasificar({ equipos }: { equipos: EquipoSinClasificarDto[] }) {
  const [filtro, setFiltro] = useState<Filtro>("conOrdenes");
  const [motivo, setMotivo] = useState<EquipoSinClasificarDto["motivo"] | null>(null);
  const [buscar, setBuscar] = useState("");
  const [pagina, setPagina] = useState(0);
  const [porPagina, setPorPagina] = useState(25);

  const conOrdenes = useMemo(() => equipos.filter((e) => e.cantOrdenes > 0).length, [equipos]);
  const porMotivo = useMemo(() => {
    const m = new Map<EquipoSinClasificarDto["motivo"], number>();
    equipos.forEach((e) => m.set(e.motivo, (m.get(e.motivo) ?? 0) + 1));
    return [...m.entries()];
  }, [equipos]);

  const filas = useMemo(() => {
    const t = buscar.trim().toLowerCase();
    return equipos.filter(
      (e) =>
        (filtro === "todos" || e.cantOrdenes > 0) &&
        (motivo == null || e.motivo === motivo) &&
        (!t || [e.equipoSapId, e.codigoLegado, e.descripcion].some((v) => v?.toLowerCase().includes(t)))
    );
  }, [equipos, filtro, motivo, buscar]);

  const visibles = filas.slice(pagina * porPagina, pagina * porPagina + porPagina);
  const cambiar = (f: () => void) => {
    f();
    setPagina(0);
  };

  return (
    <Paper variant="outlined">
      <Box sx={{ px: 2, pt: 2, pb: 1 }}>
        <Typography variant="subtitle1" fontWeight={700}>
          Equipos sin clasificar ({equipos.length})
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Equipos de SAP que no se pudieron asignar a una familia de Oracle por su Ficha (código legado). No entran en
          Disponibilidad ni en Avance, que se configuran en Oracle por Ficha; sí cuentan en Tiempo de permanencia. Los
          equipos de fábrica no tienen Ficha: es normal que aparezcan aquí.
        </Typography>
      </Box>

      {equipos.length > 0 && (
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={1.5}
          alignItems={{ md: "center" }}
          flexWrap="wrap"
          useFlexGap
          sx={{ px: 2, pb: 1.5 }}
        >
          <ToggleButtonGroup size="small" exclusive value={filtro} onChange={(_, v) => v && cambiar(() => setFiltro(v))}>
            <ToggleButton value="conOrdenes">Con órdenes ({conOrdenes})</ToggleButton>
            <ToggleButton value="todos">Todos ({equipos.length})</ToggleButton>
          </ToggleButtonGroup>
          {porMotivo.length > 1 &&
            porMotivo.map(([m, n]) => (
              <Chip
                key={m}
                label={`${MOTIVO[m] ?? m}: ${n}`}
                variant={motivo === m ? "filled" : "outlined"}
                color={motivo === m ? "primary" : "default"}
                onClick={() => cambiar(() => setMotivo(motivo === m ? null : m))}
              />
            ))}
          <Box flexGrow={1} />
          <TextField
            size="small"
            placeholder="Buscar equipo o descripción"
            value={buscar}
            onChange={(e) => cambiar(() => setBuscar(e.target.value))}
            sx={{ minWidth: { md: 280 } }}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <IconSearch size={16} />
                  </InputAdornment>
                ),
              },
            }}
          />
        </Stack>
      )}

      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Equipo SAP</TableCell>
              <TableCell>Código legado</TableCell>
              <TableCell>Descripción</TableCell>
              <TableCell>Motivo</TableCell>
              <TableCell align="right">Órdenes</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {visibles.map((e) => (
              <TableRow key={e.equipoSapId} hover>
                <TableCell sx={{ fontVariantNumeric: "tabular-nums", whiteSpace: "nowrap" }}>{e.equipoSapId}</TableCell>
                <TableCell>{e.codigoLegado ?? "—"}</TableCell>
                <TableCell>{e.descripcion ?? "—"}</TableCell>
                <TableCell>{MOTIVO[e.motivo] ?? e.motivo}</TableCell>
                <TableCell align="right">{e.cantOrdenes}</TableCell>
              </TableRow>
            ))}
            {visibles.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} align="center" sx={{ py: 4, color: "text.secondary" }}>
                  {equipos.length === 0 ? "Todos los equipos de SAP tienen familia." : "Ningún equipo coincide con el filtro."}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
      {filas.length > porPagina && (
        <TablePagination
          component="div"
          count={filas.length}
          page={pagina}
          onPageChange={(_, p) => setPagina(p)}
          rowsPerPage={porPagina}
          onRowsPerPageChange={(e) => cambiar(() => setPorPagina(parseInt(e.target.value, 10)))}
          rowsPerPageOptions={[25, 50, 100]}
          labelRowsPerPage="Filas por página"
          labelDisplayedRows={({ from, to, count }) => `${from}–${to} de ${count}`}
        />
      )}
    </Paper>
  );
}
