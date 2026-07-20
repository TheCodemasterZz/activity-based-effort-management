import { useEffect, useRef, useState } from 'react';
import { MQL_FIELD_INFO, MqlParseError, parseMqlQuery, type MqlField, type MqlNode } from '../../lib/mql';

interface MqlFilterInputProps {
  onApply: (ast: MqlNode | null) => void;
  /** Alan bazlı, otomatik tamamlamada önerilecek bilinen değerler (ör. gerçek çalışan isimleri). */
  fieldValues?: Partial<Record<MqlField, string[]>>;
}

const EXAMPLE = 'employee ~ "ayşe" AND hours > 5';

const OPERATORS_BY_LENGTH = ['!=', '!~', '>=', '<=', '=', '~', '>', '<'];

interface Suggestion {
  kind: 'field' | 'value';
  insertText: string;
  display: string;
  sub: string;
}

function findClauseStart(text: string, uptoIndex: number): number {
  const prefix = text.slice(0, uptoIndex);
  const parenIdx = prefix.lastIndexOf('(');
  let kwEnd = -1;
  const re = /\b(and|or)\b/gi;
  let m: RegExpExecArray | null;
  while ((m = re.exec(prefix))) {
    kwEnd = m.index + m[0].length;
  }
  return Math.max(parenIdx + 1, kwEnd, 0);
}

function findOperatorInClause(clauseText: string): { op: string; index: number } | null {
  for (let i = 0; i < clauseText.length; i++) {
    for (const op of OPERATORS_BY_LENGTH) {
      if (clauseText.startsWith(op, i)) return { op, index: i };
    }
  }
  return null;
}

function findWordStart(text: string, cursor: number): number {
  let start = cursor;
  while (start > 0 && !/[\s()]/.test(text[start - 1])) start--;
  return start;
}

function resolveFieldByText(raw: string) {
  const lower = raw.trim().toLocaleLowerCase('tr');
  return MQL_FIELD_INFO.find((f) => f.field.toLocaleLowerCase('tr') === lower || f.aliases.includes(lower));
}

function computeSuggestions(
  text: string,
  cursor: number,
  fieldValues: Partial<Record<MqlField, string[]>>,
): { range: { start: number; end: number }; items: Suggestion[] } {
  const clauseStart = findClauseStart(text, cursor);
  const clauseTextUpToCursor = text.slice(clauseStart, cursor);
  const opMatch = findOperatorInClause(clauseTextUpToCursor);

  if (!opMatch) {
    const wordStart = findWordStart(text, cursor);
    const word = text.slice(wordStart, cursor);
    if (word.length === 0) return { range: { start: wordStart, end: cursor }, items: [] };
    const lower = word.toLocaleLowerCase('tr');
    const items: Suggestion[] = MQL_FIELD_INFO.filter(
      (f) => f.field.toLocaleLowerCase('tr').startsWith(lower) || f.aliases.some((a) => a.startsWith(lower)),
    )
      .slice(0, 8)
      .map((f) => ({ kind: 'field', insertText: `${f.field} `, display: f.field, sub: f.label }));
    return { range: { start: wordStart, end: cursor }, items };
  }

  const operatorEndAbs = clauseStart + opMatch.index + opMatch.op.length;
  let valueStart = operatorEndAbs;
  while (valueStart < cursor && /\s/.test(text[valueStart])) valueStart++;

  const fieldPart = clauseTextUpToCursor.slice(0, opMatch.index).trim();
  const field = resolveFieldByText(fieldPart);
  if (!field) return { range: { start: valueStart, end: cursor }, items: [] };

  const values = fieldValues[field.field];
  if (!values || values.length === 0) return { range: { start: valueStart, end: cursor }, items: [] };

  const hasOpenQuote = text[valueStart] === '"' || text[valueStart] === "'";
  const contentStart = hasOpenQuote ? valueStart + 1 : valueStart;
  const partial = text.slice(contentStart, cursor);
  if (partial.length === 0) return { range: { start: contentStart, end: cursor }, items: [] };
  const lower = partial.toLocaleLowerCase('tr');
  const unique = Array.from(new Set(values));
  const items: Suggestion[] = unique
    .filter((v) => v.toLocaleLowerCase('tr').includes(lower))
    .sort((a, b) => {
      const aStarts = a.toLocaleLowerCase('tr').startsWith(lower) ? 0 : 1;
      const bStarts = b.toLocaleLowerCase('tr').startsWith(lower) ? 0 : 1;
      return aStarts - bStarts || a.localeCompare(b, 'tr');
    })
    .slice(0, 8)
    .map((v) => ({
      kind: 'value',
      insertText: hasOpenQuote ? `${v}" ` : `"${v}" `,
      display: v,
      sub: field.label,
    }));
  return { range: { start: contentStart, end: cursor }, items };
}

