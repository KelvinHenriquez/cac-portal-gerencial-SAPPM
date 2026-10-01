import axiosInstance from "@/services/axios";
import { EstadoActualizacionDto } from "@/types";

export const sistemaService = {
  getEstadoActualizacion: async (): Promise<EstadoActualizacionDto> => {
    const { data } = await axiosInstance.get<EstadoActualizacionDto>("/sistema/actualizacion");
    return data;
  },
};
