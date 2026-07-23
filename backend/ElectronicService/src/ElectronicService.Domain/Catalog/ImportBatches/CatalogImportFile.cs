using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.ImportBatches;

public sealed class CatalogImportFile
{
    /*
     * Закрытое persistence-свойство.
     *
     * EF Core использует его для записи
     * и восстановления bytea из PostgreSQL.
     *
     * Внешний код доступа к byte[] не имеет.
     */
    private byte[] ContentBytes
    {
        get;
        set;
    } = [];

    private CatalogImportFile(
        Guid batchId,
        byte[] content)
    {
        BatchId = batchId;

        /*
         * Создаём независимую копию массива.
         * Изменение исходного byte[] после Create
         * не изменит содержимое объекта.
         */
        ContentBytes = content.ToArray();
    }

    /*
     * Конструктор для EF Core.
     */
    private CatalogImportFile()
    {
    }

    /*
     * Одновременно:
     *
     * 1. Primary Key catalog_import_files;
     * 2. Foreign Key на catalog_import_batches.
     */
    public Guid BatchId
    {
        get;
        private set;
    }

    /*
     * Наружу не отдаётся изменяемый byte[].
     */
    public ReadOnlyMemory<byte> Content =>
        ContentBytes;

    internal static Result<
        CatalogImportFile,
        DomainError> Create(
            Guid batchId,
            byte[] content)
    {
        if (batchId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(
                nameof(batchId));
        }

        ArgumentNullException.ThrowIfNull(content);

        if (content.Length == 0)
        {
            return CatalogImportErrors.FileIsEmpty();
        }

        return new CatalogImportFile(
            batchId,
            content);
    }
}