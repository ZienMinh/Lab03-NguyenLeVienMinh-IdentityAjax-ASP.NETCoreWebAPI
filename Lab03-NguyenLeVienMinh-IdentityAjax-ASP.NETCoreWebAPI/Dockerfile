#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443

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