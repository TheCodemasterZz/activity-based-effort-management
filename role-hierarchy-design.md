# Organizasyon Hiyerarşisi + Rol Bazlı Yetkilendirme (İleride Değerlendirilecek)

Sistemde şu an hiçbir rol/yetkilendirme kavramı yok (bkz. `security-testing-checklist.md` —
"Authentication/Authorization eklenmeli"). Bu doküman, kurumun gerçek organizasyon şemasına göre
tasarlanan rol modelinin taslağıdır. Şimdilik uygulanmıyor — temel/büyük bir iş olduğu için ayrı
ele alınacak.

## Organizasyon Şeması

```
Uzman/Tekniker → Takım Lideri → Müdür → Direktör → Genel Müdür
```

**Proje Yöneticisi**, bu dikey zincirin **dışında**, Uzman/Tekniker seviyesinde ama farklı bir
işlev üstlenen, yatay bir rol — kimseye rapor almıyor, kimsenin eforunu onaylamıyor, sadece
**proje** yönetiyor (oluşturma/düzenleme/kişi atama).

## İki Ayrı Boyut

1. **Hiyerarşi Seviyesi** (dikey, kimin kime rapor ettiği) — görünürlük ve onay yetkisini belirler.
2. **Proje Yöneticisi** — hiyerarşi seviyesinden bağımsız, yatay bir yetki (proje CRUD).

## Önerilen Domain Modeli

- `Employee.ManagerId` (Guid?, self-referencing FK) — kendine bağlı olduğu üst; sadece Genel
  Müdür için null.
- `Employee.HierarchyLevel` (enum): `Uzman = 1, TakimLideri = 2, Mudur = 3, Direktor = 4,
  GenelMudur = 5`.
- `Employee.IsProjectManager` (bool) — hiyerarşi seviyesinden bağımsız, proje CRUD yetkisi verir.

## Görünürlük Kuralı

Biri, org şemasında **kendi altındaki herkesi** (doğrudan + dolaylı — recursive/transitive)
görebilir. Örnek: Müdür X'in altında 2 Takım Lideri, onların altında 5 Uzman varsa, Müdür X
hepsini (7 kişi) görür. Üst seviye, alttaki her şeyi görür; yönetici sadece kendi ekibini görür.

## Onay Yetkisi

Takım Lideri ve üstü, kendi altındakilerin (recursive) eforunu onaylayabilir. Uzman/Tekniker ve
Proje Yöneticisi onay veremez.

## Açık Soru (Netleştirilmeli)

Proje Yöneticisi, sadece **kendi yönettiği projelerdeki** kişileri mi görmeli (proje bazlı
görünürlük — org şemasından bağımsız, farklı takımlardan olsa bile), yoksa sadece kendi (Uzman
seviyesindeki) sınırlı görünürlüğü mü olmalı?

**Öneri**: İlki (proje bazlı görünürlük) daha mantıklı — bir PM, projesinde çalışan herkesi ekip
yapısından bağımsız görebilmeli. Ama bu, kurumun org kültürüne bağlı bir karar.

## Not

Bu model, gerçek bir authentication (giriş/oturum) katmanının da kurulmasını gerektirir — sistemde
şu an bir giriş ekranı bile yok. Rol modeli ile authentication birlikte ele alınmalı.
