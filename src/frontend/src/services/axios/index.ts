import axios from "axios";
import Cookies from "js-cookie";

const ACCESS_TOKEN_COOKIE = "accessToken";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5402";
const API_URL = `${API_BASE_URL}/api`;

const axiosInstance = axios.create({
  baseURL: API_URL,
  headers: { "Content-Type": "application/json" },
});

// Mismo patrón que TPresto: interceptor de request inyecta el JWT, interceptor de response
// redirige a /auth/login si el token expiró o es inválido (401).
axiosInstance.interceptors.request.use((config) => {
  const token = Cookies.get(ACCESS_TOKEN_COOKIE);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && typeof window !== "undefined") {
      Cookies.remove(ACCESS_TOKEN_COOKIE);
      if (!window.location.pathname.startsWith("/auth/login")) {
        window.location.href = "/auth/login";
      }
    }
    return Promise.reject(error.response?.data || { errorMessage: "Error de comunicación con el servidor." });
  }
);

export default axiosInstance;
export { ACCESS_TOKEN_COOKIE };
