"use client";
import React, { createContext, useContext, useState, useCallback } from "react";

export const SIDEBAR_WIDTH = 270;
export const MINI_SIDEBAR_WIDTH = 87;
export const TOPBAR_HEIGHT = 70;

interface SidebarContextType {
  isCollapsed: boolean;
  isHovering: boolean;
  isMobileOpen: boolean;
  toggleCollapsed: () => void;
  setHovering: (v: boolean) => void;
  toggleMobile: () => void;
  closeMobile: () => void;
}

const SidebarContext = createContext<SidebarContextType | undefined>(undefined);

// Reemplaza el store de Redux (CustomizerSlice) que usa la plantilla de TPresto para el mismo
// propósito — mismo resultado visual (sidebar colapsable, drawer móvil), sin traer Redux entero
// solo para esto.
export const SidebarProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [isHovering, setIsHovering] = useState(false);
  const [isMobileOpen, setIsMobileOpen] = useState(false);

  const toggleCollapsed = useCallback(() => setIsCollapsed((v) => !v), []);
  const setHovering = useCallback((v: boolean) => setIsHovering(v), []);
  const toggleMobile = useCallback(() => setIsMobileOpen((v) => !v), []);
  const closeMobile = useCallback(() => setIsMobileOpen(false), []);

  return (
    <SidebarContext.Provider
      value={{ isCollapsed, isHovering, isMobileOpen, toggleCollapsed, setHovering, toggleMobile, closeMobile }}
    >
      {children}
    </SidebarContext.Provider>
  );
};

export const useSidebar = (): SidebarContextType => {
  const context = useContext(SidebarContext);
  if (!context) throw new Error("useSidebar debe usarse dentro de SidebarProvider");
  return context;
};
