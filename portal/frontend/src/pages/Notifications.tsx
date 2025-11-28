import React, { useState } from 'react';
import { Bell, Mail, CheckCircle, AlertCircle, Send } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';

export default function Notifications() {
    const { token } = useAuth();
    const [loading, setLoading] = useState(false);
    const [status, setStatus] = useState<'idle' | 'success' | 'error'>('idle');
    const [logs, setLogs] = useState<string[]>([]);

    const sendTestNotification = async () => {
        setLoading(true);
        setStatus('idle');
        try {
            const response = await fetch('http://localhost:5000/api/notifications/test', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({ type: 'ApplicationCreated' })
            });

                    <h1 className="text-2xl font-bold text-white">Notification Center</h1>
                    <p className="text-gray-400">Manage and test your notification channels</p>
                </div >
            </div >

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Control Panel */}
            <div className="lg:col-span-2 space-y-6">
                <div className="bg-gray-800 rounded-xl border border-gray-700 p-6">
                    <h2 className="text-lg font-semibold text-white mb-4 flex items-center gap-2">
                        <Send className="w-5 h-5 text-blue-400" />
                        Test Dispatcher
                    </h2>

                    <div className="bg-gray-900/50 rounded-lg p-4 border border-gray-700 mb-6">
                        <div className="flex items-center gap-4 mb-4">
                            <div className="p-3 bg-blue-500/10 rounded-lg">
                                <Mail className="w-6 h-6 text-blue-400" />
                            </div>
                            <div>
                                <h3 className="text-white font-medium">Welcome Email</h3>
                                <p className="text-sm text-gray-400">Template: <code>ApplicationCreated</code></p>
                            </div>
                        </div>

                        <button
                            onClick={sendTestNotification}
                            disabled={loading}
                            className={`w-full py-2 px-4 rounded-lg font-medium transition-all flex items-center justify-center gap-2
                  ${loading
                                    ? 'bg-gray-700 text-gray-400 cursor-not-allowed'
                                    : 'bg-blue-600 hover:bg-blue-500 text-white shadow-lg shadow-blue-500/20'}`}
                        >
                            {loading ? 'Dispatching...' : 'Send Test Notification'}
                        </button>
                    </div>

                    {status === 'success' && (
                        <div className="bg-green-500/10 border border-green-500/20 rounded-lg p-4 flex items-center gap-3 text-green-400">
                            <CheckCircle className="w-5 h-5" />
                            <div>
                                <p className="font-medium">Notification Dispatched!</p>
                                <p className="text-sm opacity-80">Check your backend logs or email inbox.</p>
                            </div>
                        </div>
                    )}

                    {status === 'error' && (
                        <div className="bg-red-500/10 border border-red-500/20 rounded-lg p-4 flex items-center gap-3 text-red-400">
                            <AlertCircle className="w-5 h-5" />
                            <div>
                                <p className="font-medium">Dispatch Failed</p>
                                <p className="text-sm opacity-80">Check the console for details.</p>
                            </div>
                        </div>
                    )}
                </div>

                {/* Active Channels */}
                <div className="bg-gray-800 rounded-xl border border-gray-700 p-6">
                    <h2 className="text-lg font-semibold text-white mb-4">Active Channels</h2>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="p-4 bg-gray-900/50 rounded-lg border border-gray-700 flex items-center gap-3">
                            <div className="w-2 h-2 rounded-full bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.5)]" />
                            <span className="text-gray-300">SMTP Email</span>
                        </div>
                        <div className="p-4 bg-gray-900/50 rounded-lg border border-gray-700 flex items-center gap-3">
                            <div className="w-2 h-2 rounded-full bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.5)]" />
                            <span className="text-gray-300">Logger (Debug)</span>
                        </div>
                        <div className="p-4 bg-gray-900/50 rounded-lg border border-gray-700 flex items-center gap-3 opacity-50">
                            <div className="w-2 h-2 rounded-full bg-gray-600" />
                            <span className="text-gray-500">SMS (Twilio) - Coming Soon</span>
                        </div>
                        <div className="p-4 bg-gray-900/50 rounded-lg border border-gray-700 flex items-center gap-3 opacity-50">
                            <div className="w-2 h-2 rounded-full bg-gray-600" />
                            <span className="text-gray-500">Push (Firebase) - Coming Soon</span>
                        </div>
                    </div>
                </div>
            </div>

            {/* Live Logs */}
            <div className="bg-gray-900 rounded-xl border border-gray-800 p-6 h-full font-mono text-sm">
                <h2 className="text-gray-400 font-semibold mb-4 flex items-center gap-2">
                    <Bell className="w-4 h-4" />
                    Live Activity
                </h2>
                <div className="space-y-3">
                    {logs.length === 0 ? (
                        <p className="text-gray-600 italic">No activity yet...</p>
                    ) : (
                        logs.map((log, i) => (
                            <div key={i} className="text-gray-300 border-l-2 border-blue-500 pl-3 py-1 animate-in fade-in slide-in-from-left-2">
                                {log}
                            </div>
                        ))
                    )}
                </div>
            </div>
        </div>
        </div >
    );
}
