"use client";
import React, { createContext, useContext, useEffect, useState, useCallback } from "react";
import Cookies from "js-cookie";
import { useRouter } from "next/navigation";
import { authService } from "@/services/auth/authService";
import { ACCESS_TOKEN_COOKIE } from "@/services/axios";
import { LoginResponse, RolUsuario } from "@/types";

const USER_STORAGE_KEY = "portalgerencial_user";

export interface SesionUsuario {
  usuario: string;
  rol: RolUsuario;
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
    rol: response.rol,
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
        setUsuario(JSON.parse(raw));
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

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth debe usarse dentro de AuthProvider");
  return context;
};
