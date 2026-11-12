"""Prueba HTTP optativa: crea dos pedidos académicos en CloudBooks_CITEIN_2026.
Ejecutar solo con el servidor CITEIN local aislado y con permiso para conservar esos pedidos.
"""
import concurrent.futures
import html
import http.cookiejar
import json
import re
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request

BASE = "http://127.0.0.1:5199"


def cliente():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))


def solicitar(c, ruta, datos=None, json_respuesta=False):
    cuerpo = None if datos is None else urllib.parse.urlencode(datos).encode()
    cabeceras = {"Accept": "application/json"} if json_respuesta else {}
    solicitud = urllib.request.Request(BASE + ruta, data=cuerpo, headers=cabeceras)
    with c.open(solicitud, timeout=30) as respuesta:
        return respuesta.geturl(), respuesta.read().decode()


def token(pagina):
    encontrado = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)', pagina)
    if not encontrado:
        raise AssertionError("No se encontró token antifalsificación")
    return html.unescape(encontrado.group(1))


def sql(consulta):
    resultado = subprocess.run(["sqlcmd", "-S", r".\SQLEXPRESS", "-d", "CloudBooks_CITEIN_2026", "-E", "-C", "-b", "-h", "-1", "-W", "-Q", "SET NOCOUNT ON; " + consulta], capture_output=True, text=True, check=True)
    return resultado.stdout.strip()


def iniciar(c):
    _, pagina = solicitar(c, "/citein")
    _, pagina = solicitar(c, "/citein/iniciar", {"__RequestVerificationToken": token(pagina)})
    return html.unescape(pagina)


def elegir(c, pagina, isbn):
    libro = sql("SELECT Id FROM Libros WHERE ISBN='" + isbn + "'")
    _, pagina = solicitar(c, "/citein/elegir", {"__RequestVerificationToken": token(pagina), "libroId": libro})
    if "Confirma tu compra" not in pagina:
        raise AssertionError("No se pudo seleccionar el libro")
    return html.unescape(pagina)


def comprar(c, pagina):
    _, cuerpo = solicitar(c, "/citein/comprar", {"__RequestVerificationToken": token(pagina)}, True)
    return json.loads(cuerpo)["url"]


def main():
    if sys.argv[1:] != ["--ejecutar"]:
        raise SystemExit("Se crearán dos pedidos reales de demostración. Ejecuta con --ejecutar si ese alcance es correcto.")
    a, b = cliente(), cliente()
    pagina_a, pagina_b = iniciar(a), iniciar(b)
    assert pagina_a.count('class="tarjeta"') == 3
    _, completo = solicitar(a, "/citein/libros?todos=1")
    assert completo.count('class="tarjeta"') == 6
    fisico = elegir(a, pagina_a, "9786075502090")
    stock = int(sql("SELECT Stock FROM Libros WHERE ISBN='9786075502090'"))
    assert stock > 20, "La prueba requiere suficientes existencias para evitar revisión"
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as grupo:
        urls = list(grupo.map(lambda _: comprar(a, fisico), range(2)))
    assert urls[0] == urls[1]
    pedido = int(urls[0].rsplit("/", 1)[1])
    assert int(sql(f"SELECT COUNT(*) FROM Pedidos WHERE UsuarioId=(SELECT UsuarioId FROM Pedidos WHERE Id={pedido})")) == 1
    assert int(sql("SELECT Stock FROM Libros WHERE ISBN='9786075502090'")) == stock - 1
    _, estado = solicitar(a, "/citein/compra/estado")
    assert json.loads(estado)["url"] == urls[0]
    recargada, _ = solicitar(a, "/citein/confirmar")
    assert recargada.endswith(urls[0]), "Al recargar se debe recuperar el pedido ya registrado"
    nuevo_libro = sql("SELECT Id FROM Libros WHERE ISBN='9781951693510'")
    ruta, _ = solicitar(a, "/citein/elegir", {"__RequestVerificationToken": token(fisico), "libroId": nuevo_libro})
    assert ruta.endswith(urls[0]), "La identidad ya usada no debe iniciar una segunda compra"
    digital = elegir(b, pagina_b, "9781951693510")
    url_digital = comprar(b, digital)
    assert url_digital != urls[0]
    assert int(sql("SELECT Stock FROM Libros WHERE ISBN='9781951693510'")) == 0
    try:
        solicitar(a, url_digital)
        raise AssertionError("Una sesión pudo ver el pedido de otra")
    except urllib.error.HTTPError as error:
        assert error.code == 404
    print("Correcto: destacados, catálogo completo, doble confirmación, estado, ebook y aislamiento.")
    print("Pedidos conservados:", urls[0], url_digital)


if __name__ == "__main__":
    main()
