import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = path.resolve("outputs/learning-component-test-20261027");
const previewDir = path.resolve(".tmp/learning-component-test-previews");
const fontName = "Arial";
const headerFill = "#164E63";
const headerText = "#FFFFFF";
const borderColor = "#CBD5E1";

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

function styleDataSheet(sheet, rowCount, colCount, widths) {
  sheet.showGridLines = false;
  sheet.freezePanes.freezeRows(1);
  const used = sheet.getRangeByIndexes(0, 0, rowCount, colCount);
  used.format.font = { name: fontName, size: 10, color: "#0F172A" };
  used.format.verticalAlignment = "center";
  const header = sheet.getRangeByIndexes(0, 0, 1, colCount);
  header.format.fill = headerFill;
  header.format.font = { name: fontName, size: 10, bold: true, color: headerText };
  header.format.horizontalAlignment = "center";
  header.format.wrapText = true;
  header.format.rowHeight = 34;
  used.format.borders = {
    insideHorizontal: { style: "thin", color: borderColor },
    bottom: { style: "thin", color: borderColor },
  };
  widths.forEach((width, index) => {
    sheet.getRangeByIndexes(0, index, rowCount, 1).format.columnWidth = width;
  });
  if (rowCount > 1) {
    sheet.getRangeByIndexes(1, 0, rowCount - 1, colCount).format.rowHeight = 24;
  }
}

function addImportSheet(workbook, rows, extraHeaders = []) {
  const sheet = workbook.worksheets.add("Импорт");
  const headers = [
    "НАИМЕНОВАНИЕ",
    "АРТИКУЛ",
    "ПРОИЗВОДИТЕЛЬ",
    "ЦЕНА",
    "ОСТАТОК",
    ...extraHeaders,
  ];
  const matrix = [headers, ...rows];
  sheet.getRangeByIndexes(0, 0, matrix.length, headers.length).values = matrix;
  styleDataSheet(sheet, matrix.length, headers.length, [62, 26, 20, 14, 12, ...extraHeaders.map(() => 26)]);
  sheet.getRange(`D2:D${matrix.length}`).format.numberFormat = "#,##0.00";
  sheet.getRange(`E2:E${matrix.length}`).format.numberFormat = "#,##0";
  return sheet;
}

function addInstructionSheet(workbook, title, rows) {
  const sheet = workbook.worksheets.add("Инструкция");
  sheet.showGridLines = false;
  sheet.getRange("A2:B2").values = [[title, null]];
  sheet.getRange("A2:B2").format.font = { name: fontName, size: 14, bold: true, color: "#0F172A" };
  sheet.getRange("A3:B3").format.borders = { bottom: { style: "thin", color: headerFill } };
  const data = rows.map((text, index) => [index + 1, text]);
  if (data.length > 0) {
    sheet.getRangeByIndexes(4, 0, data.length, 2).values = data;
    sheet.getRangeByIndexes(4, 0, data.length, 2).format.font = { name: fontName, size: 10, color: "#0F172A" };
    sheet.getRangeByIndexes(4, 0, data.length, 2).format.verticalAlignment = "top";
    sheet.getRangeByIndexes(4, 1, data.length, 1).format.wrapText = true;
    sheet.getRangeByIndexes(4, 0, data.length, 2).format.borders = {
      insideHorizontal: { style: "thin", color: borderColor },
    };
  }
  sheet.getRange("A:A").format.columnWidth = 8;
  sheet.getRange("B:B").format.columnWidth = 110;
  return sheet;
}

