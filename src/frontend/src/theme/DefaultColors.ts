// Colores institucionales del Consorcio Azucarero Central (logo oficial, 10/10/2026):
// verde CAC #009845 (letras "CAC") como color principal y verde lima #93d500 (triángulo) como
// acento. light = fondo suave para hover/selección; dark = hover de botones.
const baseLightTheme = {
  palette: {
    primary: { main: "#009845", light: "#E6F5EC", dark: "#007A37", contrastText: "#ffffff" },
    secondary: { main: "#93d500", light: "#F4FBE5", dark: "#6E9F00", contrastText: "#1E2A00" },
    success: { main: "#009845", light: "#E6F5EC", dark: "#007A37", contrastText: "#ffffff" },
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
