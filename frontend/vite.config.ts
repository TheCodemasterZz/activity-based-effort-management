import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    host: true,
    // Sabit port: belirtilmeseydi Vite varsayılan 5173 doluysa sessizce 5174/5175... gibi bir
    // sonraki boş porta kayıyordu, bu da her başlatmada "adres değişiyor" hissi veriyordu.
    // strictPort: true ile 5180 doluysa (ör. eski bir süreç hâlâ ayaktaysa) sessizce başka bir
    // porta kaymak yerine hata verip durur — böylece adresin değiştiğini fark etmeden kaçırmayız.
    port: 5180,
    strictPort: true,
  },
})