async function verifyAndSave(workbook, fileName, renderTargets) {
  workbook.recalculate();
  const errors = await workbook.inspect({
    kind: "match",
    searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!",
    options: { useRegex: true, maxResults: 100 },
    summary: `formula errors in ${fileName}`,
  });
  const formulaMatches = errors.ndjson
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line))
    .filter((entry) => entry.kind !== "notice");
  if (formulaMatches.length > 0) {
    throw new Error(`Formula error scan failed for ${fileName}: ${errors.ndjson}`);
  }
  const inspect = await workbook.inspect({
    kind: "sheet,table",
    maxChars: 12000,
    tableMaxRows: 8,
    tableMaxCols: 16,
    tableMaxCellChars: 120,
  });
  await fs.writeFile(path.join(previewDir, `${fileName}.inspect.ndjson`), inspect.ndjson, "utf8");
  for (const target of renderTargets) {
    const preview = await workbook.render({
      sheetName: target.sheetName,
      range: target.range,
      scale: 1.2,
      format: "png",
    });
    await fs.writeFile(
      path.join(previewDir, `${fileName}-${target.sheetName}.png`),
      new Uint8Array(await preview.arrayBuffer()),
    );
  }
  const output = await SpreadsheetFile.exportXlsx(workbook);
  await output.save(path.join(outputDir, fileName));
}

// 00. Карта тестирования.
{
  const workbook = Workbook.create();
  const route = workbook.worksheets.add("Маршрут");
  route.showGridLines = false;
  route.getRange("A2:F2").values = [["Полная проверка импорта, обучения и комплектующих", null, null, null, null, null]];
  route.getRange("A2:F2").format.font = { name: fontName, size: 14, bold: true, color: "#0F172A" };
  route.getRange("A3:F3").format.borders = { bottom: { style: "thin", color: headerFill } };
  const routeRows = [
    ["Этап", "Файл", "Что проверяем", "Обязательное действие", "Ожидаемый результат", "Факт"],
    [1, "01_CHINT_NB1_обучение_и_заполнение.xlsx", "Подтверждение примеров и заполнение всего импорта", "Подтвердить строки-примеры, выделить их, выполнить предпросмотр и применение", "BX→B, CX→C, DX→D. Целевые строки получают источник «По примерам»", ""],
    [2, "02_CHINT_смешанные_типы.xlsx", "Изоляция производителя и типа", "Назначить типы при необходимости и подтвердить по два примера в каждой области", "Пример одного типа не заполняет другой тип", ""],
    [3, "03_конфликты_границы_приоритет.xlsx", "Конфликт примеров, граница токена, приоритет Excel", "Создать указанные подтверждения и выполнить предпросмотр", "Конфликт заблокирован, P3 не находится внутри 13P, Excel D не перезаписан", ""],
    [4, "04_DEKraft_товары_комплектующие_правила.xlsx", "Единый импорт товаров, комплектующих и правил", "Импортировать товары, затем проверить и применить правила", "Правила создаются без ручного повторного ввода", ""],
    [5, "04_DEKraft_товары_комплектующие_правила.xlsx", "Фактический подбор комплектующих", "Открыть четыре основных товара и сравнить выдачу", "Условия И, альтернативы ИЛИ, диапазоны и потребность работают", ""],
    [6, "04_DEKraft_товары_комплектующие_правила.xlsx", "Повторный импорт", "Загрузить тот же файл повторно", "Дубликаты товаров и правил не создаются", ""],
    [7, "05_DEKraft_ошибки_правил.xlsx", "Защитные проверки", "Остановиться на предпросмотре", "Широкое правило требует подтверждения, ошибки имеют конкретные причины", ""],
  ];
  route.getRangeByIndexes(4, 0, routeRows.length, 6).values = routeRows;
  styleDataSheet(route, routeRows.length + 4, 6, [10, 44, 46, 54, 58, 22]);
  route.freezePanes.freezeRows(5);
  route.getRange(`A6:F${routeRows.length + 4}`).format.wrapText = true;
  route.getRange(`F6:F${routeRows.length + 4}`).format.fill = "#FEF3C7";

  const confirmations = workbook.worksheets.add("Подтверждения");
  const confirmationRows = [
    ["Файл", "Строки Excel", "Выделяемый фрагмент", "Правильное значение", "Характеристика"],
    ["01", "2–3", "BX", "B", "Характеристика срабатывания"],
    ["01", "6–7", "CX", "C", "Характеристика срабатывания"],
    ["01", "10–11", "DX", "D", "Характеристика срабатывания"],
    ["02", "2–3", "BX", "B", "Характеристика срабатывания"],
    ["02", "6–7", "L30", "30", "Ток утечки"],
    ["02", "10–11", "ACT", "AC", "Тип дифференциального тока"],
    ["02", "14–15", "P3", "3P", "Количество полюсов"],
    ["03", "2", "BX", "B", "Характеристика срабатывания"],
    ["03", "3", "BX", "C", "Характеристика срабатывания"],
    ["03", "5–6", "CX", "C", "Характеристика срабатывания"],
    ["03", "8–9", "P3", "3P", "Количество полюсов"],
  ];
  confirmations.getRangeByIndexes(0, 0, confirmationRows.length, 5).values = confirmationRows;
  styleDataSheet(confirmations, confirmationRows.length, 5, [16, 18, 28, 24, 38]);

  const matrix = workbook.worksheets.add("Матрица комплектующих");
  const matrixRows = [
    ["Основной товар", "Ток", "Полюса", "Контакт A", "Контакт B", "Моторный привод", "Корзина 3P", "Ручка"],
    ["QA-DEK-MAIN-160-3", 160, "3", "Да", "Да", "Да", "Да", "Да"],
    ["QA-DEK-MAIN-250-3", 250, "3", "Да", "Да", "Да", "Да", "Да"],
    ["QA-DEK-MAIN-400-3", 400, "3", "Да", "Да", "Да", "Да", "Да"],
    ["QA-DEK-MAIN-400-4", 400, "4", "Нет", "Нет", "Да", "Нет", "Да"],
  ];
  matrix.getRangeByIndexes(0, 0, matrixRows.length, 8).values = matrixRows;
  styleDataSheet(matrix, matrixRows.length, 8, [28, 12, 12, 16, 16, 22, 18, 16]);
  await verifyAndSave(workbook, "00_Карта_проверки.xlsx", [
    { sheetName: "Маршрут", range: "A1:F12" },
    { sheetName: "Подтверждения", range: "A1:E12" },
    { sheetName: "Матрица комплектующих", range: "A1:H5" },
  ]);
}

