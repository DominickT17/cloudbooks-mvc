IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [NombreCompleto] nvarchar(max) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [Autores] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(150) NOT NULL,
        CONSTRAINT [PK_Autores] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [Categorias] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Categorias] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [Editoriales] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(150) NOT NULL,
        CONSTRAINT [PK_Editoriales] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [Libros] (
        [Id] int NOT NULL IDENTITY,
        [ISBN] nvarchar(20) NOT NULL,
        [Titulo] nvarchar(200) NOT NULL,
        [Sinopsis] nvarchar(max) NOT NULL,
        [AnioEdicion] int NOT NULL,
        [PrecioBase] decimal(18,2) NOT NULL,
        [Stock] int NOT NULL,
        [ImagenPortada] nvarchar(300) NOT NULL,
        [Formato] int NOT NULL,
        [Activo] bit NOT NULL,
        [EditorialId] int NOT NULL,
        [CategoriaId] int NOT NULL,
        CONSTRAINT [PK_Libros] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Libros_Categorias_CategoriaId] FOREIGN KEY ([CategoriaId]) REFERENCES [Categorias] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Libros_Editoriales_EditorialId] FOREIGN KEY ([EditorialId]) REFERENCES [Editoriales] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE TABLE [AutorLibro] (
        [AutoresId] int NOT NULL,
        [LibrosId] int NOT NULL,
        CONSTRAINT [PK_AutorLibro] PRIMARY KEY ([AutoresId], [LibrosId]),
        CONSTRAINT [FK_AutorLibro_Autores_AutoresId] FOREIGN KEY ([AutoresId]) REFERENCES [Autores] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AutorLibro_Libros_LibrosId] FOREIGN KEY ([LibrosId]) REFERENCES [Libros] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE INDEX [IX_AutorLibro_LibrosId] ON [AutorLibro] ([LibrosId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE INDEX [IX_Libros_CategoriaId] ON [Libros] ([CategoriaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE INDEX [IX_Libros_EditorialId] ON [Libros] ([EditorialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Libros_ISBN] ON [Libros] ([ISBN]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030052958_Inicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030052958_Inicial', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054512_CarritoPersistente'
)
BEGIN
    CREATE TABLE [Carritos] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Carritos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Carritos_AspNetUsers_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054512_CarritoPersistente'
)
BEGIN
    CREATE TABLE [DetallesCarrito] (
        [Id] int NOT NULL IDENTITY,
        [CarritoId] int NOT NULL,
        [LibroId] int NOT NULL,
        [Cantidad] int NOT NULL,
        CONSTRAINT [PK_DetallesCarrito] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DetalleCarrito_Cantidad] CHECK ([Cantidad] >= 1),
        CONSTRAINT [FK_DetallesCarrito_Carritos_CarritoId] FOREIGN KEY ([CarritoId]) REFERENCES [Carritos] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DetallesCarrito_Libros_LibroId] FOREIGN KEY ([LibroId]) REFERENCES [Libros] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054512_CarritoPersistente'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Carritos_UsuarioId] ON [Carritos] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054512_CarritoPersistente'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DetallesCarrito_CarritoId_LibroId] ON [DetallesCarrito] ([CarritoId], [LibroId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054512_CarritoPersistente'
)
BEGIN
    CREATE INDEX [IX_DetallesCarrito_LibroId] ON [DetallesCarrito] ([LibroId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054512_CarritoPersistente'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030054512_CarritoPersistente', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054719_Pedidos'
)
BEGIN
    CREATE TABLE [Pedidos] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioId] nvarchar(450) NOT NULL,
        [FechaUtc] datetime2 NOT NULL,
        [Estado] int NOT NULL,
        [MetodoPago] int NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [DescuentoVolumen] decimal(18,2) NOT NULL,
        [DescuentoInstitucional] decimal(18,2) NOT NULL,
        [Impuesto] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Pedidos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pedidos_AspNetUsers_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054719_Pedidos'
)
BEGIN
    CREATE TABLE [DetallesPedido] (
        [Id] int NOT NULL IDENTITY,
        [PedidoId] int NOT NULL,
        [LibroId] int NOT NULL,
        [Titulo] nvarchar(200) NOT NULL,
        [Formato] int NOT NULL,
        [Cantidad] int NOT NULL,
        [PrecioUnitario] decimal(18,2) NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_DetallesPedido] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DetallePedido_Cantidad] CHECK ([Cantidad] >= 1),
        CONSTRAINT [FK_DetallesPedido_Libros_LibroId] FOREIGN KEY ([LibroId]) REFERENCES [Libros] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DetallesPedido_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054719_Pedidos'
)
BEGIN
    CREATE INDEX [IX_DetallesPedido_LibroId] ON [DetallesPedido] ([LibroId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054719_Pedidos'
)
BEGIN
    CREATE INDEX [IX_DetallesPedido_PedidoId] ON [DetallesPedido] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054719_Pedidos'
)
BEGIN
    CREATE INDEX [IX_Pedidos_UsuarioId] ON [Pedidos] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030054719_Pedidos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030054719_Pedidos', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055429_SolicitudesInstitucionales'
)
BEGIN
    CREATE TABLE [PerfilesInstitucionales] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioId] nvarchar(450) NOT NULL,
        [Institucion] nvarchar(160) NOT NULL,
        [Nit] nvarchar(30) NOT NULL,
        [TipoInstitucion] nvarchar(100) NOT NULL,
        [Telefono] nvarchar(30) NOT NULL,
        [Direccion] nvarchar(250) NOT NULL,
        [Representante] nvarchar(160) NOT NULL,
        CONSTRAINT [PK_PerfilesInstitucionales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PerfilesInstitucionales_AspNetUsers_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055429_SolicitudesInstitucionales'
)
BEGIN
    CREATE TABLE [SolicitudesInstitucionales] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioId] nvarchar(450) NOT NULL,
        [Institucion] nvarchar(160) NOT NULL,
        [Nit] nvarchar(30) NOT NULL,
        [TipoInstitucion] nvarchar(100) NOT NULL,
        [Telefono] nvarchar(30) NOT NULL,
        [Direccion] nvarchar(250) NOT NULL,
        [Representante] nvarchar(160) NOT NULL,
        [FechaUtc] datetime2 NOT NULL,
        [Estado] int NOT NULL,
        CONSTRAINT [PK_SolicitudesInstitucionales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SolicitudesInstitucionales_AspNetUsers_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055429_SolicitudesInstitucionales'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PerfilesInstitucionales_UsuarioId] ON [PerfilesInstitucionales] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055429_SolicitudesInstitucionales'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SolicitudesInstitucionales_UsuarioId] ON [SolicitudesInstitucionales] ([UsuarioId]) WHERE [Estado] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055429_SolicitudesInstitucionales'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030055429_SolicitudesInstitucionales', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055643_SolicitudesEspeciales'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Activo] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055643_SolicitudesEspeciales'
)
BEGIN
    CREATE TABLE [SolicitudesCotizacion] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioId] nvarchar(450) NOT NULL,
        [PerfilInstitucionalId] int NOT NULL,
        [AsesorId] nvarchar(450) NULL,
        [Titulo] nvarchar(200) NOT NULL,
        [Cantidad] int NOT NULL,
        [Acabado] nvarchar(50) NOT NULL,
        [Papel] nvarchar(50) NOT NULL,
        [FechaRequerida] date NOT NULL,
        [Observaciones] nvarchar(1000) NULL,
        [FechaUtc] datetime2 NOT NULL,
        [Estado] int NOT NULL,
        CONSTRAINT [PK_SolicitudesCotizacion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SolicitudCotizacion_Cantidad] CHECK ([Cantidad] > 0),
        CONSTRAINT [FK_SolicitudesCotizacion_AspNetUsers_AsesorId] FOREIGN KEY ([AsesorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SolicitudesCotizacion_AspNetUsers_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SolicitudesCotizacion_PerfilesInstitucionales_PerfilInstitucionalId] FOREIGN KEY ([PerfilInstitucionalId]) REFERENCES [PerfilesInstitucionales] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055643_SolicitudesEspeciales'
)
BEGIN
    CREATE INDEX [IX_SolicitudesCotizacion_AsesorId] ON [SolicitudesCotizacion] ([AsesorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055643_SolicitudesEspeciales'
)
BEGIN
    CREATE INDEX [IX_SolicitudesCotizacion_PerfilInstitucionalId] ON [SolicitudesCotizacion] ([PerfilInstitucionalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055643_SolicitudesEspeciales'
)
BEGIN
    CREATE INDEX [IX_SolicitudesCotizacion_UsuarioId] ON [SolicitudesCotizacion] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055643_SolicitudesEspeciales'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030055643_SolicitudesEspeciales', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055912_Cotizaciones'
)
BEGIN
    CREATE TABLE [Cotizaciones] (
        [Id] int NOT NULL IDENTITY,
        [SolicitudCotizacionId] int NOT NULL,
        [FechaUtc] datetime2 NOT NULL,
        [FechaVencimiento] date NOT NULL,
        [Estado] int NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [DescuentoVolumen] decimal(18,2) NOT NULL,
        [DescuentoInstitucional] decimal(18,2) NOT NULL,
        [Impuesto] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Cotizaciones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cotizaciones_SolicitudesCotizacion_SolicitudCotizacionId] FOREIGN KEY ([SolicitudCotizacionId]) REFERENCES [SolicitudesCotizacion] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055912_Cotizaciones'
)
BEGIN
    CREATE TABLE [DetallesCotizacion] (
        [Id] int NOT NULL IDENTITY,
        [CotizacionId] int NOT NULL,
        [Concepto] nvarchar(200) NOT NULL,
        [Cantidad] int NOT NULL,
        [PrecioUnitario] decimal(18,2) NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_DetallesCotizacion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DetalleCotizacion_Cantidad] CHECK ([Cantidad] > 0),
        CONSTRAINT [FK_DetallesCotizacion_Cotizaciones_CotizacionId] FOREIGN KEY ([CotizacionId]) REFERENCES [Cotizaciones] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055912_Cotizaciones'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Cotizaciones_SolicitudCotizacionId] ON [Cotizaciones] ([SolicitudCotizacionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055912_Cotizaciones'
)
BEGIN
    CREATE INDEX [IX_DetallesCotizacion_CotizacionId] ON [DetallesCotizacion] ([CotizacionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030055912_Cotizaciones'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030055912_Cotizaciones', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030061039_ConcurrenciaStock'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030061039_ConcurrenciaStock', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030062138_ParametrosSistema'
)
BEGIN
    ALTER TABLE [Pedidos] ADD [TasaImpuesto] decimal(5,4) NOT NULL DEFAULT 0.12;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030062138_ParametrosSistema'
)
BEGIN
    ALTER TABLE [Cotizaciones] ADD [TasaImpuesto] decimal(5,4) NOT NULL DEFAULT 0.12;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030062138_ParametrosSistema'
)
BEGIN
    CREATE TABLE [ConfiguracionSistema] (
        [Id] int NOT NULL IDENTITY,
        [TasaImpuesto] decimal(5,4) NOT NULL,
        [UmbralStockBajo] int NOT NULL,
        [DiasVigencia] int NOT NULL,
        CONSTRAINT [PK_ConfiguracionSistema] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030062138_ParametrosSistema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DiasVigencia', N'TasaImpuesto', N'UmbralStockBajo') AND [object_id] = OBJECT_ID(N'[ConfiguracionSistema]'))
        SET IDENTITY_INSERT [ConfiguracionSistema] ON;
    EXEC(N'INSERT INTO [ConfiguracionSistema] ([Id], [DiasVigencia], [TasaImpuesto], [UmbralStockBajo])
    VALUES (1, 15, 0.12, 5)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DiasVigencia', N'TasaImpuesto', N'UmbralStockBajo') AND [object_id] = OBJECT_ID(N'[ConfiguracionSistema]'))
        SET IDENTITY_INSERT [ConfiguracionSistema] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030062138_ParametrosSistema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030062138_ParametrosSistema', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031042724_LibrosDestacados'
)
BEGIN
    ALTER TABLE [Libros] ADD [Destacado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031042724_LibrosDestacados'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031042724_LibrosDestacados', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031225336_RevisionSeguridadPedidos'
)
BEGIN
    ALTER TABLE [Pedidos] ADD [AltaPrioridad] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031225336_RevisionSeguridadPedidos'
)
BEGIN
    ALTER TABLE [Pedidos] ADD [MotivosRevision] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031225336_RevisionSeguridadPedidos'
)
BEGIN
    ALTER TABLE [Pedidos] ADD [Revisado] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031225336_RevisionSeguridadPedidos'
)
BEGIN
    ALTER TABLE [Pedidos] ADD [TitularTarjeta] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031225336_RevisionSeguridadPedidos'
)
BEGIN
    ALTER TABLE [ConfiguracionSistema] ADD [MontoRevision] decimal(18,2) NOT NULL DEFAULT 10000.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031225336_RevisionSeguridadPedidos'
)
BEGIN
    CREATE TABLE [NotificacionesPedido] (
        [Id] int NOT NULL IDENTITY,
        [PedidoId] int NOT NULL,
        [Destinatario] nvarchar(30) NOT NULL,
        [Mensaje] nvarchar(500) NOT NULL,
        [FechaUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_NotificacionesPedido] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NotificacionesPedido_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031225336_RevisionSeguridadPedidos'
)
BEGIN
    EXEC(N'UPDATE [ConfiguracionSistema] SET [MontoRevision] = 10000.0
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031225336_RevisionSeguridadPedidos'
)
BEGIN
    CREATE INDEX [IX_NotificacionesPedido_PedidoId] ON [NotificacionesPedido] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031225336_RevisionSeguridadPedidos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031225336_RevisionSeguridadPedidos', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101044608_RevisionComercialInventario'
)
BEGIN
    ALTER TABLE [Pedidos] ADD [TipoRevision] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101044608_RevisionComercialInventario'
)
BEGIN
    ALTER TABLE [ConfiguracionSistema] ADD [CantidadRevisionMinorista] int NOT NULL DEFAULT 30;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101044608_RevisionComercialInventario'
)
BEGIN
    ALTER TABLE [ConfiguracionSistema] ADD [PorcentajeRevisionInventario] int NOT NULL DEFAULT 70;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101044608_RevisionComercialInventario'
)
BEGIN
    EXEC(N'UPDATE [ConfiguracionSistema] SET [CantidadRevisionMinorista] = 30, [PorcentajeRevisionInventario] = 70
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101044608_RevisionComercialInventario'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101044608_RevisionComercialInventario', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101044802_UltimosCuatroTarjeta'
)
BEGIN
    ALTER TABLE [Pedidos] ADD [UltimosCuatro] nvarchar(4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101044802_UltimosCuatroTarjeta'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101044802_UltimosCuatroTarjeta', N'10.0.14');
