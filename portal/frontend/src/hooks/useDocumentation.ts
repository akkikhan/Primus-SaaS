import { useState, useEffect, useCallback } from 'react';
import { fetchDocumentation, DocumentationDto } from '../services/documentationService';

interface UseDocumentationReturn {
  documentation: DocumentationDto | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

/**
 * Custom hook to fetch and manage documentation data
 * @param applicationId - The ID of the application
 * @returns Documentation data, loading state, error state, and refetch function
 */
export const useDocumentation = (applicationId: string | undefined): UseDocumentationReturn => {
  const [documentation, setDocumentation] = useState<DocumentationDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const loadDocumentation = useCallback(async () => {
    if (!applicationId) {
      setError('No application ID provided');
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const data = await fetchDocumentation(applicationId);
      setDocumentation(data);
    } catch (err) {
      const fallbackMessage = err instanceof Error ? err.message : 'Failed to load documentation';
      // @ts-expect-error narrow axios-like shape for richer message without pulling axios here
      const messageFromResponse = err?.response?.data?.message;
      setError(messageFromResponse || fallbackMessage);
      setDocumentation(null);
    } finally {
      setLoading(false);
    }
  }, [applicationId]);

  useEffect(() => {
    void loadDocumentation();
  }, [loadDocumentation]);

  return {
    documentation,
    loading,
    error,
    refetch: loadDocumentation,
  };
};