// 01. Один тип, три кода характеристики.
{
  const workbook = Workbook.create();
  const rows = [
    ["Модульный автомат CHINT NB1-63 1P 6A 6kA код кривой BX", "QA-LRN-NB1-B-01", "CHINT", 510, 10],
    ["Модульный автомат CHINT NB1-63 1P 10A 6kA код кривой BX", "QA-LRN-NB1-B-02", "CHINT", 520, 11],
    ["Модульный автомат CHINT NB1-63 1P 16A 6kA код кривой BX", "QA-LRN-NB1-B-T1", "CHINT", 530, 12],
    ["Модульный автомат CHINT NB1-63 2P 25A 6kA код кривой BX", "QA-LRN-NB1-B-T2", "CHINT", 1040, 7],
    ["Модульный автомат CHINT NB1-63 1P 6A 6kA код кривой CX", "QA-LRN-NB1-C-01", "CHINT", 515, 10],
    ["Модульный автомат CHINT NB1-63 1P 10A 6kA код кривой CX", "QA-LRN-NB1-C-02", "CHINT", 525, 11],
    ["Модульный автомат CHINT NB1-63 1P 16A 6kA код кривой CX", "QA-LRN-NB1-C-T1", "CHINT", 535, 12],
    ["Модульный автомат CHINT NB1-63 3P 32A 6kA код кривой CX", "QA-LRN-NB1-C-T2", "CHINT", 1550, 6],
    ["Модульный автомат CHINT NB1-63 1P 6A 6kA код кривой DX", "QA-LRN-NB1-D-01", "CHINT", 520, 10],
    ["Модульный автомат CHINT NB1-63 1P 10A 6kA код кривой DX", "QA-LRN-NB1-D-02", "CHINT", 530, 11],
    ["Модульный автомат CHINT NB1-63 2P 16A 6kA код кривой DX", "QA-LRN-NB1-D-T1", "CHINT", 1090, 8],
    ["Модульный автомат CHINT NB1-63 3P 25A 6kA код кривой DX", "QA-LRN-NB1-D-T2", "CHINT", 1620, 5],
  ];
  addImportSheet(workbook, rows);
  await verifyAndSave(workbook, "01_CHINT_NB1_обучение_и_заполнение.xlsx", [
    { sheetName: "Импорт", range: "A1:E13" },
  ]);
}

