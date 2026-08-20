import { isAxiosError } from 'axios'
import type { ProblemDetailsResponse } from './types'

// WebAPI/Extensions/ResultExtensions.cs · başarısız IResult/IDataResult<T> her zaman
// ProblemDetails {status, title} olarak döner (Y-25/Y-28: hata yönetimi controller'larda değil).
export function extractErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError<ProblemDetailsResponse>(error) && error.response?.data?.title) {
    return error.response.data.title
  }
  return fallback
}
