async function fetchData() {
    const token = localStorage.getItem('primus_token');
    const email = localStorage.getItem('user_email');

    if (!token) {
        window.location.href = '/index.html';
        return;
    }

    document.getElementById('userEmail').textContent = email;

    // Show Loading
    document.getElementById('loadingState').classList.remove('hidden');
    document.getElementById('errorState').classList.add('hidden');
    document.getElementById('dataState').classList.add('hidden');

    try {
        // 1. Call OUR Backend (which uses Primus SDK to validate)
        const response = await fetch('/api/revenue-stats', {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Access Denied');
        }

        // 2. Show Data
        document.getElementById('revenue').textContent = data.revenue;
        document.getElementById('growth').textContent = data.growth;
        document.getElementById('users').textContent = data.activeUsers.toLocaleString();

        document.getElementById('loadingState').classList.add('hidden');
        document.getElementById('dataState').classList.remove('hidden');

    } catch (err) {
        console.error(err);
        // Show Error State
        document.getElementById('loadingState').classList.add('hidden');
        document.getElementById('errorState').classList.remove('hidden');
        document.getElementById('errorText').textContent =
            `Backend rejected the token. Reason: ${err.message}`;
    }
}

function logout() {
    localStorage.clear();
    window.location.href = '/index.html';
}

// Run on load
fetchData();
