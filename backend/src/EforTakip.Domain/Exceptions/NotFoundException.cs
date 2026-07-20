namespace EforTakip.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"'{entityName}' ({key}) bulunamadı.")
    {
    }
}
