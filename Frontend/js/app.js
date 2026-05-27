import { api, auth } from './services/api.js';

// Generic food placeholder (SVG) — used when a dish has no image URL
const FOOD_PLACEHOLDER = "data:image/svg+xml;charset=UTF-8,%3Csvg xmlns='http://www.w3.org/2000/svg' width='400' height='300'%3E%3Crect width='400' height='300' fill='%23f3f4f6'/%3E%3Ccircle cx='200' cy='130' r='52' fill='none' stroke='%23d1d5db' stroke-width='5'/%3E%3Ccircle cx='200' cy='130' r='35' fill='none' stroke='%23e5e7eb' stroke-width='3'/%3E%3Cline x1='200' y1='78' x2='200' y2='58' stroke='%23d1d5db' stroke-width='5' stroke-linecap='round'/%3E%3Ctext x='200' y='210' text-anchor='middle' font-family='Arial%2Csans-serif' font-size='13' fill='%239ca3af'%3ESin imagen%3C%2Ftext%3E%3C%2Fsvg%3E";

function setFallbackImage(img) {
    img.onerror = null;
    img.src = FOOD_PLACEHOLDER;
}
window.setFallbackImage = setFallbackImage;

// Toast notification system — replaces native alert() throughout the app
function showToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    const bgClass = type === 'error' ? 'bg-danger' : type === 'warning' ? 'bg-warning text-dark' : 'bg-success';
    const icon = type === 'error' ? 'fa-circle-xmark' : type === 'warning' ? 'fa-triangle-exclamation' : 'fa-circle-check';
    const el = document.createElement('div');
    el.className = `toast align-items-center text-white border-0 ${bgClass} shadow`;
    el.setAttribute('role', 'alert');
    el.innerHTML = `
        <div class="d-flex">
            <div class="toast-body fw-semibold">
                <i class="fa-solid ${icon} me-2"></i>${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>`;
    container.appendChild(el);
    const toast = new bootstrap.Toast(el, { delay: 3500 });
    toast.show();
    el.addEventListener('hidden.bs.toast', () => el.remove());
}

// App State
const state = {
    user: null,
    currentView: 'login',
    categories: [],
    dishes: [],
    cart: [],
    orders: [],
    filters: {
        name: '',
        category: '',
        sortByPrice: ''
    },
    adminPage: 0,
    adminPageSize: 8
};

// Router
const router = {
    async navigate(view, pushState = true) {
        if (!state.user && view !== 'login' && view !== 'register') {
            view = 'login';
        }
        state.currentView = view;

        if (pushState) {
            window.history.pushState({ view: view }, '', `#${view}`);
        }

        render();
    }
};

window.addEventListener('popstate', (event) => {
    if (event.state && event.state.view) {
        router.navigate(event.state.view, false);
    } else {
        const hash = window.location.hash.replace('#', '') || 'login';
        router.navigate(hash, false);
    }
});

// Auth Handling — backed por JWT real + localStorage
async function login(email, password) {
    try {
        const result = await api.post('/Auth/login', { email, password });
        if (!result || !result.token) return false;
        auth.save(result);
        state.user = {
            id: result.userId,
            role: result.role === 'Admin' || result.role === 'Manager' ? 'admin' : 'user',
            username: result.name
        };
        updateNav();
        router.navigate(state.user.role === 'admin' ? 'admin' : 'menu');
        return true;
    } catch (err) {
        return false;
    }
}

async function register(name, email, password) {
    try {
        const result = await api.post('/Auth/register', { name, email, password });
        if (!result || !result.token) return false;
        auth.save(result);
        state.user = {
            id: result.userId,
            role: result.role === 'Admin' || result.role === 'Manager' ? 'admin' : 'user',
            username: result.name
        };
        updateNav();
        router.navigate('menu');
        return true;
    } catch (err) {
        return false;
    }
}

function logout() {
    auth.clear();
    state.user = null;
    state.cart = [];
    updateCartBadge();
    updateNav();
    router.navigate('login');
}

function restoreSession() {
    const saved = auth.get();
    if (!saved) return false;
    state.user = {
        id: saved.userId,
        role: saved.role === 'Admin' || saved.role === 'Manager' ? 'admin' : 'user',
        username: saved.name
    };
    return true;
}

function updateNav() {
    const authNav = document.getElementById('auth-nav');
    if (!state.user) {
        authNav.innerHTML = `<button class="btn btn-outline-dark rounded-pill px-4 fw-semibold" id="btn-login-nav">Ingresar</button>`;
        document.querySelectorAll('.user-only, .admin-only').forEach(el => el.classList.add('d-none'));
        document.getElementById('btn-login-nav').addEventListener('click', () => router.navigate('login'));
        return;
    }

    authNav.innerHTML = `
        <div class="dropdown">
            <button class="btn btn-light rounded-pill px-3 dropdown-toggle border" type="button" data-bs-toggle="dropdown">
                <i class="fa-solid fa-user me-1"></i> ${state.user.username}
            </button>
            <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0">
                <li><a class="dropdown-item text-danger fw-semibold" href="#" id="btn-logout"><i class="fa-solid fa-arrow-right-from-bracket me-2"></i>Cerrar Sesión</a></li>
            </ul>
        </div>
    `;

    document.getElementById('btn-logout').addEventListener('click', (e) => { e.preventDefault(); logout(); });

    document.querySelectorAll('.user-only').forEach(el => el.classList.toggle('d-none', state.user.role !== 'user'));
    document.querySelectorAll('.admin-only').forEach(el => el.classList.toggle('d-none', state.user.role !== 'admin'));
}

