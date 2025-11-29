import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { vi } from 'vitest';
import { DocumentationPage } from '../pages/DocumentationPage';

vi.mock('../hooks/useDocumentation', () => {
  return {
    useDocumentation: (id: string) => {
      if (id === 'loading') {
        return { documentation: null, loading: true, error: null, refetch: vi.fn() };
      }
      if (id === 'error') {
        return { documentation: null, loading: false, error: 'boom', refetch: vi.fn() };
      }
      return {
        documentation: {
          applicationName: 'Demo App',
          primusClientId: 'client-123',
          stack: 'dotnet',
          generatedAt: new Date('2025-01-01T00:00:00Z').toISOString(),
          modules: [
            {
              moduleName: 'Identity',
              version: '1.0.0',
              isBreakingChange: false,
              releaseNotes: 'Fixed auth flow',
              integrationSteps: ['Add middleware', 'Configure issuer'],
              codeSnippets: { 'auth.ts': 'console.log("ok");' }
            }
          ]
        },
        loading: false,
        error: null,
        refetch: vi.fn()
      };
    }
  };
});

vi.mock('../services/docGenerator', () => ({
  generatePDF: vi.fn(),
  generateMarkdown: vi.fn(),
  generateJSON: vi.fn()
}));

const renderWithRoute = (id: string) =>
  render(
    <MemoryRouter initialEntries={[`/docs/${id}`]}>
      <Routes>
        <Route path="/docs/:id" element={<DocumentationPage />} />
      </Routes>
    </MemoryRouter>
  );

describe('DocumentationPage', () => {
  it('shows skeleton when loading', () => {
    renderWithRoute('loading');
    expect(document.querySelector('.skeleton-documentation')).toBeTruthy();
  });

  it('renders error state', () => {
    renderWithRoute('error');
    expect(screen.getByText(/Error Loading Documentation/i)).toBeInTheDocument();
    expect(screen.getByText('boom')).toBeInTheDocument();
  });

  it('renders documentation and export buttons', () => {
    renderWithRoute('ready');
    expect(screen.getByText('Demo App')).toBeInTheDocument();
    expect(screen.getByText(/client-123/i)).toBeInTheDocument();
    expect(screen.getByText('Identity')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Export PDF/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Export Markdown/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Export JSON/i })).toBeInTheDocument();
  });
});
