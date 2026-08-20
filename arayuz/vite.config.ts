import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import basicSsl from '@vitejs/plugin-basic-ssl'

// docs/MIMARI.md · Y-24: bu proxy yalnızca Vite geliştirme sunucusu seviyesinde çalışır, gerçek
// bir CORS izni açmaz — üretimde arayuz WebAPI'nin wwwroot'undan aynı origin'den servis edilir.
//
// basicSsl zorunlu: refresh/CSRF çerezleri (__Host-Csrf, RefreshToken) Secure + __Host- önekiyle
// kuruluyor (Y-48) — bu, tarayıcının çerezi yalnızca YANIT __KENDİSİ__ HTTPS üzerinden geldiyse
// kabul edeceği anlamına gelir. Vite dev sunucusu düz http://localhost:5173 üzerinden çalışsaydı
// backend'in HTTPS olması önemsizdi: tarayıcı çerezi zaten reddederdi (doğrulandı — proxy http
// iken __Host-Csrf hiç saklanmıyor, refresh her zaman 403 dönüyordu).
export default defineConfig({
  plugins: [react(), basicSsl()],
  server: {
    https: {},
    proxy: {
      '/api': {
        target: 'https://localhost:7189',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
