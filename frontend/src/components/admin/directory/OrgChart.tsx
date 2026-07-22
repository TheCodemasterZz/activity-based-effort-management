import { useCallback, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { useOrgChart } from '../../../hooks/useDirectories';
import type { OrgChartNodeDto } from '../../../api/types';

interface OrgChartProps {
  directoryId: string;
  onSelectUser: (userId: string) => void;
}

interface TreeNode {
  id: string;
  username: string;
  displayName: string;
  photoBase64: string | null;
  children: TreeNode[];
  /**
   * Yönetici, bu dizinin senkronizasyon filtresi dışında kaldığı için sistemde kaydı olmayan
   * (tıklanamaz) bir yer tutucu düğüm. Aynı isimdeki dış yönetici için tek kutu paylaşılır,
   * böylece o yöneticiye bağlı herkes hiyerarşide doğru yerde görünür.
   */
  isExternal: boolean;
}

const MIN_SCALE = 0.2;
const MAX_SCALE = 2;

/**
 * managerId dolu olan düğümler gerçek yöneticisinin altına yerleşir. managerId boş ama
 * unresolvedManagerName doluysa (yönetici sistemde senkronize değil), o isim için paylaşılan
 * tıklanamaz bir yer tutucu kök oluşturulur. Hiçbiri yoksa düğüm gerçekten köktür.
 */
function buildForest(nodes: OrgChartNodeDto[]): TreeNode[] {
  const byId = new Map<string, TreeNode>(
    nodes.map((n) => [
      n.id,
      {
        id: n.id,
        username: n.username,
        displayName: n.displayName,
        photoBase64: n.photoBase64,
        children: [],
        isExternal: false,
      },
    ]),
  );
  const externalByName = new Map<string, TreeNode>();
  const roots: TreeNode[] = [];

  for (const original of nodes) {
    const node = byId.get(original.id)!;
    const manager = original.managerId ? byId.get(original.managerId) : undefined;

    if (manager && manager.id !== node.id) {
      manager.children.push(node);
      continue;
    }

    if (original.unresolvedManagerName) {
      const key = original.unresolvedManagerName.trim().toLowerCase();
      let external = externalByName.get(key);
      if (!external) {
        external = {
          id: `external:${key}`,
          username: '',
          displayName: original.unresolvedManagerName,
          photoBase64: null,
          children: [],
          isExternal: true,
        };
        externalByName.set(key, external);
        roots.push(external);
      }
      external.children.push(node);
      continue;
    }

    roots.push(node);
  }

  return roots;
}

function countDescendants(node: TreeNode): number {
  return node.children.reduce((sum, child) => sum + 1 + countDescendants(child), 0);
}

/**
 * Büyük organizasyonlarda (yüzlerce kişi) tüm ağacı açık göstermek okunaksız bir yığın
 * oluşturur. Varsayılan olarak yalnızca kök(ler) ve doğrudan bağlı kişiler açık gelir; bir
 * kademe altındaki her şey daraltılmış başlar, kullanıcı istediği dalı kendisi genişletir.
 */
const DEFAULT_EXPANDED_DEPTH = 1;

function getDefaultCollapsedIds(forest: TreeNode[]): Set<string> {
  const ids = new Set<string>();

  const visit = (nodes: TreeNode[], depth: number) => {
    for (const node of nodes) {
      if (depth === DEFAULT_EXPANDED_DEPTH && node.children.length > 0) {
        ids.add(node.id);
        continue;
      }
      if (depth < DEFAULT_EXPANDED_DEPTH) {
        visit(node.children, depth + 1);
      }
    }
  };

  visit(forest, 0);
  return ids;
}

function Avatar({
  node,
  tone,
}: {
  node: Pick<TreeNode, 'displayName' | 'photoBase64'>;
  tone: 'root' | 'default' | 'external';
}) {
  const ring = tone === 'root' ? 'ring-2 ring-white/60' : 'ring-1 ring-slate-200';
  if (node.photoBase64) {
    return (
      <img
        src={`data:image/jpeg;base64,${node.photoBase64}`}
        alt=""
        className={`h-10 w-10 shrink-0 rounded-full object-cover ${ring}`}
      />
    );
  }
  return (
    <div
      className={
        `flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-sm font-semibold ${ring} ` +
        (tone === 'root'
          ? 'bg-white/20 text-white'
          : tone === 'external'
            ? 'bg-slate-200 text-slate-400'
            : 'bg-slate-100 text-slate-400')
      }
    >
      {node.displayName.charAt(0).toUpperCase()}
    </div>
  );
}

function OrgChartCard({
  node,
  depth,
  collapsed,
  onToggleCollapse,
  onSelectUser,
}: {
  node: TreeNode;
  depth: number;
  collapsed: Set<string>;
  onToggleCollapse: (nodeId: string) => void;
  onSelectUser: (userId: string) => void;
}) {
  const hasChildren = node.children.length > 0;
  const isCollapsed = collapsed.has(node.id);
  const isRoot = depth === 0;
  const tone = node.isExternal ? 'external' : isRoot ? 'root' : 'default';

  return (
    <li>
      <div className="relative flex flex-col items-center">
        {node.isExternal ? (
          <div
            title="Bu kişi dizinin senkronizasyon filtresi dışında kaldığı için sistemde kaydı yok."
            className="flex w-44 cursor-default flex-col items-center gap-1.5 rounded-xl border border-dashed border-slate-300 bg-slate-50 px-3 py-3 text-center"
          >
            <Avatar node={node} tone={tone} />
            <span className="min-w-0">
              <span className="block truncate text-sm font-semibold text-slate-500">
                {node.displayName}
              </span>
              <span className="block truncate text-xs text-slate-400">Dizin dışı</span>
            </span>
          </div>
        ) : (
          <button
            type="button"
            onClick={() => onSelectUser(node.id)}
            className={
              'flex w-44 flex-col items-center gap-1.5 rounded-xl px-3 py-3 text-center shadow-sm transition-shadow hover:shadow-md ' +
              (isRoot
                ? 'bg-indigo-600 text-white'
                : 'border border-slate-200 bg-white text-slate-800 hover:border-indigo-300')
            }
          >
            <Avatar node={node} tone={tone} />
            <span className="min-w-0">
              <span className={'block truncate text-sm font-semibold ' + (isRoot ? 'text-white' : 'text-slate-800')}>
                {node.displayName}
              </span>
              <span className={'block truncate text-xs ' + (isRoot ? 'text-indigo-100' : 'text-slate-400')}>
                {node.username}
              </span>
            </span>
          </button>
        )}

        {hasChildren && (
          <button
            type="button"
            onClick={() => onToggleCollapse(node.id)}
            className="relative z-10 -mt-2.5 flex h-5 items-center justify-center rounded-full border border-slate-300 bg-white px-2 text-xs font-medium text-slate-500 shadow-sm hover:border-indigo-300 hover:text-indigo-600"
            aria-label={isCollapsed ? 'Dalı genişlet' : 'Dalı daralt'}
          >
            {isCollapsed ? `+${countDescendants(node)}` : '−'}
          </button>
        )}
      </div>

      {hasChildren && !isCollapsed && (
        <ul>
          {node.children.map((child) => (
            <OrgChartCard
              key={child.id}
              node={child}
              depth={depth + 1}
              collapsed={collapsed}
              onToggleCollapse={onToggleCollapse}
              onSelectUser={onSelectUser}
            />
          ))}
        </ul>
      )}
    </li>
  );
}

interface Transform {
  scale: number;
  x: number;
  y: number;
}

/** Fare tekerleğiyle zoom, sürükleyerek kaydırma ve "Sığdır" ile ekrana otomatik ölçekleme. */
function ZoomPanCanvas({ children }: { children: React.ReactNode }) {
  const containerRef = useRef<HTMLDivElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);
  const [transform, setTransform] = useState<Transform>({ scale: 1, x: 0, y: 0 });
  const isPanning = useRef(false);
  const panOrigin = useRef({ x: 0, y: 0, tx: 0, ty: 0 });

  const fitToScreen = useCallback(() => {
    const container = containerRef.current;
    const content = contentRef.current;
    if (!container || !content) return;

    const previousTransform = content.style.transform;
    content.style.transform = 'scale(1)';
    const contentWidth = content.scrollWidth;
    const contentHeight = content.scrollHeight;
    content.style.transform = previousTransform;

    const padding = 48;
    const scale = Math.min(
      (container.clientWidth - padding) / contentWidth,
      (container.clientHeight - padding) / contentHeight,
      1,
    );
    const clampedScale = Math.min(MAX_SCALE, Math.max(MIN_SCALE, scale));
    const x = (container.clientWidth - contentWidth * clampedScale) / 2;
    const y = (container.clientHeight - contentHeight * clampedScale) / 2;
    setTransform({ scale: clampedScale, x, y });
  }, []);

  useLayoutEffect(() => {
    fitToScreen();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const zoomBy = (factor: number) => {
    const container = containerRef.current;
    if (!container) return;
    const cx = container.clientWidth / 2;
    const cy = container.clientHeight / 2;
    setTransform((t) => {
      const newScale = Math.min(MAX_SCALE, Math.max(MIN_SCALE, t.scale * factor));
      const ratio = newScale / t.scale;
      return { scale: newScale, x: cx - (cx - t.x) * ratio, y: cy - (cy - t.y) * ratio };
    });
  };

  const handleWheel = (e: React.WheelEvent<HTMLDivElement>) => {
    e.preventDefault();
    const container = containerRef.current;
    if (!container) return;
    const rect = container.getBoundingClientRect();
    const cursorX = e.clientX - rect.left;
    const cursorY = e.clientY - rect.top;
    const factor = e.deltaY > 0 ? 0.92 : 1.08;
    setTransform((t) => {
      const newScale = Math.min(MAX_SCALE, Math.max(MIN_SCALE, t.scale * factor));
      const ratio = newScale / t.scale;
      return { scale: newScale, x: cursorX - (cursorX - t.x) * ratio, y: cursorY - (cursorY - t.y) * ratio };
    });
  };

  const handlePointerDown = (e: React.PointerEvent<HTMLDivElement>) => {
    // Bir karta veya daraltma düğmesine tıklanıyorsa kaydırmayı başlatma — aksi halde
    // pointer capture, tıklamanın altındaki düğmeye ulaşmasını engelliyor.
    if ((e.target as HTMLElement).closest('button')) return;

    isPanning.current = true;
    panOrigin.current = { x: e.clientX, y: e.clientY, tx: transform.x, ty: transform.y };
    e.currentTarget.setPointerCapture(e.pointerId);
  };

  const handlePointerMove = (e: React.PointerEvent<HTMLDivElement>) => {
    if (!isPanning.current) return;
    const dx = e.clientX - panOrigin.current.x;
    const dy = e.clientY - panOrigin.current.y;
    setTransform((t) => ({ ...t, x: panOrigin.current.tx + dx, y: panOrigin.current.ty + dy }));
  };

  const handlePointerUp = () => {
    isPanning.current = false;
  };

  return (
    <div className="relative h-[65vh] min-h-[420px] overflow-hidden rounded-xl border border-slate-200 bg-slate-50">
      <div
        ref={containerRef}
        onWheel={handleWheel}
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerUp}
        className="h-full w-full cursor-grab touch-none active:cursor-grabbing"
        style={{
          backgroundImage: 'radial-gradient(rgb(203 213 225) 1px, transparent 1px)',
          backgroundSize: '20px 20px',
        }}
      >
        <div
          ref={contentRef}
          className="inline-block"
          style={{
            transform: `translate(${transform.x}px, ${transform.y}px) scale(${transform.scale})`,
            transformOrigin: '0 0',
          }}
        >
          {children}
        </div>
      </div>

      <div className="absolute bottom-3 right-3 flex items-center gap-1 rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
        <button
          type="button"
          onClick={() => zoomBy(0.85)}
          className="flex h-7 w-7 items-center justify-center rounded-md text-slate-500 hover:bg-slate-100"
          aria-label="Uzaklaştır"
        >
          −
        </button>
        <button
          type="button"
          onClick={fitToScreen}
          className="rounded-md px-2 text-xs font-medium text-slate-500 hover:bg-slate-100"
        >
          Sığdır
        </button>
        <button
          type="button"
          onClick={() => zoomBy(1.15)}
          className="flex h-7 w-7 items-center justify-center rounded-md text-slate-500 hover:bg-slate-100"
          aria-label="Yakınlaştır"
        >
          +
        </button>
      </div>
    </div>
  );
}

export function OrgChart({ directoryId, onSelectUser }: OrgChartProps) {
  const orgChart = useOrgChart(directoryId);
  const forest = useMemo(() => buildForest(orgChart.data?.nodes ?? []), [orgChart.data]);
  const defaultCollapsed = useMemo(() => getDefaultCollapsedIds(forest), [forest]);
  const [collapsedOverride, setCollapsedOverride] = useState<Set<string> | null>(null);
  const collapsed = collapsedOverride ?? defaultCollapsed;

  const toggleCollapse = (nodeId: string) => {
    setCollapsedOverride((prev) => {
      const next = new Set(prev ?? defaultCollapsed);
      if (next.has(nodeId)) {
        next.delete(nodeId);
      } else {
        next.add(nodeId);
      }
      return next;
    });
  };

  if (orgChart.isLoading) {
    return <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>;
  }

  if (!orgChart.data?.hasManagerMapping) {
    return (
      <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
        Organizasyon şeması çıkarılamıyor. AD Attributes bölümünden (Kullanıcı Klasörü → ilgili dizin)
        "Kullanıcı" tipinde bir Yönetici alanı tanımlayıp dizini yeniden senkronize edin.
      </div>
    );
  }

  if (forest.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
        Bu dizinde henüz kullanıcı yok.
      </div>
    );
  }

  return (
    <div>
      <style>{`
        .org-chart-tree, .org-chart-tree ul { display: flex; list-style: none; margin: 0; padding: 0; }
        .org-chart-tree { text-align: center; }
        .org-chart-tree li {
          display: flex;
          flex-direction: column;
          align-items: center;
          position: relative;
          padding: 2rem 0.75rem 0 0.75rem;
        }
        .org-chart-tree li::before, .org-chart-tree li::after {
          content: '';
          position: absolute;
          top: 0;
          right: 50%;
          border-top: 2px solid rgb(148 163 184);
          width: 50%;
          height: 2rem;
        }
        .org-chart-tree li::after {
          right: auto;
          left: 50%;
          border-left: 2px solid rgb(148 163 184);
        }
        .org-chart-tree li:only-child::after, .org-chart-tree li:only-child::before { display: none; }
        .org-chart-tree > li { padding-top: 0; }
        .org-chart-tree > li::before, .org-chart-tree > li::after { display: none; }
        .org-chart-tree li:first-child::before, .org-chart-tree li:last-child::after { border: 0 none; }
        .org-chart-tree li:last-child::before { border-right: 2px solid rgb(148 163 184); border-radius: 0 6px 0 0; }
        .org-chart-tree li:first-child::after { border-radius: 6px 0 0 0; }
        .org-chart-tree ul::before {
          content: '';
          position: absolute;
          top: 0;
          left: 50%;
          border-left: 2px solid rgb(148 163 184);
          width: 0;
          height: 2rem;
        }
      `}</style>

      <ZoomPanCanvas>
        <ul className="org-chart-tree p-6">
          {forest.map((root) => (
            <OrgChartCard
              key={root.id}
              node={root}
              depth={0}
              collapsed={collapsed}
              onToggleCollapse={toggleCollapse}
              onSelectUser={onSelectUser}
            />
          ))}
        </ul>
      </ZoomPanCanvas>

      <div className="mt-3 flex items-center justify-between text-xs text-slate-400">
        <span>
          {orgChart.data.nodes.length} kişi · Fare tekerleğiyle yakınlaştır, sürükleyerek kaydır,
          daraltma rozetleriyle dalları aç/kapat.
        </span>
        {forest.filter((root) => !root.isExternal).length > 1 && (
          <span>
            Birden fazla kök görünüyor — bazı çalışanların yöneticisi hiç atanmamış olabilir.
          </span>
        )}
      </div>
    </div>
  );
}
