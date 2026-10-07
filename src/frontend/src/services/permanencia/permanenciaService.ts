import axiosInstance from "@/services/axios";
import { AreaPermanenciaDto, EquipoPermanenciaDto, FamiliaPermanenciaDto, ResumenPermanenciaDto } from "@/types";

export interface PeriodoPermanencia {
  desde: string;
  hasta: string;
}

export const permanenciaService = {
  getAreas: async (): Promise<AreaPermanenciaDto[]> => {
    const { data } = await axiosInstance.get<AreaPermanenciaDto[]>("/permanencia/areas");
    return data;
  },

  getResumen: async (area: string, periodo: PeriodoPermanencia): Promise<ResumenPermanenciaDto> => {
    const { data } = await axiosInstance.get<ResumenPermanenciaDto>(`/permanencia/${encodeURIComponent(area)}`, { params: periodo });
    return data;
  },

  getFamilia: async (area: string, tipoEquipo: string, periodo: PeriodoPermanencia): Promise<FamiliaPermanenciaDto> => {
    const { data } = await axiosInstance.get<FamiliaPermanenciaDto>(
      `/permanencia/${encodeURIComponent(area)}/familias/${encodeURIComponent(tipoEquipo)}`,
      { params: periodo }
    );
    return data;
  },

  getEquipo: async (equipoSapId: string, periodo: PeriodoPermanencia): Promise<EquipoPermanenciaDto> => {
    const { data } = await axiosInstance.get<EquipoPermanenciaDto>(`/permanencia/equipos/${encodeURIComponent(equipoSapId)}`, {
      params: periodo,
    });
    return data;
  },
};
