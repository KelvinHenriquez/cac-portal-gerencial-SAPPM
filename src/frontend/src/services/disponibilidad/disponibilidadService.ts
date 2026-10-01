import axiosInstance from "@/services/axios";
import { DetalleSemanaDto, EquiposAsignacionDto, HoraDisponibilidadDto, ReporteDisponibilidadDto } from "@/types";

const base = (reporteId: number) => `/disponibilidad/${reporteId}`;

export const disponibilidadService = {
  getReporte: async (reporteId: number): Promise<ReporteDisponibilidadDto> => {
    const { data } = await axiosInstance.get<ReporteDisponibilidadDto>(base(reporteId));
    return data;
  },

  getDetalleSemana: async (reporteId: number, asignacionId: number, noSemana: number): Promise<DetalleSemanaDto> => {
    const { data } = await axiosInstance.get<DetalleSemanaDto>(`${base(reporteId)}/asignaciones/${asignacionId}/semanas/${noSemana}`);
    return data;
  },

  /** fecha en formato yyyy-MM-dd */
  getHoras: async (reporteId: number, asignacionId: number, fecha: string): Promise<HoraDisponibilidadDto[]> => {
    const { data } = await axiosInstance.get<HoraDisponibilidadDto[]>(
      `${base(reporteId)}/asignaciones/${asignacionId}/dias/${fecha.slice(0, 10)}/horas`
    );
    return data;
  },

  getEquipos: async (reporteId: number, asignacionId: number): Promise<EquiposAsignacionDto> => {
    const { data } = await axiosInstance.get<EquiposAsignacionDto>(`${base(reporteId)}/asignaciones/${asignacionId}/equipos`);
    return data;
  },
};
