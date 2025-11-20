import { create } from 'zustand';
import apiClient, { getErrorMessage } from '../services/apiClient';
import { useUIStore } from './uiStore';

export interface IntegratedModule {
  moduleId: number;
  moduleName: string;
  version: string;
  latestVersion: string;
  versionStatus: string; // "UpToDate" or "UpdateAvailable"
  isBreakingChange: boolean;
  releaseNotes: string;
  changelog?: string;
  demoCode?: string;
  releasedAt?: string;
  configJson: string;
  integratedAt: string;
}

export interface Application {
  id: number;
  name: string;
  stack: string;
  primusClientId: string;
  description?: string;
  ownerEmail: string;
  moduleCount: number;
  createdAt: string;
  updatedAt?: string; // Only in detail view
  clientSecret?: string | null;
  clientSecretLastRotatedAt?: string | null;
  hasClientSecret?: boolean;
  integratedModules?: IntegratedModule[]; // Only in detail view
}

export interface ApplicationCredential {
  primusClientId: string;
  clientSecret: string;
  rotatedAt: string;
}

interface ApplicationsState {
  applications: Application[];
  currentApplication: Application | null;
  isLoading: boolean;
  error: string | null;
  fetchApplications: () => Promise<void>;
  fetchApplication: (id: number) => Promise<void>;
  createApplication: (application: Omit<Application, 'id' | 'primusClientId' | 'createdAt' | 'ownerEmail' | 'moduleCount' | 'integratedModules'>) => Promise<Application>;
  updateApplication: (id: number, application: Partial<Application>) => Promise<void>;
  deleteApplication: (id: number) => Promise<void>;
  addModule: (applicationId: number, moduleId: number, moduleVersionId: number) => Promise<void>;
  changeModuleVersion: (applicationId: number, moduleId: number, version: string) => Promise<void>;
  removeModule: (applicationId: number, moduleId: number) => Promise<void>;
  rotateClientSecret: (applicationId: number) => Promise<ApplicationCredential>;
}

export const useApplicationsStore = create<ApplicationsState>((set, get) => ({
  applications: [],
  currentApplication: null,
  isLoading: false,
  error: null,

  fetchApplications: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.get('/applications');
      set({ applications: response.data, isLoading: false });
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to fetch applications: ${errorMessage}`);
    }
  },

  fetchApplication: async (id) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.get(`/applications/${id}`);
      set({ currentApplication: response.data, isLoading: false });
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to fetch application: ${errorMessage}`);
    }
  },

  createApplication: async (application) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.post('/applications', application);
      set((state) => ({
        applications: [...state.applications, response.data],
        isLoading: false,
      }));
      useUIStore.getState().addToast('success', 'Application created successfully');
      return response.data;
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to create application: ${errorMessage}`);
      throw error;
    }
  },

  updateApplication: async (id, application) => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.put(`/applications/${id}`, application);
      set((state) => ({
        applications: state.applications.map((a) => (a.id === id ? response.data : a)),
        isLoading: false,
      }));
      useUIStore.getState().addToast('success', 'Application updated successfully');
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to update application: ${errorMessage}`);
      throw error;
    }
  },

  deleteApplication: async (id) => {
    set({ isLoading: true, error: null });
    try {
      await apiClient.delete(`/applications/${id}`);
      set((state) => ({
        applications: state.applications.filter((a) => a.id !== id),
        isLoading: false,
      }));
      useUIStore.getState().addToast('success', 'Application deleted successfully');
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to delete application: ${errorMessage}`);
      throw error;
    }
  },

  addModule: async (applicationId, moduleId, moduleVersionId) => {
    set({ isLoading: true, error: null });
    try {
      await apiClient.post(`/applications/${applicationId}/modules`, {
        moduleId,
        moduleVersionId,
      });
      // Refetch the application to get updated module list
      await get().fetchApplication(applicationId);
      useUIStore.getState().addToast('success', 'Module added to application successfully');
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to add module: ${errorMessage}`);
      throw error;
    }
  },

  changeModuleVersion: async (applicationId, moduleId, version) => {
    set({ isLoading: true, error: null });
    try {
      await apiClient.post(`/applications/${applicationId}/modules/${moduleId}/version`, { version });
      // Refetch the application to get updated version info
      await get().fetchApplication(applicationId);
      useUIStore.getState().addToast('success', `Module version updated to ${version}`);
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to change version: ${errorMessage}`);
      throw error;
    }
  },

  removeModule: async (applicationId, moduleId) => {
    set({ isLoading: true, error: null });
    try {
      await apiClient.delete(`/applications/${applicationId}/modules/${moduleId}`);
      // Refetch the application to get updated module list
      await get().fetchApplication(applicationId);
      useUIStore.getState().addToast('success', 'Module removed from application successfully');
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      set({ error: errorMessage, isLoading: false });
      useUIStore.getState().addToast('error', `Failed to remove module: ${errorMessage}`);
      throw error;
    }
  },

  rotateClientSecret: async (applicationId) => {
    try {
      const response = await apiClient.post(`/applications/${applicationId}/credentials/rotate`);
      await get().fetchApplication(applicationId);
      useUIStore.getState().addToast('success', 'Client secret regenerated. Copy it immediately.');
      return response.data as ApplicationCredential;
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      useUIStore.getState().addToast('error', `Failed to rotate client secret: ${errorMessage}`);
      throw error;
    }
  },
}));