/** Jira'nın JQL'i mantığında, work log satırlarını serbest metin sorgusuyla filtrelemek için
 * arama çubuğu — "Mesainâme Query Language" (MQL). Enter'a basılana kadar tabloyu değiştirmez. */
export function MqlFilterInput({ onApply, fieldValues = {} }: MqlFilterInputProps) {
  const [text, setText] = useState('');
  const [cursorPos, setCursorPos] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [isHelpOpen, setIsHelpOpen] = useState(false);
  const [isAutocompleteOpen, setIsAutocompleteOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);
  const helpRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const suggestions = computeSuggestions(text, cursorPos, fieldValues);
  const clampedActiveIndex = Math.min(activeIndex, Math.max(suggestions.items.length - 1, 0));
  const showAutocomplete = isAutocompleteOpen && suggestions.items.length > 0;

  const acceptSuggestion = (item: Suggestion) => {
    const before = text.slice(0, suggestions.range.start);
    const after = text.slice(suggestions.range.end);
    const newText = before + item.insertText + after;
    const newCursor = (before + item.insertText).length;
    setText(newText);
    setIsAutocompleteOpen(false);
    setActiveIndex(0);
    requestAnimationFrame(() => {
      inputRef.current?.setSelectionRange(newCursor, newCursor);
      inputRef.current?.focus();
    });
  };

  const syncCursor = (el: HTMLInputElement) => {
    setCursorPos(el.selectionStart ?? el.value.length);
    setIsAutocompleteOpen(true);
    setActiveIndex(0);
  };

  useEffect(() => {
    if (!isHelpOpen) return;
    const handleClickOutside = (event: MouseEvent) => {
      if (helpRef.current && !helpRef.current.contains(event.target as Node)) {
        setIsHelpOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isHelpOpen]);

  const commit = (value: string) => {
    if (!value.trim()) {
      setError(null);
      onApply(null);
      return;
    }
    try {
      const ast = parseMqlQuery(value);
      setError(null);
      onApply(ast);
    } catch (err) {
      setError(err instanceof MqlParseError ? err.message : 'Sorgu ayrıştırılamadı.');
    }
  };

  const handleClear = () => {
    setText('');
    setCursorPos(0);
    setError(null);
    setIsAutocompleteOpen(false);
    onApply(null);
  };

  return (
    <div className="relative flex flex-col">
      <div
        className={
          'flex items-center gap-2 rounded-lg border bg-white px-2.5 py-1.5 ' +
          (error ? 'border-red-300' : 'border-slate-200')
        }
      >
        <span className="shrink-0 rounded bg-indigo-50 px-1.5 py-0.5 text-[10px] font-bold tracking-wide text-indigo-600">
          MQL
        </span>
        <input
          ref={inputRef}
          type="text"
          value={text}
          onChange={(e) => {
            setText(e.target.value);
            syncCursor(e.target);
          }}
          onClick={(e) => syncCursor(e.currentTarget)}
          onFocus={(e) => syncCursor(e.currentTarget)}
          onBlur={() => setIsAutocompleteOpen(false)}
          onKeyDown={(e) => {
            if (showAutocomplete) {
              if (e.key === 'ArrowDown') {
                e.preventDefault();
                setActiveIndex((i) => (i + 1) % suggestions.items.length);
                return;
              }
              if (e.key === 'ArrowUp') {
                e.preventDefault();
                setActiveIndex((i) => (i - 1 + suggestions.items.length) % suggestions.items.length);
                return;
              }
              if (e.key === 'Enter' || e.key === 'Tab') {
                e.preventDefault();
                acceptSuggestion(suggestions.items[clampedActiveIndex]);
                return;
              }
              if (e.key === 'Escape') {
                setIsAutocompleteOpen(false);
                return;
              }
            }
            if (e.key === 'Enter') commit(text);
          }}
          placeholder={`ör. ${EXAMPLE}`}
          className="min-w-0 flex-1 text-sm text-slate-700 placeholder:text-slate-400 focus:outline-none"
        />
        {text && (
          <button
            type="button"
            onClick={handleClear}
            className="shrink-0 text-slate-400 hover:text-red-600"
            aria-label="Sorguyu temizle"
          >
            ✕
          </button>
        )}
        <div ref={helpRef} className="relative shrink-0">
          <button
            type="button"
            onClick={() => {
              setIsHelpOpen((v) => !v);
              setIsAutocompleteOpen(false);
            }}
            className="flex h-5 w-5 items-center justify-center rounded-full bg-slate-100 text-xs font-semibold text-slate-500 hover:bg-slate-200"
            aria-label="MQL yardımı"
          >
            ?
          </button>

          {isHelpOpen && (
            <div className="absolute right-0 top-full z-30 mt-2 w-80 rounded-lg border border-slate-200 bg-white p-3 text-xs shadow-lg">
              <div className="mb-2 font-semibold text-slate-700">Mesainâme Query Language (MQL)</div>
              <div className="mb-2 text-slate-500">
                Jira JQL mantığında alan/operatör/değer koşullarını <code className="rounded bg-slate-100 px-1">AND</code>{' '}
                / <code className="rounded bg-slate-100 px-1">OR</code> ile birleştirin, parantezle gruplayın.
              </div>
              <div className="mb-1 font-semibold text-slate-600">Alanlar</div>
              <ul className="mb-2 space-y-0.5 text-slate-500">
                {MQL_FIELD_INFO.map((f) => (
                  <li key={f.field}>
                    <code className="rounded bg-slate-100 px-1 text-indigo-700">{f.field}</code> — {f.label}
                  </li>
                ))}
              </ul>
              <div className="mb-1 font-semibold text-slate-600">Operatörler</div>
              <div className="mb-2 text-slate-500">
                <code className="rounded bg-slate-100 px-1">=</code>{' '}
                <code className="rounded bg-slate-100 px-1">!=</code>{' '}
                <code className="rounded bg-slate-100 px-1">~</code> (içerir){' '}
                <code className="rounded bg-slate-100 px-1">!~</code>{' '}
                <code className="rounded bg-slate-100 px-1">&gt;</code>{' '}
                <code className="rounded bg-slate-100 px-1">&lt;</code>{' '}
                <code className="rounded bg-slate-100 px-1">&gt;=</code>{' '}
                <code className="rounded bg-slate-100 px-1">&lt;=</code>
              </div>
              <div className="mb-1 font-semibold text-slate-600">Örnek</div>
              <code className="block rounded bg-slate-100 px-1.5 py-1 text-indigo-700">{EXAMPLE}</code>
            </div>
          )}
        </div>
      </div>

      {showAutocomplete && (
        <div className="absolute left-0 top-full z-30 mt-1 w-full min-w-[16rem] overflow-hidden rounded-lg border border-slate-200 bg-white py-1 text-sm shadow-lg">
          {suggestions.items.map((item, index) => (
            <button
              key={`${item.kind}-${item.display}`}
              type="button"
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => acceptSuggestion(item)}
              className={
                'flex w-full items-center justify-between gap-3 px-3 py-1.5 text-left ' +
                (index === clampedActiveIndex ? 'bg-indigo-50' : 'hover:bg-slate-50')
              }
            >
              <span className="truncate text-slate-700">{item.display}</span>
              <span className="shrink-0 text-xs text-slate-400">{item.sub}</span>
            </button>
          ))}
        </div>
      )}

      {error && <div className="mt-1 text-xs text-red-600">{error}</div>}
    </div>
  );
}
