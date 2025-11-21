import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { LoginPage } from '../pages/LoginPage';
import { vi } from 'vitest';

// Mock hooks
vi.mock('@azure/msal-react', () => ({
    useMsal: () => ({
        instance: {
            loginPopup: vi.fn(),
        },
    }),
}));

vi.mock('../providers/AuthProvider', () => ({
    useAuth: () => ({
        login: vi.fn(),
        loginWithAzure: vi.fn(),
        isLoading: false,
    }),
}));

describe('Smoke Tests', () => {
    it('renders login page', () => {
        render(
            <MemoryRouter>
                <LoginPage />
            </MemoryRouter>
        );
        expect(screen.getByText('Primus SaaS Portal')).toBeInTheDocument();
        expect(screen.getByText('Sign in with Microsoft Azure ID')).toBeInTheDocument();
    });
});
