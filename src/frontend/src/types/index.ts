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
  modo: "LOCAL" | "PRODUCTION";
  jobHabilitado: boolean;
  proximaCorrida: string | null;
}

export type EstadoCorrida = "OK" | "ERROR" | "EN_CURSO";

export interface PasoCorridaDto {
  fuente: string;
  estado: EstadoCorrida;
  inicio: string;
  fin: string | null;
  registrosProcesados: number | null;
  fechaCorteDesde: string | null;
  errorMensaje: string | null;
}

export interface CorridaDto {
  corridaId: string;
  origen: string | null;
  inicio: string;
  fin: string | null;
  estado: EstadoCorrida;
  pasos: PasoCorridaDto[];
}

export interface EquipoSinClasificarDto {
  equipoSapId: string;
  codigoLegado: string | null;
  descripcion: string | null;
  puestoTrabajo: string | null;
  motivo: "SIN_CODIGO_LEGADO" | "CODIGO_NO_EXISTE_EN_ORACLE";
  cantOrdenes: number;
  detectadoEn: string;
}

export interface AlertaDto {
  tipo: string;
  fecha: string;
  destinatarios: string | null;
  enviada: boolean;
  error: string | null;
}

// ---- Estructura (Oracle Forms)

export interface ReporteResumenDto {
  reporteId: number;
  descripcion: string;
}

export interface PeriodoDto {
  periodoId: number;
  descripcion: string;
  reportes: ReporteResumenDto[];
}

export type MomentoSemana = "PASADA" | "ACTUAL" | "FUTURA";

// ---- Disponibilidad de Equipos

export type ColorSemaforo = "ROJO" | "NARANJA" | "VERDE";

export interface SemanaColumnaDto {
  noSemana: number;
  fechaInicio: string;
  fechaFin: string;
  momento: MomentoSemana;
}

export interface CeldaSemanaDto {
  noSemana: number;
  dispRealPct: number | null;
  dispRealMinPct: number | null;
  potencialPct: number | null;
  diasCalculados: number;
  semaforo: ColorSemaforo | null;
}

export interface FilaDisponibilidadDto {
  asignacionId: number;
  asignacion: string;
  negrita: boolean;
  grupo: boolean;
  esMiembro: boolean;
  enGrupo: string | null;
  cantEquipo: number;
  eqRequerido: number;
  eqDisponible: number;
  dispRealPct: number;
  potencialPct: number;
  dispCompPct: number;
  semaforo: ColorSemaforo;
  semanas: CeldaSemanaDto[];
}

export interface ReporteDisponibilidadDto {
  reporteId: number;
  descripcion: string;
  umbralRojo: number;
  umbralNaranja: number;
  baseEquipos: boolean;
  correctivasSegunAviso: boolean;
  clasesOrden: string;
  fechaCorte: string | null;
  semanas: SemanaColumnaDto[];
  todas: FilaDisponibilidadDto | null;
  filas: FilaDisponibilidadDto[];
}

export interface DiaDetalleDto {
  fecha: string | null;
  dias: number;
  dispRealPct: number;
  dispCompPct: number;
  hrsDia: number;
  hrsOtPlanificada: number;
  hrsOtPreventivo: number;
  hrsOtCorrectiva: number;
  hrsRealEfec: number;
  hrsDiaDispComp: number;
  semaforo: ColorSemaforo;
}

export interface OrdenDetalleDto {
  ordenSapId: string;
  claseOrden: string;
  tipo: "PLANIFICADA" | "PREVENTIVO" | "CORRECTIVA";
  ficha: string;
  equipoDescripcion: string | null;
  desde: string;
  hasta: string | null;
  estadoActual: string;
  observacion: string | null;
}

export interface DetalleSemanaDto {
  reporteId: number;
  asignacionId: number;
  asignacion: string;
  noSemana: number;
  fechaInicio: string;
  fechaFin: string;
  umbralRojo: number;
  umbralNaranja: number;
  dias: DiaDetalleDto[];
  resumen: DiaDetalleDto | null;
  ordenes: OrdenDetalleDto[];
}

