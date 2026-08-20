import { apiClient } from './client'

// A-36: korumalı dosyalar (rapor çıktıları) Authorization header taşıyamayan <a href> yerine
// axios blob + geçici nesne URL'i ile indirilir.
export async function downloadBlob(url: string, fallbackFileName: string): Promise<void> {
  const response = await apiClient.get<Blob>(url, { responseType: 'blob' })

  const contentDisposition = response.headers['content-disposition'] as string | undefined
  const fileName = extractFileName(contentDisposition) ?? fallbackFileName

  const objectUrl = URL.createObjectURL(response.data)
  const link = document.createElement('a')
  link.href = objectUrl
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(objectUrl)
}

function extractFileName(contentDisposition: string | undefined): string | null {
  if (!contentDisposition) {
    return null
  }

  const match = /filename="?([^";]+)"?/i.exec(contentDisposition)
  return match ? match[1] : null
}
