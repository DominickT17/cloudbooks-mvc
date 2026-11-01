# CREDENCIALES DE PRUEBA — SOLO PARA DEMOSTRACIÓN ACADÉMICA

El inicializador automático crea estas cuentas solamente en Development. El SQL de entrega también las incluye para reconstruir la demostración. Son exclusivamente académicas y no deben usarse en producción.

La cuenta institucional incluye un perfil ficticio de demostración para probar solicitudes y cotizaciones. No representa datos reales.

Contraseña de todas las cuentas: `CloudBooksDemo2026!`

| Cuenta | Rol |
| --- | --- |
| admin.dominick@cloudbooks.local | Administrador |
| admin.yeisson@cloudbooks.local | Administrador |
| asesor@cloudbooks.local | AsesorVentas |
| cliente@cloudbooks.local | ClienteMinorista |
| institucion@cloudbooks.local | ClienteInstitucional |

## Tarjetas ficticias

Tarjetas ficticias de demostración. No utilizar información bancaria real. El simulador de tarjeta se habilita únicamente en Development y no se conecta a bancos. Usa una fecha futura en formato MM/AA y un CVV ficticio de tres o cuatro dígitos.

| Número ficticio | Resultado |
| --- | --- |
| `4000 0000 0000 0002` | Autorizada |
| `4000 0000 0000 0010` | Fondos insuficientes |
| `4000 0000 0000 0028` | Rechazada |
| `4000 0000 0000 0036` | Bloqueada |

Una fecha anterior al mes actual simula una tarjeta vencida. No se almacena el número completo ni el CVV; el pedido conserva solo los últimos cuatro dígitos cuando el pago simulado se autoriza.
