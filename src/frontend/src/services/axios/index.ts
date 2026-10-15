import axios from "axios";
import Cookies from "js-cookie";

const ACCESS_TOKEN_COOKIE = "accessToken";

// El portal también se abre dentro de SAP Fiori (iframe, otro dominio): ahí el navegador no
// guarda cookies SameSite=Lax y el login rebotaba. None + Secure la permite en el iframe y
// Partitioned (CHIPS) la aísla por sitio, así sigue funcionando aunque se bloqueen las cookies
// de terceros (16/10/2026).
const OPCIONES_COOKIE = { sameSite: "none", secure: true, partitioned: true } as const;

export const tokenSesion = {
  leer: () => Cookies.get(ACCESS_TOKEN_COOKIE),
  guardar: (token: string, expires: Date) => Cookies.set(ACCESS_TOKEN_COOKIE, token, { ...OPCIONES_COOKIE, expires }),
  borrar: () => {
    Cookies.remove(ACCESS_TOKEN_COOKIE, OPCIONES_COOKIE);
    Cookies.remove(ACCESS_TOKEN_COOKIE); // la de antes (SameSite=Lax), si quedó
  },
};

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5402";
const API_URL = `${API_BASE_URL}/api`;

const axiosInstance = axios.create({
  baseURL: API_URL,
  headers: { "Content-Type": "application/json" },
});

// Mismo patrón que TPresto: interceptor de request inyecta el JWT, interceptor de response
// redirige a /auth/login si el token expiró o es inválido (401).
axiosInstance.interceptors.request.use((config) => {
  const token = tokenSesion.leer();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && typeof window !== "undefined") {
      tokenSesion.borrar();
      if (!window.location.pathname.startsWith("/auth/login")) {
        window.location.href = "/auth/login";
      }
    }
    return Promise.reject(error.response?.data || { errorMessage: "Error de comunicación con el servidor." });
  }
);

export default axiosInstance;
export { ACCESS_TOKEN_COOKIE };
