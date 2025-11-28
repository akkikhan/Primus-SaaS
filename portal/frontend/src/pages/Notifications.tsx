import React, { useEffect, useState } from 'react';
import { Bell, Mail, CheckCircle, AlertCircle, Send, Save } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';

type Prefs = {
    emailOnNewVersion: boolean;
    emailOnBreakingChange: boolean;
    emailOnSecurityUpdate: boolean;
    additionalEmails: string;
};

const defaultPrefs: Prefs = {
    emailOnNewVersion: true,
    emailOnBreakingChange: true,
    emailOnSecurityUpdate: true,
    additionalEmails: '',
};

type MetricSnapshot = {
    sent: number;
    failed: number;
    queued: number;
    avgDispatchDurationMs: number;
};

export default function Notifications() {
    const { token } = useAuth();
    const [loading, setLoading] = useState(false);
    const [prefsLoading, setPrefsLoading] = useState(true);
    const [status, setStatus] = useState<'idle' | 'success' | 'error'>('idle');
    const [logs, setLogs] = useState<string[]>([]);
    const [prefs, setPrefs] = useState<Prefs>(defaultPrefs);
    const [prefsStatus, setPrefsStatus] = useState<'idle' | 'saved' | 'error'>('idle');
    const [metrics, setMetrics] = useState<MetricSnapshot | null>(null);
    const [metricsLoading, setMetricsLoading] = useState(false);
    const [loadTestCount, setLoadTestCount] = useState(100);
    const [loadTestStatus, setLoadTestStatus] = useState<'idle' | 'running' | 'error' | 'success'>('idle');

    useEffect(() => {
        const fetchPrefs = async () => {
            try {
                const res = await fetch('http://localhost:5000/api/notification-preferences/me', {
                    headers: {
                        Authorization: `Bearer ${token}`,
                    },
                });
                if (!res.ok) throw new Error('Failed to load preferences');
                const data = await res.json();
                setPrefs({
                    emailOnNewVersion: data.emailOnNewVersion,
                    emailOnBreakingChange: data.emailOnBreakingChange,
                    emailOnSecurityUpdate: data.emailOnSecurityUpdate,
                    additionalEmails: data.additionalEmails ?? '',
                });
            } catch (e) {
                console.error(e);
            } finally {
                setPrefsLoading(false);
            }
        };

        if (token) {
            fetchPrefs();
        }
    }, [token]);

    const fetchMetrics = async () => {
        setMetricsLoading(true);
        try {
            const res = await fetch('http://localhost:5000/api/notification-preferences/metrics', {
                headers: { Authorization: `Bearer ${token}` },
            });
            if (!res.ok) throw new Error('Failed to load metrics');
            const data = await res.json();
            setMetrics({
                sent: data.sent ?? 0,
                failed: data.failed ?? 0,
                queued: data.queued ?? 0,
                avgDispatchDurationMs: data.avgDispatchDurationMs ?? 0,
            });
        } catch (e) {
            console.error(e);
            setMetrics(null);
        } finally {
            setMetricsLoading(false);
        }
    };

    const sendTestNotification = async () => {
        setLoading(true);
        setStatus('idle');
        try {
            const response = await fetch('http://localhost:5000/api/notifications/test', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({ type: 'ApplicationCreated' }),
            });

            if (!response.ok) {
                throw new Error('Failed to dispatch notification');
            }

            setStatus('success');
            setLogs((prev) => [`${new Date().toLocaleTimeString()} Dispatched ApplicationCreated`, ...prev].slice(0, 10));
        } catch (error) {
            console.error(error);
            setStatus('error');
        } finally {
            setLoading(false);
        }
    };

    const savePrefs = async () => {
        setPrefsStatus('idle');
        setPrefsLoading(true);
        try {
            const res = await fetch('http://localhost:5000/api/notification-preferences/me', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({
                    emailOnNewVersion: prefs.emailOnNewVersion,
                    emailOnBreakingChange: prefs.emailOnBreakingChange,
                    emailOnSecurityUpdate: prefs.emailOnSecurityUpdate,
                    additionalEmails: prefs.additionalEmails,
                }),
            });

            if (!res.ok) throw new Error('Failed to save preferences');
            setPrefsStatus('saved');
        } catch (e) {
            console.error(e);
            setPrefsStatus('error');
        } finally {
            setPrefsLoading(false);
        }
    };

    const runLoadTest = async () => {
        setLoadTestStatus('running');
        try {
            const res = await fetch('http://localhost:5000/api/notifications/load-test', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({ count: loadTestCount }),
            });
            if (!res.ok) throw new Error('Failed to run load test');
            const data = await res.json();
            setLogs((prev) => [
                `${new Date().toLocaleTimeString()} Queued ${data.queued} notifications in ${data.elapsedMs}ms`,
                ...prev,
            ].slice(0, 10));
            setLoadTestStatus('success');
        } catch (e) {
            console.error(e);
            setLoadTestStatus('error');
        }
    };

    return (
        <div className="p-6 space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-white">Notification Center</h1>
                    <p className="text-gray-400">Manage and test your notification channels</p>
                </div>
            </div>

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

                {/* Live Logs + Preferences */}
                <div className="space-y-6">
                    <div className="bg-gray-800 rounded-xl border border-gray-700 p-6">
                        <h2 className="text-gray-200 font-semibold mb-4 flex items-center gap-2">
                            <Save className="w-4 h-4 text-blue-400" />
                            Preferences
                        </h2>
                        <div className="space-y-3 text-sm text-gray-200">
                            <label className="flex items-center gap-3">
                                <input
                                    type="checkbox"
                                    className="h-4 w-4"
                                    checked={prefs.emailOnNewVersion}
                                    onChange={(e) => setPrefs((p) => ({ ...p, emailOnNewVersion: e.target.checked }))}
                                    disabled={prefsLoading}
                                />
                                <span>Email on new version</span>
                            </label>
                            <label className="flex items-center gap-3">
                                <input
                                    type="checkbox"
                                    className="h-4 w-4"
                                    checked={prefs.emailOnBreakingChange}
                                    onChange={(e) => setPrefs((p) => ({ ...p, emailOnBreakingChange: e.target.checked }))}
                                    disabled={prefsLoading}
                                />
                                <span>Email on breaking change</span>
                            </label>
                            <label className="flex items-center gap-3">
                                <input
                                    type="checkbox"
                                    className="h-4 w-4"
                                    checked={prefs.emailOnSecurityUpdate}
                                    onChange={(e) => setPrefs((p) => ({ ...p, emailOnSecurityUpdate: e.target.checked }))}
                                    disabled={prefsLoading}
                                />
                                <span>Email on security update</span>
                            </label>
                            <div className="space-y-2">
                                <label className="text-gray-300">Additional emails (comma-separated)</label>
                                <input
                                    type="text"
                                    className="w-full bg-gray-900 border border-gray-700 rounded px-3 py-2 text-white"
                                    value={prefs.additionalEmails}
                                    onChange={(e) => setPrefs((p) => ({ ...p, additionalEmails: e.target.value }))}
                                    placeholder="ops@example.com, security@example.com"
                                    disabled={prefsLoading}
                                />
                            </div>
                        </div>
                        <button
                            onClick={savePrefs}
                            disabled={prefsLoading}
                            className="mt-4 w-full py-2 px-4 rounded-lg font-medium bg-blue-600 hover:bg-blue-500 text-white transition-all disabled:bg-gray-700 disabled:text-gray-400"
                        >
                            {prefsLoading ? 'Saving...' : 'Save Preferences'}
                        </button>
                        {prefsStatus === 'saved' && <p className="text-green-400 text-sm mt-2">Preferences saved.</p>}
                        {prefsStatus === 'error' && <p className="text-red-400 text-sm mt-2">Failed to save preferences.</p>}
                    </div>

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

                    <div className="bg-gray-800 rounded-xl border border-gray-700 p-6 space-y-4 text-sm text-gray-200">
                        <div className="flex items-center justify-between">
                            <h3 className="font-semibold">Metrics</h3>
                            <button
                                onClick={fetchMetrics}
                                disabled={metricsLoading}
                                className="px-3 py-1 rounded bg-gray-700 hover:bg-gray-600 text-white text-xs"
                            >
                                {metricsLoading ? 'Loading...' : 'Refresh'}
                            </button>
                        </div>
                        {metrics ? (
                            <div className="grid grid-cols-2 gap-2">
                                <div className="bg-gray-900 border border-gray-700 rounded p-3">
                                    <p className="text-gray-400 text-xs">Sent</p>
                                    <p className="text-lg font-semibold">{metrics.sent}</p>
                                </div>
                                <div className="bg-gray-900 border border-gray-700 rounded p-3">
                                    <p className="text-gray-400 text-xs">Failed</p>
                                    <p className="text-lg font-semibold">{metrics.failed}</p>
                                </div>
                                <div className="bg-gray-900 border border-gray-700 rounded p-3">
                                    <p className="text-gray-400 text-xs">Queued</p>
                                    <p className="text-lg font-semibold">{metrics.queued}</p>
                                </div>
                                <div className="bg-gray-900 border border-gray-700 rounded p-3">
                                    <p className="text-gray-400 text-xs">Avg Dispatch (ms)</p>
                                    <p className="text-lg font-semibold">{metrics.avgDispatchDurationMs.toFixed(2)}</p>
                                </div>
                            </div>
                        ) : (
                            <p className="text-gray-500 text-xs">No metrics yet.</p>
                        )}
                    </div>

                    <div className="bg-gray-800 rounded-xl border border-gray-700 p-6 space-y-3 text-sm text-gray-200">
                        <div className="flex items-center justify-between">
                            <h3 className="font-semibold">Load Test</h3>
                        </div>
                        <label className="text-gray-300 text-xs">Notifications to queue (1-1000)</label>
                        <input
                            type="number"
                            min={1}
                            max={1000}
                            value={loadTestCount}
                            onChange={(e) => setLoadTestCount(Math.max(1, Math.min(1000, Number(e.target.value))))}
                            className="w-full bg-gray-900 border border-gray-700 rounded px-3 py-2 text-white"
                        />
                        <button
                            onClick={runLoadTest}
                            disabled={loadTestStatus === 'running'}
                            className="w-full py-2 px-4 rounded-lg font-medium bg-indigo-600 hover:bg-indigo-500 text-white transition-all disabled:bg-gray-700 disabled:text-gray-400"
                        >
                            {loadTestStatus === 'running' ? 'Queueing...' : 'Queue Load Test'}
                        </button>
                        {loadTestStatus === 'success' && <p className="text-green-400 text-xs">Queued successfully.</p>}
                        {loadTestStatus === 'error' && <p className="text-red-400 text-xs">Failed to queue notifications.</p>}
                    </div>
                </div>
            </div>
        </div>
    );
}
