"use client";
import { usePathname } from "next/navigation";
import { MENU_ITEMS, rutaDe } from "./MenuItems";

export function usePageTitle(): string {
  const pathname = usePathname() || "";
  const match = MENU_ITEMS.find((item) =>
    item.exact ? pathname === item.href : pathname.startsWith(rutaDe(item.href))
  );
  return match?.title || "Portal Gerencial SAP PM";
}
