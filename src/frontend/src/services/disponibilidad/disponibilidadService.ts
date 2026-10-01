import axiosInstance from "@/services/axios";
import { DetalleSemanaDto, EquiposFamiliaDetalleDto, ReporteDisponibilidadDto } from "@/types";

const base = (tipoReporte: string) => `/disponibilidad/${encodeURIComponent(tipoReporte)}`;

export const disponibilidadService = {
  getReporte: async (tipoReporte: string, semanas: number): Promise<ReporteDisponibilidadDto> => {
    const { data } = await axiosInstance.get<ReporteDisponibilidadDto>(base(tipoReporte), { params: { semanas } });
    return data;
  },

  getEquiposFamilia: async (tipoReporte: string, familiaCodigo: string): Promise<EquiposFamiliaDetalleDto> => {
    const { data } = await axiosInstance.get<EquiposFamiliaDetalleDto>(
      `${base(tipoReporte)}/familias/${encodeURIComponent(familiaCodigo)}/equipos`
    );
    return data;
  },

  getDetalleSemana: async (
    tipoReporte: string,
    familiaCodigo: string,
    anioIso: number,
    semanaIso: number
  ): Promise<DetalleSemanaDto> => {
    const { data } = await axiosInstance.get<DetalleSemanaDto>(
      `${base(tipoReporte)}/familias/${encodeURIComponent(familiaCodigo)}/semanas/${anioIso}/${semanaIso}`
    );
    return data;
  },
};
