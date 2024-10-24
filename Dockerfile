FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV PORT 8080
EXPOSE ${PORT}

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Lab03-NguyenLeVienMinh-IdentityAjax-ASP.NETCoreWebAPI/Lab03-NguyenLeVienMinh-IdentityAjax-ASP.NETCoreWebAPI.csproj", "Lab03-NguyenLeVienMinh-IdentityAjax-ASP.NETCoreWebAPI/"]
COPY ["BusinessObjects/BusinessObjects.csproj", "BusinessObjects/"]
COPY ["Repositories/Repositories.csproj", "Repositories/"]
COPY ["Services/Services.csproj", "Services/"]
RUN dotnet restore "./Lab03-NguyenLeVienMinh-IdentityAjax-ASP.NETCoreWebAPI/Lab03-NguyenLeVienMinh-IdentityAjax-ASP.NETCoreWebAPI.csproj"
COPY . .
WORKDIR "/src/Lab03-NguyenLeVienMinh-IdentityAjax-ASP.NETCoreWebAPI"
RUN dotnet build "./Lab03-NguyenLeVienMinh-IdentityAjax-ASP.NETCoreWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Lab03-NguyenLeVienMinh-IdentityAjax-ASP.NETCoreWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lab03-NguyenLeVienMinh-IdentityAjax-ASP.NETCoreWebAPI.dll"]