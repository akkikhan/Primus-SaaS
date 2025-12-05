/**
 * Voice Response Service - Text-to-Speech
 * Supports both ElevenLabs API and browser's native SpeechSynthesis
 */

const ELEVENLABS_API_KEY = import.meta.env.VITE_ELEVENLABS_API_KEY || '';
const ELEVENLABS_VOICE_ID = import.meta.env.VITE_ELEVENLABS_VOICE_ID || 'EXAVITQu4vr4xnSDxMaL'; // Default: Bella

class VoiceResponseService {
    private apiKey: string;
    private voiceId: string;
    private currentAudio: HTMLAudioElement | null = null;
    private utterance: SpeechSynthesisUtterance | null = null;

    constructor() {
        this.apiKey = ELEVENLABS_API_KEY;
        this.voiceId = ELEVENLABS_VOICE_ID;
    }

    /**
     * Speak text using ElevenLabs or fallback to browser TTS
     */
    async speak(text: string): Promise<void> {
        // Stop any currently playing audio
        this.stop();

        if (this.isElevenLabsConfigured()) {
            await this.speakWithElevenLabs(text);
        } else {
            await this.speakWithBrowserTTS(text);
        }
    }

    /**
     * Speak using ElevenLabs API (Premium quality)
     */
    private async speakWithElevenLabs(text: string): Promise<void> {
        try {
            const response = await fetch(
                `https://api.elevenlabs.io/v1/text-to-speech/${this.voiceId}`,
                {
                    method: 'POST',
                    headers: {
                        'Accept': 'audio/mpeg',
                        'Content-Type': 'application/json',
                        'xi-api-key': this.apiKey,
                    },
                    body: JSON.stringify({
                        text,
                        model_id: 'eleven_monolingual_v1',
                        voice_settings: {
                            stability: 0.4,
                            similarity_boost: 0.75,
                            style: 0.15,
                        },
                    }),
                }
            );

            if (!response.ok) {
                throw new Error('ElevenLabs API error');
            }

            const audioBlob = await response.blob();
            const audioUrl = URL.createObjectURL(audioBlob);

            this.currentAudio = new Audio(audioUrl);

            return new Promise((resolve, reject) => {
                if (!this.currentAudio) return reject();

                this.currentAudio.onended = () => {
                    URL.revokeObjectURL(audioUrl);
                    resolve();
                };

                this.currentAudio.onerror = () => {
                    URL.revokeObjectURL(audioUrl);
                    reject();
                };

                this.currentAudio.play();
            });
        } catch (error) {
            console.error('ElevenLabs TTS error:', error);
            console.log('Falling back to browser TTS');
            await this.speakWithBrowserTTS(text);
        }
    }

    /**
     * Speak using browser's native SpeechSynthesis API (Free fallback)
     */
    private async speakWithBrowserTTS(text: string): Promise<void> {
        return new Promise((resolve) => {
            if (!window.speechSynthesis) {
                console.error('Speech synthesis not supported');
                resolve();
                return;
            }

            this.utterance = new SpeechSynthesisUtterance(text);

            // Configure voice settings
            this.utterance.rate = 0.9;
            this.utterance.pitch = 1.1;
            this.utterance.volume = 1.0;

            // Try to use a better quality voice if available
            const voices = window.speechSynthesis.getVoices();
            const preferredVoice = voices.find(
                voice => voice.name.includes('Google') || voice.name.includes('Microsoft')
            );
            if (preferredVoice) {
                this.utterance.voice = preferredVoice;
            }

            this.utterance.onend = () => resolve();
            this.utterance.onerror = () => resolve();

            window.speechSynthesis.speak(this.utterance);
        });
    }

    /**
     * Stop currently playing audio
     */
    stop(): void {
        // Stop ElevenLabs audio
        if (this.currentAudio) {
            this.currentAudio.pause();
            this.currentAudio.currentTime = 0;
            this.currentAudio = null;
        }

        // Stop browser TTS
        if (window.speechSynthesis && window.speechSynthesis.speaking) {
            window.speechSynthesis.cancel();
        }

        this.utterance = null;
    }

    /**
     * Check if ElevenLabs is configured
     */
    isElevenLabsConfigured(): boolean {
        return !!this.apiKey && this.apiKey.length > 0;
    }

    /**
     * Get available browser voices
     */
    getAvailableVoices(): SpeechSynthesisVoice[] {
        if (!window.speechSynthesis) {
            return [];
        }
        return window.speechSynthesis.getVoices();
    }

    /**
     * Test TTS functionality
     */
    async test(): Promise<boolean> {
        try {
            await this.speak('Voice assistant test successful.');
            return true;
        } catch {
            return false;
        }
    }
}

export const voiceResponseService = new VoiceResponseService();