// Components
function renderLogin() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="auth-card text-center">
            <h2 class="mb-4 fw-bold" style="color: #ea4c89;"><i class="fa-solid fa-utensils me-2"></i>FoodApp</h2>
            <p class="text-muted mb-4">Ingresa a tu cuenta para continuar</p>
            <form id="form-login">
                <div class="form-floating mb-3">
                    <input type="email" class="form-control rounded-pill" id="login-user" placeholder="Email" required>
                    <label for="login-user">Email</label>
                </div>
                <div class="form-floating mb-3">
                    <input type="password" class="form-control rounded-pill" id="login-pass" placeholder="Contraseña" required>
                    <label for="login-pass">Contraseña</label>
                </div>
                <div id="login-error" class="alert alert-danger py-2 small d-none mb-3 rounded-3 text-start">
                    <i class="fa-solid fa-circle-xmark me-1"></i>Credenciales incorrectas. Verificá tu email y contraseña.
                </div>
                <button type="submit" class="btn btn-primary w-100 rounded-pill py-2 fw-bold" style="background-color: #ea4c89; border-color: #ea4c89;" id="btn-login-submit">Iniciar Sesión</button>
            </form>
            <div class="mt-4 pt-3 border-top text-center">
                <p class="text-muted small mb-2">¿No tenés cuenta? <a href="#" id="link-register" class="fw-semibold" style="color: #ea4c89;">Registrate gratis</a></p>
                <p class="text-muted mb-0" style="font-size: 0.75rem;">
                    <i class="fa-solid fa-circle-info text-info me-1"></i>
                    Admin demo: <code>admin@restaurante.com</code> / <code>admin123</code>
                </p>
            </div>
        </div>
    `;

    document.getElementById('form-login').addEventListener('submit', async (e) => {
        e.preventDefault();
        const errorEl = document.getElementById('login-error');
        const btn = document.getElementById('btn-login-submit');
        errorEl.classList.add('d-none');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Ingresando...';

        const ok = await login(
            document.getElementById('login-user').value,
            document.getElementById('login-pass').value
        );

        if (!ok) {
            errorEl.classList.remove('d-none');
            btn.disabled = false;
            btn.textContent = 'Iniciar Sesión';
        }
    });

    document.getElementById('link-register').addEventListener('click', (e) => {
        e.preventDefault();
        router.navigate('register');
    });
}

function renderRegister() {
    const app = document.getElementById('app');
    app.innerHTML = `
        <div class="auth-card text-center">
            <h2 class="mb-4 fw-bold" style="color: #ea4c89;"><i class="fa-solid fa-utensils me-2"></i>FoodApp</h2>
            <p class="text-muted mb-4">Creá tu cuenta para empezar a pedir</p>
            <form id="form-register">
                <div class="form-floating mb-3">
                    <input type="text" class="form-control rounded-pill" id="reg-name" placeholder="Nombre" required>
                    <label for="reg-name">Nombre completo</label>
                </div>
                <div class="form-floating mb-3">
                    <input type="email" class="form-control rounded-pill" id="reg-email" placeholder="Email" required>
                    <label for="reg-email">Email</label>
                </div>
                <div class="form-floating mb-3">
                    <input type="password" class="form-control rounded-pill" id="reg-pass" placeholder="Contraseña" minlength="6" required>
                    <label for="reg-pass">Contraseña (mín. 6 caracteres)</label>
                </div>
                <div class="form-floating mb-4">
                    <input type="password" class="form-control rounded-pill" id="reg-pass-confirm" placeholder="Confirmar contraseña" required>
                    <label for="reg-pass-confirm">Confirmar contraseña</label>
                </div>
                <div id="reg-error" class="alert alert-danger py-2 d-none small rounded-3 text-start mb-3"></div>
                <button type="submit" class="btn btn-primary w-100 rounded-pill py-2 fw-bold" style="background-color: #ea4c89; border-color: #ea4c89;">Crear cuenta</button>
            </form>
            <div class="mt-4 pt-3 border-top text-center">
                <p class="text-muted small mb-0">¿Ya tenés cuenta? <a href="#" id="link-login" class="fw-semibold" style="color: #ea4c89;">Ingresá acá</a></p>
            </div>
        </div>
    `;

    document.getElementById('link-login').addEventListener('click', (e) => {
        e.preventDefault();
        router.navigate('login');
    });

    document.getElementById('form-register').addEventListener('submit', async (e) => {
        e.preventDefault();
        const name = document.getElementById('reg-name').value.trim();
        const email = document.getElementById('reg-email').value.trim();
        const pass = document.getElementById('reg-pass').value;
        const confirm = document.getElementById('reg-pass-confirm').value;
        const errorEl = document.getElementById('reg-error');

        if (pass !== confirm) {
            errorEl.textContent = 'Las contraseñas no coinciden.';
            errorEl.classList.remove('d-none');
            return;
        }

        errorEl.classList.add('d-none');
        const btn = e.target.querySelector('button[type=submit]');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Registrando...';

        const ok = await register(name, email, pass);
        if (!ok) {
            errorEl.textContent = 'No se pudo crear la cuenta. El email puede estar en uso.';
            errorEl.classList.remove('d-none');
            btn.disabled = false;
            btn.textContent = 'Crear cuenta';
        }
    });
}

async function renderMenu() {
    const app = document.getElementById('app');
    app.innerHTML = '<div class="loading"><div class="spinner-border text-primary" style="color: #ea4c89 !important;"></div><p class="mt-3 fw-semibold">Preparando delicias...</p></div>';

    try {
        if (state.categories.length === 0) {
            state.categories = await api.get('/Order/categories');
        }

        const query = new URLSearchParams({
            name: state.filters.name,
            category: state.filters.category,
            sortByPrice: state.filters.sortByPrice,
            onlyActive: state.user?.role === 'user'
        }).toString();

        state.dishes = await api.get(`/Dish?${query}`);

        let html = '';

        if(state.user?.role === 'user') {
            html += `
            <div class="hero text-center">
                <h1>Descubre sabores únicos</h1>
                <p>Pide online, retira en el local o reserva tu mesa en un click.</p>
            </div>`;
        } else {
            html += `<h2 class="mb-4 fw-bold">Vista General del Menú (Admin)</h2>`;
        }

        html += `
            <div class="card shadow-sm border-0 mb-4 p-3 rounded-4 bg-body">
                <div class="row g-2 align-items-center">
                    <div class="col-md-4">
                        <div class="position-relative">
                            <i class="fa-solid fa-magnifying-glass position-absolute top-50 start-0 translate-middle-y ms-3 text-muted"></i>
                            <input type="text" id="search-name" class="form-control ps-5 rounded-pill bg-body-secondary border-0" placeholder="¿Qué se te antoja hoy?" value="${state.filters.name}">
                        </div>
                    </div>
                    <div class="col-md-3">
                        <select id="filter-category" class="form-select bg-body-secondary border-0 rounded-pill text-body fw-semibold">
                            <option value="">Todas las categorías</option>
                            ${state.categories.map(c => `<option value="${c.id}" ${state.filters.category == c.id ? 'selected' : ''}>${c.name}</option>`).join('')}
                        </select>
                    </div>
                    <div class="col-md-3">
                        <select id="sort-price" class="form-select bg-body-secondary border-0 rounded-pill text-body fw-semibold">
                            <option value="">Ordenar</option>
                            <option value="asc" ${state.filters.sortByPrice == 'asc' ? 'selected' : ''}>Precio: Menor a mayor</option>
                            <option value="desc" ${state.filters.sortByPrice == 'desc' ? 'selected' : ''}>Precio: Mayor a menor</option>
                        </select>
                    </div>
                    <div class="col-md-2">
                        <button id="btn-reset-filters" class="btn btn-outline-danger w-100 fw-bold rounded-pill"><i class="fa-solid fa-eraser me-1"></i>Limpiar</button>
                    </div>
                </div>
            </div>

            <div class="row row-cols-1 row-cols-md-2 row-cols-lg-3 row-cols-xl-4 g-4 mb-5">
                ${state.dishes.length === 0 ? '<div class="col-12 text-center py-5"><p class="text-muted h4"><i class="fa-solid fa-plate-wheat fa-3x mb-3 d-block text-muted"></i>No se encontraron platos.</p></div>' : ''}
                ${state.dishes.map(dish => `
                    <div class="col">
                        <div class="card h-100 dish-card ${!dish.isActive ? 'border-danger opacity-75' : ''}">
                            <img src="${dish.image || FOOD_PLACEHOLDER}" class="card-img-top dish-image" alt="${dish.name}" onerror="setFallbackImage(this)">
                            <span class="badge bg-dark text-white category-badge">${dish.category.name}</span>
                            <div class="card-body d-flex flex-column">
                                ${!dish.isActive ? '<span class="badge bg-danger mb-2 align-self-start">Inactivo</span>' : ''}
                                <h5 class="card-title fw-bold mb-1">${dish.name}</h5>
                                <p class="card-text text-muted small flex-grow-1 mt-2 line-clamp-3">${dish.description || 'Sin descripción'}</p>
                                <div class="d-flex justify-content-between align-items-center mt-3 pt-3 border-top border-secondary-subtle">
                                    <span class="h5 mb-0 fw-bold text-body">$${dish.price.toFixed(2)}</span>
                                    ${state.user?.role === 'user' ? `
                                        <button class="btn btn-primary rounded-pill px-3 fw-bold btn-add-cart shadow-sm" data-id="${dish.id}" style="background-color: #ea4c89; border-color: #ea4c89;">
                                            <i class="fa-solid fa-plus"></i> Añadir
                                        </button>
                                    ` : `
                                        <button class="btn btn-outline-secondary btn-sm rounded-pill px-3 text-body" onclick="router.navigate('manage-dishes')">Editar</button>
                                    `}
                                </div>
                            </div>
                        </div>
                    </div>
                `).join('')}
            </div>
        `;

        app.innerHTML = html;

        let debounceTimer;
        const applyFilters = () => {
            state.filters.name = document.getElementById('search-name').value;
            state.filters.category = document.getElementById('filter-category').value;
            state.filters.sortByPrice = document.getElementById('sort-price').value;
            renderMenu();
        };

        const searchInput = document.getElementById('search-name');

        searchInput.addEventListener('input', (e) => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                state.filters.name = e.target.value;
                state.filters.category = document.getElementById('filter-category').value;
                state.filters.sortByPrice = document.getElementById('sort-price').value;
                renderMenu().then(() => {
                    const newInput = document.getElementById('search-name');
                    if (newInput) {
                        newInput.focus();
                        newInput.setSelectionRange(newInput.value.length, newInput.value.length);
                    }
                });
            }, 350);
        });

        document.getElementById('filter-category').addEventListener('change', applyFilters);
        document.getElementById('sort-price').addEventListener('change', applyFilters);

        document.getElementById('btn-reset-filters').addEventListener('click', () => {
            state.filters = { name: '', category: '', sortByPrice: '' };
            renderMenu();
        });

        document.querySelectorAll('.btn-add-cart').forEach(btn => {
            btn.addEventListener('click', (e) => {
                addToCart(e.currentTarget.dataset.id);
            });
        });

    } catch (error) {
        app.innerHTML = `<div class="alert alert-danger shadow-sm border-0"><i class="fa-solid fa-triangle-exclamation me-2"></i>Error al cargar el menú: ${error.message}</div>`;
    }
}

function addToCart(dishId) {
    const dish = state.dishes.find(d => d.id === dishId);
    if (!dish) return;

    const existing = state.cart.find(item => item.dishId === dishId);
    if (existing) {
        existing.quantity++;
    } else {
        state.cart.push({
            dishId: dish.id,
            name: dish.name,
            price: dish.price,
            quantity: 1,
            notes: ''
        });
    }
    updateCartBadge();
    showToast(`<strong>${dish.name}</strong> agregado al carrito`);

    const badge = document.getElementById('cart-count');
    badge.classList.add('fa-bounce');
    setTimeout(() => badge.classList.remove('fa-bounce'), 1000);
}

function updateCartBadge() {
    const count = state.cart.reduce((sum, item) => sum + item.quantity, 0);
    const badge = document.getElementById('cart-count');
    if (count > 0) {
        badge.innerText = count;
        badge.classList.remove('d-none');
    } else {
        badge.classList.add('d-none');
    }
}

async function renderCart() {
    const app = document.getElementById('app');
    if (state.cart.length === 0) {
        app.innerHTML = `
            <div class="text-center my-5 py-5 bg-white rounded-4 shadow-sm border">
                <i class="fa-solid fa-basket-shopping fa-4x text-light mb-3"></i>
                <h3 class="fw-bold">Tu carrito está vacío</h3>
                <p class="text-muted">¡Vuelve al menú para elegir algo delicioso!</p>
                <button class="btn btn-primary rounded-pill px-4 fw-bold mt-2" onclick="router.navigate('menu')" style="background-color: #ea4c89; border-color: #ea4c89;">Ver Menú</button>
            </div>
        `;
        return;
    }

    const deliveryTypes = await api.get('/Order/delivery-types');
    const branches = await api.get('/Branch');

    app.innerHTML = `
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2 class="fw-bold"><i class="fa-solid fa-cart-shopping me-2 text-danger"></i>Tu Pedido</h2>
            <button class="btn btn-outline-dark rounded-pill fw-semibold" onclick="router.navigate('menu')">Seguir comprando</button>
        </div>
        <div class="row g-4">
            <div class="col-lg-8">
                <div class="card shadow-sm border-0 rounded-4 mb-4 overflow-hidden">
                    <div class="card-body p-0">
                        <table class="table table-hover align-middle mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th class="ps-4 py-3">Plato</th>
                                    <th class="text-center">Cantidad</th>
                                    <th class="text-end">Precio</th>
                                    <th class="text-end pe-4">Subtotal</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${state.cart.map((item, index) => `
                                    <tr>
                                        <td class="ps-4 py-3 border-bottom-0">
                                            <div class="d-flex align-items-center justify-content-between">
                                                <div>
                                                    <h6 class="mb-1 fw-bold">${item.name}</h6>
                                                    <input type="text" class="form-control form-control-sm bg-light border-0 cart-note" data-index="${index}" placeholder="Nota (ej: sin cebolla)" value="${item.notes}" style="max-width: 250px;">
                                                </div>
                                            </div>
                                        </td>
                                        <td class="text-center border-bottom-0">
                                            <div class="d-flex align-items-center justify-content-center">
                                                <button class="btn btn-sm btn-light border text-muted rounded-circle btn-qty" data-index="${index}" data-delta="-1" style="width: 30px; height: 30px; padding: 0;"><i class="fa-solid fa-minus pointer-events-none"></i></button>
                                                <span class="mx-3 fw-bold">${item.quantity}</span>
                                                <button class="btn btn-sm btn-light border text-muted rounded-circle btn-qty" data-index="${index}" data-delta="1" style="width: 30px; height: 30px; padding: 0;"><i class="fa-solid fa-plus pointer-events-none"></i></button>
                                            </div>
                                        </td>
                                        <td class="text-end border-bottom-0 text-muted">$${item.price.toFixed(2)}</td>
                                        <td class="text-end pe-4 border-bottom-0">
                                            <span class="fw-bold d-block mb-1">$${(item.price * item.quantity).toFixed(2)}</span>
                                            <button class="btn btn-link text-danger p-0 text-decoration-none small btn-remove" data-index="${index}">Eliminar</button>
                                        </td>
                                    </tr>
                                    <tr><td colspan="4" class="p-0 border-bottom"><hr class="m-0 text-light"></td></tr>
                                `).join('')}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
            <div class="col-lg-4">
                <div class="card shadow-sm border-0 rounded-4 sticky-top" style="top: 80px;">
                    <div class="card-body p-4">
                        <h4 class="fw-bold mb-4">Resumen</h4>
                        <div class="d-flex justify-content-between mb-3 text-muted">
                            <span>Subtotal ítems (${state.cart.reduce((s,i)=>s+i.quantity,0)})</span>
                            <span>$${state.cart.reduce((sum, item) => sum + (item.price * item.quantity), 0).toFixed(2)}</span>
                        </div>
                        <hr class="text-light my-3">
                        <div class="d-flex justify-content-between mb-4">
                            <span class="h5 fw-bold mb-0">Total</span>
                            <span class="h4 fw-bold mb-0" style="color: #ea4c89;">$${state.cart.reduce((sum, item) => sum + (item.price * item.quantity), 0).toFixed(2)}</span>
                        </div>

                        <form id="form-order" class="bg-light p-3 rounded-3">
                            <h6 class="fw-bold mb-3"><i class="fa-solid fa-truck-fast me-2"></i>Datos de Entrega</h6>
                            <div class="mb-3">
                                <label class="form-label small fw-semibold text-muted">Sucursal</label>
                                <select id="branch-id" class="form-select bg-white border-0 shadow-sm" required>
                                    ${branches.map(b => `<option value="${b.id}">${b.name} — ${b.address}</option>`).join('')}
                                </select>
                            </div>
                            <div class="mb-3">
                                <label class="form-label small fw-semibold text-muted">¿Cómo lo quieres?</label>
                                <select id="delivery-type" class="form-select bg-white border-0 shadow-sm" required>
                                    <option value="" selected disabled>Selecciona una opción</option>
                                    ${deliveryTypes.map(t => `<option value="${t.id}">${t.name}</option>`).join('')}
                                </select>
                            </div>

                            <div id="dynamic-delivery-input" class="mb-3 d-none">
                                <label id="delivery-to-label" class="form-label small fw-semibold text-muted">Detalle</label>
                                <input type="text" id="delivery-to" class="form-control bg-white border-0 shadow-sm">
                                <small id="delivery-help" class="text-muted d-none mt-1"></small>
                            </div>

                            <div id="order-error" class="alert alert-danger py-2 small d-none rounded-3 mb-3"></div>

                            <button type="submit" id="btn-confirm-order" class="btn btn-primary w-100 py-3 fw-bold rounded-pill shadow" style="background-color: #ea4c89; border-color: #ea4c89;">
                                <i class="fa-solid fa-check me-1"></i>Confirmar Pedido
                            </button>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    `;

    const dTypeSelect = document.getElementById('delivery-type');
    const dynamicInputContainer = document.getElementById('dynamic-delivery-input');
    const dToLabel = document.getElementById('delivery-to-label');
    const dToInput = document.getElementById('delivery-to');
    const dHelp = document.getElementById('delivery-help');

    dTypeSelect.addEventListener('change', (e) => {
        const val = parseInt(e.target.value);
        dynamicInputContainer.classList.remove('d-none');
        dToInput.readOnly = false;
        dToInput.required = true;
        dToInput.value = '';
        dHelp.classList.add('d-none');

        if (val === 1) {
            dToLabel.innerHTML = '<i class="fa-solid fa-location-dot me-1"></i>Dirección de entrega';
            dToInput.placeholder = "Ej: Av. San Martín 456, Piso 2";
            dToInput.type = 'text';
        } else if (val === 2) {
            dToLabel.innerHTML = '<i class="fa-solid fa-user me-1"></i>Nombre de quien retira';
            dToInput.placeholder = "Ej: Juan Pérez";
            dToInput.type = 'text';
        } else if (val === 3) {
            dToLabel.innerHTML = '<i class="fa-solid fa-chair me-1"></i>Mesa asignada';
            dToInput.readOnly = true;
            dToInput.required = false;
            dToInput.value = `Mesa ${Math.floor(Math.random() * 20) + 1}`;
            dHelp.innerText = "Te hemos asignado una mesa automáticamente. ¡Acércate y disfruta!";
            dHelp.classList.remove('d-none');
        }
    });

    document.querySelectorAll('.btn-qty').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const index = e.currentTarget.dataset.index;
            const delta = parseInt(e.currentTarget.dataset.delta);
            state.cart[index].quantity += delta;
            if (state.cart[index].quantity <= 0) state.cart.splice(index, 1);
            updateCartBadge();
            renderCart();
        });
    });

    document.querySelectorAll('.btn-remove').forEach(btn => {
        btn.addEventListener('click', (e) => {
            state.cart.splice(e.target.dataset.index, 1);
            updateCartBadge();
            renderCart();
        });
    });

    document.querySelectorAll('.cart-note').forEach(input => {
        input.addEventListener('change', (e) => {
            state.cart[e.target.dataset.index].notes = e.target.value;
        });
    });

    document.getElementById('form-order').addEventListener('submit', async (e) => {
        e.preventDefault();
        const errorEl = document.getElementById('order-error');
        errorEl.classList.add('d-none');

        const deliveryToVal = document.getElementById('delivery-to').value;
        const deliveryTypeVal = document.getElementById('delivery-type').value;

        if (!deliveryTypeVal) {
            errorEl.textContent = 'Seleccioná una opción de entrega.';
            errorEl.classList.remove('d-none');
            return;
        }
        if (!deliveryToVal) {
            errorEl.textContent = 'Completá los datos de entrega antes de confirmar.';
            errorEl.classList.remove('d-none');
            return;
        }

        const btn = document.getElementById('btn-confirm-order');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Procesando...';

        const branchSelect = document.getElementById('branch-id');
        const orderData = {
            deliveryType: parseInt(deliveryTypeVal),
            branchId: branchSelect ? parseInt(branchSelect.value) : 1,
            deliveryTo: deliveryToVal,
            notes: "",
            items: state.cart.map(item => ({
                dishId: item.dishId,
                quantity: item.quantity,
                notes: item.notes
            }))
        };

        try {
            const response = await api.post('/Order', orderData);
            if (response && response.id) {
                state.cart = [];
                updateCartBadge();
                showToast(`¡Pedido #${response.id} confirmado! Seguí el estado en Mis Pedidos.`);
                router.navigate('user-orders');
            } else {
                errorEl.textContent = response?.message || 'No se pudo crear el pedido. Intentá nuevamente.';
                errorEl.classList.remove('d-none');
                btn.disabled = false;
                btn.innerHTML = '<i class="fa-solid fa-check me-1"></i>Confirmar Pedido';
            }
        } catch (error) {
            errorEl.textContent = `Error de red: ${error.message}`;
            errorEl.classList.remove('d-none');
            btn.disabled = false;
            btn.innerHTML = '<i class="fa-solid fa-check me-1"></i>Confirmar Pedido';
        }
    });
}

