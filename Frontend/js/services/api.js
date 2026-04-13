export const BASE_URL = 'http://localhost:5000/api/v1'; // Docker API URL

export const api = {
    async get(endpoint) {
        const response = await fetch(`${BASE_URL}${endpoint}`);
        return await response.json();
    },
    async post(endpoint, data) {
        const response = await fetch(`${BASE_URL}${endpoint}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        return await response.json();
    },
    async put(endpoint, data) {
        const response = await fetch(`${BASE_URL}${endpoint}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        return await response.json();
    },
    async delete(endpoint) {
        const response = await fetch(`${BASE_URL}${endpoint}`, {
            method: 'DELETE'
        });
        return response;
    }
};

