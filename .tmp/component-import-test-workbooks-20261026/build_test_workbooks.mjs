import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "F:/Projects/ElectronicCRM/outputs/component-import-test-20261026";
const previewDir = "F:/Projects/ElectronicCRM/.tmp/component-import-test-workbooks-20261026/previews";

const colors = {
  teal: "#0F766E",
  tealDark: "#115E59",
  tealLight: "#CCFBF1",
  blueLight: "#DBEAFE",
  yellow: "#FEF3C7",
  red: "#FEE2E2",
  green: "#DCFCE7",
  gray: "#F3F4F6",
  border: "#CBD5E1",
  text: "#0F172A",
  white: "#FFFFFF",
};

const productHeaders = [
  "Производитель",
  "Артикул",
  "Наименование",
  "Назначение товара",
  "Тип товара",
  "Цена",
  "Остаток",
  "PRODUCT_SERIES",
  "RATED_CURRENT",
  "POLES",
  "BREAKING_CAPACITY",
];

const ruleHeaders = [
  "Артикул комплектующего",
  "Наименование комплектующего",
  "Производитель комплектующего",
  "Тип основного товара",
  "Потребность",
  "Активно",
  "PRODUCT_SERIES",
  "RATED_CURRENT от",
  "RATED_CURRENT до",
  "POLES",
];

function styleDataSheet(sheet, headers, rows, options = {}) {
  const lastCol = String.fromCharCode(64 + headers.length);
  const lastRow = rows.length + 1;
  sheet.showGridLines = false;
  sheet.freezePanes.freezeRows(1);
  sheet.getRange(`A1:${lastCol}1`).format = {
    fill: colors.teal,
    font: { bold: true, color: colors.white },
    verticalAlignment: "center",
    wrapText: true,
    borders: { preset: "all", style: "thin", color: colors.tealDark },
  };
  sheet.getRange(`A1:${lastCol}1`).format.rowHeight = 32;
  if (rows.length > 0) {
    sheet.getRange(`A2:${lastCol}${lastRow}`).format = {
      verticalAlignment: "center",
      wrapText: true,
      borders: { preset: "all", style: "thin", color: colors.border },
    };
    for (let row = 2; row <= lastRow; row += 1) {
      if (row % 2 === 0) sheet.getRange(`A${row}:${lastCol}${row}`).format.fill = "#F8FAFC";
    }
  }
  const widths = options.widths ?? [18, 18, 50, 22, 25, 12, 12, 20, 18, 14, 18];
  widths.forEach((width, index) => {
    sheet.getRangeByIndexes(0, index, Math.max(lastRow, 2), 1).format.columnWidth = width;
  });
  sheet.getRange(`A2:${lastCol}${Math.max(lastRow, 2)}`).format.rowHeight = 34;
  sheet.getRange(`B2:B${Math.max(lastRow, 2)}`).setNumberFormat("@");
  if (headers.includes("Артикул комплектующего")) {
    sheet.getRange(`A2:A${Math.max(lastRow, 2)}`).setNumberFormat("@");
    sheet.getRange(`F2:F200`).dataValidation = { rule: { type: "list", values: ["Да", "Нет"] } };
  } else {
    sheet.getRange(`D2:D200`).dataValidation = { rule: { type: "list", values: ["Основной товар", "Комплектующее"] } };
    sheet.getRange(`F2:F${Math.max(lastRow, 2)}`).setNumberFormat("#,##0.00");
    sheet.getRange(`G2:G${Math.max(lastRow, 2)}`).setNumberFormat("0");
    sheet.getRange(`I2:I${Math.max(lastRow, 2)}`).setNumberFormat("0.####");
    sheet.getRange(`K2:K${Math.max(lastRow, 2)}`).setNumberFormat("0.####");
  }
}

