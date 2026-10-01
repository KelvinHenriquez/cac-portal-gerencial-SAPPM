// Tipos espejo de los DTOs del backend (PortalGerencialBackEnd.Core.Dtos).

// Sección 7: un único rol de solo consulta.
export type RolUsuario = "Gerencial";

export interface LoginRequest {
  usuario: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  usuario: string;
  rol: RolUsuario;
  passwordPorVencer: boolean;
}

export interface EstadoActualizacionDto {
  ultimaActualizacion: string | null;
  horasDesdeUltimaActualizacion: number | null;
  desactualizado: boolean;
  horasAlerta: number;
  corridaEnCurso: boolean;
  ultimoErrorFecha: string | null;
  ultimoErrorMensaje: string | null;
}

// ---- Estructura (Oracle)

export interface TipoReporteDto {
  tipoReporte: string;
  descripcion: string;
}

// ---- Disponibilidad de Maquinaria (sección 8.2)

export type ColorSemaforo = "ROJO" | "NARANJA" | "VERDE";

export interface SemanaColumnaDto {
  anioIso: number;
  semanaIso: number;
  fechaInicio: string;
  fechaFin: string;
}

export interface CeldaSemanaDto {
  anioIso: number;
  semanaIso: number;
  dispRealPct: number | null;
  dispRealMinPct: number | null;
  eqDisponibleProm: number | null;
  eqRequerido: number;
  diasCalculados: number;
  semaforo: ColorSemaforo | null;
}

export interface FilaDisponibilidadDto {
  familiaCodigo: string;
  familiaNombre: string;
  cantEquipo: number;
  eqRequerido: number;
  eqDisponible: number;
  eqFueraServicio: number;
  dispRealPct: number;
  dispPotencialPct: number;
  semaforo: ColorSemaforo;
  semanas: CeldaSemanaDto[];
}

export interface ReporteDisponibilidadDto {
  tipoReporte: string;
  descripcion: string;
  umbralRojo: number;
  umbralNaranja: number;
  fechaCorte: string | null;
  semanas: SemanaColumnaDto[];
  familias: FilaDisponibilidadDto[];
  promedio: FilaDisponibilidadDto | null;
}

export interface OrdenResumenDto {
  ordenSapId: string;
  claseOrden: string;
  estadoOrden: string;
  descripcion: string | null;
  codigoLegado: string;
  equipoDescripcion: string | null;
  fechaInicioReal: string | null;
  fechaFinReal: string | null;
}

export type EstadoEquipo = "DISPONIBLE" | "FUERA_SERVICIO" | "INACTIVO" | "SIN_SAP";

export interface EquipoDetalleDto {
  codigoLegado: string;
  descripcion: string;
  equipoSapId: string | null;
  activo: boolean;
  estado: EstadoEquipo;
  ordenes: OrdenResumenDto[];
}

export interface EquiposFamiliaDetalleDto {
  familiaCodigo: string;
  familiaNombre: string;
  fecha: string;
  requeridos: { asignacion: string; cantAsignada: number; cantRequerida: number }[];
  equipos: EquipoDetalleDto[];
}

export interface DiaDisponibilidadDto {
  fecha: string;
  cantEquipo: number;
  eqRequerido: number;
  eqFueraServicio: number;
  eqDisponible: number;
  dispRealPct: number;
  semaforo: ColorSemaforo;
  ordenes: OrdenResumenDto[];
}

export interface DetalleSemanaDto {
  tipoReporte: string;
  familiaCodigo: string;
  familiaNombre: string;
  anioIso: number;
  semanaIso: number;
  fechaInicio: string;
  fechaFin: string;
  dias: DiaDisponibilidadDto[];
}

export interface ApiError {
  statusCode: number;
  errorMessage: string;
}
