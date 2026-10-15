import axiosInstance from "@/services/axios";
import { DetalleSemanaDto, ConfiabilidadReporteDto, EquiposAsignacionDto, HoraDisponibilidadDto, ReporteDisponibilidadDto } from "@/types";

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

  /** desde/hasta (yyyy-MM-dd): período del MTTR/MTBF de cada equipo. */
  getEquipos: async (reporteId: number, asignacionId: number, periodo?: { desde: string; hasta: string }): Promise<EquiposAsignacionDto> => {
    const { data } = await axiosInstance.get<EquiposAsignacionDto>(`${base(reporteId)}/asignaciones/${asignacionId}/equipos`, { params: periodo });
    return data;
  },

  /** MTTR y MTBF por asignación entre esas fechas (yyyy-MM-dd). */
  getConfiabilidad: async (reporteId: number, desde: string, hasta: string): Promise<ConfiabilidadReporteDto> => {
    const { data } = await axiosInstance.get<ConfiabilidadReporteDto>(`${base(reporteId)}/confiabilidad`, { params: { desde, hasta } });
    return data;
  },
};