async function renderUserOrders() {
    const app = document.getElementById('app');
    app.innerHTML = '<div class="loading"><div class="spinner-border text-primary" style="color: #ea4c89 !important;"></div><p class="mt-3 fw-semibold">Cargando tus pedidos...</p></div>';

    try {
        const allOrders = await api.get('/Order');
        const orders = allOrders.slice(0, 5);

        if(orders.length === 0) {
            app.innerHTML = `
                <div class="text-center my-5 py-5 bg-white rounded-4 shadow-sm border">
                    <i class="fa-solid fa-receipt fa-4x text-light mb-3"></i>
                    <h3 class="fw-bold">No tienes pedidos recientes</h3>
                    <button class="btn btn-primary rounded-pill px-4 fw-bold mt-3" onclick="router.navigate('menu')" style="background-color: #ea4c89; border-color: #ea4c89;">Ir al Menú</button>
                </div>
            `;
            return;
        }

        let html = `
            <div class="d-flex justify-content-between align-items-center mb-4">
                <h2 class="fw-bold"><i class="fa-solid fa-receipt me-2 text-primary" style="color: #ea4c89 !important;"></i>Mis Pedidos Recientes</h2>
                <button class="btn btn-outline-dark rounded-pill fw-semibold btn-sm" onclick="renderUserOrders()"><i class="fa-solid fa-rotate-right me-1"></i>Actualizar Estado</button>
            </div>
            <div class="row g-4">
        `;

        orders.forEach(o => {
            const statusId = o.overallStatus.id;

            html += `
            <div class="col-12">
                <div class="card shadow-sm border-0 rounded-4 p-4 mb-3">
                    <div class="d-flex justify-content-between align-items-center mb-3 pb-3 border-bottom">
                        <div>
                            <span class="text-muted small">Pedido #${o.id}</span>
                            <h5 class="fw-bold mb-0 mt-1">${o.deliveryType.name} - ${o.deliveryTo}</h5>
                        </div>
                        <div class="text-end">
                            <span class="h4 fw-bold text-dark mb-0">$${o.price.toFixed(2)}</span>
                            <span class="d-block text-muted small">${new Date(o.createdAt).toLocaleString()}</span>
                        </div>
                    </div>

                    <div class="order-progress">
                        <div class="progress-step ${statusId >= 1 ? (statusId > 1 ? 'completed' : 'active') : ''}">
                            <div class="progress-icon"><i class="fa-solid fa-clock"></i></div>
                            Pendiente
                        </div>
                        <div class="progress-step ${statusId >= 2 ? (statusId > 2 ? 'completed' : 'active') : ''}">
                            <div class="progress-icon"><i class="fa-solid fa-fire-burner"></i></div>
                            Cocina
                        </div>
                        <div class="progress-step ${statusId >= 3 ? (statusId > 3 ? 'completed' : 'active') : ''}">
                            <div class="progress-icon"><i class="fa-solid fa-box-open"></i></div>
                            Listo
                        </div>
                        <div class="progress-step ${statusId >= 4 ? (statusId > 4 ? 'completed' : 'active') : ''}">
                            <div class="progress-icon"><i class="fa-solid fa-truck"></i></div>
                            En camino
                        </div>
                        <div class="progress-step ${statusId >= 5 ? 'completed' : ''}">
                            <div class="progress-icon"><i class="fa-solid fa-check"></i></div>
                            Entregado
                        </div>
                    </div>

                    <div class="bg-light rounded-3 p-3 mt-4">
                        <h6 class="fw-bold mb-2">Detalle de platos:</h6>
                        <ul class="list-unstyled mb-0 small text-muted">
                            ${o.items.map(item => `
                                <li class="mb-1"><i class="fa-solid fa-circle text-secondary" style="font-size: 0.4rem; vertical-align: middle; margin-right: 8px;"></i>${item.quantity}x ${item.dishName}</li>
                            `).join('')}
                        </ul>
                    </div>
                </div>
            </div>`;
        });

        html += `</div>`;
        app.innerHTML = html;

    } catch (error) {
        app.innerHTML = `<div class="alert alert-danger shadow-sm border-0">Error: ${error.message}</div>`;
    }
}

