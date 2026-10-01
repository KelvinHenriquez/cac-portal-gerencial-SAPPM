import axiosInstance from "@/services/axios";
import { AlertaDto, CorridaDto, EquipoSinClasificarDto, EstadoActualizacionDto } from "@/types";

/** Evento de ventana que avisa que hubo una corrida nueva (el encabezado se refresca al oírlo). */
export const EVENTO_DATOS_ACTUALIZADOS = "portalgerencial:datos-actualizados";

export const sistemaService = {
  getEstadoActualizacion: async (): Promise<EstadoActualizacionDto> => {
    const { data } = await axiosInstance.get<EstadoActualizacionDto>("/sistema/actualizacion");
    return data;
  },

  getCorridas: async (cantidad = 20): Promise<CorridaDto[]> => {
    const { data } = await axiosInstance.get<CorridaDto[]>("/sistema/corridas", { params: { cantidad } });
    return data;
  },

  /** Corre extracción + recálculo ahora y devuelve el resultado (tarda unos segundos). */
  ejecutarCorrida: async (): Promise<CorridaDto> => {
    const { data } = await axiosInstance.post<CorridaDto>("/sistema/corridas", null, { timeout: 15 * 60 * 1000 });
    window.dispatchEvent(new Event(EVENTO_DATOS_ACTUALIZADOS));
    return data;
  },

  getEquiposSinClasificar: async (): Promise<EquipoSinClasificarDto[]> => {
    const { data } = await axiosInstance.get<EquipoSinClasificarDto[]>("/sistema/equipos-sin-clasificar");
    return data;
  },

  getAlertas: async (cantidad = 10): Promise<AlertaDto[]> => {
    const { data } = await axiosInstance.get<AlertaDto[]>("/sistema/alertas", { params: { cantidad } });
    return data;
  },
};
