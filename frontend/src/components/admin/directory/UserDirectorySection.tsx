import { useState } from 'react';
import { AttributeMappingsSection } from './AttributeMappingsSection';
import { DirectoryForm } from './DirectoryForm';
import { DirectoryList } from './DirectoryList';
import type { DirectoryDto } from '../../../api/types';

type View =
  | { kind: 'list' }
  | { kind: 'form'; directory: DirectoryDto | null }
  | { kind: 'attributeMappings'; directory: DirectoryDto };

/** Uygulamada router yok; bu bölümün alt ekranları yerel görünüm durumuyla yönetilir. */
export function UserDirectorySection() {
  const [view, setView] = useState<View>({ kind: 'list' });

  if (view.kind === 'form') {
    return <DirectoryForm directory={view.directory} onClose={() => setView({ kind: 'list' })} />;
  }

  if (view.kind === 'attributeMappings') {
    return (
      <AttributeMappingsSection directory={view.directory} onBack={() => setView({ kind: 'list' })} />
    );
  }

  return (
    <DirectoryList
      onAdd={() => setView({ kind: 'form', directory: null })}
      onEdit={(directory) => setView({ kind: 'form', directory })}
      onViewAttributeMappings={(directory) => setView({ kind: 'attributeMappings', directory })}
    />
  );
}
