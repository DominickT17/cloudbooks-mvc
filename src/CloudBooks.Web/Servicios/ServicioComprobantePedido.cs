using CloudBooks.Web.Modelos;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CloudBooks.Web.Servicios;

public class ServicioComprobantePedido(IWebHostEnvironment entorno)
{
    public byte[] Generar(Pedido pedido)
    {
        var logo = Path.Combine(entorno.WebRootPath, "imagenes", "marca", "cloudbooks.png");
        static string Dinero(decimal importe) => "Q" + importe.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Margin(38);
            pagina.DefaultTextStyle(estilo => estilo.FontSize(10).FontColor("#25364b"));
            pagina.Header().Row(fila =>
            {
                fila.ConstantItem(145).Image(logo);
                fila.RelativeItem().AlignRight().Column(columna => { columna.Item().Text("COMPROBANTE DE COMPRA").FontSize(17).Bold().FontColor("#167ce0"); columna.Item().Text(pedido.Codigo).Bold(); columna.Item().Text("Documento académico · No es factura fiscal"); });
            });
            pagina.Content().PaddingTop(25).Column(columna =>
            {
                columna.Spacing(8);
                columna.Item().Text($"Fecha: {pedido.FechaUtc:dd/MM/yyyy HH:mm} UTC · Estado: {pedido.EstadoTexto}");
                columna.Item().Text($"Cliente: {pedido.Usuario.NombreCompleto}");
                var metodo = pedido.MetodoPago == MetodoPagoSimulado.TarjetaSimulada ? "Tarjeta simulada" : "Transferencia simulada";
                columna.Item().Text($"Método: {metodo}" + (pedido.UltimosCuatro == null ? "" : $" · Terminación {pedido.UltimosCuatro}"));
                columna.Item().PaddingTop(12).Table(tabla =>
                {
                    tabla.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2); });
                    tabla.Header(h => { foreach (var titulo in new[] { "Libro", "Cant.", "Precio", "Subtotal" }) h.Cell().Background("#eaf3fc").Padding(6).Text(titulo).Bold(); });
                    foreach (var detalle in pedido.Detalles) { tabla.Cell().Padding(6).Text(detalle.Titulo); tabla.Cell().Padding(6).Text(detalle.Cantidad.ToString()); tabla.Cell().Padding(6).Text(Dinero(detalle.PrecioUnitario)); tabla.Cell().Padding(6).Text(Dinero(detalle.Subtotal)); }
                });
                columna.Item().AlignRight().Width(230).PaddingTop(12).Column(resumen =>
                {
                    foreach (var (etiqueta, importe) in new[] { ("Subtotal", pedido.Subtotal), ("Descuento por volumen", -pedido.DescuentoVolumen), ("Descuento institucional", -pedido.DescuentoInstitucional), ("IVA", pedido.Impuesto), ("Total", pedido.Total) })
                        resumen.Item().PaddingVertical(3).Row(f => { f.RelativeItem().Text(etiqueta); f.ConstantItem(85).AlignRight().Text(Dinero(importe)); });
                });
            });
            pagina.Footer().AlignCenter().Text("Gracias por confiar en CloudBooks.").FontColor("#52677c");
        })).GeneratePdf();
    }
}
