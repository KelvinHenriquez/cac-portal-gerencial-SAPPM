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

export interface ApiError {
  statusCode: number;
  errorMessage: string;
}