async function renderAdmin() {
    const app = document.getElementById('app');
    app.innerHTML = '<div class="loading"><div class="spinner-border text-danger"></div><p class="mt-3 fw-semibold">Sincronizando sistema...</p></div>';

    try {
        const orders = await api.get('/Order');
        const statuses = await api.get('/Order/statuses');

        const totalPages = Math.ceil(orders.length / state.adminPageSize);
        const start = state.adminPage * state.adminPageSize;
        const pageOrders = orders.slice(start, start + state.adminPageSize);

        app.innerHTML = `
            <div class="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <h2 class="fw-bold mb-1"><i class="fa-solid fa-bell-concierge me-2" style="color:#ea4c89;"></i>Centro de Comandas</h2>
                    <span class="text-muted small">${orders.length} pedido${orders.length !== 1 ? 's' : ''} registrado${orders.length !== 1 ? 's' : ''}</span>
                </div>
                <button class="btn btn-dark rounded-pill fw-bold px-4" onclick="renderAdmin()">
                    <i class="fa-solid fa-rotate-right me-1"></i>Actualizar
                </button>
            </div>

            <!-- KPI Cards -->
            <div class="row g-3 mb-4">
                <div class="col-6 col-md-3">
                    <div class="card border-0 rounded-4 text-center p-3 h-100" style="background:var(--bs-warning-bg-subtle);">
                        <h1 class="display-5 fw-bold mb-1 text-warning-emphasis">${orders.filter(o => o.overallStatus.id === 1).length}</h1>
                        <span class="small fw-semibold text-muted"><i class="fa-solid fa-clock me-1"></i>Pendientes</span>
                    </div>
                </div>
                <div class="col-6 col-md-3">
                    <div class="card border-0 rounded-4 text-center p-3 h-100" style="background:var(--bs-primary-bg-subtle);">
                        <h1 class="display-5 fw-bold mb-1 text-primary-emphasis">${orders.filter(o => o.overallStatus.id === 2).length}</h1>
                        <span class="small fw-semibold text-muted"><i class="fa-solid fa-fire-burner me-1"></i>En preparación</span>
                    </div>
                </div>
                <div class="col-6 col-md-3">
                    <div class="card border-0 rounded-4 text-center p-3 h-100" style="background:var(--bs-info-bg-subtle);">
                        <h1 class="display-5 fw-bold mb-1 text-info-emphasis">${orders.filter(o => o.overallStatus.id === 3).length}</h1>
                        <span class="small fw-semibold text-muted"><i class="fa-solid fa-box-open me-1"></i>Listos</span>
                    </div>
                </div>
                <div class="col-6 col-md-3">
                    <div class="card border-0 rounded-4 text-center p-3 h-100" style="background:var(--bs-success-bg-subtle);">
                        <h1 class="display-5 fw-bold mb-1 text-success-emphasis">${orders.filter(o => o.overallStatus.id === 5).length}</h1>
                        <span class="small fw-semibold text-muted"><i class="fa-solid fa-check me-1"></i>Entregados</span>
                    </div>
                </div>
            </div>

            <!-- Order Cards -->
            <div class="row g-3 mb-4">
                ${pageOrders.length === 0 ? `
                    <div class="col-12">
                        <div class="text-center py-5 text-muted bg-white rounded-4 shadow-sm">
                            <i class="fa-solid fa-inbox fa-3x mb-3 d-block"></i>
                            <h5 class="fw-semibold">No hay pedidos registrados</h5>
                        </div>
                    </div>
                ` : pageOrders.map(o => `
                    <div class="col-12 col-lg-6">
                        <div class="card admin-order-card">
                            <div class="card-header bg-body-tertiary border-0 d-flex justify-content-between align-items-start px-4 py-3">
                                <div>
                                    <span class="fw-bold fs-6 text-body">#${o.id}</span>
                                    <span class="text-muted small ms-2">${new Date(o.createdAt).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })}</span>
                                    <div class="mt-1">
                                        <span class="badge bg-light text-dark border me-1">${o.deliveryType.name}</span>
                                        <span class="small text-body fw-semibold">${o.deliveryTo}</span>
                                    </div>
                                </div>
                                <div class="text-end flex-shrink-0 ms-3">
                                    <span class="badge ${getStatusBadgeClass(o.overallStatus.id)} rounded-pill px-3 py-2">${o.overallStatus.name}</span>
                                    <div class="fw-bold text-body mt-1">$${o.price.toFixed(2)}</div>
                                </div>
                            </div>
                            ${o.items.map(item => `
                                <div class="order-item-row">
                                    <div class="flex-grow-1 me-3 min-width-0">
                                        <span class="fw-semibold text-body">${item.quantity}× ${item.dishName}</span>
                                        ${item.notes ? `<div class="small text-danger mt-1"><i class="fa-solid fa-triangle-exclamation me-1"></i>${item.notes}</div>` : ''}
                                    </div>
                                    <div class="dropdown flex-shrink-0">
                                        <button class="btn btn-sm ${getStatusBtnClass(item.status.id)} dropdown-toggle rounded-pill px-3 fw-semibold" type="button" data-bs-toggle="dropdown" style="font-size:0.78rem;white-space:nowrap;">
                                            ${item.status.name}
                                        </button>
                                        <ul class="dropdown-menu dropdown-menu-end shadow border-0">
                                            ${statuses.map(s => `
                                                <li>
                                                    <a class="dropdown-item small py-2 btn-change-status" href="#" data-item-id="${item.id}" data-status-id="${s.id}">
                                                        <span class="me-2 badge ${getStatusBadgeClass(s.id)}" style="width:8px;height:8px;border-radius:50%;padding:0;display:inline-block;">&nbsp;</span>${s.name}
                                                    </a>
                                                </li>
                                            `).join('')}
                                        </ul>
                                    </div>
                                </div>
                            `).join('')}
                        </div>
                    </div>
                `).join('')}
            </div>

            <!-- Pagination -->
            ${totalPages > 1 ? `
                <nav class="d-flex justify-content-between align-items-center mt-2">
                    <span class="text-muted small">Página ${state.adminPage + 1} de ${totalPages} &nbsp;·&nbsp; ${orders.length} pedidos</span>
                    <div class="d-flex gap-2">
                        <button class="btn btn-outline-secondary rounded-pill px-4 fw-semibold" id="btn-prev-page" ${state.adminPage === 0 ? 'disabled' : ''}>
                            <i class="fa-solid fa-chevron-left me-1"></i>Anterior
                        </button>
                        <button class="btn btn-outline-secondary rounded-pill px-4 fw-semibold" id="btn-next-page" ${state.adminPage >= totalPages - 1 ? 'disabled' : ''}>
                            Siguiente<i class="fa-solid fa-chevron-right ms-1"></i>
                        </button>
                    </div>
                </nav>
            ` : ''}
        `;

        document.querySelectorAll('.btn-change-status').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                e.preventDefault();
                const itemId = e.currentTarget.dataset.itemId;
                const statusId = e.currentTarget.dataset.statusId;
                await api.put(`/Order/item/${itemId}/status/${statusId}`, {});
                renderAdmin();
            });
        });

        document.getElementById('btn-prev-page')?.addEventListener('click', () => {
            state.adminPage--;
            renderAdmin();
        });

        document.getElementById('btn-next-page')?.addEventListener('click', () => {
            state.adminPage++;
            renderAdmin();
        });

    } catch (error) {
        app.innerHTML = `<div class="alert alert-danger shadow-sm border-0">Error: ${error.message}</div>`;
    }
}

