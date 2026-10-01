import axiosInstance from "@/services/axios";
import { TipoReporteDto } from "@/types";

export const estructuraService = {
  getTiposReporte: async (): Promise<TipoReporteDto[]> => {
    const { data } = await axiosInstance.get<TipoReporteDto[]>("/estructura/tipos-reporte");
    return data;
  },
};