END;

COMMIT;
GO

-- Datos de demostración académica; ejecutar solo en una base vacía.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'24df8016-c172-4314-b387-b695603bcd7e', N'Administrador', N'ADMINISTRADOR', N'3cc68d91-380c-4cf9-a657-137b3955c63c');
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'2f4b5cda-fd3f-4ebe-89de-af9c32d92782', N'AsesorVentas', N'ASESORVENTAS', N'07b76471-83d7-464e-ab34-5cfd9bcceca8');
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'3c099731-dffd-4cf9-9b0a-7f9b7c9406b0', N'ClienteInstitucional', N'CLIENTEINSTITUCIONAL', N'1d0ddc66-37e7-4f69-8404-2a5719d7ab6a');
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'd0ca1f2c-6c87-4891-b0f3-a33646a386c3', N'ClienteMinorista', N'CLIENTEMINORISTA', N'10250ef8-5fc7-4576-8854-90bae9017659');
INSERT INTO [AspNetUsers] ([Id], [NombreCompleto], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Activo]) VALUES (N'15ba725c-b5c7-4869-8971-a1da704e83a1', N'Andrea Morales', N'asesor@cloudbooks.local', N'ASESOR@CLOUDBOOKS.LOCAL', N'asesor@cloudbooks.local', N'ASESOR@CLOUDBOOKS.LOCAL', 1, N'AQAAAAIAAYagAAAAEP6nxyYOMagphYiOIdnQGAKV+eDj+3TvGmXsRewbYtBnDK3skH5/XHyHnpyQT+w8yA==', N'OVEZFZO3IWROKFDFYR6H3JYYQOCWYTED', N'bc5a8c83-6dc6-464a-a07d-da6aedc1fb31', NULL, 0, 0, NULL, 1, 0, 1);
INSERT INTO [AspNetUsers] ([Id], [NombreCompleto], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Activo]) VALUES (N'2f73c8b0-bb69-4991-896a-3d5787a34240', N'Yeisson Alexander Poroj Toc', N'admin.yeisson@cloudbooks.local', N'ADMIN.YEISSON@CLOUDBOOKS.LOCAL', N'admin.yeisson@cloudbooks.local', N'ADMIN.YEISSON@CLOUDBOOKS.LOCAL', 1, N'AQAAAAIAAYagAAAAEMAIec9PBi8D+IoSrbWPeYftZXhpDQe8OUsbDZWSI0n+b1t1EtX8KSpO/xVvCL3PFw==', N'5LM666FX2CLS7ANQS3X7EKQLM2IRMZWL', N'752d04a5-141b-4288-8eab-d4d9fca8eb20', NULL, 0, 0, NULL, 1, 0, 1);
INSERT INTO [AspNetUsers] ([Id], [NombreCompleto], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Activo]) VALUES (N'527ad705-8b88-4dc1-a786-b558299a5913', N'María López', N'cliente@cloudbooks.local', N'CLIENTE@CLOUDBOOKS.LOCAL', N'cliente@cloudbooks.local', N'CLIENTE@CLOUDBOOKS.LOCAL', 1, N'AQAAAAIAAYagAAAAEN+PVLO1VQXbcEoF+tYEUs3lr/RKrcVKrx16QAEEKSrgyEWstRoz185jeHsC26KpcA==', N'O7WEMOMU2Z6D232HEFGAZXRPRCPCYMZT', N'388f2666-1040-4743-82bf-4a9ecf0d04cb', NULL, 0, 0, NULL, 1, 0, 1);
INSERT INTO [AspNetUsers] ([Id], [NombreCompleto], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Activo]) VALUES (N'69004d49-e4f6-4ba0-9f39-078828fe57a2', N'Carlos Méndez', N'institucion@cloudbooks.local', N'INSTITUCION@CLOUDBOOKS.LOCAL', N'institucion@cloudbooks.local', N'INSTITUCION@CLOUDBOOKS.LOCAL', 1, N'AQAAAAIAAYagAAAAEE03QNewvaguXnf8NCR+nRghyaVDdSwZ4g2vRujoiCqryMyj2L7LnBBKLp+qoKNroQ==', N'36ME25DMHIH2GRKGJCTCG3QNROSX32UT', N'35ec0f41-6443-4ffc-83bd-a1f69ea4a87c', NULL, 0, 0, NULL, 1, 0, 1);
INSERT INTO [AspNetUsers] ([Id], [NombreCompleto], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Activo]) VALUES (N'f3eb9c33-459c-4534-a431-ba9299f2217a', N'Dóminick Ricardo Cifuentes Tomás', N'admin.dominick@cloudbooks.local', N'ADMIN.DOMINICK@CLOUDBOOKS.LOCAL', N'admin.dominick@cloudbooks.local', N'ADMIN.DOMINICK@CLOUDBOOKS.LOCAL', 1, N'AQAAAAIAAYagAAAAEGelg3eOk1fc2LY2OqFtOz9J2e/b1DO5cgVLyGBG5yH3hFlOlA+YoVEO7a2DeUEQKw==', N'IO75GV3LFAZRWTKSDO67RO7RC6MJ3TCY', N'350d3d7b-1106-418c-9f47-fe47443ee980', NULL, 0, 0, NULL, 1, 0, 1);
SET IDENTITY_INSERT [Categorias] ON;
INSERT INTO [Categorias] ([Id], [Nombre]) VALUES (1, N'Tecnología');
INSERT INTO [Categorias] ([Id], [Nombre]) VALUES (2, N'Narrativa');
INSERT INTO [Categorias] ([Id], [Nombre]) VALUES (3, N'Educación');
INSERT INTO [Categorias] ([Id], [Nombre]) VALUES (4, N'Historia');
SET IDENTITY_INSERT [Categorias] OFF;
SET IDENTITY_INSERT [Editoriales] ON;
INSERT INTO [Editoriales] ([Id], [Nombre]) VALUES (1, N'Nube Editorial');
INSERT INTO [Editoriales] ([Id], [Nombre]) VALUES (2, N'Casa del Quetzal');
INSERT INTO [Editoriales] ([Id], [Nombre]) VALUES (3, N'Luna de Papel');
SET IDENTITY_INSERT [Editoriales] OFF;
SET IDENTITY_INSERT [Autores] ON;
INSERT INTO [Autores] ([Id], [Nombre]) VALUES (1, N'Laura Méndez');
INSERT INTO [Autores] ([Id], [Nombre]) VALUES (2, N'Ana Lucía Pérez');
INSERT INTO [Autores] ([Id], [Nombre]) VALUES (3, N'Mateo Rivera');
INSERT INTO [Autores] ([Id], [Nombre]) VALUES (4, N'Elena Solís');
INSERT INTO [Autores] ([Id], [Nombre]) VALUES (5, N'Sofía Barrios');
INSERT INTO [Autores] ([Id], [Nombre]) VALUES (6, N'Diego Castillo');
SET IDENTITY_INSERT [Autores] OFF;
SET IDENTITY_INSERT [Libros] ON;
INSERT INTO [Libros] ([Id], [ISBN], [Titulo], [Sinopsis], [AnioEdicion], [PrecioBase], [Stock], [ImagenPortada], [Formato], [Activo], [EditorialId], [CategoriaId]) VALUES (1, N'9789929000001', N'Arquitectura para sistemas humanos', N'Una mirada práctica al diseño de sistemas que responden a las necesidades de las personas.', 2024, 185, 12, N'/imagenes/libros/9789929000001.svg', 0, 1, 1, 1);
INSERT INTO [Libros] ([Id], [ISBN], [Titulo], [Sinopsis], [AnioEdicion], [PrecioBase], [Stock], [ImagenPortada], [Formato], [Activo], [EditorialId], [CategoriaId]) VALUES (2, N'9789929000002', N'Los caminos del altiplano', N'Una familia descubre sus raíces a través de los pueblos y paisajes del altiplano.', 2023, 140, 3, N'/imagenes/libros/9789929000002.svg', 1, 1, 2, 2);
INSERT INTO [Libros] ([Id], [ISBN], [Titulo], [Sinopsis], [AnioEdicion], [PrecioBase], [Stock], [ImagenPortada], [Formato], [Activo], [EditorialId], [CategoriaId]) VALUES (3, N'9789929000003', N'Ideas que construyen futuro', N'Herramientas para aprender, compartir conocimiento y emprender proyectos en comunidad.', 2025, 110, 20, N'/imagenes/libros/9789929000003.svg', 0, 1, 1, 3);
INSERT INTO [Libros] ([Id], [ISBN], [Titulo], [Sinopsis], [AnioEdicion], [PrecioBase], [Stock], [ImagenPortada], [Formato], [Activo], [EditorialId], [CategoriaId]) VALUES (4, N'9789929000004', N'El jardín de las palabras', N'Relatos breves sobre la memoria, las amistades y las pequeñas cosas cotidianas.', 2022, 95, 0, N'/imagenes/libros/9789929000004.svg', 2, 1, 3, 2);
INSERT INTO [Libros] ([Id], [ISBN], [Titulo], [Sinopsis], [AnioEdicion], [PrecioBase], [Stock], [ImagenPortada], [Formato], [Activo], [EditorialId], [CategoriaId]) VALUES (5, N'9789929000005', N'Diseño digital con propósito', N'Principios de diseño para crear productos digitales claros, útiles y accesibles.', 2025, 225, 7, N'/imagenes/libros/9789929000005.svg', 1, 1, 1, 1);
INSERT INTO [Libros] ([Id], [ISBN], [Titulo], [Sinopsis], [AnioEdicion], [PrecioBase], [Stock], [ImagenPortada], [Formato], [Activo], [EditorialId], [CategoriaId]) VALUES (6, N'9789929000006', N'La ciudad entre montañas', N'Una exploración de la historia y la vida cotidiana de una ciudad rodeada de montañas.', 2021, 160, 0, N'/imagenes/libros/9789929000006.svg', 0, 1, 2, 4);
INSERT INTO [Libros] ([Id], [ISBN], [Titulo], [Sinopsis], [AnioEdicion], [PrecioBase], [Stock], [ImagenPortada], [Formato], [Activo], [EditorialId], [CategoriaId]) VALUES (7, N'9789929000007', N'Aprender en comunidad', N'Estrategias sencillas para construir espacios de aprendizaje participativos.', 2024, 89, 0, N'/imagenes/libros/9789929000007.svg', 2, 1, 3, 3);
INSERT INTO [Libros] ([Id], [ISBN], [Titulo], [Sinopsis], [AnioEdicion], [PrecioBase], [Stock], [ImagenPortada], [Formato], [Activo], [EditorialId], [CategoriaId]) VALUES (8, N'9789929000008', N'Mapas de la memoria', N'Historias y fotografías imaginadas que recorren la memoria de varias generaciones.', 2023, 175, 5, N'/imagenes/libros/9789929000008.svg', 1, 1, 2, 4);
INSERT INTO [Libros] ([Id], [ISBN], [Titulo], [Sinopsis], [AnioEdicion], [PrecioBase], [Stock], [ImagenPortada], [Formato], [Activo], [EditorialId], [CategoriaId]) VALUES (9, N'9789929000009', N'La biblioteca del viento', N'Una joven bibliotecaria encuentra cartas que transforman su comunidad.', 2025, 125, 18, N'/imagenes/libros/9789929000009.svg', 0, 1, 3, 2);
INSERT INTO [Libros] ([Id], [ISBN], [Titulo], [Sinopsis], [AnioEdicion], [PrecioBase], [Stock], [ImagenPortada], [Formato], [Activo], [EditorialId], [CategoriaId]) VALUES (10, N'9789929000010', N'Código y pensamiento', N'Una introducción a la programación y la resolución de problemas paso a paso.', 2024, 199, 9, N'/imagenes/libros/9789929000010.svg', 0, 1, 1, 1);
SET IDENTITY_INSERT [Libros] OFF;
INSERT INTO [AutorLibro] ([AutoresId], [LibrosId]) VALUES (1, 1);
INSERT INTO [AutorLibro] ([AutoresId], [LibrosId]) VALUES (2, 2);
INSERT INTO [AutorLibro] ([AutoresId], [LibrosId]) VALUES (3, 3);
INSERT INTO [AutorLibro] ([AutoresId], [LibrosId]) VALUES (4, 4);
INSERT INTO [AutorLibro] ([AutoresId], [LibrosId]) VALUES (1, 5);
INSERT INTO [AutorLibro] ([AutoresId], [LibrosId]) VALUES (2, 6);
INSERT INTO [AutorLibro] ([AutoresId], [LibrosId]) VALUES (3, 7);
INSERT INTO [AutorLibro] ([AutoresId], [LibrosId]) VALUES (4, 8);
INSERT INTO [AutorLibro] ([AutoresId], [LibrosId]) VALUES (5, 9);
INSERT INTO [AutorLibro] ([AutoresId], [LibrosId]) VALUES (6, 10);
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'2f73c8b0-bb69-4991-896a-3d5787a34240', N'24df8016-c172-4314-b387-b695603bcd7e');
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'f3eb9c33-459c-4534-a431-ba9299f2217a', N'24df8016-c172-4314-b387-b695603bcd7e');
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'15ba725c-b5c7-4869-8971-a1da704e83a1', N'2f4b5cda-fd3f-4ebe-89de-af9c32d92782');
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'69004d49-e4f6-4ba0-9f39-078828fe57a2', N'3c099731-dffd-4cf9-9b0a-7f9b7c9406b0');
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'527ad705-8b88-4dc1-a786-b558299a5913', N'd0ca1f2c-6c87-4891-b0f3-a33646a386c3');
SET IDENTITY_INSERT [PerfilesInstitucionales] ON;
INSERT INTO [PerfilesInstitucionales] ([Id], [UsuarioId], [Institucion], [Nit], [TipoInstitucion], [Telefono], [Direccion], [Representante]) VALUES (1, N'69004d49-e4f6-4ba0-9f39-078828fe57a2', N'Biblioteca Horizonte', N'1234567-8', N'Biblioteca', N'5555 0100', N'Zona 1, Quetzaltenango', N'Carlos Méndez');
SET IDENTITY_INSERT [PerfilesInstitucionales] OFF;
COMMIT;
GO
