import React, { useState, useRef, useEffect } from 'react';
import { Mic, MicOff, X, Loader2, Volume2, VolumeX, Radio } from 'lucide-react';
import { whisperService } from '../services/whisperService';
import { voiceResponseService } from '../services/voiceResponseService';
import { commandProcessor } from '../services/commandProcessor';

type TranscriptEntry = {
    id: string;
    text: string;
    timestamp: Date;
    isUser: boolean;
};

export default function VoiceOverlay() {
    const [isOpen, setIsOpen] = useState(false);
    const [isListening, setIsListening] = useState(false);
    const [isProcessing, setIsProcessing] = useState(false);
    const [isSpeaking, setIsSpeaking] = useState(false);
    const [transcript, setTranscript] = useState<TranscriptEntry[]>([]);
    const [currentTranscript, setCurrentTranscript] = useState('');
    const [isMuted, setIsMuted] = useState(false);
    const [waveAnimation, setWaveAnimation] = useState(false);

    const mediaRecorderRef = useRef<MediaRecorder | null>(null);
    const audioChunksRef = useRef<Blob[]>([]);
    const transcriptEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        transcriptEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [transcript]);

    const startListening = async () => {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            const mediaRecorder = new MediaRecorder(stream);
            mediaRecorderRef.current = mediaRecorder;
            audioChunksRef.current = [];

            mediaRecorder.ondataavailable = (event) => {
                audioChunksRef.current.push(event.data);
            };

            mediaRecorder.onstop = async () => {
                const audioBlob = new Blob(audioChunksRef.current, { type: 'audio/webm' });
                await processAudio(audioBlob);
                stream.getTracks().forEach(track => track.stop());
            };

            mediaRecorder.start();
            setIsListening(true);
            setWaveAnimation(true);
            setCurrentTranscript('Listening...');
        } catch (error) {
            console.error('Error accessing microphone:', error);
            addTranscript('⚠️ Microphone access denied. Please enable microphone permissions.', false);
        }
    };

    const stopListening = () => {
        if (mediaRecorderRef.current && isListening) {
            mediaRecorderRef.current.stop();
            setIsListening(false);
            setWaveAnimation(false);
            setCurrentTranscript('');
        }
    };

    const processAudio = async (audioBlob: Blob) => {
        setIsProcessing(true);
        try {
            // Transcribe with Whisper
            const transcribedText = await whisperService.transcribe(audioBlob);

            if (transcribedText) {
                addTranscript(transcribedText, true);

                // Process command
                const response = await commandProcessor.process(transcribedText);
                addTranscript(response, false);

                // Speak response if not muted
                if (!isMuted) {
                    setIsSpeaking(true);
                    await voiceResponseService.speak(response);
                    setIsSpeaking(false);
                }
            }
        } catch (error) {
            console.error('Error processing audio:', error);
            addTranscript('Sorry, I encountered an error processing your request.', false);
        } finally {
            setIsProcessing(false);
        }
    };

    const addTranscript = (text: string, isUser: boolean) => {
        const entry: TranscriptEntry = {
            id: Date.now().toString(),
            text,
            timestamp: new Date(),
            isUser,
        };
        setTranscript(prev => [...prev, entry]);
    };

    const toggleMute = () => {
        setIsMuted(!isMuted);
        if (isSpeaking) {
            voiceResponseService.stop();
            setIsSpeaking(false);
        }
    };

    const clearTranscript = () => {
        setTranscript([]);
    };

    return (
        <>
            {/* Floating Trigger Button */}
            <button
                onClick={() => setIsOpen(!isOpen)}
                className={`fixed bottom-6 right-6 z-50 p-4 rounded-full shadow-2xl transition-all duration-300 transform hover:scale-110 focus:outline-none focus:ring-4 focus:ring-purple-500/50 ${isListening
                        ? 'bg-gradient-to-r from-red-500 to-pink-500 animate-pulse'
                        : 'bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-500 hover:to-blue-500'
                    }`}
                aria-label="Voice Assistant"
            >
                {isListening ? (
                    <Radio className="w-6 h-6 text-white animate-pulse" />
                ) : (
                    <Mic className="w-6 h-6 text-white" />
                )}

                {/* Pulse Ring Animation */}
                {isListening && (
                    <span className="absolute inset-0 rounded-full bg-purple-500 animate-ping opacity-75"></span>
                )}
            </button>

            {/* Voice Overlay Panel */}
            {isOpen && (
                <div className="fixed bottom-24 right-6 z-50 w-96 bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900 rounded-2xl shadow-2xl border border-gray-700 backdrop-blur-xl overflow-hidden animate-in slide-in-from-bottom-4 fade-in duration-300">
                    {/* Header */}
                    <div className="bg-gradient-to-r from-purple-600/20 to-blue-600/20 border-b border-gray-700 p-4 flex items-center justify-between">
                        <div className="flex items-center gap-3">
                            <div className="relative">
                                <div className="w-10 h-10 rounded-full bg-gradient-to-r from-purple-500 to-blue-500 flex items-center justify-center">
                                    <Mic className="w-5 h-5 text-white" />
                                </div>
                                {isListening && (
                                    <span className="absolute -top-1 -right-1 w-3 h-3 bg-red-500 rounded-full border-2 border-gray-900 animate-pulse"></span>
                                )}
                            </div>
                            <div>
                                <h3 className="text-white font-semibold">Voice Assistant</h3>
                                <p className="text-xs text-gray-400">
                                    {isListening ? 'Listening...' : isProcessing ? 'Processing...' : 'Ready'}
                                </p>
                            </div>
                        </div>
                        <button
                            onClick={() => setIsOpen(false)}
                            className="p-2 hover:bg-gray-700/50 rounded-lg transition-colors"
                        >
                            <X className="w-5 h-5 text-gray-400" />
                        </button>
                    </div>

                    {/* Transcript Area */}
                    <div className="h-80 overflow-y-auto p-4 space-y-3 bg-gray-900/50">
                        {transcript.length === 0 ? (
                            <div className="flex flex-col items-center justify-center h-full text-center space-y-3">
                                <div className="w-16 h-16 rounded-full bg-purple-500/10 flex items-center justify-center">
                                    <Mic className="w-8 h-8 text-purple-400" />
                                </div>
                                <div>
                                    <p className="text-gray-300 font-medium">Ready to listen</p>
                                    <p className="text-sm text-gray-500 mt-1">
                                        Press the microphone button to start
                                    </p>
                                </div>
                            </div>
                        ) : (
                            transcript.map((entry) => (
                                <div
                                    key={entry.id}
                                    className={`flex ${entry.isUser ? 'justify-end' : 'justify-start'} animate-in slide-in-from-bottom-2 fade-in`}
                                >
                                    <div
                                        className={`max-w-[80%] rounded-2xl px-4 py-2 ${entry.isUser
                                                ? 'bg-gradient-to-r from-purple-600 to-blue-600 text-white'
                                                : 'bg-gray-800 text-gray-200 border border-gray-700'
                                            }`}
                                    >
                                        <p className="text-sm">{entry.text}</p>
                                        <p className="text-xs opacity-60 mt-1">
                                            {entry.timestamp.toLocaleTimeString()}
                                        </p>
                                    </div>
                                </div>
                            ))
                        )}
                        <div ref={transcriptEndRef} />
                    </div>

                    {/* Current Listening Indicator */}
                    {currentTranscript && (
                        <div className="px-4 py-2 bg-purple-900/20 border-t border-gray-700">
                            <p className="text-sm text-purple-300 italic flex items-center gap-2">
                                {waveAnimation && (
                                    <span className="flex gap-1">
                                        <span className="w-1 h-3 bg-purple-400 rounded-full animate-pulse" style={{ animationDelay: '0ms' }}></span>
                                        <span className="w-1 h-3 bg-purple-400 rounded-full animate-pulse" style={{ animationDelay: '150ms' }}></span>
                                        <span className="w-1 h-3 bg-purple-400 rounded-full animate-pulse" style={{ animationDelay: '300ms' }}></span>
                                    </span>
                                )}
                                {currentTranscript}
                            </p>
                        </div>
                    )}

                    {/* Controls */}
                    <div className="border-t border-gray-700 p-4 bg-gray-900/30">
                        <div className="flex items-center justify-between gap-3">
                            <button
                                onClick={toggleMute}
                                className="p-3 rounded-xl bg-gray-800 hover:bg-gray-700 transition-all border border-gray-700"
                                title={isMuted ? 'Unmute' : 'Mute'}
                            >
                                {isMuted ? (
                                    <VolumeX className="w-5 h-5 text-gray-400" />
                                ) : (
                                    <Volume2 className="w-5 h-5 text-blue-400" />
                                )}
                            </button>

                            <button
                                onClick={isListening ? stopListening : startListening}
                                disabled={isProcessing}
                                className={`flex-1 py-3 px-6 rounded-xl font-semibold transition-all shadow-lg flex items-center justify-center gap-2 ${isListening
                                        ? 'bg-gradient-to-r from-red-500 to-pink-500 hover:from-red-600 hover:to-pink-600 text-white'
                                        : 'bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white'
                                    } disabled:opacity-50 disabled:cursor-not-allowed`}
                            >
                                {isProcessing ? (
                                    <>
                                        <Loader2 className="w-5 h-5 animate-spin" />
                                        Processing...
                                    </>
                                ) : isListening ? (
                                    <>
                                        <MicOff className="w-5 h-5" />
                                        Stop
                                    </>
                                ) : (
                                    <>
                                        <Mic className="w-5 h-5" />
                                        Start
                                    </>
                                )}
                            </button>

                            <button
                                onClick={clearTranscript}
                                disabled={transcript.length === 0}
                                className="p-3 rounded-xl bg-gray-800 hover:bg-gray-700 transition-all border border-gray-700 disabled:opacity-30 disabled:cursor-not-allowed"
                                title="Clear transcript"
                            >
                                <X className="w-5 h-5 text-gray-400" />
                            </button>
                        </div>
                    </div>

                    {/* Status Indicator */}
                    {isSpeaking && (
                        <div className="absolute top-4 right-16 bg-blue-500/20 border border-blue-500/50 rounded-full px-3 py-1 flex items-center gap-2 animate-in fade-in">
                            <Volume2 className="w-4 h-4 text-blue-400 animate-pulse" />
                            <span className="text-xs text-blue-300">Speaking...</span>
                        </div>
                    )}
                </div>
            )}
        </>
    );
}
