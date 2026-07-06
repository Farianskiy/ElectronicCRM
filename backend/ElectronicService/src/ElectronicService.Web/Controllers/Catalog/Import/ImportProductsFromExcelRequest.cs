using Microsoft.AspNetCore.Http;

namespace ElectronicService.Web.Controllers.Catalog.Import;

public sealed class ImportProductsFromExcelRequest
{
    public IFormFile? File { get; init; }
}