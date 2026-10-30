using CloudBooks.Web.Modelos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CloudBooks.Web.Servicios;

public class ServicioPdfCotizaciones(IWebHostEnvironment entorno)
{
    public byte[] Generar(Cotizacion cotizacion)
    {
        var logo = Path.Combine(entorno.WebRootPath, "imagenes", "marca", "cloudbooks.png");
        string dinero(decimal importe) => "Q" + importe.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Margin(38); pagina.DefaultTextStyle(t => t.FontSize(10).FontColor("#25364b"));
            pagina.Header().Row(fila =>
            {
                fila.ConstantItem(145).Image(logo);
                fila.RelativeItem().AlignRight().Column(columna =>
                {
                    columna.Item().Text("COTIZACIÓN").FontSize(19).Bold().FontColor("#167ce0");
                    columna.Item().Text(cotizacion.Codigo).Bold();
                    columna.Item().Text("Zona 1, Quetzaltenango, Guatemala");
                    columna.Item().Text("+502 5575 1509 · dominickt1703@gmail.com");
                });
            });
            pagina.Content().PaddingTop(25).Column(columna =>
            {
                columna.Spacing(8);
                columna.Item().Text($"Fecha: {cotizacion.FechaUtc:dd/MM/yyyy} UTC    Válida hasta: {cotizacion.FechaVencimiento:dd/MM/yyyy}    Estado: {cotizacion.Estado}");
                columna.Item().Text($"Cliente: {cotizacion.Solicitud.Usuario.NombreCompleto}");
                columna.Item().Text($"Institución: {cotizacion.Solicitud.PerfilInstitucional.Institucion} · NIT: {cotizacion.Solicitud.PerfilInstitucional.Nit}");
                columna.Item().Text($"Asesor: {cotizacion.Solicitud.Asesor?.NombreCompleto ?? "Sin asignar"}");
                columna.Item().PaddingTop(12).Table(tabla =>
                {
                    tabla.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2); });
                    tabla.Header(h => { foreach (var titulo in new[] { "Concepto", "Cant.", "Precio", "Subtotal" }) h.Cell().Background("#eaf3fc").Padding(6).Text(titulo).Bold(); });
                    foreach (var detalle in cotizacion.Detalles)
                    {
                        tabla.Cell().Padding(6).Text(detalle.Concepto); tabla.Cell().Padding(6).Text(detalle.Cantidad.ToString());
                        tabla.Cell().Padding(6).Text(dinero(detalle.PrecioUnitario)); tabla.Cell().Padding(6).Text(dinero(detalle.Subtotal));
                    }
                });
                columna.Item().AlignRight().Width(230).PaddingTop(12).Column(resumen =>
                {
                    foreach (var (etiqueta, importe) in new[] { ("Subtotal", cotizacion.Subtotal), ("Descuento por volumen", -cotizacion.DescuentoVolumen), ("Descuento institucional", -cotizacion.DescuentoInstitucional), ($"IVA ({cotizacion.TasaImpuesto * 100:0.##} %)", cotizacion.Impuesto), ("Total", cotizacion.Total) })
                        resumen.Item().PaddingVertical(3).Row(f => { f.RelativeItem().Text(etiqueta); f.ConstantItem(85).AlignRight().Text(dinero(importe)); });
                });
            });
            pagina.Footer().AlignCenter().Text("Gracias por confiar en CloudBooks.").FontColor("#52677c");
        })).GeneratePdf();
    }
}
