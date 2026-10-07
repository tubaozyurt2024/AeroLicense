import { AxiosError, AxiosHeaders, type AxiosResponse } from 'axios';
import { describe, expect, it, vi } from 'vitest';
import { ApiError, applyFieldErrors, toApiError, userMessage } from './problem';

const axiosError = (status: number, data: unknown) => {
  const response = { status, data, headers: {}, config: { headers: new AxiosHeaders() }, statusText: '' } as AxiosResponse;
  return new AxiosError('fail', 'ERR', undefined, undefined, response);
};

describe('ProblemDetails ayrıştırma', () => {
  it('alan adlarını camelCase yapar ve correlationId\'yi alır', () => {
    const e = toApiError(axiosError(400, { title: 'Doğrulama hatası', errors: { ApplicantNationalId: ['Geçersiz'] }, correlationId: 'abc' }));
    expect(e.fieldErrors).toEqual({ applicantNationalId: ['Geçersiz'] });
    expect(e.correlationId).toBe('abc');
  });

  it('5xx\'te teknik detay değil genel mesaj gösterilir', () => {
    const message = userMessage(axiosError(500, { title: 'x', detail: 'NullReferenceException at ...' }));
    expect(message).not.toContain('NullReference');
  });

  it('yanıtsız hata (ağ) ayrı mesaj verir', () => {
    expect(userMessage(new AxiosError('Network Error'))).toMatch(/Sunucuya ulaşılamadı/);
  });

  it('forma eşlenemeyen alan hatasında false döner (çağıran genel mesaj gösterir)', () => {
    const setError = vi.fn();
    const error = new ApiError(400, 't', undefined, { reason: ['a'], unknown: ['b'] }, undefined);
    expect(applyFieldErrors(error, setError, ['reason'])).toBe(false);
    expect(setError).toHaveBeenCalledWith('reason', { type: 'server', message: 'a' });
  });
});
