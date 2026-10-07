import { isAxiosError } from 'axios';
import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';
import { t } from '@/i18n';

/**
 * Backend'in RFC 9457 ProblemDetails yanıtının uygulama içi karşılığı. Bileşenler Axios'u tanımaz,
 * sadece bu tipi görür: HTTP istemcisi değişse bile hata gösterimi değişmez.
 */
export class ApiError extends Error {
  constructor(
    readonly status: number, // 0 = ağ hatası / zaman aşımı (yanıt yok)
    readonly title: string | undefined,
    readonly detail: string | undefined,
    readonly fieldErrors: Readonly<Record<string, string[]>>,
    readonly correlationId: string | undefined,
    readonly headers: Readonly<Record<string, string>> = {},
  ) {
    super(detail ?? title ?? `HTTP ${status}`);
    this.name = 'ApiError';
  }

  get isValidation() {
    return this.status === 400 && Object.keys(this.fieldErrors).length > 0;
  }
}

type ProblemBody = { title?: string; detail?: string; errors?: Record<string, string[]>; correlationId?: string };

// Backend alan adlarını C# özelliği olarak (PascalCase) döner: "ApplicantNationalId" → "applicantNationalId".
const toCamel = (key: string) => (key ? key[0]!.toLowerCase() + key.slice(1) : key);

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  if (isAxiosError(error)) {
    const response = error.response;
    if (!response) return new ApiError(0, undefined, undefined, {}, undefined);

    const body = (typeof response.data === 'object' && response.data !== null ? response.data : {}) as ProblemBody;
    const fieldErrors = Object.fromEntries(Object.entries(body.errors ?? {}).map(([k, v]) => [toCamel(k), v]));
    const correlationId = body.correlationId ?? (response.headers['x-correlation-id'] as string | undefined);
    return new ApiError(response.status, body.title, body.detail, fieldErrors, correlationId);
  }
  return new ApiError(-1, undefined, error instanceof Error ? error.message : undefined, {}, undefined);
}

/**
 * Kullanıcıya gösterilecek mesaj. 4xx'te backend'in (Türkçe, kullanıcıya yönelik) açıklaması kullanılır;
 * 5xx ve ağ hatalarında teknik detay GÖSTERİLMEZ, sadece genel mesaj + destek kodu.
 */
export function userMessage(error: unknown): string {
  const e = toApiError(error);
  if (e.status === 0) return t.errors.network;
  if (e.status === 429) return t.errors.tooManyRequests;
  if (e.status >= 500 || e.status < 0) return t.errors.server;
  return e.detail ?? e.title ?? t.errors.generic;
}

/**
 * 400 ValidationProblemDetails'teki alan hatalarını ilgili form alanlarının altına yazar.
 * Formda karşılığı olmayan hata varsa false döner; çağıran genel hata mesajı gösterir.
 */
export function applyFieldErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: readonly Path<T>[],
): boolean {
  const e = toApiError(error);
  if (!e.isValidation) return false;

  let unmapped = false;
  for (const [key, messages] of Object.entries(e.fieldErrors)) {
    const field = fields.find((f) => f === key);
    if (field && messages[0]) setError(field, { type: 'server', message: messages[0] });
    else unmapped = true;
  }
  return !unmapped;
}
