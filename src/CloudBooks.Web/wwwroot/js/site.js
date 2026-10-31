document.querySelectorAll('input[type="password"]').forEach(input => {
  const wrapper = document.createElement('div'); wrapper.className = 'campo-clave';
  input.parentNode.insertBefore(wrapper, input); wrapper.appendChild(input);
  const button = document.createElement('button'); button.type = 'button'; button.className = 'mostrar-clave';
  button.setAttribute('aria-label', 'Mostrar contraseña'); button.setAttribute('aria-pressed', 'false'); button.innerHTML = '<i data-lucide="eye"></i>';
  wrapper.appendChild(button);
  button.addEventListener('click', () => { const visible = input.type === 'password'; input.type = visible ? 'text' : 'password'; button.setAttribute('aria-label', visible ? 'Ocultar contraseña' : 'Mostrar contraseña'); button.setAttribute('aria-pressed', String(visible)); button.innerHTML = `<i data-lucide="${visible ? 'eye-off' : 'eye'}"></i>`; lucide.createIcons(); });
});
const menu = document.querySelector('.menu-boton');
menu?.addEventListener('click', () => { const abierto = menu.getAttribute('aria-expanded') === 'true'; menu.setAttribute('aria-expanded', String(!abierto)); document.querySelector('#menu-principal').classList.toggle('abierto', !abierto); });
const clave = document.querySelector('input[name="Contrasena"]');
if (clave && document.querySelector('.requisitos')) {
 const reglas = {longitud: v => v.length >= 6, mayuscula: v => /[A-Z]/.test(v), minuscula: v => /[a-z]/.test(v), numero: v => /[0-9]/.test(v), simbolo: v => /[^a-zA-Z0-9]/.test(v)};
 clave.addEventListener('input', () => document.querySelectorAll('[data-regla]').forEach(item => item.classList.toggle('cumplido', reglas[item.dataset.regla](clave.value))));
}
lucide.createIcons();
document.querySelectorAll('[data-cantidad]').forEach(button => button.addEventListener('click', () => {
 const input = button.parentElement.querySelector('input[type="number"]');
 const mensaje = button.closest('form')?.querySelector('.mensaje-cantidad');
 if (button.dataset.cantidad === '1' && Number(input.value) >= Number(input.max)) { if (mensaje) mensaje.textContent = 'Has alcanzado la cantidad disponible.'; return; }
 if (mensaje) mensaje.textContent = '';
 if (button.dataset.cantidad === '1') input.stepUp(); else input.stepDown();
 input.dispatchEvent(new Event('change', { bubbles: true }));
}));

// El servidor conserva las reglas y devuelve el mismo carrito usado en la página completa.
const dialogoCarrito = document.querySelector('#mini-carrito');
const contenidoCarrito = document.querySelector('#contenido-mini-carrito');
let ultimoFoco;
function abrirCarrito(html) {
 contenidoCarrito.innerHTML = html;
 const cantidad = contenidoCarrito.querySelector('[data-total-carrito]')?.dataset.totalCarrito;
 document.querySelectorAll('.nav-carrito .contador').forEach(elemento => elemento.textContent = cantidad);
 ultimoFoco = document.activeElement;
 if (!dialogoCarrito.open) dialogoCarrito.showModal();
 contenidoCarrito.querySelector('[data-cerrar-mini]')?.focus();
}
dialogoCarrito?.addEventListener('click', evento => { if (evento.target.closest('[data-cerrar-mini]') || evento.target === dialogoCarrito) dialogoCarrito.close(); });
dialogoCarrito?.addEventListener('close', () => ultimoFoco?.focus());
document.querySelectorAll('[data-abrir-mini]').forEach(enlace => enlace.addEventListener('click', async evento => {
 evento.preventDefault(); try { const respuesta = await fetch('/Carrito/Mini'); if (!respuesta.ok) throw new Error(); abrirCarrito(await respuesta.text()); } catch { location.href = enlace.href; }
}));
document.querySelectorAll('form[data-mini-carrito]').forEach(formulario => formulario.addEventListener('submit', async evento => {
 if (evento.submitter?.name === 'comprarAhora' || !dialogoCarrito?.showModal) return;
 evento.preventDefault(); const boton = evento.submitter; if (boton) boton.disabled = true;
 try { const respuesta = await fetch(formulario.action, { method:'POST', body:new FormData(formulario), headers:{'X-Requested-With':'XMLHttpRequest'} }); if (!respuesta.ok) throw new Error(); abrirCarrito(await respuesta.text()); }
 catch { location.href = "/Carrito"; } finally { if (boton) boton.disabled = false; }
}));
