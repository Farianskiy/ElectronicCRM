import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "F:/Projects/ElectronicCRM/outputs/real-breaker-learning-test-20261027";
const previewDir = "F:/Projects/ElectronicCRM/.tmp/real-breaker-workbooks-20261027/previews";
const headers = ["Наименование", "Артикул", "Производитель", "Цена", "Остаток"];

const files = [
  {
    name: "08_Модульные_автоматы_реальные_серии.xlsx",
    rows: [
      ["Выключатель автоматический ВА47-29 1P 6А 4,5кА характеристика C IEK", "MVA20-1-006-C", "IEK", 390, 18],
      ["Выключатель автоматический ВА47-29 1P 10А 4,5кА характеристика C IEK", "MVA20-1-010-C", "IEK", 405, 22],
      ["Выключатель автоматический ВА47-29 1P 16А 4,5кА характеристика C IEK", "MVA20-1-016-C", "IEK", 420, 30],
      ["Выключатель автоматический ВА47-29 1P 25А 4,5кА характеристика C IEK", "MVA20-1-025-C", "IEK", 435, 14],
      ["Выключатель автоматический ВА47-29 2P 16А 4,5кА характеристика C IEK", "MVA20-2-016-C", "IEK", 805, 9],
      ["Выключатель автоматический ВА47-29 3P 25А 4,5кА характеристика D IEK", "MVA20-3-025-D", "IEK", 1260, 6],
      ["Автоматический выключатель ВА 47-63 1P 10А C 4,5кА EKF PROxima", "mcb4763-1-10C-pro", "EKF", 275, 25],
      ["Автоматический выключатель ВА 47-63 1P 16А C 4,5кА EKF PROxima", "mcb4763-1-16C-pro", "EKF", 280, 27],
      ["Автоматический выключатель ВА 47-63 1P 25А C 4,5кА EKF PROxima", "mcb4763-1-25C-pro", "EKF", 295, 16],
      ["Автоматический выключатель ВА 47-63 2P 32А C 4,5кА EKF PROxima", "mcb4763-2-32C-pro", "EKF", 610, 11],
      ["Автоматический выключатель ВА 47-63 3P 40А D 4,5кА EKF PROxima", "mcb4763-3-40D-pro", "EKF", 990, 7],
      ["Автоматический выключатель AV-10 1P 13А C 10кА EKF AVERES", "av10-1-13C", "EKF", 920, 8],
      ["Автоматический выключатель NB1-63 1P 6А C 6кА CHINT", "NB1-63-1P-C6", "CHINT", 510, 20],
      ["Автоматический выключатель NB1-63 1P 10А C 6кА CHINT", "NB1-63-1P-C10", "CHINT", 520, 24],
      ["Автоматический выключатель NB1-63 1P 16А C 6кА CHINT", "NB1-63-1P-C16", "CHINT", 530, 28],
      ["Автоматический выключатель NB1-63 2P 25А C 6кА CHINT", "NB1-63-2P-C25", "CHINT", 1040, 13],
      ["Автоматический выключатель NB1-63 3P 32А D 6кА CHINT", "NB1-63-3P-D32", "CHINT", 1640, 6],
      ["Автоматический выключатель NB1-63 4P 40А C 6кА CHINT", "NB1-63-4P-C40", "CHINT", 2180, 4],
    ],
  },
  {
    name: "09_Дифференциальные_автоматы_реальные_серии.xlsx",
    rows: [
      ["Автоматический выключатель дифференциального тока АВДТ32 1P+N 10А C 30мА тип AC IEK", "MAD22-2-010-C-30", "IEK", 2450, 9],
      ["Автоматический выключатель дифференциального тока АВДТ32 1P+N 16А C 30мА тип AC IEK", "MAD22-2-016-C-30", "IEK", 2490, 12],
      ["Автоматический выключатель дифференциального тока АВДТ32 1P+N 25А C 30мА тип AC IEK", "MAD22-2-025-C-30", "IEK", 2520, 8],
      ["Автоматический выключатель дифференциального тока АВДТ32 1P+N 32А C 30мА тип AC IEK", "MAD22-2-032-C-30", "IEK", 2580, 7],
      ["Дифференциальный автомат АД12 2P 16А C 30мА тип AC IEK", "MAD10-2-016-C-30", "IEK", 2210, 6],
      ["Дифференциальный автомат АД12 2P 25А C 30мА тип AC IEK", "MAD10-2-025-C-30", "IEK", 2260, 5],
      ["Автоматический выключатель дифференциального тока АВДТ-63М 1P+N 16А C 30мА EKF PROxima", "da63m-16-30-pro", "EKF", 2380, 10],
      ["Автоматический выключатель дифференциального тока АВДТ-63М 1P+N 25А C 30мА EKF PROxima", "da63m-25-30-pro", "EKF", 2420, 8],
      ["Автоматический выключатель дифференциального тока АВДТ-63М 1P+N 32А C 30мА EKF PROxima", "da63m-32-30-pro", "EKF", 2470, 7],
      ["Автоматический выключатель дифференциального тока АВДТ-63N 3P+N 25А C 30мА EKF PROxima", "da63n-4-25-30-pro", "EKF", 5180, 3],
      ["Дифференциальный автомат NXBLE-63 1P+N 10А C 30мА 6кА CHINT", "NXBLE-63-1PN-C10-30", "CHINT", 2690, 9],
      ["Дифференциальный автомат NXBLE-63 1P+N 16А C 30мА 6кА CHINT", "NXBLE-63-1PN-C16-30", "CHINT", 2720, 11],
      ["Дифференциальный автомат NXBLE-63 1P+N 25А C 30мА 6кА CHINT", "NXBLE-63-1PN-C25-30", "CHINT", 2780, 8],
      ["Дифференциальный автомат NXBLE-63 1P+N 32А C 30мА 6кА CHINT", "NXBLE-63-1PN-C32-30", "CHINT", 2820, 7],
      ["Дифференциальный автомат NXBLE-63 3P+N 40А C 30мА 6кА CHINT", "NXBLE-63-3PN-C40-30", "CHINT", 5950, 3],
    ],
  },
  {
    name: "10_УЗО_реальные_серии.xlsx",
    rows: [
      ["Выключатель дифференциального тока ВД1-63 2P 16А 30мА тип AC IEK", "MDV10-2-016-030", "IEK", 1630, 10],
      ["Выключатель дифференциального тока ВД1-63 2P 25А 30мА тип AC IEK", "MDV10-2-025-030", "IEK", 1660, 14],
      ["Выключатель дифференциального тока ВД1-63 2P 40А 30мА тип AC IEK", "MDV10-2-040-030", "IEK", 1690, 12],
      ["Выключатель дифференциального тока ВД1-63 2P 63А 30мА тип AC IEK", "MDV10-2-063-030", "IEK", 1750, 8],
      ["Выключатель дифференциального тока ВД1-63 4P 25А 30мА тип AC IEK", "MDV10-4-025-030", "IEK", 2860, 6],
      ["Выключатель дифференциального тока ВД1-63 4P 40А 30мА тип AC IEK", "MDV10-4-040-030", "IEK", 2940, 5],
      ["УЗО ВД-100 2P 25А 30мА тип AC электромеханическое EKF PROxima", "rccb-2-25-30-pro", "EKF", 1540, 13],
      ["УЗО ВД-100 2P 40А 30мА тип AC электромеханическое EKF PROxima", "rccb-2-40-30-pro", "EKF", 1590, 11],
      ["УЗО ВД-100 2P 63А 100мА тип AC электромеханическое EKF PROxima", "rccb-2-63-100-pro", "EKF", 1650, 7],
      ["УЗО ВД-100 4P 40А 30мА тип AC электромеханическое EKF PROxima", "rccb-4-40-30-pro", "EKF", 2780, 5],
      ["Выключатель дифференциального тока NL1-63 2P 25А 30мА тип AC CHINT", "NL1-63-2P-25-30", "CHINT", 1830, 9],
      ["Выключатель дифференциального тока NL1-63 2P 40А 30мА тип AC CHINT", "NL1-63-2P-40-30", "CHINT", 1870, 10],
      ["Выключатель дифференциального тока NL1-63 2P 63А 30мА тип AC CHINT", "NL1-63-2P-63-30", "CHINT", 1930, 7],
      ["Выключатель дифференциального тока NL1-63 4P 40А 30мА тип AC CHINT", "NL1-63-4P-40-30", "CHINT", 3260, 4],
      ["Выключатель дифференциального тока NL1-63 4P 63А 100мА тип AC CHINT", "NL1-63-4P-63-100", "CHINT", 3380, 3],
    ],
  },
  {
    name: "11_Мотор_автоматы_реальные_серии.xlsx",
    rows: [
      ["Автомат защиты двигателя ПРК32 1,6-2,5А IEK", "MPR10-1.6-2.5", "IEK", 3750, 7],
      ["Автомат защиты двигателя ПРК32 2,5-4А IEK", "MPR10-2.5-4", "IEK", 3780, 8],
      ["Автомат защиты двигателя ПРК32 4-6,3А IEK", "MPR10-4-6.3", "IEK", 3810, 9],
      ["Автомат защиты двигателя ПРК32 6-10А IEK", "MPR10-6-10", "IEK", 3860, 10],
      ["Автомат защиты двигателя ПРК32 9-14А IEK", "MPR10-9-14", "IEK", 3920, 6],
      ["Автомат защиты двигателя ПРК32 13-18А IEK", "MPR10-13-18", "IEK", 3980, 5],
      ["Автомат защиты двигателя ПРК32 20-25А IEK", "MPR10-20-25", "IEK", 4120, 4],
      ["Автомат защиты двигателя NS2-25 1,6-2,5А CHINT", "NS2-25-1.6-2.5", "CHINT", 3490, 7],
      ["Автомат защиты двигателя NS2-25 2,5-4А CHINT", "NS2-25-2.5-4", "CHINT", 3520, 8],
      ["Автомат защиты двигателя NS2-25 4-6,3А CHINT", "NS2-25-4-6.3", "CHINT", 3560, 8],
      ["Автомат защиты двигателя NS2-25 6-10А CHINT", "NS2-25-6-10", "CHINT", 3610, 9],
      ["Автомат защиты двигателя NS2-25 9-14А CHINT", "NS2-25-9-14", "CHINT", 3680, 6],
      ["Автомат защиты двигателя TeSys GV2ME06 1-1,6А Schneider Electric", "GV2ME06", "Schneider Electric", 6420, 5],
      ["Автомат защиты двигателя TeSys GV2ME07 1,6-2,5А Schneider Electric", "GV2ME07", "Schneider Electric", 6480, 5],
      ["Автомат защиты двигателя TeSys GV2ME08 2,5-4А Schneider Electric", "GV2ME08", "Schneider Electric", 6540, 6],
      ["Автомат защиты двигателя TeSys GV2ME10 4-6,3А Schneider Electric", "GV2ME10", "Schneider Electric", 6620, 5],
      ["Автомат защиты двигателя TeSys GV2ME14 6-10А Schneider Electric", "GV2ME14", "Schneider Electric", 6740, 4],
      ["Автомат защиты двигателя TeSys GV2ME16 9-14А Schneider Electric", "GV2ME16", "Schneider Electric", 6880, 3],
    ],
  },
  {
    name: "12_Корпусные_автоматы_реальные_серии.xlsx",
    rows: [
      ["Автоматический выключатель в литом корпусе ВА88-32 3P 16А IEK", "SVA10-3-0016", "IEK", 4850, 6],
      ["Автоматический выключатель в литом корпусе ВА88-32 3P 25А IEK", "SVA10-3-0025", "IEK", 4920, 7],
      ["Автоматический выключатель в литом корпусе ВА88-32 3P 40А IEK", "SVA10-3-0040", "IEK", 5050, 6],
      ["Автоматический выключатель в литом корпусе ВА88-32 3P 63А IEK", "SVA10-3-0063", "IEK", 5190, 5],
      ["Автоматический выключатель в литом корпусе ВА88-32 3P 100А IEK", "SVA10-3-0100", "IEK", 5480, 4],
      ["Автоматический выключатель в литом корпусе NM1-125S 3P 25А CHINT", "NM1-125S-3P-25", "CHINT", 5210, 6],
      ["Автоматический выключатель в литом корпусе NM1-125S 3P 40А CHINT", "NM1-125S-3P-40", "CHINT", 5340, 6],
      ["Автоматический выключатель в литом корпусе NM1-125S 3P 63А CHINT", "NM1-125S-3P-63", "CHINT", 5480, 5],
      ["Автоматический выключатель в литом корпусе NM1-125S 3P 100А CHINT", "NM1-125S-3P-100", "CHINT", 5760, 4],
      ["Автоматический выключатель в литом корпусе NM1-250S 3P 160А CHINT", "NM1-250S-3P-160", "CHINT", 9280, 3],
      ["Автоматический выключатель EasyPact CVS100B 3P 25А Schneider Electric", "LV510301", "Schneider Electric", 12400, 4],
      ["Автоматический выключатель EasyPact CVS100B 3P 40А Schneider Electric", "LV510303", "Schneider Electric", 12800, 4],
      ["Автоматический выключатель EasyPact CVS100B 3P 63А Schneider Electric", "LV510305", "Schneider Electric", 13200, 3],
      ["Автоматический выключатель EasyPact CVS100B 3P 100А Schneider Electric", "LV510307", "Schneider Electric", 13900, 3],
      ["Автоматический выключатель EasyPact CVS160B 3P 160А Schneider Electric", "LV516303", "Schneider Electric", 18600, 2],
    ],
  },
  {
    name: "13_Смешанная_проверка_реальных_серий.xlsx",
    rows: [
      ["Выключатель автоматический ВА47-29 1P 13А 4,5кА характеристика B IEK", "MVA20-1-013-B", "IEK", 425, 12],
      ["Автоматический выключатель AV-10 3P 50А C 10кА EKF AVERES", "av10-3-50C", "EKF", 2860, 4],
      ["Автоматический выключатель NB1-63 2P 20А D 6кА CHINT", "NB1-63-2P-D20", "CHINT", 1080, 8],
      ["Автоматический выключатель дифференциального тока АВДТ32 1P+N 20А C 30мА тип AC IEK", "MAD22-2-020-C-30", "IEK", 2510, 7],
      ["Автоматический выключатель дифференциального тока АВДТ-63М 1P+N 40А C 30мА EKF PROxima", "da63m-40-30-pro", "EKF", 2540, 6],
      ["Дифференциальный автомат NXBLE-63 1P+N 40А C 30мА 6кА CHINT", "NXBLE-63-1PN-C40-30", "CHINT", 2890, 5],
      ["Выключатель дифференциального тока ВД1-63 2P 32А 100мА тип AC IEK", "MDV10-2-032-100", "IEK", 1680, 8],
      ["УЗО ВД-100 4P 40А 100мА тип AC электромеханическое EKF PROxima", "rccb-4-40-100-pro", "EKF", 2810, 4],
      ["Выключатель дифференциального тока NL1-63 4P 25А 300мА тип AC CHINT", "NL1-63-4P-25-300", "CHINT", 3180, 3],
      ["Автомат защиты двигателя ПРК32 10-16А IEK", "MPR10-10-16", "IEK", 3960, 5],
      ["Автомат защиты двигателя NS2-25 14-20А CHINT", "NS2-25-14-20", "CHINT", 3720, 5],
      ["Автомат защиты двигателя TeSys GV2ME21 17-23А Schneider Electric", "GV2ME21", "Schneider Electric", 7140, 3],
      ["Автоматический выключатель в литом корпусе ВА88-35 3P 125А IEK", "SVA20-3-0125", "IEK", 8920, 3],
      ["Автоматический выключатель в литом корпусе NM1-250S 3P 225А CHINT", "NM1-250S-3P-225", "CHINT", 9680, 2],
      ["Автоматический выключатель EasyPact CVS250B 3P 200А Schneider Electric", "LV525303", "Schneider Electric", 24200, 2],
    ],
  },
];

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

