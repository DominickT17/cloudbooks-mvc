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
 if (button.dataset.cantidad === '1') input.stepUp(); else input.stepDown();
 input.dispatchEvent(new Event('change', { bubbles: true }));
}));
