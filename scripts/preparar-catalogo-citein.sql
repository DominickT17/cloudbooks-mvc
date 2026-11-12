-- Únicamente inserta libros ausentes. No actualiza precios, stock ni pedidos existentes.
-- Ejecutar con sqlcmd conectado explícitamente a CloudBooks_CITEIN_2026 (UTF-8).
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'CloudBooks_CITEIN_2026' THROW 50001, 'Base incorrecta: se requiere CloudBooks_CITEIN_2026.', 1;
BEGIN TRANSACTION;
DECLARE @libros TABLE (ISBN nvarchar(20) PRIMARY KEY, Titulo nvarchar(200), Sinopsis nvarchar(max), Anio int, Precio decimal(18,2), Stock int, Imagen nvarchar(300), Formato int, Categoria nvarchar(150), Editorial nvarchar(150));
INSERT @libros VALUES
(N'9786075502090',N'Álgebra',N'Cuarta edición de la serie Baldor. Precio académico simulado.',2019,165,200,N'/imagenes/citein/algebra.svg',1,N'Matemáticas',N'Grupo Editorial Patria'),
(N'9786075502076',N'Aritmética',N'Cuarta edición de la serie Baldor. Precio académico simulado.',2019,150,200,N'/imagenes/citein/aritmetica.svg',1,N'Matemáticas',N'Grupo Editorial Patria'),
(N'9786075502069',N'Geometría y trigonometría',N'Cuarta edición de la serie Baldor. Precio académico simulado.',2019,175,200,N'/imagenes/citein/geometria.svg',1,N'Matemáticas',N'Grupo Editorial Patria'),
(N'9786073221245',N'Física universitaria, volumen 1',N'Decimotercera edición. Precio académico simulado.',2013,235,200,N'/imagenes/citein/fisica-pearson.svg',0,N'Física',N'Pearson Educación'),
(N'9781951693510',N'Cálculo volumen 1',N'Edición digital en español. Acceso gratuito en OpenStax. Precio de demostración, sin entrega de archivo.',2022,35,0,N'/imagenes/citein/calculo.svg',2,N'Matemáticas',N'OpenStax'),
(N'9781951693909',N'Física universitaria volumen 1',N'Edición digital en español. Acceso gratuito en OpenStax. Precio de demostración, sin entrega de archivo.',2021,35,0,N'/imagenes/citein/fisica-digital.svg',2,N'Física',N'OpenStax');
DECLARE @autores TABLE (ISBN nvarchar(20), Nombre nvarchar(150), PRIMARY KEY (ISBN,Nombre));
INSERT @autores VALUES
(N'9786075502090',N'Aurelio Baldor'),(N'9786075502076',N'Aurelio Baldor'),(N'9786075502069',N'Aurelio Baldor'),
(N'9786073221245',N'Hugh D. Young'),(N'9786073221245',N'Roger A. Freedman'),
(N'9781951693510',N'Gilbert Strang'),(N'9781951693510',N'Edwin “Jed” Herman'),
(N'9781951693909',N'Samuel J. Ling'),(N'9781951693909',N'Jeff Sanny'),(N'9781951693909',N'William Moebs');
-- Una coincidencia con otra obra detiene todo: nunca se reescriben productos históricos.
IF EXISTS (SELECT 1 FROM @libros n JOIN dbo.Libros l ON l.ISBN=n.ISBN
    JOIN dbo.Categorias c ON c.Id=l.CategoriaId JOIN dbo.Editoriales e ON e.Id=l.EditorialId
    WHERE l.Titulo<>n.Titulo OR l.AnioEdicion<>n.Anio OR l.Formato<>n.Formato OR l.ImagenPortada<>n.Imagen
       OR c.Nombre<>n.Categoria OR e.Nombre<>n.Editorial
       OR EXISTS (SELECT Nombre FROM @autores a WHERE a.ISBN=n.ISBN EXCEPT SELECT a.Nombre FROM dbo.AutorLibro al JOIN dbo.Autores a ON a.Id=al.AutoresId WHERE al.LibrosId=l.Id)
       OR EXISTS (SELECT a.Nombre FROM dbo.AutorLibro al JOIN dbo.Autores a ON a.Id=al.AutoresId WHERE al.LibrosId=l.Id EXCEPT SELECT Nombre FROM @autores a WHERE a.ISBN=n.ISBN))
    THROW 50002, 'Un ISBN ya existe con datos incompatibles; revisa manualmente antes de continuar.', 1;
INSERT dbo.Categorias(Nombre) SELECT DISTINCT n.Categoria FROM @libros n WHERE NOT EXISTS (SELECT 1 FROM dbo.Categorias c WHERE c.Nombre=n.Categoria);
INSERT dbo.Editoriales(Nombre) SELECT DISTINCT n.Editorial FROM @libros n WHERE NOT EXISTS (SELECT 1 FROM dbo.Editoriales e WHERE e.Nombre=n.Editorial);
INSERT dbo.Autores(Nombre) SELECT DISTINCT n.Nombre FROM @autores n WHERE NOT EXISTS (SELECT 1 FROM dbo.Autores a WHERE a.Nombre=n.Nombre);
DECLARE @nuevos TABLE (Id int, ISBN nvarchar(20));
INSERT dbo.Libros (ISBN,Titulo,Sinopsis,AnioEdicion,PrecioBase,Stock,ImagenPortada,Formato,Activo,EditorialId,CategoriaId,Destacado)
OUTPUT inserted.Id,inserted.ISBN INTO @nuevos
SELECT n.ISBN,n.Titulo,n.Sinopsis,n.Anio,n.Precio,n.Stock,n.Imagen,n.Formato,1,
    (SELECT MIN(Id) FROM dbo.Editoriales WHERE Nombre=n.Editorial),
    (SELECT MIN(Id) FROM dbo.Categorias WHERE Nombre=n.Categoria),0
FROM @libros n WHERE NOT EXISTS (SELECT 1 FROM dbo.Libros l WHERE l.ISBN=n.ISBN);
INSERT dbo.AutorLibro(AutoresId,LibrosId)
SELECT (SELECT MIN(Id) FROM dbo.Autores a WHERE a.Nombre=aut.Nombre),n.Id FROM @nuevos n JOIN @autores aut ON aut.ISBN=n.ISBN;
SELECT l.ISBN,l.Titulo,l.Stock,l.PrecioBase,l.Formato FROM dbo.Libros l JOIN @libros n ON n.ISBN=l.ISBN ORDER BY l.ISBN;
COMMIT;