// 02. Несколько типов в одном импорте.
{
  const workbook = Workbook.create();
  const rows = [
    ["Модульный автомат CHINT NB1-63 1P 6A 6kA код кривой BX", "QA-MIX-MCB-E1", "CHINT", 510, 10],
    ["Модульный автомат CHINT NB1-63 1P 10A 6kA код кривой BX", "QA-MIX-MCB-E2", "CHINT", 520, 11],
    ["Модульный автомат CHINT NB1-63 1P 16A 6kA код кривой BX", "QA-MIX-MCB-T1", "CHINT", 530, 12],
    ["Модульный автомат CHINT NB1-63 2P 25A 6kA код кривой BX", "QA-MIX-MCB-T2", "CHINT", 1040, 7],
    ["Дифференциальный автомат CHINT NXBLE-63 1P+N C10 код утечки L30", "QA-MIX-RCBO-E1", "CHINT", 2450, 8],
    ["Дифференциальный автомат CHINT NXBLE-63 1P+N C16 код утечки L30", "QA-MIX-RCBO-E2", "CHINT", 2520, 9],
    ["Дифференциальный автомат CHINT NXBLE-63 1P+N C20 код утечки L30", "QA-MIX-RCBO-T1", "CHINT", 2580, 7],
    ["Дифференциальный автомат CHINT NXBLE-63 1P+N C25 код утечки L30", "QA-MIX-RCBO-T2", "CHINT", 2650, 6],
    ["УЗО CHINT NL1-63 2P 25A 30mA код типа ACT", "QA-MIX-RCD-E1", "CHINT", 1850, 8],
    ["УЗО CHINT NL1-63 2P 40A 30mA код типа ACT", "QA-MIX-RCD-E2", "CHINT", 1920, 9],
    ["УЗО CHINT NL1-63 4P 40A 30mA код типа ACT", "QA-MIX-RCD-T1", "CHINT", 3350, 5],
    ["УЗО CHINT NL1-63 4P 63A 30mA код типа ACT", "QA-MIX-RCD-T2", "CHINT", 3580, 4],
    ["Мотор-автомат CHINT NS2-25 6-10A полюсный код P3", "QA-MIX-MOTOR-E1", "CHINT", 3200, 5],
    ["Мотор-автомат CHINT NS2-25 9-14A полюсный код P3", "QA-MIX-MOTOR-E2", "CHINT", 3250, 6],
    ["Мотор-автомат CHINT NS2-25 13-18A полюсный код P3", "QA-MIX-MOTOR-T1", "CHINT", 3300, 4],
    ["Мотор-автомат CHINT NS2-25 17-23A полюсный код P3", "QA-MIX-MOTOR-T2", "CHINT", 3380, 3],
  ];
  addImportSheet(workbook, rows);
  await verifyAndSave(workbook, "02_CHINT_смешанные_типы.xlsx", [
    { sheetName: "Импорт", range: "A1:E17" },
  ]);
}

