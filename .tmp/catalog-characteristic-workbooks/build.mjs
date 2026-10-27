import fs from "node:fs/promises";
import path from "node:path";
import { FileBlob, SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "F:/Projects/ElectronicCRM/outputs/real-catalog-learning-test";
const previewDir = "F:/Projects/ElectronicCRM/.tmp/catalog-characteristic-workbooks/previews";

if (process.argv.includes("--inspect-source")) {
  const input = await FileBlob.load(
    "F:/Projects/ElectronicCRM/outputs/real-catalog-learning-test/05_Смешанный_файл_без_типа_и_характеристик.xlsx",
  );
  const sourceWorkbook = await SpreadsheetFile.importXlsx(input);
  const sourceInspection = await sourceWorkbook.inspect({
    kind: "table",
    range: "A1:H15",
    include: "values,formulas",
    tableMaxRows: 15,
    tableMaxCols: 8,
  });
  console.log(sourceInspection.ndjson);
  process.exit(0);
}

const files = [
  {
    name: "06_Новые_номиналы_известных_серий.xlsx",
    title: "Новые номиналы известных серий",
    purpose: "Проверка семейного переноса стабильных признаков без копирования номиналов конкретной модели.",
    rows: [
      ["Авт. выкл. AV 10 1P 20A 10кА C", "TEST-AV10-C20", "EKF", 1250, 8],
      ["Авт. выкл. AV 10 3P 32A 10кА D", "TEST-AV10-D32", "EKF", 2790, 5],
      ["Автомат защиты двигателя ПРК32 25 Ir=20-25A", "TEST-PRK32-25", "IEK", 4150, 3],
      ["Автомат защиты двигателя ПРК32 32 Ir=25-32A", "TEST-PRK32-32", "IEK", 4680, 2],
      ["Дифференциальный автомат АВДТ32 1P+N C20 30мА", "TEST-AVDT32-C20", "IEK", 3520, 4],
      ["Дифференциальный автомат АВДТ32 1P+N C32 30мА", "TEST-AVDT32-C32", "IEK", 3780, 4],
      ["УЗО ВД1-63 2P 32A 30мА", "TEST-VD1-63-32", "IEK", 2860, 6],
      ["УЗО ВД1-63 4P 63A 100мА", "TEST-VD1-63-63", "IEK", 5190, 2],
    ],
  },
  {
    name: "07_Похожие_слова_другие_серии.xlsx",
    title: "Похожие слова, но другие серии",
    purpose: "Негативная проверка: общие слова и номиналы не должны считаться доказательством одного семейства.",
    rows: [
      ["Автоматический выключатель XZ 20 1P 20A 10кА C", "TEST-XZ20-C20", "EKF", 1190, 7],
      ["Автоматический выключатель QF 32 3P 32A 10кА D", "TEST-QF32-D32", "EKF", 2650, 3],
      ["Автомат защиты двигателя MTR40 20-25A", "TEST-MTR40-25", "IEK", 4090, 3],
      ["Автомат защиты двигателя PKZ50 25-32A", "TEST-PKZ50-32", "IEK", 4520, 2],
      ["Дифференциальный автомат DX40 1P+N C20 30мА", "TEST-DX40-C20", "IEK", 3410, 4],
      ["Дифференциальный автомат RCBO50 1P+N C32 30мА", "TEST-RCBO50-C32", "IEK", 3690, 4],
      ["УЗО RCD40 2P 32A 30мА", "TEST-RCD40-32", "IEK", 2790, 5],
      ["УЗО RCCB80 4P 63A 100мА", "TEST-RCCB80-63", "IEK", 5070, 2],
    ],
  },
];

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

for (const definition of files) {
  const workbook = Workbook.create();
  const sheet = workbook.worksheets.add("Импорт");
  sheet.showGridLines = false;
  sheet.freezePanes.freezeRows(1);

  sheet.getRange("A1:E1").values = [[
    "НАИМЕНОВАНИЕ",
    "АРТИКУЛ",
    "ПРОИЗВОДИТЕЛЬ",
    "ЦЕНА",
    "ОСТАТОК",
  ]];
  sheet.getRange(`A2:E${1 + definition.rows.length}`).values = definition.rows;

  sheet.getRange("A1:E1").format = {
    fill: "#0F766E",
    font: { name: "Arial", size: 10, bold: true, color: "#FFFFFF" },
    horizontalAlignment: "center",
    verticalAlignment: "center",
    borders: { preset: "outside", style: "thin", color: "#0F766E" },
  };
  sheet.getRange(`A2:E${1 + definition.rows.length}`).format = {
    font: { name: "Arial", size: 10, color: "#1E293B" },
    verticalAlignment: "center",
    borders: {
      insideHorizontal: { style: "thin", color: "#E2E8F0" },
      bottom: { style: "thin", color: "#CBD5E1" },
    },
  };
  sheet.getRange(`D2:D${1 + definition.rows.length}`).format.numberFormat = "#,##0.00";
  sheet.getRange(`E2:E${1 + definition.rows.length}`).format.numberFormat = "#,##0";
  sheet.getRange("A:A").format.columnWidth = 58;
  sheet.getRange("B:B").format.columnWidth = 24;
  sheet.getRange("C:C").format.columnWidth = 18;
  sheet.getRange("D:E").format.columnWidth = 14;
  sheet.getRange("1:1").format.rowHeight = 24;

  workbook.recalculate();

  const inspection = await workbook.inspect({
    kind: "table",
    sheetId: sheet.sheetId,
    range: `A1:E${1 + definition.rows.length}`,
    include: "values,formulas",
    tableMaxRows: 20,
    tableMaxCols: 6,
  });
  console.log(`${definition.name}\n${inspection.ndjson}`);

  const errors = await workbook.inspect({
    kind: "match",
    searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!",
    options: { useRegex: true, maxResults: 100 },
    summary: `formula errors in ${definition.name}`,
  });
  console.log(errors.ndjson);

  const preview = await workbook.render({
    sheetName: "Импорт",
    range: `A1:E${1 + definition.rows.length}`,
    scale: 1.5,
    format: "png",
  });
  await fs.writeFile(
    path.join(previewDir, definition.name.replace(/\.xlsx$/i, ".png")),
    new Uint8Array(await preview.arrayBuffer()),
  );

  const output = await SpreadsheetFile.exportXlsx(workbook);
  await output.save(path.join(outputDir, definition.name));
}
