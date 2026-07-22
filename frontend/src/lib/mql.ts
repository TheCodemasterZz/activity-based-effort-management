/**
 * MQL — Mesainâme Query Language. Work log kayıtlarını serbest metinle filtrelemek
 * için küçük bir sorgu dili: alan/operatör/değer
 * karşılaştırmaları AND/OR ile birleştirilebilir, parantezle gruplanabilir.
 *
 * Örnek: employee ~ "ayşe" AND hours > 5
 *        (project = "Awesome Frozen Shoes Projesi" OR project = "Rustic Metal Mouse Projesi") AND activityL1 = "Testing"
 */

export type MqlOp = '=' | '!=' | '~' | '!~' | '>' | '<' | '>=' | '<=';

export type MqlNode =
  | { type: 'and'; left: MqlNode; right: MqlNode }
  | { type: 'or'; left: MqlNode; right: MqlNode }
  | { type: 'compare'; field: string; op: MqlOp; value: string | number };

/** MQL bir kaydı (work log satırı, proje satırı, vb.) düz bir alan/değer haritası olarak görür —
 * hangi alanların var olduğu ve nasıl karşılaştırılacağı (`MqlFieldInfo.kind`) tamamen çağıran
 * ekrana bağlıdır (bkz. MQL_FIELD_INFO / ProjectsPage'teki proje alan şeması). */
export type MqlRecord = Record<string, string | number>;

export class MqlParseError extends Error {}

export interface MqlFieldInfo {
  field: string;
  label: string;
  aliases: string[];
  kind: 'text' | 'number' | 'date';
}

export type MqlField = 'employee' | 'project' | 'activityL1' | 'activityL2' | 'hours' | 'description' | 'date';

/** Work log ekranlarının (Report/PlanWork/CapacityManagement/PlanningAccuracy) varsayılan MQL
 * alan şeması. Yeni bir ekran farklı bir veri modeli için kendi MqlFieldInfo[] listesini
 * tanımlayıp parseMqlQuery/evaluateMql/MqlFilterInput'a geçebilir — parser/UI mantığı ortak,
 * sadece alan şeması ekrana özgüdür (bkz. ProjectsPage'teki proje alan listesi). */
export const MQL_FIELD_INFO: MqlFieldInfo[] = [
  { field: 'employee', label: 'Kişi', aliases: ['employee', 'kisi', 'kişi', 'person'], kind: 'text' },
  { field: 'project', label: 'Proje', aliases: ['project', 'proje'], kind: 'text' },
  { field: 'activityL1', label: 'Activity L1', aliases: ['activityl1', 'aktivite1', 'activity1'], kind: 'text' },
  { field: 'activityL2', label: 'Activity L2', aliases: ['activityl2', 'aktivite2', 'activity2'], kind: 'text' },
  { field: 'hours', label: 'Saat', aliases: ['hours', 'saat'], kind: 'number' },
  { field: 'description', label: 'Açıklama', aliases: ['description', 'aciklama', 'açıklama'], kind: 'text' },
  { field: 'date', label: 'Tarih (yyyy-mm-dd)', aliases: ['date', 'tarih', 'workdate'], kind: 'date' },
];

function buildFieldByAlias(fieldInfoList: MqlFieldInfo[]): Map<string, string> {
  const map = new Map<string, string>();
  for (const info of fieldInfoList) {
    for (const alias of info.aliases) map.set(alias, info.field);
  }
  return map;
}

type TokenType = 'word' | 'string' | 'op' | 'and' | 'or' | 'lparen' | 'rparen';
interface Token {
  type: TokenType;
  text: string;
}

function tokenize(input: string): Token[] {
  const tokens: Token[] = [];
  let i = 0;
  const n = input.length;

  while (i < n) {
    const c = input[i];

    if (/\s/.test(c)) {
      i++;
      continue;
    }
    if (c === '(') {
      tokens.push({ type: 'lparen', text: '(' });
      i++;
      continue;
    }
    if (c === ')') {
      tokens.push({ type: 'rparen', text: ')' });
      i++;
      continue;
    }
    if (c === '"' || c === "'") {
      const quote = c;
      let j = i + 1;
      let buf = '';
      while (j < n && input[j] !== quote) {
        buf += input[j];
        j++;
      }
      if (j >= n) throw new MqlParseError(`Kapatılmamış tırnak işareti: ${input.slice(i)}`);
      tokens.push({ type: 'string', text: buf });
      i = j + 1;
      continue;
    }
    if (input.startsWith('!=', i)) {
      tokens.push({ type: 'op', text: '!=' });
      i += 2;
      continue;
    }
    if (input.startsWith('>=', i)) {
      tokens.push({ type: 'op', text: '>=' });
      i += 2;
      continue;
    }
    if (input.startsWith('<=', i)) {
      tokens.push({ type: 'op', text: '<=' });
      i += 2;
      continue;
    }
    if (input.startsWith('!~', i)) {
      tokens.push({ type: 'op', text: '!~' });
      i += 2;
      continue;
    }
    if (c === '=' || c === '>' || c === '<' || c === '~') {
      tokens.push({ type: 'op', text: c });
      i++;
      continue;
    }

    let j = i;
    let buf = '';
    while (j < n && !/[\s()=<>~!]/.test(input[j])) {
      buf += input[j];
      j++;
    }
    if (buf.length === 0) throw new MqlParseError(`Beklenmeyen karakter: '${c}'`);

    const lower = buf.toLocaleLowerCase('tr');
    if (lower === 'and') tokens.push({ type: 'and', text: buf });
    else if (lower === 'or') tokens.push({ type: 'or', text: buf });
    else tokens.push({ type: 'word', text: buf });
    i = j;
  }

  return tokens;
}

