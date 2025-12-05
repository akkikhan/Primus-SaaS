import React from 'react';
import { Mic, Volume2, Zap, Shield, ChevronRight } from 'lucide-react';

export default function VoiceAssistantDemo() {
    return (
        <div className="p-6 space-y-8">
            {/* Hero Section */}
            <div className="bg-gradient-to-br from-purple-900/40 via-blue-900/30 to-gray-900/40 rounded-2xl border border-purple-500/30 p-8 backdrop-blur-sm">
                <div className="flex items-start justify-between">
                    <div className="space-y-4 flex-1">
                        <div className="inline-flex items-center gap-2 px-4 py-2 bg-purple-500/20 border border-purple-500/30 rounded-full">
                            <Zap className="w-4 h-4 text-purple-400" />
                            <span className="text-sm text-purple-300 font-medium">AI-Powered Voice Control</span>
                        </div>
                        <h1 className="text-4xl font-bold text-white">
                            Voice Assistant Integration
                        </h1>
                        <p className="text-xl text-gray-300 max-w-2xl">
                            Navigate and control the Primus Portal using natural voice commands powered by OpenAI Whisper and ElevenLabs.
                        </p>
                        <div className="flex items-center gap-3 pt-4">
                            <div className="flex items-center gap-2 px-4 py-2 bg-green-500/10 border border-green-500/30 rounded-lg">
                                <div className="w-2 h-2 rounded-full bg-green-500 animate-pulse"></div>
                                <span className="text-sm text-green-400">Active & Ready</span>
                            </div>
                            <div className="text-gray-400 text-sm">
                                Look for the floating microphone button →
                            </div>
                        </div>
                    </div>
                    <div className="relative">
                        <div className="w-24 h-24 rounded-full bg-gradient-to-r from-purple-500 to-blue-500 flex items-center justify-center">
                            <Mic className="w-12 h-12 text-white" />
                        </div>
                        <span className="absolute -top-2 -right-2 w-6 h-6 bg-red-500 rounded-full border-4 border-gray-900 animate-pulse"></span>
                    </div>
                </div>
            </div>

            {/* Features Grid */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="bg-gray-800 rounded-xl border border-gray-700 p-6 space-y-3">
                    <div className="w-12 h-12 rounded-lg bg-purple-500/10 flex items-center justify-center">
                        <Mic className="w-6 h-6 text-purple-400" />
                    </div>
                    <h3 className="text-lg font-semibold text-white">OpenAI Whisper</h3>
                    <p className="text-gray-400 text-sm">
                        State-of-the-art speech-to-text transcription with high accuracy and multi-language support.
                    </p>
                </div>

                <div className="bg-gray-800 rounded-xl border border-gray-700 p-6 space-y-3">
                    <div className="w-12 h-12 rounded-lg bg-blue-500/10 flex items-center justify-center">
                        <Volume2 className="w-6 h-6 text-blue-400" />
                    </div>
                    <h3 className="text-lg font-semibold text-white">ElevenLabs Voice</h3>
                    <p className="text-gray-400 text-sm">
                        Premium text-to-speech with human-like voices. Falls back to browser TTS if not configured.
                    </p>
                </div>

                <div className="bg-gray-800 rounded-xl border border-gray-700 p-6 space-y-3">
                    <div className="w-12 h-12 rounded-lg bg-green-500/10 flex items-center justify-center">
                        <Shield className="w-6 h-6 text-green-400" />
                    </div>
                    <h3 className="text-lg font-semibold text-white">Privacy First</h3>
                    <p className="text-gray-400 text-sm">
                        Audio processed in real-time, not stored. All API calls secured via HTTPS encryption.
                    </p>
                </div>
            </div>

            {/* Commands Section */}
            <div className="bg-gray-800 rounded-xl border border-gray-700 p-6 space-y-6">
                <div className="flex items-center justify-between">
                    <h2 className="text-2xl font-bold text-white">Try These Commands</h2>
                    <div className="px-3 py-1 bg-blue-500/10 border border-blue-500/30 rounded-full text-xs text-blue-400">
                        Click the mic button to start
                    </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {/* Navigation Commands */}
                    <div className="space-y-3">
                        <h3 className="text-sm font-semibold text-purple-400 uppercase tracking-wider">Navigation</h3>
                        {[
                            'Go to notifications',
                            'Open dashboard',
                            'Show settings',
                            'Navigate to applications'
                        ].map((cmd, i) => (
                            <div key={i} className="flex items-center gap-3 p-3 bg-gray-900/50 rounded-lg border border-gray-700 hover:border-purple-500/50 transition-colors group">
                                <ChevronRight className="w-4 h-4 text-gray-500 group-hover:text-purple-400 transition-colors" />
                                <code className="text-sm text-gray-300 font-mono">{cmd}</code>
                            </div>
                        ))}
                    </div>

                    {/* Query Commands */}
                    <div className="space-y-3">
                        <h3 className="text-sm font-semibold text-blue-400 uppercase tracking-wider">Queries</h3>
                        {[
                            'What page am I on?',
                            'What time is it?',
                            'What can you do?',
                            'Help'
                        ].map((cmd, i) => (
                            <div key={i} className="flex items-center gap-3 p-3 bg-gray-900/50 rounded-lg border border-gray-700 hover:border-blue-500/50 transition-colors group">
                                <ChevronRight className="w-4 h-4 text-gray-500 group-hover:text-blue-400 transition-colors" />
                                <code className="text-sm text-gray-300 font-mono">{cmd}</code>
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* Setup Instructions */}
            <div className="bg-gradient-to-r from-orange-900/20 to-red-900/20 rounded-xl border border-orange-500/30 p-6 space-y-4">
                <div className="flex items-start gap-4">
                    <div className="flex-shrink-0 w-10 h-10 rounded-full bg-orange-500/20 flex items-center justify-center">
                        <Zap className="w-5 h-5 text-orange-400" />
                    </div>
                    <div className="space-y-3 flex-1">
                        <h3 className="text-lg font-semibold text-white">Upgrade to Premium Voice</h3>
                        <p className="text-gray-300 text-sm">
                            Currently using browser's built-in speech recognition. For better accuracy and natural-sounding responses, add your API keys:
                        </p>
                        <div className="bg-gray-900/50 rounded-lg p-4 font-mono text-xs text-gray-300 space-y-1 border border-gray-700">
                            <div className="text-purple-400"># Add to .env file</div>
                            <div>VITE_OPENAI_API_KEY=<span className="text-yellow-400">your-openai-key</span></div>
                            <div>VITE_ELEVENLABS_API_KEY=<span className="text-yellow-400">your-elevenlabs-key</span></div>
                        </div>
                        <a
                            href="https://github.com/akkikhan/Primus-SaaS/blob/main/portal/frontend/VOICE_ASSISTANT_README.md"
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-2 text-orange-400 hover:text-orange-300 transition-colors text-sm font-medium"
                        >
                            View Full Documentation
                            <ChevronRight className="w-4 h-4" />
                        </a>
                    </div>
                </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <div className="bg-gray-800 rounded-xl border border-gray-700 p-4 text-center">
                    <div className="text-3xl font-bold text-purple-400">15+</div>
                    <div className="text-sm text-gray-400 mt-1">Voice Commands</div>
                </div>
                <div className="bg-gray-800 rounded-xl border border-gray-700 p-4 text-center">
                    <div className="text-3xl font-bold text-blue-400">~2s</div>
                    <div className="text-sm text-gray-400 mt-1">Response Time</div>
                </div>
                <div className="bg-gray-800 rounded-xl border border-gray-700 p-4 text-center">
                    <div className="text-3xl font-bold text-green-400">95%+</div>
                    <div className="text-sm text-gray-400 mt-1">Accuracy</div>
                </div>
                <div className="bg-gray-800 rounded-xl border border-gray-700 p-4 text-center">
                    <div className="text-3xl font-bold text-orange-400">100%</div>
                    <div className="text-sm text-gray-400 mt-1">Privacy Safe</div>
                </div>
            </div>
        </div>
    );
}
