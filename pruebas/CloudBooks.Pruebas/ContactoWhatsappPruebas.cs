using CloudBooks.Web.Servicios;

namespace CloudBooks.Pruebas;

public class ContactoWhatsappPruebas
{
    [Theory]
    [InlineData("5575 1509", "+50255751509")]
    [InlineData("+502 5575-1509", "+50255751509")]
    [InlineData("123", null)]
    [InlineData("+155751509", null)]
    public void NormalizaTelefonosGuatemaltecos(string entrada, string? esperado) => Assert.Equal(esperado, ContactoWhatsapp.NormalizarGuatemala(entrada));

    [Fact]
    public void EnlaceCodificaElMensaje()
    {
        Assert.Equal("https://wa.me/50255751509?text=Hola%2C%20CloudBooks%20%C2%BFc%C3%B3mo%20est%C3%A1s%3F", ContactoWhatsapp.CrearEnlace("55751509", "Hola, CloudBooks ¿cómo estás?"));
        Assert.Null(ContactoWhatsapp.CrearEnlace("123", "Hola"));
    }
}