function addInstructionSheet(workbook, title, purpose, steps, expected, warnings = []) {
  const sheet = workbook.worksheets.add("Инструкция");
  sheet.showGridLines = false;
  sheet.getRange("A1:H1").merge();
  sheet.getRange("A1").values = [[title]];
  sheet.getRange("A1:H1").format = {
    fill: colors.tealDark,
    font: { bold: true, color: colors.white, size: 16 },
    verticalAlignment: "center",
  };
  sheet.getRange("A1:H1").format.rowHeight = 38;
  sheet.getRange("A3:H3").merge();
  sheet.getRange("A3").values = [[purpose]];
  sheet.getRange("A3:H3").format = {
    fill: colors.blueLight,
    font: { bold: true, color: colors.text },
    wrapText: true,
    verticalAlignment: "center",
    borders: { preset: "outside", style: "thin", color: colors.border },
  };
  sheet.getRange("A3:H3").format.rowHeight = 46;

  let row = 5;
  const addSection = (heading, items, fill) => {
    sheet.getRange(`A${row}:H${row}`).merge();
    sheet.getRange(`A${row}`).values = [[heading]];
    sheet.getRange(`A${row}:H${row}`).format = {
      fill,
      font: { bold: true, color: colors.text },
      borders: { preset: "outside", style: "thin", color: colors.border },
    };
    row += 1;
    items.forEach((item, index) => {
      sheet.getRange(`A${row}`).values = [[index + 1]];
      sheet.getRange(`B${row}:H${row}`).merge();
      sheet.getRange(`B${row}`).values = [[item]];
      sheet.getRange(`A${row}:H${row}`).format = {
        wrapText: true,
        verticalAlignment: "center",
        borders: { preset: "all", style: "thin", color: colors.border },
      };
      sheet.getRange(`A${row}`).format.font = { bold: true, color: colors.tealDark };
      sheet.getRange(`A${row}:H${row}`).format.rowHeight = 36;
      row += 1;
    });
    row += 1;
  };

  addSection("Порядок проверки", steps, colors.tealLight);
  addSection("Ожидаемый результат", expected, colors.green);
  if (warnings.length > 0) addSection("Важно", warnings, colors.yellow);

  sheet.getRange("A1:A80").format.columnWidth = 6;
  sheet.getRange("B1:H80").format.columnWidth = 18;
  sheet.freezePanes.freezeRows(1);
  return sheet;
}

function addDataSheets(workbook, products, rules, options = {}) {
  const productsSheet = workbook.worksheets.add("Товары");
  productsSheet.getRange(`A1:K${products.length + 1}`).values = [productHeaders, ...products];
  styleDataSheet(productsSheet, productHeaders, products);
  productsSheet.tabColor = colors.teal;

  const rulesSheet = workbook.worksheets.add("Совместимость");
  rulesSheet.getRange(`A1:J${rules.length + 1}`).values = [ruleHeaders, ...rules];
  styleDataSheet(rulesSheet, ruleHeaders, rules, {
    widths: [19, 48, 22, 24, 24, 12, 20, 18, 18, 14],
  });
  rulesSheet.tabColor = "#2563EB";

  if (options.errorRows) {
    options.errorRows.forEach((rowNumber) => {
      rulesSheet.getRange(`A${rowNumber}:J${rowNumber}`).format.fill = colors.red;
    });
  }
  if (options.warningRows) {
    options.warningRows.forEach((rowNumber) => {
      rulesSheet.getRange(`A${rowNumber}:J${rowNumber}`).format.fill = colors.yellow;
    });
  }
}

