import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const paths = process.argv.slice(2);
for (const path of paths) {
  const input = await FileBlob.load(path);
  const workbook = await SpreadsheetFile.importXlsx(input);
  const summary = await workbook.inspect({
    kind: "sheet,table",
    maxChars: 12000,
    tableMaxRows: 12,
    tableMaxCols: 16,
    tableMaxCellChars: 120,
  });
  process.stdout.write(`\n=== ${path} ===\n${summary.ndjson}\n`);
}
