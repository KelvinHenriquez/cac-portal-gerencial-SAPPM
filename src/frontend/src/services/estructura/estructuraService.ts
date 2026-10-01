import axiosInstance from "@/services/axios";
import { ProgramaDto, TipoReporteDto } from "@/types";

export const estructuraService = {
  getTiposReporte: async (): Promise<TipoReporteDto[]> => {
    const { data } = await axiosInstance.get<TipoReporteDto[]>("/estructura/tipos-reporte");
    return data;
  },

  /** Programas de reparación, del más reciente al más viejo. */
  getProgramas: async (): Promise<ProgramaDto[]> => {
    const { data } = await axiosInstance.get<ProgramaDto[]>("/estructura/programas");
    return data;
  },
};
