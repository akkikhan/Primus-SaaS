/**
 * Whisper Service - OpenAI Speech-to-Text Integration
 * Handles audio transcription using OpenAI's Whisper API
 */

const OPENAI_API_KEY = import.meta.env.VITE_OPENAI_API_KEY || '';
const WHISPER_API_URL = 'https://api.openai.com/v1/audio/transcriptions';

class WhisperService {
    private apiKey: string;

    constructor() {
        this.apiKey = OPENAI_API_KEY;
    }

    /**
     * Transcribe audio blob to text using Whisper API
     */
    async transcribe(audioBlob: Blob): Promise<string> {
        if (!this.apiKey) {
            console.warn('OpenAI API key not configured. Using fallback transcription.');
            return this.fallbackTranscription();
        }

        try {
            // Convert webm to compatible format if needed
            const audioFile = await this.prepareAudioFile(audioBlob);

            const formData = new FormData();
            formData.append('file', audioFile, 'audio.webm');
            formData.append('model', 'whisper-1');
            formData.append('language', 'en'); // Can be auto-detected by removing this
            formData.append('response_format', 'text');

            const response = await fetch(WHISPER_API_URL, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${this.apiKey}`,
                },
                body: formData,
            });

            if (!response.ok) {
                const error = await response.text();
                throw new Error(`Whisper API error: ${error}`);
            }

            const transcription = await response.text();
            return transcription.trim();
        } catch (error) {
            console.error('Whisper transcription error:', error);
            return this.fallbackTranscription();
        }
    }

    /**
     * Prepare audio file for API submission
     */
    private async prepareAudioFile(blob: Blob): Promise<File> {
        // Create a File object from the Blob
        return new File([blob], 'audio.webm', { type: 'audio/webm' });
    }

    /**
     * Fallback transcription using browser's built-in SpeechRecognition API
     * This is less accurate but doesn't require OpenAI API key
     */
    private fallbackTranscription(): Promise<string> {
        return new Promise((resolve) => {
            // Check if browser supports SpeechRecognition
            const SpeechRecognition =
                (window as any).SpeechRecognition ||
                (window as any).webkitSpeechRecognition;

            if (!SpeechRecognition) {
                resolve('Speech recognition not supported. Please configure OpenAI API key for Whisper.');
                return;
            }

            const recognition = new SpeechRecognition();
            recognition.lang = 'en-US';
            recognition.continuous = false;
            recognition.interimResults = false;

            recognition.onresult = (event: any) => {
                const transcript = event.results[0][0].transcript;
                resolve(transcript);
            };

            recognition.onerror = (event: any) => {
                console.error('Speech recognition error:', event.error);
                resolve('Could not transcribe audio. Please try again.');
            };

            recognition.start();

            // Auto-stop after 5 seconds
            setTimeout(() => {
                recognition.stop();
            }, 5000);
        });
    }

    /**
     * Check if Whisper API is configured and available
     */
    isConfigured(): boolean {
        return !!this.apiKey && this.apiKey.length > 0;
    }

    /**
     * Test the Whisper API connection
     */
    async testConnection(): Promise<boolean> {
        if (!this.isConfigured()) {
            return false;
        }

        try {
            // Create a minimal test audio blob
            const silentBlob = this.createSilentAudioBlob();
            await this.transcribe(silentBlob);
            return true;
        } catch {
            return false;
        }
    }

    /**
     * Create a silent audio blob for testing
     */
    private createSilentAudioBlob(): Blob {
        const buffer = new ArrayBuffer(44);
        const view = new DataView(buffer);

        // WAV header
        const writeString = (offset: number, string: string) => {
            for (let i = 0; i < string.length; i++) {
                view.setUint8(offset + i, string.charCodeAt(i));
            }
        };

        writeString(0, 'RIFF');
        view.setUint32(4, 36, true);
        writeString(8, 'WAVE');
        writeString(12, 'fmt ');
        view.setUint32(16, 16, true);
        view.setUint16(20, 1, true);
        view.setUint16(22, 1, true);
        view.setUint32(24, 44100, true);
        view.setUint32(28, 88200, true);
        view.setUint16(32, 2, true);
        view.setUint16(34, 16, true);
        writeString(36, 'data');
        view.setUint32(40, 0, true);

        return new Blob([buffer], { type: 'audio/wav' });
    }
}

export const whisperService = new WhisperService();
