/**
 * Command Processor - Intelligent voice command interpretation
 * Processes natural language commands to control the portal
 */

import { NavigateFunction } from 'react-router-dom';

type CommandResponse = {
    text: string;
    action?: () => void;
};

class CommandProcessor {
    private navigateFunction: NavigateFunction | null = null;

    /**
     * Set the navigation function for route changes
     */
    setNavigate(navigate: NavigateFunction): void {
        this.navigateFunction = navigate;
    }

    /**
     * Process voice command and return response
     */
    async process(command: string): Promise<string> {
        const lowerCommand = command.toLowerCase().trim();

        // Navigation commands
        if (this.matchesAny(lowerCommand, ['go to notifications', 'open notifications', 'show notifications', 'notifications page'])) {
            return this.navigate('/notifications', 'Opening notifications page');
        }

        if (this.matchesAny(lowerCommand, ['go to dashboard', 'open dashboard', 'show dashboard', 'home page', 'go home'])) {
            return this.navigate('/dashboard', 'Opening dashboard');
        }

        if (this.matchesAny(lowerCommand, ['go to logging', 'open logging', 'show logs', 'logs page'])) {
            return this.navigate('/logging', 'Opening logging page');
        }

        if (this.matchesAny(lowerCommand, ['go to identity', 'open identity', 'show identity', 'identity page'])) {
            return this.navigate('/identity', 'Opening identity page');
        }

        if (this.matchesAny(lowerCommand, ['go to settings', 'open settings', 'show settings', 'settings page'])) {
            return this.navigate('/settings', 'Opening settings');
        }

        // Action commands
        if (this.matchesAny(lowerCommand, ['send test notification', 'send notification', 'test notification'])) {
            return this.executeAction('notification-test', 'Sending test notification');
        }

        if (this.matchesAny(lowerCommand, ['refresh', 'reload', 'update'])) {
            window.location.reload();
            return 'Refreshing the page';
        }

        // Query commands
        if (this.matchesAny(lowerCommand, ['what can you do', 'help', 'commands', 'what are your commands'])) {
            return this.getHelpText();
        }

        if (this.matchesAny(lowerCommand, ['what is this', 'about', 'info', 'information'])) {
            return 'This is the Primus SaaS Portal - your central hub for managing notifications, logging, and identity validation across your applications.';
        }

        if (this.matchesAny(lowerCommand, ['what page am i on', 'where am i', 'current page'])) {
            return `You're currently on ${window.location.pathname}`;
        }

        // Time and date
        if (this.matchesAny(lowerCommand, ['what time is it', 'current time', 'time'])) {
            const time = new Date().toLocaleTimeString();
            return `The current time is ${time}`;
        }

        if (this.matchesAny(lowerCommand, ['what is the date', 'current date', 'today'])) {
            const date = new Date().toLocaleDateString('en-US', {
                weekday: 'long',
                year: 'numeric',
                month: 'long',
                day: 'numeric'
            });
            return `Today is ${date}`;
        }

        // Greetings
        if (this.matchesAny(lowerCommand, ['hello', 'hi', 'hey', 'greetings'])) {
            return 'Hello! How can I help you navigate the Primus Portal?';
        }

        if (this.matchesAny(lowerCommand, ['thank you', 'thanks', 'appreciate it'])) {
            return "You're welcome! Let me know if you need anything else.";
        }

        if (this.matchesAny(lowerCommand, ['goodbye', 'bye', 'see you', 'exit'])) {
            return 'Goodbye! Have a great day!';
        }

        // Default fallback - use AI-like response
        return this.generateIntelligentResponse(command);
    }

    /**
     * Navigate to a route
     */
    private navigate(path: string, message: string): string {
        if (this.navigateFunction) {
            this.navigateFunction(path);
            return message;
        }
        return `${message} (Navigation not configured)`;
    }

    /**
     * Execute a custom action
     */
    private executeAction(actionId: string, message: string): string {
        // Emit custom event that components can listen to
        window.dispatchEvent(new CustomEvent('voice-action', {
            detail: { actionId }
        }));
        return message;
    }

    /**
     * Check if command matches any of the patterns
     */
    private matchesAny(command: string, patterns: string[]): boolean {
        return patterns.some(pattern =>
            command.includes(pattern) ||
            this.fuzzyMatch(command, pattern)
        );
    }

    /**
     * Fuzzy matching for similar commands
     */
    private fuzzyMatch(str1: string, str2: string): boolean {
        const words1 = str1.split(' ');
        const words2 = str2.split(' ');

        let matches = 0;
        for (const word of words2) {
            if (words1.some(w => w.includes(word) || word.includes(w))) {
                matches++;
            }
        }

        return matches / words2.length >= 0.6; // 60% similarity threshold
    }

    /**
     * Generate intelligent response for unrecognized commands
     */
    private generateIntelligentResponse(command: string): string {
        const responses = [
            `I heard "${command}", but I'm not sure how to help with that. Try saying "help" to see what I can do.`,
            `Interesting request! I don't have a specific action for "${command}" yet. Would you like to see available commands?`,
            `I'm still learning! I didn't understand "${command}". Say "help" to see what I can assist with.`,
        ];

        return responses[Math.floor(Math.random() * responses.length)];
    }

    /**
     * Get help text with available commands
     */
    private getHelpText(): string {
        return `I can help you with:
• Navigation: "Go to notifications", "Open dashboard", "Show settings"
• Actions: "Send test notification", "Refresh page"
• Information: "What page am I on?", "What time is it?"
• General: "Hello", "Thank you", "Help"

Just speak naturally and I'll do my best to understand!`;
    }
}

export const commandProcessor = new CommandProcessor();