export interface HoraDisponibilidadDto {
  hora: number;
  cantEquipo: number;
  eqRequerido: number;
  eqDisponible: number;
  dispPct: number;
  potencialPct: number;
}

export type EstadoEquipo = "DISPONIBLE" | "FUERA_SERVICIO" | "INACTIVO" | "SIN_SAP";

export interface EquipoDetalleDto {
  ficha: string;
  equipoCodigo: string | null;
  descripcion: string;
  equipoSapId: string | null;
  estado: EstadoEquipo;
  ordenes: OrdenDetalleDto[];
}

export interface EquiposAsignacionDto {
  asignacion: string;
  fecha: string;
  cantRequerida: number;
  equipos: EquipoDetalleDto[];
}

// ---- Avance de Órdenes de Trabajo

export type MedidaAvance = "ORDENES" | "ACTIVIDADES";
export type RefPlan = "EJECUTADO" | "PROGRAMADO" | "NO_PROGRAMADO";

export interface SemanaAvanceDto {
  noSemana: number;
  fechaInicio: string;
  fechaFin: string;
  momento: MomentoSemana;
}

export interface CeldaAvanceDto {
  noSemana: number;
  enPlan: boolean;
  progPct: number | null;
  realPct: number | null;
  progAcumPct: number | null;
  realAcumPct: number | null;
}

export interface FilaAvanceDto {
  familiaCodigo: string;
  familiaNombre: string;
  cantEquipo: number;
  fechaEstInicio: string;
  fechaEstFin: string;
  tProg: number;
  progCierre: number;
  totalEjec: number;
  progAct: number;
  pendAnt: number;
  tPendR: number;
  ordenesTotal: number;
  ordenesCerradas: number;
  actTotal: number;
  actTerminadas: number;
  semanas: CeldaAvanceDto[];
}

export interface ReporteAvanceDto {
  reporteId: number;
  nombre: string;
  periodoId: number;
  periodoDescripcion: string;
  fechaInicio: string;
  fechaFin: string;
  fechaCorte: string | null;
  medida: MedidaAvance;
  refPlan: RefPlan;
  basePlan: "ACTIVIDADES" | "DIAS";
  semanas: SemanaAvanceDto[];
  familias: FilaAvanceDto[];
}

export interface ActividadDto {
  ordenSapId: string;
  operacion: string;
  ficha: string;
  texto: string | null;
  sistema: string | null;
  subsistema: string | null;
  componente: string | null;
  programada: boolean;
  fechaTerminada: string | null;
  dias: number;
}

export interface DetalleSemanaAvanceDto {
  familiaCodigo: string;
  familiaNombre: string;
  noSemana: number;
  fechaInicio: string;
  fechaFin: string;
  programado: number;
  realEjProg: number;
  realEjNoProg: number;
  realEjecutado: number;
  realOrdenes: number;
  dias: { fecha: string; actTerminadas: number; ordenesCerradas: number }[];
  actividades: ActividadDto[];
  ordenesCerradas: { ordenSapId: string; ficha: string; descripcion: string | null; fechaCierre: string }[];
}

export interface AnalisisFilaDto {
  nombre: string;
  actTotal: number;
  actTerminadas: number;
  diasActProg: number;
  diasActTerm: number;
  avancePct: number;
}

export interface AnalisisEquipoDto extends AnalisisFilaDto {
  ficha: string;
  ordenSapId: string;
  estado: string;
  fechaRealInicio: string | null;
  fechaRealFin: string | null;
}

export interface AnalisisAvanceDto {
  familiaCodigo: string;
  familiaNombre: string;
  total: AnalisisFilaDto;
  equipos: AnalisisEquipoDto[];
  sistemas: AnalisisFilaDto[];
}

export interface ParametroDto {
  clave: string;
  seccion: string;
  nombre: string;
  descripcion: string | null;
  valor: string;
  opciones: string[] | null;
  actualizadoEn: string;
  actualizadoPor: string | null;
}

export interface ParametrosDto {
  puedeEditar: boolean;
  parametros: ParametroDto[];
}

export interface ApiError {
  statusCode: number;
  errorMessage: string;
}