async function finalize(workbook, fileName) {
  workbook.recalculate();
  const formulaErrors = await workbook.inspect({
    kind: "match",
    searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
    options: { useRegex: true, maxResults: 100 },
    maxChars: 5000,
  });
  const keyRanges = [];
  for (const sheetName of ["Товары", "Совместимость", "Инструкция"]) {
    keyRanges.push((await workbook.inspect({ kind: "region", sheetId: sheetName, range: "A1:K16", maxChars: 5000 })).ndjson);
    const preview = await workbook.render({ sheetName, autoCrop: "all", scale: 1, format: "png" });
    const previewBytes = new Uint8Array(await preview.arrayBuffer());
    await fs.writeFile(path.join(previewDir, `${fileName}-${sheetName}.png`), previewBytes);
  }
  const output = await SpreadsheetFile.exportXlsx(workbook);
  const outputPath = path.join(outputDir, `${fileName}.xlsx`);
  await output.save(outputPath);
  return {
    outputPath,
    formulaErrors: formulaErrors.ndjson,
    keyRanges,
  };
}

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const happy = Workbook.create();
addDataSheets(happy, [
  ["DEKraft", "22504DEK", "Автомат 3П 400А 50кА ВА-335Е с электронным расцепителем", "Основной товар", "Силовой автомат", 9535.99, 8, "BA-335E", 400, "3", 50],
  ["DEKraft", "22505DEK", "Автомат 4П 400А 50кА ВА-335Е с электронным расцепителем", "Основной товар", "Силовой автомат", 10990, 5, "BA-335E", 400, "4", 50],
  ["DEKraft", "22580DEK", "Контакт дополнительный и сигнальный левый ВА-335Е", "Комплектующее", "", 1490, 11, "", "", "", ""],
  ["DEKraft", "22553DEK", "Контакт дополнительный левый 1НО1НЗ ВА-335Е", "Комплектующее", "", 1190, 13, "", "", "", ""],
  ["DEKraft", "22562DEK", "Контакт дополнительный левый 2НО2НЗ ВА-335Е", "Комплектующее", "", 1270, 7, "", "", "", ""],
  ["DEKraft", "22571DEK", "Контакт сигнальный левый ВА-335Е", "Комплектующее", "", 980, 9, "", "", "", ""],
  ["DEKraft", "22531DEK", "Привод моторный МП-335Е AC 230В для ВА-335Е", "Комплектующее", "", 6120, 4, "", "", "", ""],
  ["DEKraft", "22600DEK", "Расцепитель минимального напряжения левый AC 230В ВА-335Е", "Комплектующее", "", 2380, 6, "", "", "", ""],
  ["DEKraft", "22601DEK", "Расцепитель минимального напряжения левый AC 400В ВА-335Е", "Комплектующее", "", 2380, 3, "", "", "", ""],
  ["DEKraft", "22533DEK", "Ручка на дверь шкафа РП-335Е для ВА-335Е", "Комплектующее", "", 1840, 14, "", "", "", ""],
  ["DEKraft", "22544DEK", "Корзина выкатного типа КА-335Е для ВА-335Е 3П", "Комплектующее", "", 8590, 2, "", "", "", ""],
], [
  ["22580DEK", "Контакт дополнительный и сигнальный левый ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", ""],
  ["22553DEK", "Контакт дополнительный левый 1НО1НЗ ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", ""],
  ["22562DEK", "Контакт дополнительный левый 2НО2НЗ ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", ""],
  ["22571DEK", "Контакт сигнальный левый ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", ""],
  ["22531DEK", "Привод моторный МП-335Е AC 230В для ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", ""],
  ["22600DEK", "Расцепитель минимального напряжения левый AC 230В ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", ""],
  ["22601DEK", "Расцепитель минимального напряжения левый AC 400В ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", ""],
  ["22533DEK", "Ручка на дверь шкафа РП-335Е для ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", ""],
  ["22544DEK", "Корзина выкатного типа КА-335Е для ВА-335Е 3П", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", "3"],
]);
addInstructionSheet(
  happy,
  "Тест 1. Единый импорт товаров и совместимых аксессуаров",
  "Реальный пример DEKraft ВА-335Е: основные товары и комплектующие находятся в одном файле, а правила создаются по серии. Для корзины дополнительно проверяется число полюсов.",
  [
    "Загрузите файл в разделе «Импорт», проверьте распознавание строк и примените товары в каталог.",
    "В результате импорта убедитесь, что две основные позиции получили тип «Силовой автомат», а аксессуары — общий тип «Комплектующие».",
    "В том же результате откройте блок правил совместимости, выполните проверку и примените правила.",
    "В Ассистенте найдите артикул 22504DEK и раскройте комплектующие.",
    "Повторно загрузите этот же файл: товары и правила не должны дублироваться.",
  ],
  [
    "Для 22504DEK показываются все аксессуары серии BA-335E, включая корзину 22544DEK.",
    "Для 22505DEK показываются аксессуары серии BA-335E, но корзина 22544DEK не показывается, потому что правило ограничено значением POLES = 3.",
    "Пустая потребность выводит позиции в блоке «Совместимые аксессуары» и не делает их обязательными.",
    "При повторном импорте система сообщает об обновлении/отсутствии изменений, но не создаёт дубликаты.",
  ],
  [
    "В колонке POLES используются только цифры 3 и 4. Это специально проверяет нормализацию значения без суффикса P.",
    "Наименования и цены взяты как реалистичный тестовый пример; перед использованием в рабочем каталоге цены следует сверить с актуальным прайсом.",
  ],
);

const ranges = Workbook.create();
addDataSheets(ranges, [
  ["DEKraft", "22503DEK", "Автомат 3П 250А 50кА ВА-335Е с электронным расцепителем", "Основной товар", "Силовой автомат", 8982.01, 6, "BA-335E", 250, "3", 50],
  ["DEKraft", "22504DEK", "Автомат 3П 400А 50кА ВА-335Е с электронным расцепителем", "Основной товар", "Силовой автомат", 9535.99, 8, "BA-335E", 400, "3", 50],
  ["DEKraft", "22505DEK", "Автомат 4П 400А 50кА ВА-335Е с электронным расцепителем", "Основной товар", "Силовой автомат", 10990, 5, "BA-335E", 400, "4", 50],
  ["DEKraft", "22580DEK", "Контакт дополнительный и сигнальный левый ВА-335Е", "Комплектующее", "", 1490, 11, "", "", "", ""],
  ["DEKraft", "22553DEK", "Контакт дополнительный левый 1НО1НЗ ВА-335Е", "Комплектующее", "", 1190, 13, "", "", "", ""],
  ["DEKraft", "22531DEK", "Привод моторный МП-335Е AC 230В для ВА-335Е", "Комплектующее", "", 6120, 4, "", "", "", ""],
], [
  ["22580DEK", "Контакт дополнительный и сигнальный левый ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "Дополнительный контакт", "Да", "BA-335E", 160, 400, "3"],
  ["22553DEK", "Контакт дополнительный левый 1НО1НЗ ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "Дополнительный контакт", "Да", "BA-335E", 160, 630, "3"],
  ["22531DEK", "Привод моторный МП-335Е AC 230В для ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", 250, 630, ""],
  ["22531DEK", "Привод моторный МП-335Е AC 230В для ВА-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", 100, 160, ""],
]);
addInstructionSheet(
  ranges,
  "Тест 2. Диапазоны, несколько условий и потребность",
  "Проверка логики И/ИЛИ: внутри строки одновременно применяются серия, диапазон тока и число полюсов; повторные строки одного комплектующего образуют альтернативные варианты.",
  [
    "Сначала в разделе комплектующих для типа «Силовой автомат» создайте потребность «Дополнительный контакт» как необязательную, количество 1.",
    "Загрузите файл, примените товары, затем проверьте и примените правила из этого же файла.",
    "Найдите 22503DEK, 22504DEK и 22505DEK и сравните состав блоков «Дополнительный контакт» и «Совместимые аксессуары».",
    "Обратите внимание на две строки артикула 22531DEK: это два альтернативных диапазона, а не два отдельных правила для показа.",
  ],
  [
    "22580DEK подходит к 22503DEK и 22504DEK: серия BA-335E, ток 160–400 и 3 полюса совпали.",
    "22580DEK не подходит к 22505DEK: ток совпадает, но POLES = 4, тогда как правило требует 3.",
    "22553DEK попадает в ту же потребность и подходит трёхполюсным товарам с током 160–630 А.",
    "22531DEK отображается как обычный аксессуар для токов 100–160 ИЛИ 250–630 А; значение 200 А не подходит ни одному варианту.",
  ],
  [
    "Если потребность не создана заранее, первые две строки правил должны получить понятную ошибку «Потребность не найдена». Это ожидаемая защита.",
    "Границы диапазонов включаются: 160, 250, 400 и 630 считаются допустимыми значениями.",
  ],
);

const errors = Workbook.create();
addDataSheets(errors, [
  ["DEKraft", "22504DEK", "Автомат 3П 400А 50кА ВА-335Е с электронным расцепителем", "Основной товар", "Силовой автомат", 9535.99, 8, "BA-335E", 400, "3", 50],
  ["DEKraft", "22504DEK", "Дубликат той же пары производитель + артикул", "Основной товар", "Силовой автомат", 9535.99, 8, "BA-335E", 400, "3", 50],
], [
  ["22544DEK", "Корзина выкатного типа КА-335Е для ВА-335Е 3П", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "", "", "", ""],
  ["99999DEK", "Отсутствующее комплектующее", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", ""],
  ["22580DEK", "Контакт с неверно указанным производителем", "IEK", "POWER_CIRCUIT_BREAKER", "", "Да", "BA-335E", "", "", ""],
  ["22580DEK", "Контакт с несуществующим типом", "DEKraft", "НЕСУЩЕСТВУЮЩИЙ_ТИП", "", "Да", "BA-335E", "", "", ""],
  ["22580DEK", "Контакт с неверным состоянием", "DEKraft", "POWER_CIRCUIT_BREAKER", "", "Иногда", "BA-335E", "", "", ""],
], { warningRows: [2], errorRows: [3, 4, 5, 6] });
addInstructionSheet(
  errors,
  "Тест 3. Ошибки, защита от широких правил и дубликатов",
  "Негативный сценарий. Файл предназначен только для предварительной проверки. Не применяйте его к рабочему каталогу.",
  [
    "Загрузите файл и остановитесь на предварительном просмотре — не нажимайте применение импорта.",
    "Проверьте лист «Товары»: повтор пары DEKraft + 22504DEK должен быть отмечен как дубликат либо конфликт обновления.",
    "Проверьте правила: строки с отсутствующим товаром, неверным производителем, неизвестным типом и значением «Иногда» должны быть отклонены.",
    "Первая строка правил не содержит условий. Система должна пометить её как широкое правило и потребовать отдельное подтверждение.",
  ],
  [
    "Ошибочные строки не применяются и содержат конкретное объяснение причины.",
    "Широкое правило 22544DEK не применяется без явного подтверждения пользователя.",
    "Система не создаёт второй товар с той же парой производитель + артикул.",
  ],
  [
    "Красные строки — ожидаемые ошибки; жёлтая строка — намеренно широкое правило.",
    "Этот файл безопасен только при проверке без применения.",
  ],
);

const results = [];
results.push(await finalize(happy, "01-единый-импорт-DEKraft-BA-335E"));
results.push(await finalize(ranges, "02-диапазоны-и-потребности-DEKraft-BA-335E"));
results.push(await finalize(errors, "03-ошибки-и-защитные-проверки"));

console.log(JSON.stringify(results, null, 2));
