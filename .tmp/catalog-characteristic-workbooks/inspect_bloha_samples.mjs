import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const files = process.argv.slice(2);

for (const file of files) {
  const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(file));
  console.log(JSON.stringify({ file, sheets: workbook.worksheets.items.map((sheet) => ({ name: sheet.name, id: sheet.sheetId })) }));

  for (const sheet of workbook.worksheets.items) {
    const usedRange = sheet.getUsedRange(false);
    console.log(JSON.stringify({ usedRange: usedRange?.address, values: usedRange?.values, formulas: usedRange?.formulas }));
    const inspection = await workbook.inspect({
      kind: "table",
      range: `${sheet.name}!${usedRange?.address ?? "A1:AZ300"}`,
      include: "values,formulas",
      tableMaxRows: 300,
      tableMaxCols: 52,
    });
    console.log(`--- ${sheet.name} ---`);
    console.log(inspection.ndjson);
  }
}