for (const file of files) {
  const workbook = Workbook.create();
  const sheet = workbook.worksheets.add("Импорт");
  sheet.showGridLines = false;
  sheet.getRange("A1:E1").values = [headers];
  sheet.getRange(`A2:E${file.rows.length + 1}`).values = file.rows;
  sheet.getRange(`A1:E${file.rows.length + 1}`).format.font = {
    name: "Arial",
    size: 10,
    color: "#1F2937",
  };
  sheet.getRange("A1:E1").format = {
    fill: "#0F766E",
    font: { name: "Arial", size: 10, bold: true, color: "#FFFFFF" },
    horizontalAlignment: "center",
    verticalAlignment: "center",
    borders: { preset: "outside", style: "thin", color: "#0B5E58" },
  };
  sheet.getRange(`A2:E${file.rows.length + 1}`).format.verticalAlignment = "center";
  sheet.getRange(`A2:A${file.rows.length + 1}`).format.wrapText = true;
  sheet.getRange(`D2:D${file.rows.length + 1}`).format.numberFormat = "#,##0";
  sheet.getRange(`E2:E${file.rows.length + 1}`).format.numberFormat = "#,##0";
  sheet.getRange("A:A").format.columnWidth = 72;
  sheet.getRange("B:B").format.columnWidth = 25;
  sheet.getRange("C:C").format.columnWidth = 22;
  sheet.getRange("D:D").format.columnWidth = 13;
  sheet.getRange("E:E").format.columnWidth = 12;
  sheet.getRange("1:1").format.rowHeight = 26;
  sheet.getRange(`2:${file.rows.length + 1}`).format.rowHeight = 34;
  sheet.freezePanes.freezeRows(1);
  const table = sheet.tables.add(`A1:E${file.rows.length + 1}`, true, `ImportTable${files.indexOf(file) + 1}`);
  table.style = "TableStyleMedium2";
  table.showFilterButton = true;
  workbook.recalculate();

  const check = await workbook.inspect({
    kind: "table",
    range: `Импорт!A1:E${Math.min(file.rows.length + 1, 6)}`,
    include: "values,formulas",
    tableMaxRows: 6,
    tableMaxCols: 5,
    maxChars: 5000,
  });
  console.log(file.name, check.ndjson);
  const errors = await workbook.inspect({
    kind: "match",
    searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!",
    options: { useRegex: true, maxResults: 50 },
    summary: `formula errors in ${file.name}`,
  });
  console.log(errors.ndjson);

  const preview = await workbook.render({
    sheetName: "Импорт",
    range: `A1:E${Math.min(file.rows.length + 1, 12)}`,
    scale: 1,
    format: "png",
  });
  await fs.writeFile(
    path.join(previewDir, file.name.replace(/\.xlsx$/i, ".png")),
    new Uint8Array(await preview.arrayBuffer()),
  );
  const xlsx = await SpreadsheetFile.exportXlsx(workbook);
  await xlsx.save(path.join(outputDir, file.name));
}

console.log(JSON.stringify(files.map(file => ({ name: file.name, rows: file.rows.length }))));