// 03. Негативные сценарии нового заполнения.
{
  const workbook = Workbook.create();
  const rows = [
    ["Модульный автомат CHINT NB1-63 1P 6A код кривой BX", "QA-NEG-BX-E1", "CHINT", 500, 5, null],
    ["Модульный автомат CHINT NB1-63 1P 10A код кривой BX", "QA-NEG-BX-E2", "CHINT", 510, 5, null],
    ["Модульный автомат CHINT NB1-63 1P 16A код кривой BX", "QA-NEG-BX-T1", "CHINT", 520, 5, null],
    ["Модульный автомат CHINT NB1-63 1P 6A код кривой CX", "QA-NEG-CX-E1", "CHINT", 500, 5, null],
    ["Модульный автомат CHINT NB1-63 1P 10A код кривой CX", "QA-NEG-CX-E2", "CHINT", 510, 5, null],
    ["Модульный автомат CHINT NB1-63 1P 16A код кривой CX", "QA-NEG-CX-EXCEL", "CHINT", 520, 5, "D"],
    ["Модульный автомат CHINT NB1-63 3P 6A код P3", "QA-NEG-P3-E1", "CHINT", 800, 5, null],
    ["Модульный автомат CHINT NB1-63 3P 10A код P3", "QA-NEG-P3-E2", "CHINT", 820, 5, null],
    ["Модульный автомат CHINT NB1-63 специальный код 13P", "QA-NEG-P3-BOUNDARY", "CHINT", 830, 5, null],
    ["Модульный автомат CHINT NB1-63 специальный код P3", "QA-NEG-P3-EXACT", "CHINT", 840, 5, null],
  ];
  addImportSheet(workbook, rows, ["Характеристика срабатывания"]);
  await verifyAndSave(workbook, "03_конфликты_границы_приоритет.xlsx", [
    { sheetName: "Импорт", range: "A1:F11" },
  ]);
}

