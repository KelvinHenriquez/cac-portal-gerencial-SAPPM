import axiosInstance from "@/services/axios";
import { AnalisisAvanceDto, DetalleSemanaAvanceDto, MedidaAvance, RefPlan, ReporteAvanceDto } from "@/types";

const base = (reporteId: number) => `/avance/${reporteId}`;

export const avanceService = {
  getReporte: async (reporteId: number, medida: MedidaAvance, refPlan: RefPlan): Promise<ReporteAvanceDto> => {
    const { data } = await axiosInstance.get<ReporteAvanceDto>(base(reporteId), { params: { medida, refPlan } });
    return data;
  },

  getDetalleSemana: async (reporteId: number, familiaCodigo: string, noSemana: number): Promise<DetalleSemanaAvanceDto> => {
    const { data } = await axiosInstance.get<DetalleSemanaAvanceDto>(
      `${base(reporteId)}/familias/${encodeURIComponent(familiaCodigo)}/semanas/${noSemana}`
    );
    return data;
  },

  getAnalisis: async (reporteId: number, familiaCodigo: string): Promise<AnalisisAvanceDto> => {
    const { data } = await axiosInstance.get<AnalisisAvanceDto>(`${base(reporteId)}/familias/${encodeURIComponent(familiaCodigo)}/analisis`);
    return data;
  },
};
