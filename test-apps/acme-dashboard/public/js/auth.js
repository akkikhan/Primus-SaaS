const PORTAL_API = 'http://localhost:5267/api';

document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    const btn = document.querySelector('button');
    const errorDiv = document.getElementById('errorMessage');

    // UI Loading State
    btn.disabled = true;
    btn.querySelector('#btnText').textContent = 'Authenticating...';
    errorDiv.classList.add('hidden');

    try {
        // 1. Call Proxy to get Token
        const response = await fetch('/login-proxy', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || 'Login failed');
        }

        // 2. Save Token
        localStorage.setItem('primus_token', data.token);
        localStorage.setItem('user_email', data.email);

        // 3. Redirect to Dashboard
        window.location.href = '/dashboard.html';

    } catch (err) {
        errorDiv.textContent = err.message;
        errorDiv.classList.remove('hidden');
        btn.disabled = false;
        btn.querySelector('#btnText').textContent = 'Sign In';
    }
});
