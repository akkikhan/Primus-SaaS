import { create } from 'zustand';
import apiClient, { getErrorMessage } from '../services/apiClient';
import { useUIStore } from './uiStore';

export interface ModuleVersion {
  id: number;
  version: string;
  releaseNotes: string;
  changelog: string;
  demoCode: string;
  releasedAt: string;
  isBreakingChange: boolean;
  moduleId: number;
  supportedStacks?: string[];
}

export interface Module {
  id: number;
  name: string;
  description: string;
  moduleKey?: string;
  moduleVersions: ModuleVersion[];
  latestVersion?: string;
  latestReleasedAt?: string;
  usageCount?: number;
  status?: string;
}

interface ModulesState {
  modules: Module[];
  isLoading: boolean;
  error: string | null;
  fetchModules: () => Promise<void>;
  createModule: (module: Omit<Module, 'id' | 'moduleVersions'>) => Promise<void>;
  updateModule: (id: number, module: Partial<Module>) => Promise<void>;
  deleteModule: (id: number) => Promise<void>;
  addVersion: (moduleId: number, version: Omit<ModuleVersion, 'id' | 'moduleId'>) => Promise<void>;
}

export const useModulesStore = create<ModulesState>((set, get) => ({
  modules: [],
  isLoading: false,
  error: null,

  fetchModules: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.get('/modules');
      set({ modules: response.data, isLoading: false });
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to fetch modules: ${errorMessage}`);
    }
  },

  createModule: async (module) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post('/modules', module);
      set((state) => ({
        modules: [...state.modules, response.data],
        isLoading: false,
      }));
      useUIStore.getState().addToast('success', 'Module created successfully');
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to create module: ${errorMessage}`);
      throw error;
    }
  },

  updateModule: async (id, module) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.put(`/modules/${id}`, module);
      set((state) => ({
        modules: state.modules.map((m) => (m.id === id ? response.data : m)),
        isLoading: false,
      }));
      useUIStore.getState().addToast('success', 'Module updated successfully');
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to update module: ${errorMessage}`);
      throw error;
    }
  },

  deleteModule: async (id) => {
    set({ isLoading: true, error: null });
    try {
      await apiClient.delete(`/modules/${id}`);
      set((state) => ({
        modules: state.modules.filter((m) => m.id !== id),
        isLoading: false,
      }));
      useUIStore.getState().addToast('success', 'Module deleted successfully');
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to delete module: ${errorMessage}`);
      throw error;
    }
  },

  addVersion: async (moduleId, version) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post(`/modules/${moduleId}/versions`, version);
      set((state) => ({
        modules: state.modules.map((m) =>
          m.id === moduleId
            ? { ...m, moduleVersions: [...m.moduleVersions, response.data] }
            : m
        ),
        isLoading: false,
      }));
      useUIStore.getState().addToast('success', 'Version added successfully');
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to add version: ${errorMessage}`);
      throw error;
    }
  },
}));
