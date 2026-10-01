import axiosInstance from "@/services/axios";
import { LoginRequest, LoginResponse } from "@/types";

export const authService = {
  login: async (request: LoginRequest): Promise<LoginResponse> => {
    const { data } = await axiosInstance.post<LoginResponse>("/auth/login", request);
    return data;
  },
};
