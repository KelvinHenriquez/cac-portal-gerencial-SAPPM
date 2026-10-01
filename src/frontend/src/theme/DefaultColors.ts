// Misma paleta base que usa la plantilla de TPresto (Modernize) — el teal "#0acaab" es el color
// de marca de esa plantilla, no de CAC. Es el único archivo que hay que tocar para cambiarlo por
// un color propio de CAC más adelante (basta con reemplazar primary.main/light/dark).
const baseLightTheme = {
  palette: {
    primary: { main: "#0acaab", light: "#E6FFFA", dark: "#13deb9", contrastText: "#ffffff" },
    secondary: { main: "#49BEFF", light: "#E8F7FF", dark: "#23afdb", contrastText: "#ffffff" },
    success: { main: "#0acaab", light: "#E6FFFA", dark: "#13deb9", contrastText: "#ffffff" },
    info: { main: "#539BFF", light: "#EBF3FE", dark: "#1682d4", contrastText: "#ffffff" },
    error: { main: "#FA896B", light: "#FDEDE8", dark: "#f3704d", contrastText: "#ffffff" },
    warning: { main: "#FFAE1F", light: "#FEF5E5", dark: "#ae8e59", contrastText: "#ffffff" },
    grey: {
      100: "#F2F6FA",
      200: "#EAEFF4",
      300: "#DFE5EF",
      400: "#7C8FAC",
      500: "#5A6A85",
      600: "#2A3547",
    },
    text: { primary: "#2A3547", secondary: "#5A6A85" },
    divider: "#e5eaef",
    background: { default: "#f4f6f8", paper: "#ffffff" },
  },
};

export default baseLightTheme;
