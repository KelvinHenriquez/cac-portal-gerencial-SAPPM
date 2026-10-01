import axiosInstance from "@/services/axios";
import { ReporteAvanceDto } from "@/types";

export const avanceService = {
  getReporte: async (anioPrograma: string): Promise<ReporteAvanceDto> => {
    const { data } = await axiosInstance.get<ReporteAvanceDto>(`/avance/${encodeURIComponent(anioPrograma)}`);
    return data;
  },
};
