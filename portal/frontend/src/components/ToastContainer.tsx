import React from 'react';
import { useUIStore } from '../state/uiStore';
import './ToastContainer.css';

const ToastContainer: React.FC = () => {
  const { toasts, removeToast } = useUIStore();

  if (toasts.length === 0) return null;

  return (
    <div className="toast-container">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`toast toast-${toast.type}`}
          onClick={() => removeToast(toast.id)}
        >
          <div className="toast-icon">
            {toast.type === 'success' && 'OK'}
            {toast.type === 'error' && 'ERR'}
            {toast.type === 'warning' && '!'}
            {toast.type === 'info' && 'INFO'}
          </div>
          <div className="toast-message">{toast.message}</div>
          <button
            className="toast-close"
            onClick={(e) => {
              e.stopPropagation();
              removeToast(toast.id);
            }}
          >
            x
          </button>
        </div>
      ))}
    </div>
  );
};

export default ToastContainer;
