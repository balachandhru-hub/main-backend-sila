import { ArrowLeft, Compass } from 'lucide-react';
import { Link } from 'wouter';
import { CloudMark } from '@/components/cloud-mark';

export default function NotFound() {
  return (
    <main className="sila-not-found" data-testid="page-not-found">
      <div className="sila-not-found__inner">
        <CloudMark />
        <Compass size={32} strokeWidth={1.3} />
        <p className="eyebrow"><span className="eyebrow__mark" />Signal not found</p>
        <h1>This route is out of range.</h1>
        <p>The page you are looking for does not exist in this Cloud workspace.</p>
        <Link href="/dashboard" className="sila-button sila-button--primary" data-testid="link-back-dashboard">
          <ArrowLeft size={16} /> Return to overview
        </Link>
      </div>
    </main>
  );
}
