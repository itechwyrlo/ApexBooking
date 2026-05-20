import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import './Tabs.styles.css';

interface TabItem<T extends string> {
  id: T;
  label: string;
  icon?: IconDefinition;
  count?: number;
}

interface TabsProps<T extends string> {
  tabs: Array<TabItem<T>>;
  activeTab: T;
  onChange: (id: T) => void;
}

function Tabs<T extends string>({ tabs, activeTab, onChange }: TabsProps<T>) {
  return (
    <nav className="apex-tab-nav" role="tablist">
      {tabs.map((t) => (
        <button
          key={t.id}
          type="button"
          role="tab"
          aria-selected={activeTab === t.id}
          className={`apex-tab-btn${activeTab === t.id ? ' active' : ''}`}
          onClick={() => onChange(t.id)}
        >
          {t.icon && <FontAwesomeIcon icon={t.icon} />}
          <span className="apex-tab-label">{t.label}</span>
          {t.count !== undefined && (
            <span className="apex-tab-count">{t.count}</span>
          )}
        </button>
      ))}
    </nav>
  );
}

export { Tabs };
export type { TabItem };
