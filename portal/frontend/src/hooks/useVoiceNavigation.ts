import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { commandProcessor } from '../services/commandProcessor';

/**
 * Hook to set up voice command navigation integration
 * Call this at the app root level to enable voice-based navigation
 */
export function useVoiceNavigation() {
    const navigate = useNavigate();

    useEffect(() => {
        // Initialize command processor with navigation function
        commandProcessor.setNavigate(navigate);

        return () => {
            // Cleanup if needed
        };
    }, [navigate]);
}
