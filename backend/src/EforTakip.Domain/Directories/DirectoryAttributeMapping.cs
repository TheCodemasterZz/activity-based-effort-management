using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class DirectoryAttributeMapping : Entity, IAggregateRoot
{
    public string AdAttributeName { get; private set; } = default!;
    public string SystemFieldName { get; private set; } = default!;
    public string FieldType { get; private set; } = default!;
    public bool IsSynced { get; private set; }
    public int SortOrder { get; private set; }

    private DirectoryAttributeMapping()
    {
        // EF Core
    }

    public static DirectoryAttributeMapping Create(
        string adAttributeName, string systemFieldName, string fieldType, bool isSynced, int sortOrder)
    {
        Validate(adAttributeName, systemFieldName, fieldType);

        return new DirectoryAttributeMapping
        {
            AdAttributeName = adAttributeName.Trim(),
            SystemFieldName = systemFieldName.Trim(),
            FieldType = fieldType.Trim(),
            IsSynced = isSynced,
            SortOrder = sortOrder
        };
    }

    public void Update(
        string adAttributeName, string systemFieldName, string fieldType, bool isSynced, int sortOrder)
    {
        Validate(adAttributeName, systemFieldName, fieldType);

        AdAttributeName = adAttributeName.Trim();
        SystemFieldName = systemFieldName.Trim();
        FieldType = fieldType.Trim();
        IsSynced = isSynced;
        SortOrder = sortOrder;
    }

    private static void Validate(string adAttributeName, string systemFieldName, string fieldType)
    {
        if (string.IsNullOrWhiteSpace(adAttributeName))
            throw new BusinessRuleValidationException("AD alan adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(systemFieldName))
            throw new BusinessRuleValidationException("Sistem alan adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(fieldType))
            throw new BusinessRuleValidationException("Alan tipi boş olamaz.");
    }
}
