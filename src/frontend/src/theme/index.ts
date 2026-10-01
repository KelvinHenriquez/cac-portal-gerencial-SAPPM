"use client";
import { createTheme, Theme } from "@mui/material/styles";
import { esES } from "@mui/material/locale";
import baseLightTheme from "./DefaultColors";
import typography from "./Typography";
import shadows from "./Shadows";
import components from "./Components";

// Mismo sistema visual que la plantilla de TPresto (Modernize): tipografía Plus Jakarta Sans,
// sombras suaves, tarjetas redondeadas, botones sin sombra. Sección 12 lo toma como acelerador
// de patrones igual que el resto de la arquitectura — el color de marca (teal) se puede
// reemplazar por uno propio de CAC en DefaultColors.ts sin tocar nada más.
const theme = createTheme(
  {
    ...baseLightTheme,
    shape: { borderRadius: 10 },
    shadows: shadows as Theme["shadows"],
    typography,
  },
  esES
);

theme.components = components(theme);

export default theme;
