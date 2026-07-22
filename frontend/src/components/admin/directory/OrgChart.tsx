import { useMemo, useState } from 'react';
import { useOrgChart } from '../../../hooks/useDirectories';
import type { OrgChartNodeDto } from '../../../api/types';

interface OrgChartProps {
  directoryId: string;
  onSelectUser: (userId: string) => void;
}

interface TreeNode extends OrgChartNodeDto {
  children: TreeNode[];
}

/** managerId null olan veya yöneticisi bu dizinin senkron kapsamında olmayan kullanıcılar köktür. */
function buildForest(nodes: OrgChartNodeDto[]): TreeNode[] {
  const byId = new Map<string, TreeNode>(nodes.map((n) => [n.id, { ...n, children: [] }]));
  const roots: TreeNode[] = [];

  for (const node of byId.values()) {
    const manager = node.managerId ? byId.get(node.managerId) : undefined;
    if (manager && manager.id !== node.id) {
      manager.children.push(node);
    } else {
      roots.push(node);
    }
  }

  return roots;
}

function Avatar({ node }: { node: OrgChartNodeDto }) {
  if (node.photoBase64) {
    return (
      <img
        src={`data:image/jpeg;base64,${node.photoBase64}`}
        alt=""
        className="h-8 w-8 shrink-0 rounded-full object-cover ring-1 ring-slate-200"
      />
    );
  }
  return (
    <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-slate-100 text-xs font-semibold text-slate-400 ring-1 ring-slate-200">
      {node.displayName.charAt(0).toUpperCase()}
    </div>
  );
}

function OrgChartRow({
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

  return (
    <li>
      <div
        className={
          'flex items-center gap-2 py-1.5' + (depth > 0 ? ' border-l border-slate-200' : '')
        }
        style={{ paddingLeft: `${depth * 1.5}rem` }}
      >
        {hasChildren ? (
          <button
            type="button"
            onClick={() => onToggleCollapse(node.id)}
            className="flex h-5 w-5 shrink-0 items-center justify-center text-slate-400 hover:text-slate-600"
            aria-label={isCollapsed ? 'Dalı genişlet' : 'Dalı daralt'}
          >
            {isCollapsed ? '▸' : '▾'}
          </button>
        ) : (
          <span className="w-5 shrink-0" />
        )}

        <button
          type="button"
          onClick={() => onSelectUser(node.id)}
          className="flex min-w-0 flex-1 items-center gap-2 rounded-md px-2 py-1 text-left hover:bg-slate-50"
        >
          <Avatar node={node} />
          <span className="min-w-0">
            <span className="block truncate text-sm font-medium text-slate-800">
              {node.displayName}
            </span>
            <span className="block truncate text-xs text-slate-400">{node.username}</span>
          </span>
        </button>
      </div>

      {hasChildren && !isCollapsed && (
        <ul>
          {node.children.map((child) => (
            <OrgChartRow
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

export function OrgChart({ directoryId, onSelectUser }: OrgChartProps) {
  const orgChart = useOrgChart(directoryId);
  const forest = useMemo(() => buildForest(orgChart.data?.nodes ?? []), [orgChart.data]);
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());

  const toggleCollapse = (nodeId: string) => {
    setCollapsed((prev) => {
      const next = new Set(prev);
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
        Organizasyon şeması çıkarılamıyor. Alan Eşlemeleri bölümünden "Kullanıcı" tipinde bir
        Yönetici alanı tanımlayıp dizini yeniden senkronize edin.
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
      <ul>
        {forest.map((root) => (
          <OrgChartRow
            key={root.id}
            node={root}
            depth={0}
            collapsed={collapsed}
            onToggleCollapse={toggleCollapse}
            onSelectUser={onSelectUser}
          />
        ))}
      </ul>

      {forest.length > 1 && (
        <p className="mt-4 text-xs text-slate-400">
          Birden fazla kök görünüyor — bazı yöneticiler bu dizinin senkronizasyon filtresi dışında
          kalmış olabilir.
        </p>
      )}
    </div>
  );
}
