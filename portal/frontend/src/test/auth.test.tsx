import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { AuthProvider, useAuth } from '../providers/AuthProvider';
import apiClient from '../services/apiClient';

// Mock apiClient
vi.mock('../services/apiClient');

describe('AuthProvider', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it('should initialize with no user when localStorage is empty', () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <AuthProvider>{children}</AuthProvider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.user).toBeNull();
    expect(result.current.token).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);
  });

  it('should restore auth state from localStorage on mount', async () => {
    const mockUser = { id: 1, email: 'test@example.com' };
    const mockToken = 'mock-jwt-token';

    localStorage.setItem('authToken', mockToken);
    localStorage.setItem('authUser', JSON.stringify(mockUser));

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <AuthProvider>{children}</AuthProvider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.user).toEqual(mockUser);
    expect(result.current.token).toBe(mockToken);
    expect(result.current.isAuthenticated).toBe(true);
  });

  it('should login successfully and store credentials', async () => {
    const mockUser = { id: 1, email: 'test@example.com' };
    const mockToken = 'new-jwt-token';

    vi.mocked(apiClient.post).mockResolvedValue({
      data: { token: mockToken, email: mockUser.email, role: 'Admin' },
    });

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <AuthProvider>{children}</AuthProvider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    await act(async () => {
      await result.current.login('test@example.com', 'password123');
    });

    const expectedUser = { id: 0, email: mockUser.email, role: 'Admin' };

    expect(result.current.user).toEqual(expectedUser);
    expect(result.current.token).toBe(mockToken);
    expect(result.current.isAuthenticated).toBe(true);
    expect(localStorage.getItem('authToken')).toBe(mockToken);
    expect(localStorage.getItem('authUser')).toBe(JSON.stringify(expectedUser));
  });

  it('should logout and clear credentials', async () => {
    const mockUser = { id: 1, email: 'test@example.com' };
    const mockToken = 'mock-jwt-token';

    localStorage.setItem('authToken', mockToken);
    localStorage.setItem('authUser', JSON.stringify(mockUser));

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <AuthProvider>{children}</AuthProvider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    act(() => {
      result.current.logout();
    });

    expect(result.current.user).toBeNull();
    expect(result.current.token).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);
    expect(localStorage.getItem('authToken')).toBeNull();
    expect(localStorage.getItem('authUser')).toBeNull();
  });

  it('should handle login errors gracefully', async () => {
    vi.mocked(apiClient.post).mockRejectedValue(new Error('Invalid credentials'));

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <AuthProvider>{children}</AuthProvider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    await expect(
      act(async () => {
        await result.current.login('test@example.com', 'wrong-password');
      })
    ).rejects.toThrow('Invalid credentials');

    expect(result.current.user).toBeNull();
    expect(result.current.token).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);
  });
});
