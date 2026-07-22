import { clearSession, getSession } from '../lib/auth';
import type { ProblemDetails } from './types';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5298';

export class ApiError extends Error {
  status: number;
  problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.detail ?? problem?.title ?? `İstek başarısız oldu (${status}).`);
    this.status = status;
    this.problem = problem;
  }
}

type QueryValue = string | number | boolean | null | undefined;

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const url = new URL(path, BASE_URL);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== null && value !== undefined && value !== '') {
        url.searchParams.set(key, String(value));
      }
    }
  }
  return url.toString();
}

async function request<T>(
  method: string,
  path: string,
  options?: { query?: Record<string, QueryValue>; body?: unknown },
): Promise<T> {
  const headers: Record<string, string> = {};

  if (options?.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const session = getSession();
  if (session) {
    headers.Authorization = `Bearer ${session.token}`;
  }

  const response = await fetch(buildUrl(path, options?.query), {
    method,
    headers: Object.keys(headers).length > 0 ? headers : undefined,
    body: options?.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    // 401: token süresi dolmuş veya geçersiz — oturumu düşür, kullanıcı login ekranına gitsin.
    if (response.status === 401) {
      clearSession();
    }

    let problem: ProblemDetails | null = null;
    try {
      problem = await response.json();
    } catch {
      problem = null;
    }
    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  // Bazı 201 Created yanıtları (ör. ProjectsController.Create) gövdesiz döner — .json() boş
  // gövdede "Unexpected end of JSON input" fırlatır, önce metni okuyup boşsa undefined dönüyoruz.
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const apiClient = {
  get: <T>(path: string, query?: Record<string, QueryValue>) => request<T>('GET', path, { query }),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, { body }),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, { body }),
  patch: <T>(path: string, body?: unknown) => request<T>('PATCH', path, { body }),
  delete: <T>(path: string) => request<T>('DELETE', path, {}),
};
