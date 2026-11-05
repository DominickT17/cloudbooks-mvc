(() => {
 const form = document.querySelector('.checkout-formulario');
 if (!form) return;
 const numero = form.querySelector('#numero');
 const vencimiento = form.querySelector('#vencimiento');
 const cvv = form.querySelector('#cvv');
 const digitos = valor => valor.replace(/[^0-9]/g, '');
 const reglas = [
  [numero, 16, valor => valor.match(/.{1,4}/g)?.join('-') ?? ''],
  [vencimiento, 4, valor => valor.length > 2 ? `${valor.slice(0, 2)}/${valor.slice(2)}` : valor],
  [cvv, 4, valor => valor]
 ];
 function error(campo, mensaje) {
  document.getElementById(`error-${campo.id}`).textContent = mensaje;
  if (mensaje) campo.setAttribute('aria-invalid', 'true');
  else campo.removeAttribute('aria-invalid');
 }
 for (const [campo, maximo, formato] of reglas) {
  campo.addEventListener('input', () => {
   const posicion = digitos(campo.value.slice(0, campo.selectionStart)).length;
   const limpio = digitos(campo.value).slice(0, maximo);
   campo.value = formato(limpio);
   let cursor = 0, vistos = 0;
   while (cursor < campo.value.length && vistos < posicion) {
    if (/[0-9]/.test(campo.value[cursor])) vistos++;
    cursor++;
   }
   campo.setSelectionRange(cursor, cursor);
   error(campo, '');
  });
  campo.addEventListener('keydown', evento => {
   const inicio = campo.selectionStart, fin = campo.selectionEnd;
   if (inicio !== fin || !['Backspace', 'Delete'].includes(evento.key)) return;
   const indice = evento.key === 'Backspace' ? inicio - 1 : inicio;
   if (indice < 0 || indice >= campo.value.length || /[0-9]/.test(campo.value[indice])) return;
   evento.preventDefault();
   const borrar = evento.key === 'Backspace' ? indice - 1 : indice + 1;
   if (borrar < 0 || borrar >= campo.value.length) return;
   campo.setRangeText('', borrar, borrar + 1, 'start');
   campo.dispatchEvent(new Event('input', { bubbles: true }));
  });
 }
 function validarFecha() {
  const valor = digitos(vencimiento.value);
  if (valor.length !== 4) return 'Ingresa el vencimiento en formato MM/AA.';
  const mes = Number(valor.slice(0, 2)), anio = 2000 + Number(valor.slice(2));
  if (mes < 1 || mes > 12) return 'Ingresa un mes entre 01 y 12.';
  const hoy = new Date();
  if (anio < hoy.getUTCFullYear() || (anio === hoy.getUTCFullYear() && mes < hoy.getUTCMonth() + 1)) return 'La tarjeta simulada está vencida.';
  return '';
 }
 vencimiento.addEventListener('blur', () => { if (vencimiento.value) error(vencimiento, validarFecha()); });
 form.addEventListener('submit', evento => {
  if (form.querySelector('[name="metodo"]:checked')?.value !== '0') return;
  const mensajes = [[numero, digitos(numero.value).length === 16 ? '' : 'Ingresa 16 dígitos.'], [vencimiento, validarFecha()], [cvv, /^[0-9]{3,4}$/.test(cvv.value) ? '' : 'Ingresa 3 o 4 dígitos.']];
  mensajes.forEach(([campo, mensaje]) => error(campo, mensaje));
  const primero = mensajes.find(([, mensaje]) => mensaje);
  if (primero) { evento.preventDefault(); primero[0].focus(); return; }
  numero.value = digitos(numero.value);
 });
})();
