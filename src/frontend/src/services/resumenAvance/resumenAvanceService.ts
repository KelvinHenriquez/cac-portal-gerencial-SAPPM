import axiosInstance from "@/services/axios";
import { ConfigResumenDto, FilaConfigResumenDto, ResumenAvanceDto } from "@/types";

const base = (area: string) => `/resumen-avance/${area}`;

export const resumenAvanceService = {
  /** semanaFin (yyyy-MM-dd): la semana a mostrar; sin ella, la actual. */
  getResumen: async (area: string, semanaFin: string | null, medida: string): Promise<ResumenAvanceDto> => {
    const { data } = await axiosInstance.get<ResumenAvanceDto>(base(area), {
      params: { medida, ...(semanaFin ? { semanaFin } : {}) },
    });
    return data;
  },

  getConfig: async (area: string): Promise<ConfigResumenDto> => {
    const { data } = await axiosInstance.get<ConfigResumenDto>(`${base(area)}/config`);
    return data;
  },

  guardarConfig: async (area: string, filas: FilaConfigResumenDto[]): Promise<ConfigResumenDto> => {
    const { data } = await axiosInstance.put<ConfigResumenDto>(`${base(area)}/config`, { filas });
    return data;
  },

  /** Texto vacío = borrar la nota. */
  guardarNota: async (area: string, nota: { reporteId: number; familiaCodigo: string; semanaFin: string; texto: string }) => {
    await axiosInstance.put(`${base(area)}/notas`, nota);
  },
};
