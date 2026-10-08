import axiosInstance from "@/services/axios";
import { AlertaDto, CorridaDto, EquipoSinClasificarDto, EstadoActualizacionDto, ParametroDto, ParametrosDto } from "@/types";

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
  /** desde (yyyy-MM-dd, solo administradores): volver a traer de SAP desde esa fecha. */
  ejecutarCorrida: async (desde?: string): Promise<CorridaDto> => {
    const { data } = await axiosInstance.post<CorridaDto>("/sistema/corridas", null, {
      timeout: 30 * 60 * 1000,
      params: desde ? { desde } : undefined,
    });
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

  getParametros: async (): Promise<ParametrosDto> => {
    const { data } = await axiosInstance.get<ParametrosDto>("/sistema/parametros");
    return data;
  },

  actualizarParametro: async (clave: string, valor: string): Promise<ParametroDto> => {
    const { data } = await axiosInstance.put<ParametroDto>(`/sistema/parametros/${encodeURIComponent(clave)}`, { valor });
    return data;
  },
};
