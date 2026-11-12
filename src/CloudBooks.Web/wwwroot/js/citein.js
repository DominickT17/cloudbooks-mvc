(() => {
 const avisoRed = document.getElementById('aviso-red');
 const clavePendiente = 'citein-compra-pendiente';
 function pendiente(valor) { try { if (valor) sessionStorage.setItem(clavePendiente, '1'); else sessionStorage.removeItem(clavePendiente); } catch { /* El servidor sigue evitando duplicados si el navegador bloquea storage. */ } }
 function hayPendiente() { try { return sessionStorage.getItem(clavePendiente) === '1'; } catch { return false; } }
 if (document.querySelector('[data-compra-registrada]')) pendiente(false);
 const textoRed = 'No se pudo conectar con CloudBooks. Comprueba la red e inténtalo nuevamente.';
 function red() { if (avisoRed) avisoRed.hidden = navigator.onLine; }
 window.addEventListener('offline', red); window.addEventListener('online', red); red();
 document.querySelectorAll('form:not([data-compra])').forEach(form => form.addEventListener('submit', async e => {
   e.preventDefault(); const button = form.querySelector('button[type="submit"]');
   if (button?.disabled) return;
   if (button) { button.disabled = true; button.setAttribute('aria-busy', 'true'); }
   const controlador = new AbortController(), limite = setTimeout(() => controlador.abort(), 10000);
   try {
     const respuesta = await fetch(form.action, { method: form.method.toUpperCase(), body: new FormData(form), signal: controlador.signal });
     if (!respuesta.ok) throw Error();
     location.assign(respuesta.url);
   } catch { if (avisoRed) avisoRed.hidden = false; if (button) { button.disabled = false; button.removeAttribute('aria-busy'); } }
   finally { clearTimeout(limite); }
 }));
 window.addEventListener('pageshow', () => document.querySelectorAll('form:not([data-compra]) button[aria-busy]').forEach(b => { b.disabled = false; b.removeAttribute('aria-busy'); }));
 const form = document.querySelector('form[data-compra]'); if (!form) return;
 const boton = form.querySelector('button[type="submit"]'), comprobar = form.querySelector('.comprobar-estado'), mensaje = form.querySelector('.estado-compra');
 let ocupado = false, incierto = false;
 async function fetchConLimite(url, opciones, milisegundos = 10000) {
   const controlador = new AbortController(), limite = setTimeout(() => controlador.abort(), milisegundos);
   try { return await fetch(url, {...opciones, signal: controlador.signal}); } finally { clearTimeout(limite); }
 }
 function bloquear(valor) { ocupado = valor; boton.disabled = valor || incierto; boton.setAttribute('aria-busy', String(valor)); }
 async function consultar() {
   if (ocupado) return; bloquear(true); mensaje.textContent = 'Comprobando el estado de la compra…'; comprobar.hidden = true;
   try {
     const respuesta = await fetchConLimite(form.dataset.estadoUrl, { cache: 'no-store', headers: { Accept: 'application/json' } });
     if (!respuesta.ok || !(respuesta.headers.get('content-type') || '').includes('application/json')) throw Error();
     const estado = await respuesta.json();
     if (estado.registrado && estado.url) { location.assign(estado.url); return; }
     incierto = false; pendiente(false); mensaje.textContent = 'No se registró ningún pedido. Puedes intentar nuevamente.';
   } catch { incierto = true; mensaje.textContent = textoRed + ' Antes de repetir la compra, comprueba su estado.'; comprobar.hidden = false; }
   finally { bloquear(false); }
 }
 comprobar.addEventListener('click', consultar);
 form.addEventListener('submit', async e => {
   e.preventDefault(); if (ocupado || incierto) return;
   pendiente(true); bloquear(true); mensaje.textContent = 'Procesando compra simulada…';
   try {
     const respuesta = await fetchConLimite(form.action, { method: 'POST', body: new FormData(form), headers: { Accept: 'application/json' } });
     if (!(respuesta.headers.get('content-type') || '').includes('application/json')) throw Error();
     const datos = await respuesta.json();
     if (respuesta.ok && datos.url) { location.assign(datos.url); return; }
     if (respuesta.status === 400 && datos.error) { pendiente(false); mensaje.textContent = datos.error; return; }
     throw Error();
   } catch { incierto = true; mensaje.textContent = textoRed; }
   finally { bloquear(false); }
   if (incierto) await consultar();
 });
 window.addEventListener('online', () => { if (incierto && !ocupado) consultar(); });
 if (hayPendiente()) { incierto = true; bloquear(false); consultar(); }
})();