async function renderManageDishes() {
    const app = document.getElementById('app');
    app.innerHTML = '<div class="loading"><div class="spinner-border text-danger"></div></div>';

    try {
        if (state.categories.length === 0) state.categories = await api.get('/Order/categories');
        const dishes = await api.get('/Dish?onlyActive=false');

        app.innerHTML = `
            <div class="d-flex justify-content-between align-items-center mb-4">
                <h2 class="fw-bold text-danger"><i class="fa-solid fa-pen-to-square me-2"></i>Gestión del Menú</h2>
                <button class="btn btn-primary rounded-pill fw-bold shadow-sm" data-bs-toggle="modal" data-bs-target="#dishModal" id="btn-add-new" style="background-color: #ea4c89; border-color: #ea4c89;"><i class="fa-solid fa-plus me-1"></i>Nuevo Plato</button>
            </div>

            <div class="card shadow-sm border-0 rounded-4 overflow-hidden">
                <div class="table-responsive">
                    <table class="table table-hover align-middle mb-0">
                        <thead class="table-light">
                            <tr>
                                <th class="ps-4">Nombre</th>
                                <th>Categoría</th>
                                <th>Precio</th>
                                <th>Estado</th>
                                <th class="text-end pe-4">Acciones</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${dishes.map(d => `
                                <tr>
                                    <td class="ps-4">
                                        <div class="d-flex align-items-center">
                                            <img src="${d.image || FOOD_PLACEHOLDER}" class="rounded shadow-sm me-3" style="width: 40px; height: 40px; object-fit: cover;" onerror="setFallbackImage(this)">
                                            <strong>${d.name}</strong>
                                        </div>
                                    </td>
                                    <td><span class="badge bg-light text-dark border">${d.category.name}</span></td>
                                    <td class="fw-bold text-dark">$${d.price.toFixed(2)}</td>
                                    <td>
                                        <div class="form-check form-switch m-0">
                                            <input class="form-check-input" type="checkbox" role="switch" ${d.isActive ? 'checked' : ''} disabled>
                                            <label class="form-check-label small text-muted">${d.isActive ? 'Público' : 'Oculto'}</label>
                                        </div>
                                    </td>
                                    <td class="text-end pe-4">
                                        <button class="btn btn-light btn-sm text-primary border shadow-sm btn-edit-dish rounded-circle me-1" data-id="${d.id}" style="width: 32px; height: 32px;"><i class="fa-solid fa-pen pointer-events-none"></i></button>
                                        <button class="btn btn-light btn-sm text-danger border shadow-sm btn-delete-dish rounded-circle" data-id="${d.id}" style="width: 32px; height: 32px;"><i class="fa-solid fa-trash pointer-events-none"></i></button>
                                    </td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Modal -->
            <div class="modal fade" id="dishModal" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content border-0 shadow-lg rounded-4">
                        <div class="modal-header border-0 bg-light pb-0 px-4 pt-4">
                            <h5 class="modal-title fw-bold" id="modalTitle">Nuevo Plato</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body p-4 bg-light">
                            <form id="dish-form">
                                <input type="hidden" id="dish-id">

                                <div id="dish-form-error" class="alert alert-danger py-2 small d-none rounded-3 mb-3"></div>

                                <div class="card shadow-sm border-0 rounded-3 p-3 mb-3">
                                    <div class="form-floating mb-3">
                                        <input type="text" id="dish-name" class="form-control fw-bold" placeholder="Nombre" required>
                                        <label>Nombre del Plato</label>
                                    </div>
                                    <div class="form-floating">
                                        <textarea id="dish-description" class="form-control" placeholder="Descripción" style="height: 80px"></textarea>
                                        <label>Ingredientes / Descripción</label>
                                    </div>
                                </div>

                                <div class="row g-3 mb-3">
                                    <div class="col-6">
                                        <div class="form-floating card shadow-sm border-0 rounded-3">
                                            <input type="number" step="0.01" min="0.01" id="dish-price" class="form-control fw-bold text-success" placeholder="0.00" required>
                                            <label>Precio ($)</label>
                                        </div>
                                    </div>
                                    <div class="col-6">
                                        <div class="form-floating card shadow-sm border-0 rounded-3">
                                            <select id="dish-category" class="form-select" required>
                                                ${state.categories.map(c => `<option value="${c.id}">${c.name}</option>`).join('')}
                                            </select>
                                            <label>Categoría</label>
                                        </div>
                                    </div>
                                </div>

                                <div class="card shadow-sm border-0 rounded-3 p-3 mb-3">
                                    <div class="form-floating mb-3">
                                        <input type="url" id="dish-image" class="form-control" placeholder="URL directa de la imagen (clic derecho → Copiar dirección de imagen)">
                                        <label><i class="fa-solid fa-image me-1"></i>URL de Fotografía (opcional)</label>
                                    </div>
                                    <img id="dish-image-preview" src="${FOOD_PLACEHOLDER}" alt="Vista previa">
                                </div>

                                <div class="card shadow-sm border-0 rounded-3 p-3 mb-4 border-start border-success border-4">
                                    <div class="form-check form-switch m-0">
                                        <input class="form-check-input fs-5 mt-0" type="checkbox" role="switch" id="dish-active" checked>
                                        <label class="form-check-label fw-bold ms-2 mt-1" for="dish-active">Plato Disponible para Clientes</label>
                                    </div>
                                </div>

                                <button type="submit" id="btn-save-dish" class="btn btn-dark w-100 py-3 fw-bold rounded-pill shadow">
                                    <i class="fa-solid fa-floppy-disk me-1"></i>Guardar Plato
                                </button>
                            </form>
                        </div>
                    </div>
                </div>
            </div>
        `;

        const modal = new bootstrap.Modal(document.getElementById('dishModal'));
        const imageInput = document.getElementById('dish-image');
        const imagePreview = document.getElementById('dish-image-preview');

        // Live image preview
        imageInput.addEventListener('input', () => {
            const url = imageInput.value.trim();
            if (url) {
                imagePreview.src = url;
                imagePreview.onerror = () => setFallbackImage(imagePreview);
            } else {
                imagePreview.src = FOOD_PLACEHOLDER;
            }
        });

        document.getElementById('btn-add-new').addEventListener('click', () => {
            document.getElementById('modalTitle').innerText = 'Nuevo Plato';
            document.getElementById('dish-form').reset();
            document.getElementById('dish-id').value = '';
            document.getElementById('dish-form-error').classList.add('d-none');
            imagePreview.src = FOOD_PLACEHOLDER;
        });

        document.querySelectorAll('.btn-edit-dish').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const id = e.currentTarget.dataset.id;
                const dish = dishes.find(d => d.id === id);
                document.getElementById('modalTitle').innerText = 'Editar Plato';
                document.getElementById('dish-id').value = dish.id;
                document.getElementById('dish-name').value = dish.name;
                document.getElementById('dish-description').value = dish.description || '';
                document.getElementById('dish-price').value = dish.price;
                document.getElementById('dish-category').value = dish.category.id;
                document.getElementById('dish-image').value = dish.image || '';
                document.getElementById('dish-active').checked = dish.isActive;
                document.getElementById('dish-form-error').classList.add('d-none');
                imagePreview.src = dish.image || FOOD_PLACEHOLDER;
                imagePreview.onerror = dish.image ? () => setFallbackImage(imagePreview) : null;
                modal.show();
            });
        });

        document.getElementById('dish-form').addEventListener('submit', async (e) => {
            e.preventDefault();
            const errorEl = document.getElementById('dish-form-error');
            errorEl.classList.add('d-none');

            const price = parseFloat(document.getElementById('dish-price').value);
            if (!price || price <= 0) {
                errorEl.textContent = 'El precio debe ser un valor mayor a cero.';
                errorEl.classList.remove('d-none');
                errorEl.classList.add('shake');
                setTimeout(() => errorEl.classList.remove('shake'), 400);
                return;
            }

            const id = document.getElementById('dish-id').value;
            const dishData = {
                name: document.getElementById('dish-name').value.trim(),
                description: document.getElementById('dish-description').value.trim(),
                price,
                category: parseInt(document.getElementById('dish-category').value),
                image: document.getElementById('dish-image').value.trim(),
                isActive: document.getElementById('dish-active').checked
            };

            const btn = document.getElementById('btn-save-dish');
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Guardando...';

            try {
                const response = id
                    ? await api.put(`/Dish/${id}`, dishData)
                    : await api.post('/Dish', dishData);

                if (response && response.id) {
                    modal.hide();
                    showToast(`Plato ${id ? 'actualizado' : 'creado'} exitosamente`);
                    renderManageDishes();
                } else {
                    const msg = response?.message || response?.title || 'Ocurrió un error al guardar el plato.';
                    errorEl.textContent = msg;
                    errorEl.classList.remove('d-none');
                    errorEl.classList.add('shake');
                    setTimeout(() => errorEl.classList.remove('shake'), 400);
                }
            } catch (error) {
                errorEl.textContent = error.message;
                errorEl.classList.remove('d-none');
            } finally {
                btn.disabled = false;
                btn.innerHTML = '<i class="fa-solid fa-floppy-disk me-1"></i>Guardar Plato';
            }
        });

        document.querySelectorAll('.btn-delete-dish').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                if (confirm('¿Estás seguro de eliminar este plato?\n\nSi tiene órdenes asignadas, se realizará un borrado lógico (quedará oculto).')) {
                    const id = e.currentTarget.dataset.id;
                    const response = await api.delete(`/Dish/${id}`);
                    if (response === null) {
                        showToast('Plato eliminado correctamente', 'success');
                        renderManageDishes();
                    } else {
                        showToast(response?.message || 'No se pudo eliminar el plato.', 'error');
                    }
                }
            });
        });

    } catch (error) {
        app.innerHTML = `<div class="alert alert-danger shadow-sm border-0">Error: ${error.message}</div>`;
    }
}

function getStatusBadgeClass(id) {
    switch(id) {
        case 1: return 'bg-secondary';
        case 2: return 'bg-warning text-dark';
        case 3: return 'bg-info text-white';
        case 4: return 'bg-primary';
        case 5: return 'bg-success';
        default: return 'bg-dark';
    }
}

function getStatusBtnClass(id) {
    switch(id) {
        case 1: return 'btn-outline-secondary bg-white';
        case 2: return 'btn-outline-warning bg-white text-dark';
        case 3: return 'btn-outline-info bg-white text-dark';
        case 4: return 'btn-outline-primary bg-white';
        case 5: return 'btn-outline-success bg-white';
        default: return 'btn-outline-dark bg-white';
    }
}

// Global render
function render() {
    switch(state.currentView) {
        case 'login': renderLogin(); break;
        case 'register': renderRegister(); break;
        case 'menu': renderMenu(); break;
        case 'cart': renderCart(); break;
        case 'user-orders': renderUserOrders(); break;
        case 'admin': renderAdmin(); break;
        case 'manage-dishes': renderManageDishes(); break;
        default: renderLogin();
    }
}

// Global Nav setup
window.router = router;
window.renderAdmin = renderAdmin;
window.renderUserOrders = renderUserOrders;
document.getElementById('nav-menu').addEventListener('click', (e) => { e.preventDefault(); router.navigate('menu'); });
document.getElementById('nav-home').addEventListener('click', (e) => { e.preventDefault(); router.navigate('menu'); });
document.getElementById('nav-cart').addEventListener('click', (e) => { e.preventDefault(); router.navigate('cart'); });
document.getElementById('nav-orders').addEventListener('click', (e) => { e.preventDefault(); router.navigate('user-orders'); });
document.getElementById('nav-admin').addEventListener('click', (e) => { e.preventDefault(); router.navigate('admin'); });
document.getElementById('nav-manage-dishes').addEventListener('click', (e) => { e.preventDefault(); router.navigate('manage-dishes'); });

// Init app: si hay sesión persistida, entrar directo a la vista correspondiente.
if (restoreSession()) {
    updateNav();
    router.navigate(state.user.role === 'admin' ? 'admin' : 'menu');
} else {
    router.navigate('login');
}
