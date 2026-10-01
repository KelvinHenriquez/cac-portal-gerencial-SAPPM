// Mismos ajustes de componentes MUI que la plantilla de TPresto (Modernize) — genéricos
// (botones sin sombra, tarjetas redondeadas, hover coloreado), sin ninguna lógica de negocio.
import { Theme } from "@mui/material/styles";

const components = (theme: Theme) => ({
  MuiCssBaseline: {
    styleOverrides: {
      "*": { boxSizing: "border-box" as const },
      html: { height: "100%", width: "100%" },
      a: { textDecoration: "none" },
      body: { height: "100%", margin: 0, padding: 0 },
      hr: { height: "1px", border: 0, borderTop: `1px solid ${theme.palette.divider}` },
    },
  },
  MuiButtonGroup: { styleOverrides: { root: { boxShadow: "none" } } },
  MuiPaper: { styleOverrides: { root: { backgroundImage: "none" } } },
  MuiFab: {
    styleOverrides: {
      root: { boxShadow: "none" },
      sizeSmall: { width: 30, height: 30, minHeight: 30 },
    },
  },
  MuiIconButton: {
    styleOverrides: {
      root: {
        "&:hover": { backgroundColor: theme.palette.primary.light, color: theme.palette.primary.main },
      },
      colorPrimary: { "&:hover": { backgroundColor: theme.palette.primary.main, color: "white" } },
    },
  },
  MuiButton: {
    styleOverrides: {
      root: { textTransform: "none" as const, boxShadow: "none" },
      textPrimary: {
        backgroundColor: theme.palette.primary.light,
        "&:hover": { backgroundColor: theme.palette.primary.main, color: "white" },
      },
      outlinedPrimary: { "&:hover": { backgroundColor: theme.palette.primary.main, color: "white" } },
    },
  },
  MuiCardHeader: {
    styleOverrides: { root: { padding: "16px 24px" }, title: { fontSize: "1.125rem" } },
  },
  MuiCard: {
    styleOverrides: { root: { width: "100%", backgroundImage: "none", borderRadius: "12px" } },
  },
  MuiCardContent: { styleOverrides: { root: { padding: "24px" } } },
  MuiTableCell: { styleOverrides: { root: { borderBottom: `1px solid ${theme.palette.divider}` } } },
  MuiTableRow: { styleOverrides: { root: { "&:last-child td": { borderBottom: 0 } } } },
  MuiLinearProgress: {
    styleOverrides: { root: { backgroundColor: theme.palette.grey[200], borderRadius: "6px" } },
  },
  MuiDivider: { styleOverrides: { root: { borderColor: theme.palette.divider } } },
  MuiChip: { styleOverrides: { root: { fontWeight: 600, fontSize: "0.75rem" } } },
  MuiAlert: {
    styleOverrides: {
      standardSuccess: { backgroundColor: theme.palette.success.light, color: theme.palette.success.dark },
      standardError: { backgroundColor: theme.palette.error.light, color: theme.palette.error.dark },
      standardWarning: { backgroundColor: theme.palette.warning.light, color: theme.palette.warning.dark },
      standardInfo: { backgroundColor: theme.palette.info.light, color: theme.palette.info.dark },
    },
  },
  MuiOutlinedInput: {
    styleOverrides: {
      root: {
        borderRadius: "8px",
        "& .MuiOutlinedInput-notchedOutline": { borderColor: theme.palette.grey[300] },
        "&:hover .MuiOutlinedInput-notchedOutline": { borderColor: theme.palette.grey[400] },
      },
      input: { padding: "12px 14px" },
      inputSizeSmall: { padding: "8px 14px" },
    },
  },
  MuiTooltip: {
    styleOverrides: {
      tooltip: { color: theme.palette.background.paper, background: theme.palette.text.primary },
    },
  },
  MuiDrawer: { styleOverrides: { paper: { borderColor: theme.palette.divider } } },
  MuiDialogTitle: { styleOverrides: { root: { fontSize: "1.25rem" } } },
  MuiPopover: {
    styleOverrides: {
      paper: {
        boxShadow: "rgb(145 158 171 / 30%) 0px 0px 2px 0px, rgb(145 158 171 / 12%) 0px 12px 24px -4px",
      },
    },
  },
});

export default components;
