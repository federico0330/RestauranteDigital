export const BASE_URL = 'http://localhost:5000/api/v1';

const AUTH_KEY = 'restaurante.auth';

export const auth = {
    save(payload) {
        localStorage.setItem(AUTH_KEY, JSON.stringify(payload));
    },
    get() {
        const raw = localStorage.getItem(AUTH_KEY);
        return raw ? JSON.parse(raw) : null;
    },
    clear() {
        localStorage.removeItem(AUTH_KEY);
    },
    isAuthenticated() {
        return !!this.get()?.token;
    },
    isAdmin() {
        const role = this.get()?.role;
        return role === 'Admin' || role === 'Manager';
    }
};

function buildHeaders(extra = {}) {
    const headers = { 'Content-Type': 'application/json', ...extra };
    const token = auth.get()?.token;
    if (token) headers['Authorization'] = `Bearer ${token}`;
    return headers;
}

async function handle(response) {
    if (response.status === 401) {
        auth.clear();
        throw new Error('No autorizado. Volvé a iniciar sesión.');
    }
    if (response.status === 204) return null;
    const text = await response.text();
    return text ? JSON.parse(text) : null;
}

export const api = {
    async get(endpoint) {
        const response = await fetch(`${BASE_URL}${endpoint}`, { headers: buildHeaders() });
        return handle(response);
    },
    async post(endpoint, data) {
        const response = await fetch(`${BASE_URL}${endpoint}`, {
            method: 'POST',
            headers: buildHeaders(),
            body: JSON.stringify(data)
        });
        return handle(response);
    },
    async put(endpoint, data) {
        const response = await fetch(`${BASE_URL}${endpoint}`, {
            method: 'PUT',
            headers: buildHeaders(),
            body: JSON.stringify(data)
        });
        return handle(response);
    },
    async delete(endpoint) {
        const response = await fetch(`${BASE_URL}${endpoint}`, {
            method: 'DELETE',
            headers: buildHeaders()
        });
        return handle(response);
    }
};
