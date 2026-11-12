(() => {
 const panel = document.getElementById('monitor-detalle'), historial = document.getElementById('historial-pedidos'), tarjeta = document.getElementById('panel-ultimo'), aviso = document.getElementById('aviso-monitor');
 const conexion = document.getElementById('conexion'), botonModo = document.getElementById('modo-presentacion');
 let anterior = null, timer, enCurso = false, errores = 0, avisoTimer;
 const dinero = n => 'Q ' + Number(n).toFixed(2);
 function texto(tag, clase, valor) { const n = document.createElement(tag); if (clase) n.className = clase; n.textContent = String(valor); return n; }
 function campo(titulo, valor) { const d = document.createElement('div'); d.append(texto('span','',titulo), texto('strong','',valor)); return d; }
 function actualizarPedido(p) {
   panel.replaceChildren(); if (!p) { panel.className = 'monitor-vacio'; panel.textContent = 'Aún no se han recibido pedidos de demostración.'; return; }
   panel.className = 'monitor-pedido';
   const portada = document.createElement('div'); portada.className = 'monitor-portada'; const img = document.createElement('img'); img.src = p.imagen; img.alt = 'Cubierta ilustrativa de ' + p.libro; portada.append(img);
   const info = document.createElement('div'); info.className = 'monitor-info'; const encabezado = document.createElement('div'); encabezado.className = 'monitor-pedido-cabecera'; encabezado.append(texto('span','monitor-codigo',p.codigo),texto('span',p.enRevision ? 'monitor-estado revision' : 'monitor-estado',p.estado));
   info.append(encabezado,texto('h3','',p.libro),texto('p','monitor-formato',p.digital ? 'Libro digital' : 'Libro físico'),texto('p','monitor-cliente',p.cliente));
   const datos = document.createElement('div'); datos.className = 'monitor-datos'; datos.append(campo('Total',dinero(p.total)),campo('Cantidad',p.cantidad + (p.cantidad === 1 ? ' ejemplar' : ' ejemplares')),campo('Fecha y hora',p.hora)); info.append(datos);
   const inventario = texto('p','monitor-inventario',p.digital ? 'Libro digital — Sin descuento de inventario físico' : 'Existencias físicas actuales: ' + p.existencias); info.append(inventario);
   if (p.enRevision) info.append(texto('p','monitor-revision','Pendiente de revisión administrativa. El inventario físico todavía no ha sido descontado.'));
   const enlace = texto('a','monitor-boton','Ver detalle del pedido'); enlace.href = p.detalleUrl; info.append(enlace); panel.append(portada,info);
 }
 function actualizarHistorial(pedidos) {
   historial.replaceChildren(); if (!pedidos.length) { historial.append(texto('p','monitor-vacio','Todavía no hay pedidos CITEIN.')); return; }
   for (const p of pedidos) { const fila = document.createElement('div'); fila.className = 'monitor-fila'; const primero = document.createElement('div'); primero.append(texto('strong','',p.codigo),texto('span','',p.libro)); const segundo = document.createElement('div'); segundo.append(texto('strong','',dinero(p.total)),texto('span','',p.estado + ' · ' + p.hora)); fila.append(primero,segundo); historial.append(fila); }
 }
 async function actualizar() {
   if (document.hidden || enCurso) return; enCurso = true;
   const controlador = new AbortController(); const limite = setTimeout(() => controlador.abort(), 6000);
   try {
     const r = await fetch('/citein/monitor/datos', {cache:'no-store',headers:{Accept:'application/json'},signal:controlador.signal});
     if (!r.ok || !(r.headers.get('content-type') || '').includes('application/json')) throw Error();
     const datos = await r.json(); if (!Array.isArray(datos.pedidos)) throw Error();
     errores = 0; conexion.textContent = 'Conectado'; conexion.dataset.estado = 'conectado';
     document.getElementById('total-pedidos').textContent = datos.total; document.getElementById('ultima-actualizacion').textContent = datos.actualizacion;
     const reciente = datos.pedidos[0]; document.getElementById('ultimo-codigo').textContent = reciente?.codigo || 'Sin pedidos';
     if (reciente && anterior !== null && reciente.id > anterior) { aviso.textContent = 'Nuevo pedido recibido'; tarjeta.classList.remove('pedido-nuevo'); void tarjeta.offsetWidth; tarjeta.classList.add('pedido-nuevo'); clearTimeout(avisoTimer); avisoTimer = setTimeout(() => { aviso.textContent = ''; tarjeta.classList.remove('pedido-nuevo'); }, 2500); }
     anterior = reciente?.id ?? 0;
     actualizarPedido(reciente); actualizarHistorial(datos.pedidos);
   } catch { errores++; conexion.textContent = errores === 1 ? 'Reconectando' : 'Sin conexión con el servidor'; conexion.dataset.estado = 'error'; }
   finally { clearTimeout(limite); enCurso = false; }
 }
 function iniciar() { clearInterval(timer); if (!document.hidden) { actualizar(); timer = setInterval(actualizar, 4000); } }
 document.addEventListener('visibilitychange', iniciar); window.addEventListener('pagehide', () => { clearInterval(timer); clearTimeout(avisoTimer); });
 botonModo.addEventListener('click', async () => {
   const activo = document.body.classList.toggle('modo-presentacion'); botonModo.textContent = activo ? 'Salir de presentación' : 'Modo presentación'; botonModo.setAttribute('aria-pressed', String(activo));
   try { if (activo && !document.fullscreenElement) await document.documentElement.requestFullscreen(); else if (!activo && document.fullscreenElement) await document.exitFullscreen(); } catch { /* La vista ampliada también funciona sin la API de pantalla completa. */ }
 });
 document.addEventListener('fullscreenchange', () => { if (!document.fullscreenElement && document.body.classList.contains('modo-presentacion')) { document.body.classList.remove('modo-presentacion'); botonModo.textContent = 'Modo presentación'; botonModo.setAttribute('aria-pressed','false'); } });
 iniciar();
})();
