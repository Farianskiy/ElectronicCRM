import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const sourcePath = "C:/Users/Farianskiy/Downloads/допы чинт nm8-12.xlsx";
const rulesPath = "C:/Users/Farianskiy/Downloads/допы чинт-77 (1).xlsx";

async function rows(path, sheetName) {
  const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(path));
  const sheet = workbook.worksheets.getItem(sheetName);
  return sheet.getUsedRange(false).values;
}

const source = await rows(sourcePath, "Лист1");
const rules = await rows(rulesPath, "Правила совместимости");
const headers = source[0];
const data = source.slice(1);
const ruleHeaders = rules[0];
const ruleData = rules.slice(1);

const filled = Object.fromEntries(headers.map((header, index) => [header, data.filter((row) => row[index] !== null && row[index] !== "").length]));
const obviousPole = data.filter((row) => /(?:^|\s)([34])P(?:\s|$|\))/i.test(String(row[1])));
const obviousPoleFilled = obviousPole.filter((row) => row[4] !== null && row[4] !== "");
const wrongPole = obviousPoleFilled.filter((row) => {
  const expected = Number(String(row[1]).match(/(?:^|\s)([34])P(?:\s|$|\))/i)?.[1]);
  return Number(row[4]) !== expected;
});
const unexplainedPole = data.filter((row) => row[4] !== null && row[4] !== "" && !/(?:^|\s)([34])P(?:\s|$|\))/i.test(String(row[1])));
const releaseMention = data.filter((row) => /(?:^|[\s/(])(TM|EN|EM)(?:[\s/)]|$)/i.test(String(row[1])));
const releaseFilled = releaseMention.filter((row) => row[5] !== null && row[5] !== "");
const ruleArticles = new Set(ruleData.map((row) => String(row[0])));
const sourceArticles = new Set(data.map((row) => String(row[0])));
const rulesNotInSource = ruleData.filter((row) => !sourceArticles.has(String(row[0])));
const sourceNotInRules = data.filter((row) => !ruleArticles.has(String(row[0])));
const ruleFilled = Object.fromEntries(ruleHeaders.map((header, index) => [header, ruleData.filter((row) => row[index] !== null && row[index] !== "").length]));

console.log(JSON.stringify({
  sourceRows: data.length,
  sourceColumns: headers,
  filled,
  obviousPoleMentions: obviousPole.length,
  obviousPoleFilled: obviousPoleFilled.length,
  wrongPole: wrongPole.map((row) => ({ article: row[0], name: row[1], extracted: row[4] })),
  unexplainedPole: unexplainedPole.map((row) => ({ article: row[0], name: row[1], extracted: row[4] })),
  releaseMentions: releaseMention.length,
  releaseFilled: releaseFilled.length,
  rulesRows: ruleData.length,
  rulesColumns: ruleHeaders,
  ruleFilled,
  rulesNotInSource: rulesNotInSource.length,
  sourceNotInRules: sourceNotInRules.length,
  firstSourceNotInRules: sourceNotInRules.slice(0, 10).map((row) => row[0]),
}, null, 2));
