"use client";
import React, { createContext, useContext, useEffect, useState, useCallback } from "react";
import Cookies from "js-cookie";
import { useRouter } from "next/navigation";
import { authService } from "@/services/auth/authService";
import { ACCESS_TOKEN_COOKIE } from "@/services/axios";
import { LoginResponse, Permisos, RolUsuario } from "@/types";

const USER_STORAGE_KEY = "portalgerencial_user";

export interface SesionUsuario {
  usuario: string;
  roles: RolUsuario[];
  permisos: Permisos;
  passwordPorVencer: boolean;
}

interface AuthContextType {
  usuario: SesionUsuario | null;
  loading: boolean;
  login: (usuario: string, password: string) => Promise<SesionUsuario>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

function guardarSesion(response: LoginResponse) {
  const expires = new Date(response.expiresAt);
  Cookies.set(ACCESS_TOKEN_COOKIE, response.accessToken, { expires, sameSite: "lax" });
  const sesion: SesionUsuario = {
    usuario: response.usuario,
    roles: response.roles,
    permisos: response.permisos,
    passwordPorVencer: response.passwordPorVencer,
  };
  localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(sesion));
  return sesion;
}

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [usuario, setUsuario] = useState<SesionUsuario | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const token = Cookies.get(ACCESS_TOKEN_COOKIE);
    const raw = typeof window !== "undefined" ? localStorage.getItem(USER_STORAGE_KEY) : null;
    if (token && raw) {
      try {
        const sesion = JSON.parse(raw) as SesionUsuario;
        // Sesión de antes de los roles (10/10/2026): sin permisos, hay que volver a entrar.
        if (sesion?.permisos?.areas) setUsuario(sesion);
        else {
          Cookies.remove(ACCESS_TOKEN_COOKIE);
          localStorage.removeItem(USER_STORAGE_KEY);
        }
      } catch {
        setUsuario(null);
      }
    }
    setLoading(false);
  }, []);

  const login = useCallback(async (nombreUsuario: string, password: string) => {
    const response = await authService.login({ usuario: nombreUsuario, password });
    const sesion = guardarSesion(response);
    setUsuario(sesion);
    return sesion;
  }, []);

  const logout = useCallback(() => {
    Cookies.remove(ACCESS_TOKEN_COOKIE);
    localStorage.removeItem(USER_STORAGE_KEY);
    setUsuario(null);
    router.push("/auth/login");
  }, [router]);

  return (
    <AuthContext.Provider value={{ usuario, loading, login, logout }}>{children}</AuthContext.Provider>
  );
};

/** Permisos del usuario en sesión (todo en falso si no hay sesión). */
export function usePermisos(): Permisos & { veArea: (area: string) => boolean } {
  const { usuario } = useAuth();
  const p = usuario?.permisos ?? { areas: [], editarParametros: false, editarParametrosTecnicos: false, actualizar: false, areasEditables: [] };
  return { ...p, veArea: (area: string) => p.areas.includes(area.toUpperCase()) };
}

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth debe usarse dentro de AuthProvider");
  return context;
};
