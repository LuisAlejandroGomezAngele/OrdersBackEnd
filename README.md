API Setup Guide
Descripción
Este proyecto es una API desarrollada en ASP.NET Core. Proporciona endpoints para manejar operaciones relacionadas con ventas, productos, y pedidos.

Requisitos previos
Antes de comenzar, asegúrate de tener instaladas las siguientes herramientas:

.NET SDK: Descargar .NET SDK
SQL Server: Configura una instancia de SQL Server para la base de datos.
Postman (opcional): Para probar los endpoints de la API.

Librerías principales:
Entity Framework Core: Proporciona soporte para consultas y operaciones de base de datos.

dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Swashbuckle.AspNetCore
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package Microsoft.AspNetCore.Mvc.NewtonsoftJson
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package FluentValidation.AspNetCore


La API estará disponible en http://localhost:5146 