// 04. Единый импорт товаров, комплектующих и правил.
{
  const workbook = Workbook.create();
  const products = workbook.worksheets.add("Товары");
  const productRows = [
    ["Производитель", "Артикул", "Наименование", "Назначение товара", "Тип товара", "Цена", "Остаток", "PRODUCT_SERIES", "RATED_CURRENT", "POLES", "BREAKING_CAPACITY"],
    ["DEKraft", "QA-DEK-MAIN-160-3", "Силовой автомат ВА-335Е 3П 160А 50кА", "Основной товар", "Силовой автомат", 8200, 6, "BA-335E", 160, "3", 50],
    ["DEKraft", "QA-DEK-MAIN-250-3", "Силовой автомат ВА-335Е 3П 250А 50кА", "Основной товар", "Силовой автомат", 8900, 5, "BA-335E", 250, "3", 50],
    ["DEKraft", "QA-DEK-MAIN-400-3", "Силовой автомат ВА-335Е 3П 400А 50кА", "Основной товар", "Силовой автомат", 9600, 4, "BA-335E", 400, "3", 50],
    ["DEKraft", "QA-DEK-MAIN-400-4", "Силовой автомат ВА-335Е 4П 400А 50кА", "Основной товар", "Силовой автомат", 11000, 3, "BA-335E", 400, "4", 50],
    ["DEKraft", "QA-DEK-CONT-A", "Контакт дополнительный ВА-335Е исполнение A", "Комплектующее", null, 1490, 12, null, null, null, null],
    ["DEKraft", "QA-DEK-CONT-B", "Контакт дополнительный ВА-335Е исполнение B", "Комплектующее", null, 1690, 10, null, null, null, null],
    ["DEKraft", "QA-DEK-MOTOR", "Привод моторный МП-335Е AC 230В", "Комплектующее", null, 6120, 5, null, null, null, null],
    ["DEKraft", "QA-DEK-BASKET-3P", "Корзина выкатного исполнения КА-335Е 3П", "Комплектующее", null, 8590, 3, null, null, null, null],
    ["DEKraft", "QA-DEK-HANDLE", "Ручка на дверь шкафа РП-335Е", "Комплектующее", null, 1840, 14, null, null, null, null],
  ];
  products.getRangeByIndexes(0, 0, productRows.length, 11).values = productRows;
  styleDataSheet(products, productRows.length, 11, [18, 26, 54, 20, 24, 14, 12, 20, 18, 12, 20]);
  products.getRange(`F2:F${productRows.length}`).format.numberFormat = "#,##0.00";
  products.getRange(`G2:G${productRows.length}`).format.numberFormat = "#,##0";

  const rules = workbook.worksheets.add("Совместимость");
  const ruleRows = [
    ["Артикул комплектующего", "Наименование комплектующего", "Производитель комплектующего", "Тип основного товара", "Потребность", "Активно", "PRODUCT_SERIES", "RATED_CURRENT от", "RATED_CURRENT до", "POLES"],
    ["QA-DEK-CONT-A", "Контакт дополнительный исполнение A", "DEKraft", "POWER_CIRCUIT_BREAKER", "Дополнительный контакт", "Да", "BA-335E", 160, 400, "3"],
    ["QA-DEK-CONT-B", "Контакт дополнительный исполнение B", "DEKraft", "POWER_CIRCUIT_BREAKER", "Дополнительный контакт", "Да", "BA-335E", 160, 630, "3"],
    ["QA-DEK-MOTOR", "Привод моторный МП-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", null, "Да", "BA-335E", 100, 160, null],
    ["QA-DEK-MOTOR", "Привод моторный МП-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", null, "Да", "BA-335E", 250, 630, null],
    ["QA-DEK-BASKET-3P", "Корзина КА-335Е 3П", "DEKraft", "POWER_CIRCUIT_BREAKER", null, "Да", "BA-335E", null, null, "3"],
    ["QA-DEK-HANDLE", "Ручка РП-335Е", "DEKraft", "POWER_CIRCUIT_BREAKER", null, "Да", "BA-335E", null, null, null],
  ];
  rules.getRangeByIndexes(0, 0, ruleRows.length, 10).values = ruleRows;
  styleDataSheet(rules, ruleRows.length, 10, [26, 46, 24, 30, 30, 12, 22, 20, 20, 12]);

  addInstructionSheet(workbook, "Проверка единого импорта комплектующих и правил", [
    "Сначала создайте для типа «Силовой автомат» потребность «Дополнительный контакт» с количеством 1.",
    "Загрузите этот файл в обычном импорте и примените лист «Товары» в каталог.",
    "В том же пакете откройте проверку правил совместимости и примените строки без ошибок.",
    "Повторные строки QA-DEK-MOTOR образуют альтернативные диапазоны: 100–160 ИЛИ 250–630.",
    "QA-DEK-CONT-A и QA-DEK-CONT-B должны отображаться внутри потребности только у трёхполюсных ВА-335Е.",
    "QA-DEK-BASKET-3P подходит только трёхполюсным товарам. QA-DEK-HANDLE подходит всей серии.",
    "Повторная загрузка файла не должна создавать дубликаты товаров или правил.",
  ]);
  await verifyAndSave(workbook, "04_DEKraft_товары_комплектующие_правила.xlsx", [
    { sheetName: "Товары", range: "A1:K10" },
    { sheetName: "Совместимость", range: "A1:J7" },
    { sheetName: "Инструкция", range: "A1:B12" },
  ]);
}

