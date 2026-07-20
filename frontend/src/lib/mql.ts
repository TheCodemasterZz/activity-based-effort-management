/**
 * MQL — Mesainâme Query Language. Jira'nın JQL'i mantığında, work log kayıtlarını
 * serbest metinle filtrelemek için küçük bir sorgu dili: alan/operatör/değer
 * karşılaştırmaları AND/OR ile birleştirilebilir, parantezle gruplanabilir.
 *
 * Örnek: employee ~ "ayşe" AND hours > 5
 *        (project = "Awesome Frozen Shoes Projesi" OR customer = "Öztuna - Aclan") AND activityL1 = "Testing"
 */

export type MqlField = 'employee' | 'project' | 'customer' | 'activityL1' | 'activityL2' | 'hours' | 'description' | 'date';

export type MqlOp = '=' | '!=' | '~' | '!~' | '>' | '<' | '>=' | '<=';

export type MqlNode =
  | { type: 'and'; left: MqlNode; right: MqlNode }
  | { type: 'or'; left: MqlNode; right: MqlNode }
  | { type: 'compare'; field: MqlField; op: MqlOp; value: string | number };

export interface MqlRecord {
  employee: string;
  project: string;
  customer: string;
  activityL1: string;
  activityL2: string;
  hours: number;
  description: string;
  date: string;
}

export class MqlParseError extends Error {}

export const MQL_FIELD_INFO: { field: MqlField; label: string; aliases: string[]; kind: 'text' | 'number' | 'date' }[] = [
  { field: 'employee', label: 'Kişi', aliases: ['employee', 'kisi', 'kişi', 'person'], kind: 'text' },
  { field: 'project', label: 'Proje', aliases: ['project', 'proje'], kind: 'text' },
  { field: 'customer', label: 'Müşteri', aliases: ['customer', 'musteri', 'müşteri'], kind: 'text' },
  { field: 'activityL1', label: 'Activity L1', aliases: ['activityl1', 'aktivite1', 'activity1'], kind: 'text' },
  { field: 'activityL2', label: 'Activity L2', aliases: ['activityl2', 'aktivite2', 'activity2'], kind: 'text' },
  { field: 'hours', label: 'Saat', aliases: ['hours', 'saat'], kind: 'number' },
  { field: 'description', label: 'Açıklama', aliases: ['description', 'aciklama', 'açıklama'], kind: 'text' },
  { field: 'date', label: 'Tarih (yyyy-mm-dd)', aliases: ['date', 'tarih', 'workdate'], kind: 'date' },
];

const FIELD_BY_ALIAS = new Map<string, MqlField>();
for (const info of MQL_FIELD_INFO) {
  for (const alias of info.aliases) FIELD_BY_ALIAS.set(alias, info.field);
}

function normalizeField(raw: string): MqlField | null {
  return FIELD_BY_ALIAS.get(raw.toLocaleLowerCase('tr')) ?? null;
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

  constructor(tokens: Token[]) {
    this.tokens = tokens;
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
    const field = normalizeField(fieldToken.text);
    if (!field) {
      const known = MQL_FIELD_INFO.map((f) => f.field).join(', ');
      throw new MqlParseError(`Bilinmeyen alan: '${fieldToken.text}'. Kullanılabilir alanlar: ${known}`);
    }

    const opToken = this.expect('op');
    const valueToken = this.next();
    if (!valueToken || (valueToken.type !== 'word' && valueToken.type !== 'string')) {
      throw new MqlParseError(`'${fieldToken.text} ${opToken.text}' sonrası bir değer bekleniyor`);
    }

    let value: string | number = valueToken.text;
    if (field === 'hours') {
      const num = Number(valueToken.text);
      if (Number.isNaN(num)) throw new MqlParseError(`'hours' alanı için sayısal bir değer bekleniyor: '${valueToken.text}'`);
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

/** MQL metnini AST'ye çevirir; boş/geçersiz sorguda MqlParseError fırlatır. */
export function parseMqlQuery(input: string): MqlNode {
  const trimmed = input.trim();
  if (!trimmed) throw new MqlParseError('Boş sorgu');

  const tokens = tokenize(trimmed);
  const parser = new MqlParser(tokens);
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

function evaluateCompare(node: Extract<MqlNode, { type: 'compare' }>, record: MqlRecord): boolean {
  const info = MQL_FIELD_INFO.find((f) => f.field === node.field)!;

  if (info.kind === 'number') return compareOrdered(record[node.field] as number, node.op, node.value);
  if (info.kind === 'date') return compareOrdered(String(record[node.field]), node.op, String(node.value));
  return compareText(String(record[node.field]), node.op, node.value);
}

/** Bir MQL AST'sini tek bir kayıt (work log satırı) üzerinde değerlendirir. */
export function evaluateMql(node: MqlNode, record: MqlRecord): boolean {
  if (node.type === 'and') return evaluateMql(node.left, record) && evaluateMql(node.right, record);
  if (node.type === 'or') return evaluateMql(node.left, record) || evaluateMql(node.right, record);
  return evaluateCompare(node, record);
}
