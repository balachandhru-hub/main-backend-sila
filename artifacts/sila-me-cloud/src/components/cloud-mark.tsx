import { Link } from 'wouter';
import logoUrl from '@/assets/sila-logo.png';

export function CloudMark({ compact = false }: { compact?: boolean }) {
  return (
    <Link
      href="/dashboard"
      className={`cloud-mark ${compact ? 'cloud-mark--compact' : ''}`}
      data-testid="link-cloud-mark"
      aria-label="SILA ME Cloud home"
    >
      <img src={logoUrl} alt="SILA" className="cloud-mark__image" />
    </Link>
  );
}