// 05. Ошибки правил. Используется после положительного файла.
{
  const workbook = Workbook.create();
  const products = workbook.worksheets.add("Товары");
  const productRows = [
    ["Производитель", "Артикул", "Наименование", "Назначение товара", "Тип товара", "Цена", "Остаток", "PRODUCT_SERIES", "RATED_CURRENT", "POLES", "BREAKING_CAPACITY"],
    ["DEKraft", "QA-DEK-MAIN-400-3", "Существующий основной товар для проверки", "Основной товар", "Силовой автомат", 9600, 4, "BA-335E", 400, "3", 50],
    ["DEKraft", "QA-DEK-MAIN-400-3", "Намеренный дубликат производителя и артикула", "Основной товар", "Силовой автомат", 9600, 4, "BA-335E", 400, "3", 50],
  ];
  products.getRangeByIndexes(0, 0, productRows.length, 11).values = productRows;
  styleDataSheet(products, productRows.length, 11, [18, 28, 54, 20, 24, 14, 12, 20, 18, 12, 20]);

  const rules = workbook.worksheets.add("Совместимость");
  const ruleRows = [
    ["Артикул комплектующего", "Наименование комплектующего", "Производитель комплектующего", "Тип основного товара", "Потребность", "Активно", "PRODUCT_SERIES", "RATED_CURRENT от", "RATED_CURRENT до", "POLES"],
    ["QA-DEK-HANDLE", "Широкое правило без условий", "DEKraft", "POWER_CIRCUIT_BREAKER", null, "Да", null, null, null, null],
    ["QA-DEK-NOT-FOUND", "Несуществующее комплектующее", "DEKraft", "POWER_CIRCUIT_BREAKER", null, "Да", "BA-335E", null, null, null],
    ["QA-DEK-CONT-A", "Неверный производитель", "IEK", "POWER_CIRCUIT_BREAKER", null, "Да", "BA-335E", null, null, null],
    ["QA-DEK-CONT-A", "Несуществующий тип", "DEKraft", "UNKNOWN_TYPE", null, "Да", "BA-335E", null, null, null],
    ["QA-DEK-CONT-A", "Несуществующая потребность", "DEKraft", "POWER_CIRCUIT_BREAKER", "Несуществующая потребность", "Да", "BA-335E", null, null, null],
    ["QA-DEK-CONT-A", "Некорректное состояние", "DEKraft", "POWER_CIRCUIT_BREAKER", null, "Иногда", "BA-335E", null, null, null],
    ["QA-DEK-MOTOR", "Некорректный диапазон", "DEKraft", "POWER_CIRCUIT_BREAKER", null, "Да", "BA-335E", 630, 100, null],
    ["QA-DEK-BASKET-3P", "Намеренный повтор", "DEKraft", "POWER_CIRCUIT_BREAKER", null, "Да", "BA-335E", null, null, "3"],
    ["QA-DEK-BASKET-3P", "Намеренный повтор", "DEKraft", "POWER_CIRCUIT_BREAKER", null, "Да", "BA-335E", null, null, "3"],
  ];
  rules.getRangeByIndexes(0, 0, ruleRows.length, 10).values = ruleRows;
  styleDataSheet(rules, ruleRows.length, 10, [26, 42, 24, 30, 32, 12, 22, 20, 20, 12]);

  addInstructionSheet(workbook, "Защитные проверки импорта правил", [
    "Файл предназначен для предпросмотра. Не применяйте его к рабочей базе.",
    "Первая строка правила не содержит условий и должна требовать отдельного подтверждения широкого правила.",
    "Отсутствующий товар, неверный производитель, неизвестный тип, неизвестная потребность и значение «Иногда» должны быть отклонены.",
    "Диапазон 630–100 должен быть отклонён как некорректный.",
    "Вторая одинаковая строка QA-DEK-BASKET-3P должна быть отмечена как повтор.",
    "На листе товаров повтор пары DEKraft + QA-DEK-MAIN-400-3 не должен создавать второй товар.",
  ]);
  await verifyAndSave(workbook, "05_DEKraft_ошибки_правил.xlsx", [
    { sheetName: "Товары", range: "A1:K3" },
    { sheetName: "Совместимость", range: "A1:J10" },
    { sheetName: "Инструкция", range: "A1:B11" },
  ]);
}

process.stdout.write(`${outputDir}\n`);
