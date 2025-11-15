import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useApplicationsStore, type Application } from '../state/applicationsStore';
import apiClient from '../services/apiClient';

// Mock apiClient
vi.mock('../services/apiClient');

describe('Applications Store', () => {
  const createMockApplication = (overrides: Partial<Application> = {}): Application => ({
    id: overrides.id ?? 1,
    name: overrides.name ?? 'App 1',
    stack: overrides.stack ?? 'NodeJS',
    primusClientId: overrides.primusClientId ?? 'client-1',
    description: overrides.description ?? 'Test app',
    ownerEmail: overrides.ownerEmail ?? 'owner@example.com',
    moduleCount: overrides.moduleCount ?? 0,
    createdAt: overrides.createdAt ?? new Date().toISOString(),
    updatedAt: overrides.updatedAt,
    integratedModules: overrides.integratedModules ?? [],
  });

  beforeEach(() => {
    vi.clearAllMocks();
    // Reset store state
    const { setState } = useApplicationsStore;
    setState({ applications: [], currentApplication: null, isLoading: false, error: null });
  });

  it('should fetch applications successfully', async () => {
    const mockApplications = [
      createMockApplication({ id: 1, name: 'App 1', primusClientId: 'client-1' }),
      createMockApplication({ id: 2, name: 'App 2', primusClientId: 'client-2' }),
    ];

    vi.mocked(apiClient.get).mockResolvedValue({ data: mockApplications });

    const { result } = renderHook(() => useApplicationsStore());

    await act(async () => {
      await result.current.fetchApplications();
    });

    expect(result.current.applications).toHaveLength(2);
    expect(result.current.applications[0].name).toBe('App 1');
    expect(result.current.isLoading).toBe(false);
  });

  it('should create application successfully', async () => {
    const newApp = { name: 'New App', stack: 'NodeJS', primusClientId: 'new-client', description: 'Test app' };
    const createdApp = createMockApplication({ id: 1, ...newApp });

    vi.mocked(apiClient.post).mockResolvedValue({ data: createdApp });
    vi.mocked(apiClient.get).mockResolvedValue({ data: [createdApp] });

    const { result } = renderHook(() => useApplicationsStore());

    await act(async () => {
      await result.current.createApplication(newApp);
    });

    expect(apiClient.post).toHaveBeenCalledWith('/applications', newApp);
    expect(result.current.applications).toHaveLength(1);
    expect(result.current.applications[0].name).toBe('New App');
  });

  it('should delete application successfully', async () => {
    const mockApplications = [
      createMockApplication({ id: 1, name: 'App 1', primusClientId: 'client-1' }),
      createMockApplication({ id: 2, name: 'App 2', primusClientId: 'client-2' }),
    ];

    vi.mocked(apiClient.get).mockResolvedValue({ data: mockApplications });
    vi.mocked(apiClient.delete).mockResolvedValue({ data: {} });

    const { result } = renderHook(() => useApplicationsStore());

    // Fetch initial data
    await act(async () => {
      await result.current.fetchApplications();
    });

    expect(result.current.applications).toHaveLength(2);

    // Delete one application
    vi.mocked(apiClient.get).mockResolvedValue({ data: [mockApplications[1]] });

    await act(async () => {
      await result.current.deleteApplication(1);
    });

    expect(apiClient.delete).toHaveBeenCalledWith('/applications/1');
    expect(result.current.applications).toHaveLength(1);
    expect(result.current.applications[0].id).toBe(2);
  });

  it('should handle fetch errors gracefully', async () => {
    vi.mocked(apiClient.get).mockRejectedValue(new Error('Network error'));

    const { result } = renderHook(() => useApplicationsStore());

    await act(async () => {
      try {
        await result.current.fetchApplications();
      } catch (error) {
        // Error expected
      }
    });

    expect(result.current.applications).toHaveLength(0);
    expect(result.current.isLoading).toBe(false);
  });

  it('should add module to application', async () => {
    const mockApp = createMockApplication({ id: 1, primusClientId: 'client-1' });

    vi.mocked(apiClient.post).mockResolvedValue({ data: {} });
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockApp });

    const { result } = renderHook(() => useApplicationsStore());

    await act(async () => {
      await result.current.addModule(1, 10, 5);
    });

    expect(apiClient.post).toHaveBeenCalledWith('/applications/1/modules', {
      moduleId: 10,
      moduleVersionId: 5,
    });
  });

  it('should remove module from application', async () => {
    const mockApp = createMockApplication({ id: 1, primusClientId: 'client-1' });

    vi.mocked(apiClient.delete).mockResolvedValue({ data: {} });
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockApp });

    const { result } = renderHook(() => useApplicationsStore());

    await act(async () => {
      await result.current.removeModule(1, 10);
    });

    expect(apiClient.delete).toHaveBeenCalledWith('/applications/1/modules/10');
  });
});
