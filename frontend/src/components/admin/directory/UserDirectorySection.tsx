import { useState } from 'react';
import { DirectoryForm } from './DirectoryForm';
import { DirectoryList } from './DirectoryList';
import { DirectoryUserCard } from './DirectoryUserCard';
import { DirectoryUserList } from './DirectoryUserList';
import type { DirectoryDto } from '../../../api/types';

type View =
  | { kind: 'list' }
  | { kind: 'form'; directory: DirectoryDto | null }
  | { kind: 'users'; directory: DirectoryDto }
  | { kind: 'userDetail'; directory: DirectoryDto; userId: string };

/** Uygulamada router yok; bu bölümün alt ekranları yerel görünüm durumuyla yönetilir. */
export function UserDirectorySection() {
  const [view, setView] = useState<View>({ kind: 'list' });

  if (view.kind === 'form') {
    return <DirectoryForm directory={view.directory} onClose={() => setView({ kind: 'list' })} />;
  }

  if (view.kind === 'users') {
    return (
      <DirectoryUserList
        directory={view.directory}
        onBack={() => setView({ kind: 'list' })}
        onSelectUser={(userId) =>
          setView({ kind: 'userDetail', directory: view.directory, userId })
        }
      />
    );
  }

  if (view.kind === 'userDetail') {
    return (
      <DirectoryUserCard
        userId={view.userId}
        onBack={() => setView({ kind: 'users', directory: view.directory })}
        onSelectUser={(userId) => setView({ kind: 'userDetail', directory: view.directory, userId })}
      />
    );
  }

  return (
    <DirectoryList
      onAdd={() => setView({ kind: 'form', directory: null })}
      onEdit={(directory) => setView({ kind: 'form', directory })}
      onViewUsers={(directory) => setView({ kind: 'users', directory })}
    />
  );
}
