import { WorkLogForm, type WorkLogFormInitialValues, type WorkLogFormProps } from './WorkLogForm';
import { WORK_LOG_ENTRY_TYPE } from '../../api/types';

export type { WorkLogFormInitialValues };

type WorkLogFormModalProps = Omit<WorkLogFormProps, 'cancelLabel'>;

/** Bağımsız (tek sütun) Work Log / Plan ekleme-düzenleme modalı — kendi overlay ve başlığını
 * çizer, gerçek form mantığının tamamı paylaşımlı `WorkLogForm`'da yaşar (bkz. CellWorkLogsModal,
 * aynı formu iki sütunlu bir yerleşimde gömülü olarak kullanır). */
export function WorkLogFormModal({
  mode,
  workLogId,
  allowDateRange = true,
  initialValues,
  entryType = WORK_LOG_ENTRY_TYPE.Actual,
  onClose,
  onDeleted,
}: WorkLogFormModalProps) {
  const isPlanned = entryType === WORK_LOG_ENTRY_TYPE.Planned;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="max-h-[90vh] w-full max-w-md overflow-y-auto rounded-xl bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-800">
            {isPlanned
              ? mode === 'edit' ? 'Plan Düzenle' : 'Plan Ekle'
              : mode === 'edit' ? 'Work Log Düzenle' : 'Work Log Ekle'}
          </h2>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-600">
            ✕
          </button>
        </div>

        <WorkLogForm
          mode={mode}
          workLogId={workLogId}
          allowDateRange={allowDateRange}
          initialValues={initialValues}
          entryType={entryType}
          onClose={onClose}
          onDeleted={onDeleted}
        />
      </div>
    </div>
  );
}
