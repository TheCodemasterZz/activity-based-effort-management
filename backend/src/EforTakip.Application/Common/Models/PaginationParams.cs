namespace EforTakip.Application.Common.Models;

public abstract class PaginationParams
{
    // 100'dü — Work Log/Plan Work sayfaları tek bir "sayfasız" büyük istekle (pageSize: 1000)
    // tüm dönemi çekip kendi tabloları içinde gösteriyor; bir ayın 25 kişilik tam verisi bu
    // sınırı kolayca aşabiliyordu ve sonuç sessizce (hatasız) kırpılıyordu. 5000'e çıkarıldı.
    private const int MaxPageSize = 5000;
    private int _pageSize = 20;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string? SortBy { get; set; }

    public bool Descending { get; set; }
}
