import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useModulesStore } from '../state/modulesStore';
import apiClient from '../services/apiClient';

// Mock apiClient
vi.mock('../services/apiClient');

describe('Modules Store', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Reset store state
    const { setState } = useModulesStore;
    setState({ modules: [], isLoading: false });
  });

  it('should fetch modules successfully', async () => {
    const mockModules = [
      {
        id: 1,
        name: 'Auth Module',
        description: 'Authentication module',
        moduleVersions: [
          { id: 1, moduleId: 1, versionNumber: '1.0.0', releaseNotes: 'Initial release', isBreakingChange: false, releasedAt: new Date().toISOString() },
        ],
      },
      {
        id: 2,
        name: 'Payment Module',
        description: 'Payment processing module',
        moduleVersions: [],
      },
    ];

    vi.mocked(apiClient.get).mockResolvedValue({ data: mockModules });

    const { result } = renderHook(() => useModulesStore());

    await act(async () => {
      await result.current.fetchModules();
    });

    expect(result.current.modules).toHaveLength(2);
    expect(result.current.modules[0].name).toBe('Auth Module');
    expect(result.current.modules[0].moduleVersions).toHaveLength(1);
    expect(result.current.isLoading).toBe(false);
  });

  it('should create module successfully', async () => {
    const newModule = { name: 'New Module', description: 'Test module' };
    const createdModule = { id: 1, ...newModule, moduleVersions: [] };

    vi.mocked(apiClient.post).mockResolvedValue({ data: createdModule });
    vi.mocked(apiClient.get).mockResolvedValue({ data: [createdModule] });

    const { result } = renderHook(() => useModulesStore());

    await act(async () => {
      await result.current.createModule(newModule);
    });

    expect(apiClient.post).toHaveBeenCalledWith('/modules', newModule);
    expect(result.current.modules).toHaveLength(1);
    expect(result.current.modules[0].name).toBe('New Module');
  });

  it('should delete module successfully', async () => {
    const mockModules = [
      { id: 1, name: 'Module 1', description: 'Desc 1', moduleVersions: [] },
      { id: 2, name: 'Module 2', description: 'Desc 2', moduleVersions: [] },
    ];

    vi.mocked(apiClient.get).mockResolvedValue({ data: mockModules });
    vi.mocked(apiClient.delete).mockResolvedValue({ data: {} });

    const { result } = renderHook(() => useModulesStore());

    // Fetch initial data
    await act(async () => {
      await result.current.fetchModules();
    });

    expect(result.current.modules).toHaveLength(2);

    // Delete one module
    vi.mocked(apiClient.get).mockResolvedValue({ data: [mockModules[1]] });

    await act(async () => {
      await result.current.deleteModule(1);
    });

    expect(apiClient.delete).toHaveBeenCalledWith('/modules/1');
    expect(result.current.modules).toHaveLength(1);
    expect(result.current.modules[0].id).toBe(2);
  });

  it('should add version to module', async () => {
    const versionData = {
      version: '2.0.0',
      releaseNotes: 'Major update',
      changelog: '',
      demoCode: '',
      isBreakingChange: true,
      releasedAt: new Date().toISOString(),
    };

    const mockModules = [
      {
        id: 1,
        name: 'Module 1',
        description: 'Desc 1',
        moduleVersions: [
          {
            id: 1,
            moduleId: 1,
            version: '1.0.0',
            releaseNotes: 'Initial',
            changelog: '',
            demoCode: '',
            isBreakingChange: false,
            releasedAt: new Date().toISOString(),
          },
        ],
      },
    ];

    vi.mocked(apiClient.post).mockResolvedValue({ data: {} });
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockModules });

    const { result } = renderHook(() => useModulesStore());

    await act(async () => {
      await result.current.addVersion(1, versionData);
    });

    expect(apiClient.post).toHaveBeenCalledWith('/modules/1/versions', versionData);
  });

  it('should handle fetch errors gracefully', async () => {
    vi.mocked(apiClient.get).mockRejectedValue(new Error('Network error'));

    const { result } = renderHook(() => useModulesStore());

    await act(async () => {
      try {
        await result.current.fetchModules();
      } catch {
        // Error expected
      }
    });

    expect(result.current.modules).toHaveLength(0);
    expect(result.current.isLoading).toBe(false);
  });

  it('should set loading state during fetch', async () => {
    let resolvePromise: (value: unknown) => void;
    const delayedPromise = new Promise<unknown>((resolve) => {
      resolvePromise = resolve;
    });

    vi.mocked(apiClient.get).mockReturnValue(delayedPromise as unknown as Promise<unknown>);

    const { result } = renderHook(() => useModulesStore());

    // Start fetching
    void act(() => {
      void result.current.fetchModules();
    });

    // Wait a tick for state to update
    await new Promise((resolve) => setTimeout(resolve, 0));

    // Check loading state is true during fetch
    expect(result.current.isLoading).toBe(true);

    // Resolve the promise
    await act(async () => {
      resolvePromise!({ data: [] });
      await delayedPromise;
    });

    // Check loading state is false after fetch
    expect(result.current.isLoading).toBe(false);
  });
});