class MqlParser {
  private pos = 0;
  private tokens: Token[];
  private fieldInfoList: MqlFieldInfo[];
  private fieldByAlias: Map<string, string>;

  constructor(tokens: Token[], fieldInfoList: MqlFieldInfo[]) {
    this.tokens = tokens;
    this.fieldInfoList = fieldInfoList;
    this.fieldByAlias = buildFieldByAlias(fieldInfoList);
  }

  private peek(): Token | undefined {
    return this.tokens[this.pos];
  }

  private next(): Token | undefined {
    return this.tokens[this.pos++];
  }

  private expect(type: TokenType): Token {
    const token = this.next();
    if (!token || token.type !== type) {
      throw new MqlParseError(`Beklenmeyen ifade: '${token?.text ?? 'sorgu sonu'}'`);
    }
    return token;
  }

  parseExpr(): MqlNode {
    return this.parseOr();
  }

  private parseOr(): MqlNode {
    let left = this.parseAnd();
    while (this.peek()?.type === 'or') {
      this.next();
      const right = this.parseAnd();
      left = { type: 'or', left, right };
    }
    return left;
  }

  private parseAnd(): MqlNode {
    let left = this.parseTerm();
    while (this.peek()?.type === 'and') {
      this.next();
      const right = this.parseTerm();
      left = { type: 'and', left, right };
    }
    return left;
  }

  private parseTerm(): MqlNode {
    if (this.peek()?.type === 'lparen') {
      this.next();
      const inner = this.parseExpr();
      this.expect('rparen');
      return inner;
    }
    return this.parseComparison();
  }

  private parseComparison(): MqlNode {
    const fieldToken = this.expect('word');
    const field = this.fieldByAlias.get(fieldToken.text.toLocaleLowerCase('tr'));
    if (!field) {
      const known = this.fieldInfoList.map((f) => f.field).join(', ');
      throw new MqlParseError(`Bilinmeyen alan: '${fieldToken.text}'. Kullanılabilir alanlar: ${known}`);
    }

    const opToken = this.expect('op');
    const valueToken = this.next();
    if (!valueToken || (valueToken.type !== 'word' && valueToken.type !== 'string')) {
      throw new MqlParseError(`'${fieldToken.text} ${opToken.text}' sonrası bir değer bekleniyor`);
    }

    let value: string | number = valueToken.text;
    const fieldInfo = this.fieldInfoList.find((f) => f.field === field)!;
    if (fieldInfo.kind === 'number') {
      const num = Number(valueToken.text);
      if (Number.isNaN(num)) throw new MqlParseError(`'${field}' alanı için sayısal bir değer bekleniyor: '${valueToken.text}'`);
      value = num;
    }

    return { type: 'compare', field, op: opToken.text as MqlOp, value };
  }

  finish(): void {
    if (this.pos < this.tokens.length) {
      throw new MqlParseError(`Beklenmeyen ifade: '${this.peek()!.text}'`);
    }
  }
}

/** MQL metnini AST'ye çevirir; boş/geçersiz sorguda MqlParseError fırlatır. `fieldInfoList`
 * verilmezse work log ekranlarının varsayılan alan şeması (MQL_FIELD_INFO) kullanılır. */
export function parseMqlQuery(input: string, fieldInfoList: MqlFieldInfo[] = MQL_FIELD_INFO): MqlNode {
  const trimmed = input.trim();
  if (!trimmed) throw new MqlParseError('Boş sorgu');

  const tokens = tokenize(trimmed);
  const parser = new MqlParser(tokens, fieldInfoList);
  const node = parser.parseExpr();
  parser.finish();
  return node;
}

function compareText(raw: string, op: MqlOp, value: string | number): boolean {
  const rawStr = raw.toLocaleLowerCase('tr');
  const valStr = String(value).toLocaleLowerCase('tr');
  switch (op) {
    case '=':
      return rawStr === valStr;
    case '!=':
      return rawStr !== valStr;
    case '~':
      return rawStr.includes(valStr);
    case '!~':
      return !rawStr.includes(valStr);
    default:
      return false;
  }
}

function compareOrdered(raw: string | number, op: MqlOp, value: string | number): boolean {
  switch (op) {
    case '=':
      return raw === value;
    case '!=':
      return raw !== value;
    case '>':
      return raw > value;
    case '<':
      return raw < value;
    case '>=':
      return raw >= value;
    case '<=':
      return raw <= value;
    default:
      return false;
  }
}

function evaluateCompare(node: Extract<MqlNode, { type: 'compare' }>, record: MqlRecord, fieldInfoList: MqlFieldInfo[]): boolean {
  const info = fieldInfoList.find((f) => f.field === node.field)!;

  if (info.kind === 'number') return compareOrdered(record[node.field] as number, node.op, node.value);
  if (info.kind === 'date') return compareOrdered(String(record[node.field]), node.op, String(node.value));
  return compareText(String(record[node.field]), node.op, node.value);
}

/** Bir MQL AST'sini tek bir kayıt üzerinde değerlendirir. `fieldInfoList` verilmezse work log
 * ekranlarının varsayılan alan şeması (MQL_FIELD_INFO) kullanılır — ast bu şemayla parse
 * edilmediyse (ör. başka bir ekranın alan şemasıyla) burada da AYNI listeyi vermek gerekir. */
export function evaluateMql(node: MqlNode, record: MqlRecord, fieldInfoList: MqlFieldInfo[] = MQL_FIELD_INFO): boolean {
  if (node.type === 'and') return evaluateMql(node.left, record, fieldInfoList) && evaluateMql(node.right, record, fieldInfoList);
  if (node.type === 'or') return evaluateMql(node.left, record, fieldInfoList) || evaluateMql(node.right, record, fieldInfoList);
  return evaluateCompare(node, record, fieldInfoList);
}
