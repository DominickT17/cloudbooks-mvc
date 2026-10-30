# Arquitectura

CloudBooks es un monolito ASP.NET Core MVC. Los controladores reciben y autorizan peticiones, los servicios concentran las reglas y EF Core persiste las entidades en SQL Server. Razor presenta modelos sin consultar directamente la base.

Identity administra cuentas y roles. Las solicitudes de cuenta y sus cambios de rol se resuelven en una transacción. Las solicitudes comerciales se asignan por carga; el servicio de cotizaciones reutiliza el motor de descuentos y el PDF se genera bajo demanda con QuestPDF, después de comprobar el acceso.

Los roles `ClienteMinorista` y `ClienteInstitucional` son alternativas comerciales: la aprobación retira el primero y asigna el segundo. Otros roles existentes no se eliminan.
