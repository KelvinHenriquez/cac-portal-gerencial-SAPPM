import axiosInstance from "@/services/axios";
import { PeriodoDto, ReporteResumenDto } from "@/types";

export const estructuraService = {
  /** Reportes de disponibilidad activos, en el orden configurado en Oracle. */
  getReportesDisponibilidad: async (): Promise<ReporteResumenDto[]> => {
    const { data } = await axiosInstance.get<ReporteResumenDto[]>("/estructura/disponibilidad/reportes");
    return data;
  },

  /** Períodos de reparación con sus reportes de avance, del más reciente al más viejo. */
  getPeriodos: async (): Promise<PeriodoDto[]> => {
    const { data } = await axiosInstance.get<PeriodoDto[]>("/estructura/avance/periodos");
    return data;
  },
};
