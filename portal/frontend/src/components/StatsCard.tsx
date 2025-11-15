import type { ReactNode } from 'react';
import './StatsCard.css';

interface StatsCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon?: ReactNode;
}

export const StatsCard = ({ title, value, subtitle, icon }: StatsCardProps) => (
  <div className="stats-card">
    <div className="stats-card__icon">{icon}</div>
    <div className="stats-card__body">
      <span className="stats-card__title">{title}</span>
      <span className="stats-card__value">{value}</span>
      {subtitle && <span className="stats-card__subtitle">{subtitle}</span>}
    </div>
  </div>
);
