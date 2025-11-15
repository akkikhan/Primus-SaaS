import React from 'react';
import './Skeleton.css';

interface SkeletonProps {
  variant?: 'text' | 'rect' | 'circle' | 'card';
  width?: string | number;
  height?: string | number;
  count?: number;
}

export const Skeleton: React.FC<SkeletonProps> = ({ 
  variant = 'text', 
  width = '100%', 
  height = variant === 'text' ? '1em' : '100%',
  count = 1 
}) => {
  const getStyle = () => {
    const style: Record<string, string> = {};
    if (width) {
      style.width = typeof width === 'number' ? `${width}px` : width;
    }
    if (height) {
      style.height = typeof height === 'number' ? `${height}px` : height;
    }
    return style;
  };

  const skeletons = Array.from({ length: count }, (_, i) => (
    <div
      key={i}
      className={`skeleton skeleton-${variant}`}
      style={getStyle()}
    />
  ));

  return <>{skeletons}</>;
};

// Specialized skeleton components for common patterns
export const SkeletonCard: React.FC = () => (
  <div className="skeleton-card">
    <Skeleton variant="rect" height={120} />
    <div className="skeleton-card-content">
      <Skeleton variant="text" width="70%" height="1.5em" />
      <Skeleton variant="text" width="90%" height="1em" />
      <Skeleton variant="text" width="60%" height="1em" />
    </div>
  </div>
);

export const SkeletonTable: React.FC<{ rows?: number }> = ({ rows = 5 }) => (
  <div className="skeleton-table">
    <div className="skeleton-table-header">
      <Skeleton variant="text" width="15%" />
      <Skeleton variant="text" width="40%" />
      <Skeleton variant="text" width="15%" />
      <Skeleton variant="text" width="15%" />
      <Skeleton variant="text" width="15%" />
    </div>
    {Array.from({ length: rows }, (_, i) => (
      <div key={i} className="skeleton-table-row">
        <Skeleton variant="text" width="15%" />
        <Skeleton variant="text" width="40%" />
        <Skeleton variant="text" width="15%" />
        <Skeleton variant="text" width="15%" />
        <Skeleton variant="rect" width="80px" height="32px" />
      </div>
    ))}
  </div>
);

export const SkeletonStats: React.FC = () => (
  <div className="skeleton-stats">
    <Skeleton variant="text" width="40%" height="1.2em" />
    <Skeleton variant="text" width="60%" height="2.5em" />
    <Skeleton variant="text" width="50%" height="0.9em" />
  </div>
);
