import { ArrowRight, Construction, MapPin, Search } from 'lucide-react';
import { useState } from 'react';
import { getGetAccessContextQueryKey, useGetAccessContext } from '@workspace/api-client-react';
import { QueryError, SilaDataTable, SilaPageHeader, StatusBadge, DevPendingMarker } from '@/components/sila-ui';

type Props = { title: string; description: string; eyebrow?: string; columns?: string[] };
const request = { credentials: 'include' as const };

export default function FuturePage({ title, description, eyebrow = 'Workspace module', columns = ['Area', 'Status', 'Next step'] }: Props) {
  const [search, setSearch] = useState('');
  const [tab, setTab] = useState('Overview');
  const [filtersOpen, setFiltersOpen] = useState(false);
  return (
    <>
      <SilaPageHeader eyebrow={eyebrow} title={title} description={description} actions={<span className="sila-status sila-status--warn" data-testid="status-api-integration-pending">API integration pending</span>} />
      <div className="sila-filter-row" data-testid="future-filter-row">
        <input className="sila-input" value={search} onChange={(event) => setSearch(event.target.value)} placeholder={`Search ${title.toLowerCase()}`} aria-label={`Search ${title}`} data-testid="input-future-search" />
        <button className={`sila-button ${filtersOpen ? 'sila-button--primary' : ''}`} type="button" onClick={() => setFiltersOpen((open) => !open)} data-testid="button-future-filter"><Search size={14} /> Filters</button>
        {search && <button className="sila-button sila-button--quiet" type="button" onClick={() => setSearch('')} data-testid="button-clear-future-search">Clear search</button>}
        <DevPendingMarker />
      </div>
      {filtersOpen && <div className="sila-card sila-filter-panel" data-testid="future-filter-panel"><label><span>View</span><select className="sila-select" data-testid="select-future-status"><option>All statuses</option><option>Needs attention</option><option>Ready for connection</option></select></label><button className="sila-button sila-button--quiet" type="button" onClick={() => setFiltersOpen(false)} data-testid="button-close-future-filter">Done</button></div>}
      <div className="sila-tabs" role="tablist" aria-label={`${title} views`} data-testid="future-tabs">
        {['Overview', 'Needs attention', 'History'].map((item) => <button type="button" role="tab" aria-selected={tab === item} className={`sila-tab ${tab === item ? 'sila-tab--active' : ''}`} onClick={() => setTab(item)} key={item} data-testid={`button-future-tab-${item.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`}>{item}</button>)}
      </div>
      <section className="sila-card" data-testid="future-module-shell">
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '18px 18px 2px' }}><Construction size={16} color="var(--sila-blue)" /><strong style={{ fontSize: 13 }}>Operating view ready for connection</strong></div>
        <SilaDataTable empty emptyTitle={search ? `No matches for “${search}”` : `No ${tab.toLowerCase()} records yet`} emptyDescription="No production endpoint is connected for this module. The shell is ready for the Cloud service when it is released." />
      </section>
      <div style={{ marginTop: 16, color: 'var(--sila-muted)', fontSize: 11 }} data-testid="text-future-module-note"><ArrowRight size={13} style={{ verticalAlign: 'middle', marginRight: 5 }} />{columns.join(' · ')}</div>
    </>
  );
}

export function FutureRoute(props: Props) {
  return <FuturePage {...props} />;
}

export function LocationsPage() {
  const query = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const [search, setSearch] = useState('');
  const units = query.data?.units ?? [];
  const normalizedSearch = search.trim().toLowerCase();
  const rows = units.filter((unit) => `${unit.name} ${unit.code} ${unit.kind} ${unit.status}`.toLowerCase().includes(normalizedSearch));

  return (
    <>
      <SilaPageHeader
        eyebrow="Master data / Locations"
        title="Locations"
        description="View the operating units currently available in your Cloud access scope."
        actions={<span className="sila-status sila-status--good" data-testid="status-api-connected">Connected</span>}
      />
      <div className="sila-filter-row" data-testid="locations-filter-row">
        <input
          className="sila-input"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search locations"
          aria-label="Search locations"
          data-testid="input-search-locations"
        />
        {search && <button className="sila-button sila-button--quiet" type="button" onClick={() => setSearch('')} data-testid="button-clear-location-search">Clear search</button>}
      </div>
      {query.isError ? (
        <QueryError onRetry={() => query.refetch()} />
      ) : (
        <SilaDataTable
          loading={query.isPending}
          empty={!query.isPending && rows.length === 0}
          emptyTitle={normalizedSearch ? 'No matching locations' : 'No locations in scope'}
          emptyDescription={normalizedSearch ? 'Try a different name, code, or location type.' : 'No operating units are available in your current Cloud access scope.'}
        >
          <table className="sila-table">
            <thead>
              <tr><th>Location</th><th>Code</th><th>Type</th><th>Status</th></tr>
            </thead>
            <tbody>
              {rows.map((unit) => (
                <tr key={unit.id}>
                  <td><MapPin size={14} style={{ verticalAlign: 'middle', marginRight: 7 }} /><strong>{unit.name}</strong></td>
                  <td>{unit.code}</td>
                  <td>{unit.kind.replaceAll('_', ' ')}</td>
                  <td><StatusBadge value={unit.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </SilaDataTable>
      )}
      <div style={{ marginTop: 16, color: 'var(--sila-muted)', fontSize: 11 }} data-testid="text-locations-source">
        <ArrowRight size={13} style={{ verticalAlign: 'middle', marginRight: 5 }} />Operating units from the authenticated access context
      </div>
    </>
  );
}
