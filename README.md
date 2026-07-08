# Efor Takip Yazılımı

Aktivite bazlı efor takibi, planlaması ve yönetimi için kurumsal ölçekli yazılım.

## Teknoloji Yığını

- **Backend**: ASP.NET Core (.NET 8) Web API — `backend/`
- **Frontend**: React + TypeScript (Vite) — `frontend/`
- **Veritabanı**: PostgreSQL
- **Dağıtım**: On-premise

## Proje Yapısı

```
efor_takip_yazilimi/
├── backend/          # ASP.NET Core Web API çözümü
│   ├── EforTakip.sln
│   └── src/
│       └── EforTakip.Api/
└── frontend/         # React + TypeScript (Vite) uygulaması
```

## Geliştirme

### Backend

```
cd backend
dotnet run --project src/EforTakip.Api
```

### Frontend

```
cd frontend
npm install
npm run dev
```
