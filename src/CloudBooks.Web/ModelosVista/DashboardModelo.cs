using CloudBooks.Web.Modelos;

namespace CloudBooks.Web.ModelosVista;

public record LibroVendido(int LibroId, string Titulo, int Unidades);
public class DashboardModelo
{
    public decimal Ventas { get; set; }
    public int Pedidos { get; set; }
    public int Pendientes { get; set; }
    public int Aprobados { get; set; }
    public int Enviados { get; set; }
    public int Cancelados { get; set; }
    public int CotizacionesPendientes { get; set; }
    public int Clientes { get; set; }
    public int Institucionales { get; set; }
    public List<Libro> StockBajo { get; set; } = [];
    public List<LibroVendido> MasVendidos { get; set; } = [];
}
