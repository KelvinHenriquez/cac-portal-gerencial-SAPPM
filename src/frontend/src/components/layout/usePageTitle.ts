"use client";
import { usePathname } from "next/navigation";
import { MENU_ITEMS } from "./MenuItems";

export function usePageTitle(): string {
  const pathname = usePathname() || "";
  const match = MENU_ITEMS.find((item) =>
    item.exact ? pathname === item.href : pathname.startsWith(item.href)
  );
  return match?.title || "Portal Gerencial SAP PM";
}
