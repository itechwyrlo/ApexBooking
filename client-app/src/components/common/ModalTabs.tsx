export interface IModalTab {
  id: string
  label: string
}

interface IModalTabsProps {
  tabs: IModalTab[]
  activeTab: string
  onChange: (id: string) => void
}

/**
 * Reuses the app's existing scrollable underline-tab styling (`.tabs-scroll` /
 * `.tab-link`, see SettingsLayout) rather than Bootstrap's raw `.nav-tabs`, so
 * modal tabs look and behave the same as every other tab strip in the app.
 * Pair with a `key={activeTab}` wrapper using the `.list-fade-in` class
 * around each tab's content for the fade-in-on-switch transition.
 */
export function ModalTabs({ tabs, activeTab, onChange }: IModalTabsProps) {
  return (
    <div className="tabs-scroll mb-3" role="tablist">
      {tabs.map((tab) => (
        <button
          key={tab.id}
          type="button"
          role="tab"
          aria-selected={activeTab === tab.id}
          className={`tab-link ${activeTab === tab.id ? 'active' : ''}`.trim()}
          onClick={() => onChange(tab.id)}
        >
          {tab.label}
        </button>
      ))}
    </div>
  )
}
